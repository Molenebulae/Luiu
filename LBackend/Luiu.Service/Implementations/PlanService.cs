using AutoMapper;
using Luiu.Domain.Conmon;
using Luiu.Domain.Exceptions;
using Luiu.Domain.Models;
using Luiu.Service.DTOs.V1.Client;
using Luiu.Service.Enums;
using Luiu.Service.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Luiu.Service.Implementations
{
    public class PlanService : BaseService<PlanService>
    {
        private readonly IStorageService _storageService;
        private readonly DemoSessionService _demoSessionService;
        private const int TripCommentMaxLength = 50;
        private const int TripDetailSpotAliasMaxLength = 20;
        private const int TripDetailNotesMaxLength = 50;

        private sealed class TripCommentAccess
        {
            public TMember Member { get; init; } = null!;
            public TTrip Trip { get; init; } = null!;
            public bool IsOwner { get; init; }
        }

        public PlanService(
            LuiuDbContext context,
            ILogger<PlanService> logger,
            IMapper mapper,
            IStorageService storageService,
            DemoSessionService demoSessionService) : base(context, logger, mapper)
        {
            _storageService = storageService;
            _demoSessionService = demoSessionService;
        }

        // PlanList CRUD：私密行程不可被推薦，並相容 PrivacyStatus=2 的願意推薦狀態。
        private static bool? ResolveIsSuggest(byte? privacyStatus, bool? isSuggest)
        {
            return privacyStatus == 2 || (privacyStatus == 1 && isSuggest == true);
        }

        private static string? NormalizeTripTag(string? tripTag)
        {
            var normalizedTripTag = tripTag?.Trim();
            return string.IsNullOrEmpty(normalizedTripTag) ? null : normalizedTripTag;
        }

        private async Task<TMember> ResolveMemberAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new AppBadRequestException("UserID 不可為空");
            }

            var member = await _context.TMembers
                .FirstOrDefaultAsync(m => m.UserId == userId && m.IsDelete == false);

            if (member == null)
            {
                throw new AppNotFoundException("找不到指定使用者");
            }

            return member;
        }

        private async Task<TTrip> GetOwnedTripAsync(string userId, int tripId)
        {
            var member = await ResolveMemberAsync(userId);
            var demoSession = await _demoSessionService.GetCurrentSessionAsync(throwIfInvalid: true);
            var query = _context.TTrips
                .Where(t => t.OwnerId == member.MemberId && t.TripId == tripId && t.IsDeleted == false);

            query = demoSession == null
                ? query.Where(t => t.DemoSessionId == null)
                : query.Where(t => t.DemoSessionId == demoSession.DemoSessionId);

            var trip = await query.FirstOrDefaultAsync();

            if (trip == null)
            {
                throw new AppNotFoundException("找不到指定行程");
            }

            return trip;
        }

        private async Task<TripCommentAccess> GetTripCommentAccessAsync(string userId, int tripId)
        {
            var member = await ResolveMemberAsync(userId);
            var demoSession = await _demoSessionService.GetCurrentSessionAsync(throwIfInvalid: true);
            var query = _context.TTrips
                .Where(t => t.TripId == tripId && t.IsDeleted == false);

            query = demoSession == null
                ? query.Where(t => t.DemoSessionId == null)
                : query.Where(t => t.DemoSessionId == demoSession.DemoSessionId);

            var trip = await query.FirstOrDefaultAsync();

            if (trip == null)
            {
                throw new AppNotFoundException("找不到指定行程");
            }

            return new TripCommentAccess
            {
                Member = member,
                Trip = trip,
                IsOwner = trip.OwnerId == member.MemberId
            };
        }

        private static string ResolveTripCommentContent(string? content)
        {
            var normalizedContent = content?.Trim();
            if (string.IsNullOrWhiteSpace(normalizedContent))
            {
                throw new AppBadRequestException("請輸入留言內容");
            }

            if (normalizedContent.Length > TripCommentMaxLength)
            {
                throw new AppBadRequestException($"留言內容不可超過 {TripCommentMaxLength} 個字");
            }

            return normalizedContent;
        }

        private async Task ValidateTripCommentParentAsync(int tripId, int? parentId)
        {
            if (!parentId.HasValue)
            {
                return;
            }

            var parent = await _context.TTripComments
                .FirstOrDefaultAsync(c => c.CommentId == parentId.Value
                                       && c.TripId == tripId
                                       && c.IsDeleted == false);

            if (parent == null)
            {
                throw new AppNotFoundException("找不到指定主留言");
            }

            if (parent.ParentId.HasValue)
            {
                throw new AppBadRequestException("只能回覆主留言");
            }
        }

        private static void ValidateTripDetailRequest(TTrip trip, TripDetailCreateRequestDTO request)
        {
            if (request.DayNumber < 1)
            {
                throw new AppBadRequestException("天數必須大於 0");
            }

            if (request.SortOrder < 1)
            {
                throw new AppBadRequestException("排序必須大於 0");
            }

            var maxDayNumber = trip.EndDate.DayNumber - trip.StartDate.DayNumber + 1;
            if (request.DayNumber > maxDayNumber)
            {
                throw new AppBadRequestException("天數不可超過行程日期範圍");
            }
        }

        private static void ValidateTripDetailTextFields(TripDetailCreateRequestDTO request)
        {
            if (request.SpotAlias?.Length > TripDetailSpotAliasMaxLength)
            {
                throw new AppBadRequestException($"景點別名不可超過 {TripDetailSpotAliasMaxLength} 個字");
            }

            if (request.Notes?.Length > TripDetailNotesMaxLength)
            {
                throw new AppBadRequestException($"行程備註不可超過 {TripDetailNotesMaxLength} 個字");
            }
        }

        private async Task<int> ResolveTripDetailSpotIdAsync(TripDetailCreateRequestDTO request)
        {
            if (request.SpotId > 0)
            {
                var existingSpot = await _context.TSpots
                    .FirstOrDefaultAsync(s => s.SpotId == request.SpotId);

                if (existingSpot != null)
                {
                    await ApplySpotSupplementAsync(existingSpot, request.Spot);
                    return existingSpot.SpotId;
                }
            }

            if (request.Spot == null)
            {
                throw new AppNotFoundException("找不到指定景點，且未提供景點詳細資訊");
            }

            var googleMapId = request.Spot.GoogleMapID?.Trim();
            if (string.IsNullOrWhiteSpace(googleMapId))
            {
                throw new AppBadRequestException("新增景點時 GoogleMapID 不可為空");
            }

            var existingGoogleSpot = await _context.TSpots
                .FirstOrDefaultAsync(s => s.GoogleMapId == googleMapId);

            if (existingGoogleSpot != null)
            {
                await ApplySpotSupplementAsync(existingGoogleSpot, request.Spot);
                return existingGoogleSpot.SpotId;
            }

            var spotName = request.Spot.SpotName?.Trim();
            if (string.IsNullOrWhiteSpace(spotName))
            {
                throw new AppBadRequestException("新增景點時 SpotName 不可為空");
            }

            var now = DateTime.Now;
            var spot = new TSpot
            {
                RegionId = await ResolveSpotRegionIdAsync(request.Spot),
                MemberId = request.Spot.MemberID,
                SpotName = Truncate(spotName, 200),
                Longitude = request.Spot.Longitude,
                Latitude = request.Spot.Latitude,
                Tel = Truncate(request.Spot.Tel?.Trim(), 50),
                Address = request.Spot.Address?.Trim(),
                OfficialUrl = request.Spot.OfficialURL?.Trim(),
                OpeningHoursJson = request.Spot.OpeningHoursJson,
                LastUpdated = now,
                Rating = request.Spot.Rating,
                UserRatingCount = request.Spot.UserRatingCount,
                GoogleMapUrl = request.Spot.GoogleMapURL?.Trim(),
                GoogleMapId = googleMapId,
                PriceLevel = Truncate(request.Spot.PriceLevel?.Trim(), 50),
                PhotoUrl = request.Spot.PhotoUrl?.Trim(),
                PhotoReference = Truncate(request.Spot.PhotoReference?.Trim(), 512),
                ViewCount = 0,
                FavoriteCount = 0,
                PlanCount = 0,
                RecordCount = 0,
                RefCount = 0
            };

            _context.TSpots.Add(spot);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Spot created while creating trip detail. GoogleMapId={GoogleMapId}, SpotId={SpotId}",
                spot.GoogleMapId,
                spot.SpotId);

            return spot.SpotId;
        }

        private async Task ApplySpotSupplementAsync(TSpot spot, TripDetailSpotRequestDTO? requestSpot)
        {
            if (requestSpot == null)
            {
                return;
            }

            var changed = false;
            var photoUrl = requestSpot.PhotoUrl?.Trim();
            if (!string.IsNullOrWhiteSpace(photoUrl) && spot.PhotoUrl != photoUrl)
            {
                spot.PhotoUrl = photoUrl;
                changed = true;
            }

            var photoReference = requestSpot.PhotoReference?.Trim();
            if (!string.IsNullOrWhiteSpace(photoReference) && spot.PhotoReference != photoReference)
            {
                spot.PhotoReference = Truncate(photoReference, 512);
                changed = true;
            }

            if (!string.IsNullOrWhiteSpace(requestSpot.OpeningHoursJson) && spot.OpeningHoursJson != requestSpot.OpeningHoursJson)
            {
                spot.OpeningHoursJson = requestSpot.OpeningHoursJson;
                changed = true;
            }

            if (changed)
            {
                spot.LastUpdated = DateTime.Now;
                await _context.SaveChangesAsync();
            }
        }

        private async Task<int> ResolveSpotRegionIdAsync(TripDetailSpotRequestDTO spot)
        {
            if (spot.RegionID.HasValue)
            {
                if (spot.RegionID.Value < 1)
                {
                    throw new AppBadRequestException("RegionID 必須大於 0");
                }

                var regionExists = await _context.TRegions
                    .AnyAsync(r => r.RegionId == spot.RegionID.Value);

                if (!regionExists)
                {
                    throw new AppNotFoundException("找不到指定地區");
                }

                return spot.RegionID.Value;
            }

            var address = spot.Address?.Trim();
            if (!string.IsNullOrWhiteSpace(address))
            {
                var normalizedAddress = address.Replace("臺", "台");
                var regions = await _context.TRegions.ToListAsync();
                var matchedRegion = regions.FirstOrDefault(region =>
                    normalizedAddress.Contains(region.RegionName.Replace("臺", "台")));

                if (matchedRegion != null)
                {
                    return matchedRegion.RegionId;
                }
            }

            throw new AppBadRequestException("新增景點時請提供 RegionID，或提供可判斷地區的 Address");
        }

        private static string? Truncate(string? value, int maxLength)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
            {
                return value;
            }

            return value[..maxLength];
        }

        private async Task ReorderTripDetailsAsync(
            int tripId,
            byte dayNumber,
            int? movedDetailId = null,
            byte? desiredSortOrder = null)
        {
            var details = await _context.TTripDetails
                .Where(d => d.TripId == tripId && d.DayNumber == dayNumber && d.IsDeleted == false)
                .OrderBy(d => d.SortOrder)
                .ThenBy(d => d.DetailId)
                .ToListAsync();

            if (movedDetailId.HasValue)
            {
                var moved = details.FirstOrDefault(d => d.DetailId == movedDetailId.Value);
                if (moved != null)
                {
                    details.Remove(moved);
                    var insertIndex = Math.Clamp((desiredSortOrder ?? moved.SortOrder) - 1, 0, details.Count);
                    details.Insert(insertIndex, moved);
                }
            }

            for (var index = 0; index < details.Count; index++)
            {
                details[index].SortOrder = (byte)(index + 1);
                details[index].UpdateAt = DateTime.Now;
            }

            _logger.LogInformation(
                "Trip details reordered. TripId={TripId}, DayNumber={DayNumber}, DetailCount={DetailCount}",
                tripId,
                dayNumber,
                details.Count);
        }

        private static PlanListResponseDTO MapPlanListResponse(TMember member, TTrip trip)
        {
            return new PlanListResponseDTO
            {
                TripId = trip.TripId,
                TripName = trip.TripName,
                TripDesc = trip.TripDesc,
                TripTag = trip.TripTag,
                StartDate = trip.StartDate,
                EndDate = trip.EndDate,
                PrivacyStatus = trip.PrivacyStatus,
                IsSuggest = trip.IsSuggest,
                OfficeOper = trip.OfficeOper,
                ShortCode = trip.ShortCode,
                ListId = trip.ListId,
                PhotoUrl = trip.PhotoUrl,
                OwnerName = member.Name,
                OwnerIconUrl = member.AvatarUrl,
                CreateAt = trip.CreateAt,
                UpdateAt = trip.UpdateAt
            };
        }

        private static PlanDetailItemDTO MapTripDetailItem(TTripDetail detail, TTrip trip, TSpot? spot)
        {
            return new PlanDetailItemDTO
            {
                DetailId = detail.DetailId,
                SpotAlias = detail.SpotAlias,
                Notes = detail.Notes,
                DayNumber = detail.DayNumber,
                SortOrder = detail.SortOrder,
                ArrivalTime = detail.ArrivalTime,
                StayDuration = detail.StayDuration,
                TransportMode = detail.TransportMode,
                TransportTime = detail.TransportTime,
                Budget = detail.Budget,
                SpotId = detail.SpotId,
                SpotName = spot?.SpotName ?? string.Empty,
                Address = spot?.Address,
                Latitude = spot?.Latitude,
                Longitude = spot?.Longitude,
                GoogleMapId = spot?.GoogleMapId,
                OpeningHoursJson = spot?.OpeningHoursJson,
                PhotoUrl = spot?.PhotoUrl,
                PhotoReference = spot?.PhotoReference,
                Tel = spot?.Tel,
                Rating = spot?.Rating,
                UserRatingCount = spot?.UserRatingCount,
                PriceLevel = spot?.PriceLevel,
                VersionId = detail.VersionId,
                PrivacyStatus = trip.PrivacyStatus,
                IsSuggest = trip.IsSuggest,
                OfficeOper = trip.OfficeOper,
                CreateAt = detail.CreateAt,
                UpdateAt = detail.UpdateAt,
                PolylineEncoded = detail.PolylineEncoded,
                IsDeleted = detail.IsDeleted
            };
        }

        private async Task<List<TripCommentResponseDTO>> GetTripCommentsForAccessAsync(TripCommentAccess access)
        {
            return await (from comment in _context.TTripComments
                          join member in _context.TMembers on comment.UserId equals member.MemberId
                          where comment.TripId == access.Trip.TripId
                             && comment.IsDeleted == false
                          orderby comment.CreateAt, comment.CommentId
                          select new TripCommentResponseDTO
                          {
                              CommentId = comment.CommentId,
                              TripId = comment.TripId,
                              Content = comment.Content,
                              ParentId = comment.ParentId,
                              UserId = comment.UserId,
                              UserName = member.Name,
                              UserIcon = member.AvatarUrl,
                              CreateAt = comment.CreateAt,
                              CanEdit = comment.UserId == access.Member.MemberId,
                              CanDelete = access.IsOwner || comment.UserId == access.Member.MemberId
                          })
                .ToListAsync();
        }

        private async Task<TripCommentResponseDTO> GetTripCommentResponseAsync(TripCommentAccess access, int commentId)
        {
            var result = await (from comment in _context.TTripComments
                                join member in _context.TMembers on comment.UserId equals member.MemberId
                                where comment.TripId == access.Trip.TripId
                                   && comment.CommentId == commentId
                                   && comment.IsDeleted == false
                                select new TripCommentResponseDTO
                                {
                                    CommentId = comment.CommentId,
                                    TripId = comment.TripId,
                                    Content = comment.Content,
                                    ParentId = comment.ParentId,
                                    UserId = comment.UserId,
                                    UserName = member.Name,
                                    UserIcon = member.AvatarUrl,
                                    CreateAt = comment.CreateAt,
                                    CanEdit = comment.UserId == access.Member.MemberId,
                                    CanDelete = access.IsOwner || comment.UserId == access.Member.MemberId
                                })
                .FirstOrDefaultAsync();

            if (result == null)
            {
                throw new AppNotFoundException("找不到指定留言");
            }

            return result;
        }

        private async Task<Dictionary<int, TSpot>> GetSpotMapAsync(IEnumerable<int> spotIds)
        {
            return await _context.TSpots
                .Where(s => spotIds.Contains(s.SpotId))
                .ToDictionaryAsync(s => s.SpotId);
        }

        private async Task<PlanDetailItemDTO> GetTripDetailItemAsync(string userId, int tripId, int detailId)
        {
            var trip = await GetOwnedTripAsync(userId, tripId);
            var detail = await _context.TTripDetails
                .FirstOrDefaultAsync(td => td.TripId == trip.TripId
                                        && td.DetailId == detailId
                                        && td.IsDeleted == false);

            if (detail == null)
            {
                throw new AppNotFoundException("找不到指定行程明細");
            }

            var spot = await _context.TSpots
                .FirstOrDefaultAsync(s => s.SpotId == detail.SpotId);

            return MapTripDetailItem(detail, trip, spot);
        }

        private static void ApplyTripDetailRequest(
            TTripDetail detail,
            TripDetailCreateRequestDTO request,
            int spotId,
            DateTime now,
            bool isCreate)
        {
            detail.SpotAlias = request.SpotAlias;
            detail.Notes = request.Notes;
            detail.DayNumber = request.DayNumber;
            detail.SortOrder = request.SortOrder;
            detail.ArrivalTime = request.ArrivalTime;
            detail.StayDuration = request.StayDuration;
            detail.TransportMode = request.TransportMode;
            detail.TransportTime = request.TransportTime;
            detail.Budget = request.Budget;
            detail.SpotId = spotId;
            detail.VersionId = request.VersionId;
            detail.IsMaster = request.IsMaster;
            detail.SuggestBy = request.SuggestBy;
            detail.IsDeleted = false;
            detail.UpdateAt = now;
            detail.PolylineEncoded = string.Empty;

            if (isCreate)
            {
                detail.CreateAt = now;
            }
        }

        private async Task<List<PlanDetailItemDTO>> GetTripDetailItemsAsync(TTrip trip)
        {
            var details = await _context.TTripDetails
                .Where(td => td.TripId == trip.TripId && td.IsDeleted == false)
                .OrderBy(td => td.DayNumber)
                .ThenBy(td => td.SortOrder)
                .ThenBy(td => td.DetailId)
                .ToListAsync();

            var spotMap = await GetSpotMapAsync(details.Select(d => d.SpotId).Distinct());
            return details
                .Select(detail =>
                {
                    spotMap.TryGetValue(detail.SpotId, out var spot);
                    return MapTripDetailItem(detail, trip, spot);
                })
                .ToList();
        }

        // PlanList CRUD：POST / PUT 完成後共用此方法回傳與 GET 列表相同的巢狀 DTO。
        private async Task<PlanListResponseDTO> GetPlanListItemAsync(TMember member, int tripId)
        {
            var demoSession = await _demoSessionService.GetCurrentSessionAsync(throwIfInvalid: true);
            var query = _context.TTrips
                .Where(t => t.OwnerId == member.MemberId
                         && t.TripId == tripId
                         && t.IsDeleted == false);

            query = demoSession == null
                ? query.Where(t => t.DemoSessionId == null)
                : query.Where(t => t.DemoSessionId == demoSession.DemoSessionId);

            var trip = await query.FirstOrDefaultAsync();

            if (trip == null)
            {
                throw new AppNotFoundException("找不到指定行程");
            }

            return MapPlanListResponse(member, trip);
        }

        public async Task<PlanDetailResponseDTO> GetPlanDetailAsync(string userId, int tripId)
        {
            var trip = await GetOwnedTripAsync(userId, tripId);
            var member = await _context.TMembers
                .FirstOrDefaultAsync(m => m.MemberId == trip.OwnerId && m.IsDelete == false);

            if (member == null)
            {
                throw new AppNotFoundException("找不到指定使用者");
            }

            var result = new PlanDetailResponseDTO
            {
                TripId = trip.TripId,
                TripName = trip.TripName,
                OwnerName = member.Name,
                PhotoUrl = trip.PhotoUrl,
                StartDate = trip.StartDate,
                EndDate = trip.EndDate,
                TripDesc = trip.TripDesc,
                TripTag = trip.TripTag,
                PrivacyStatus = trip.PrivacyStatus,
                IsSuggest = trip.IsSuggest,
                OfficeOper = trip.OfficeOper,
                ShortCode = trip.ShortCode,
                //OwnerId = t.OwnerId,
                ListId = trip.ListId,
                CreateAt = trip.CreateAt,
                UpdateAt = trip.UpdateAt,
                //DayNumber = td.DayNumber,
                //SortOrder = td.SortOrder,
                //ArrivalTime = td.ArrivalTime,
                //StayDuration = td.StayDuration,
                //TransportMode = td.TransportMode,
                //TransportTime = td.TransportTime,
            };

            var details = await _context.TTripDetails
                .Where(td => td.TripId == tripId && td.IsDeleted == false)
                .OrderBy(td => td.DayNumber)
                .ThenBy(td => td.SortOrder)
                .ToListAsync();

            var spotMap = await GetSpotMapAsync(details.Select(d => d.SpotId).Distinct());
            result.TripDetails = details
                .Select(detail =>
                {
                    spotMap.TryGetValue(detail.SpotId, out var spot);
                    return MapTripDetailItem(detail, trip, spot);
                })
                .ToList();
            result.TripComments = await GetTripCommentsForAccessAsync(new TripCommentAccess
            {
                Member = member,
                Trip = trip,
                IsOwner = true
            });

            return result;
        }

        public async Task<List<TripCommentResponseDTO>> GetTripCommentsAsync(string userId, int tripId)
        {
            var access = await GetTripCommentAccessAsync(userId, tripId);
            return await GetTripCommentsForAccessAsync(access);
        }

        public async Task<TripCommentResponseDTO> CreateTripCommentAsync(
            string userId,
            int tripId,
            TripCommentCreateRequestDTO request)
        {
            var access = await GetTripCommentAccessAsync(userId, tripId);

            var content = ResolveTripCommentContent(request.Content);
            await ValidateTripCommentParentAsync(access.Trip.TripId, request.ParentId);

            var now = DateTime.Now;
            var comment = new TTripComment
            {
                Content = content,
                CreateAt = now,
                ParentId = request.ParentId,
                TripId = access.Trip.TripId,
                UserId = access.Member.MemberId,
                IsDeleted = false
            };

            _context.TTripComments.Add(comment);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Trip comment created. UserId={UserId}, TripId={TripId}, CommentId={CommentId}, ParentId={ParentId}",
                userId,
                tripId,
                comment.CommentId,
                comment.ParentId);
            return await GetTripCommentResponseAsync(access, comment.CommentId);
        }

        public async Task<TripCommentResponseDTO> UpdateTripCommentAsync(
            string userId,
            int tripId,
            int commentId,
            TripCommentUpdateRequestDTO request)
        {
            var access = await GetTripCommentAccessAsync(userId, tripId);
            var comment = await _context.TTripComments
                .FirstOrDefaultAsync(c => c.TripId == access.Trip.TripId
                                       && c.CommentId == commentId
                                       && c.IsDeleted == false);

            if (comment == null)
            {
                throw new AppNotFoundException("找不到指定留言");
            }

            if (comment.UserId != access.Member.MemberId)
            {
                throw new AppForbiddenException("只能編輯自己的留言");
            }

            comment.Content = ResolveTripCommentContent(request.Content);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Trip comment updated. UserId={UserId}, TripId={TripId}, CommentId={CommentId}",
                userId,
                tripId,
                comment.CommentId);
            return await GetTripCommentResponseAsync(access, comment.CommentId);
        }

        public async Task<bool> DeleteTripCommentAsync(string userId, int tripId, int commentId)
        {
            var access = await GetTripCommentAccessAsync(userId, tripId);
            var comment = await _context.TTripComments
                .FirstOrDefaultAsync(c => c.TripId == access.Trip.TripId
                                       && c.CommentId == commentId
                                       && c.IsDeleted == false);

            if (comment == null)
            {
                throw new AppNotFoundException("找不到指定留言");
            }

            if (!access.IsOwner && comment.UserId != access.Member.MemberId)
            {
                throw new AppForbiddenException("您沒有權限刪除此留言");
            }

            comment.IsDeleted = true;
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Trip comment deleted. UserId={UserId}, TripId={TripId}, CommentId={CommentId}",
                userId,
                tripId,
                comment.CommentId);
            return true;
        }

        public async Task<List<PlanListResponseDTO>> GetPlanListAsync(string userId)
        {
            var member = await ResolveMemberAsync(userId);
            var demoSession = await _demoSessionService.GetCurrentSessionAsync(throwIfInvalid: true);

            // PlanList CRUD：GET 只回傳指定會員擁有且未軟刪除的行程。
            var query = _context.TTrips
                .Where(t => t.OwnerId == member.MemberId && t.IsDeleted == false);

            query = demoSession == null
                ? query.Where(t => t.DemoSessionId == null)
                : query.Where(t => t.DemoSessionId == demoSession.DemoSessionId);

            var trips = await query.OrderByDescending(t => t.CreateAt).ToListAsync();

            return trips
                .Select(trip => MapPlanListResponse(member, trip))
                .ToList();
        }

        public async Task<PlanListResponseDTO> CreatePlanAsync(string userId, PlanListCreateRequestDTO request)
        {
            // PlanList CRUD：POST 建立前先確認會員存在，避免建立孤兒行程。
            var member = await ResolveMemberAsync(userId);
            var demoSession = await _demoSessionService.GetCurrentSessionAsync(throwIfInvalid: true);
            if (demoSession != null)
            {
                await _demoSessionService.IncrementQuotaAsync(DemoQuotaType.CreatedTrip);
            }

            // TODO: PlanList CRUD：POST / PUT 前端已驗證圖片格式與大小，後端仍應依照
            CDictionary.UploadPolicies.TryGetValue("trips", out var policy);
            var photoUrl = request.Photo == null
                ? null
                : await _storageService.SaveFileAsync(request.Photo, policy);

            var now = DateTime.Now;
            var trip = new TTrip
            {
                TripName = request.TripName,
                TripDesc = request.TripDesc,
                TripTag = NormalizeTripTag(request.TripTag),
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                PrivacyStatus = request.PrivacyStatus,
                IsSuggest = ResolveIsSuggest(request.PrivacyStatus, request.IsSuggest),
                OfficeOper = request.OfficeOper,
                TripGuid = Guid.NewGuid(),
                ShortCode = request.ShortCode,
                OwnerId = member.MemberId,
                ListId = request.ListId,
                IsDeleted = false,
                CreateAt = now,
                UpdateAt = now,
                PhotoUrl = photoUrl,
                DemoSessionId = demoSession?.DemoSessionId
            };

            if (demoSession != null)
            {
                trip.IsSuggest = false;
                trip.OfficeOper = 0;
            }

            _context.TTrips.Add(trip);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Plan created. UserId={UserId}, TripId={TripId}", userId, trip.TripId);
            return await GetPlanListItemAsync(member, trip.TripId);
        }

        public async Task<PlanListResponseDTO> UpdatePlanAsync(string userId, int tripId, PlanListUpdateRequestDTO request)
        {
            var member = await ResolveMemberAsync(userId);
            var trip = await GetOwnedTripAsync(userId, tripId);

            trip.TripName = request.TripName;
            trip.TripDesc = request.TripDesc;
            trip.TripTag = NormalizeTripTag(request.TripTag);
            trip.StartDate = request.StartDate;
            trip.EndDate = request.EndDate;
            trip.PrivacyStatus = request.PrivacyStatus;
            trip.IsSuggest = ResolveIsSuggest(request.PrivacyStatus, request.IsSuggest);
            if (request.OfficeOper.HasValue)
            {
                trip.OfficeOper = request.OfficeOper;
            }
            if (request.ShortCode != null)
            {
                trip.ShortCode = request.ShortCode;
            }
            if (request.ListId.HasValue)
            {
                trip.ListId = request.ListId;
            }
            if (request.Photo != null)
            {
                // TODO: PlanList CRUD：POST / PUT 前端已驗證圖片格式與大小，後端仍應依照
                CDictionary.UploadPolicies.TryGetValue("trips", out var policy);
                var photoUrl = await _storageService.SaveFileAsync(request.Photo, policy);
                trip.PhotoUrl = photoUrl;
            }
            if (_demoSessionService.IsDemoRequest())
            {
                trip.IsSuggest = false;
                trip.OfficeOper = 0;
            }
            trip.UpdateAt = DateTime.Now;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Plan updated. UserId={UserId}, TripId={TripId}", userId, trip.TripId);
            return await GetPlanListItemAsync(member, trip.TripId);
        }

        public async Task<UpdateTripSuggestResponseDTO> UpdateTripSuggestAsync(
            string userId,
            int tripId,
            UpdateTripSuggestRequestDTO request)
        {
            if (_demoSessionService.IsDemoRequest())
            {
                throw new AppForbiddenException("Demo 帳號不可變更推薦狀態");
            }

            var trip = await GetOwnedTripAsync(userId, tripId);

            if (!request.IsSuggest.HasValue && !request.OfficeOper.HasValue)
            {
                throw new AppBadRequestException("請提供推薦狀態或官方精選狀態");
            }

            if (request.IsSuggest.HasValue)
            {
                trip.IsSuggest = ResolveIsSuggest(trip.PrivacyStatus, request.IsSuggest);
            }

            if (request.OfficeOper.HasValue)
            {
                if (request.OfficeOper.Value != 0 && request.OfficeOper.Value != 1)
                {
                    throw new AppBadRequestException("官方精選狀態只能為 0 或 1");
                }

                trip.OfficeOper = request.OfficeOper;
            }

            trip.UpdateAt = DateTime.Now;

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Trip suggest updated. UserId={UserId}, TripId={TripId}, IsSuggest={IsSuggest}, OfficeOper={OfficeOper}",
                userId,
                trip.TripId,
                trip.IsSuggest,
                trip.OfficeOper);

            return new UpdateTripSuggestResponseDTO
            {
                TripId = trip.TripId,
                IsSuggest = trip.IsSuggest,
                OfficeOper = trip.OfficeOper
            };
        }

        public async Task<bool> DeletePlanAsync(string userId, int tripId)
        {
            // PlanList CRUD：DELETE 採軟刪除，保留資料並排除於列表之外。
            var trip = await GetOwnedTripAsync(userId, tripId);

            trip.IsDeleted = true;
            trip.UpdateAt = DateTime.Now;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Plan deleted. UserId={UserId}, TripId={TripId}", userId, tripId);
            return true;
        }

        public async Task<PlanDetailItemDTO> CreateTripDetailAsync(
            string userId,
            int tripId,
            TripDetailCreateRequestDTO request)
        {
            var trip = await GetOwnedTripAsync(userId, tripId);
            ValidateTripDetailRequest(trip, request);
            ValidateTripDetailTextFields(request);
            var spotId = await ResolveTripDetailSpotIdAsync(request);

            var detail = new TTripDetail
            {
                TripId = trip.TripId,
            };
            ApplyTripDetailRequest(detail, request, spotId, DateTime.Now, isCreate: true);

            _context.TTripDetails.Add(detail);
            await _context.SaveChangesAsync();

            await ReorderTripDetailsAsync(trip.TripId, detail.DayNumber, detail.DetailId, request.SortOrder);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Trip detail created. UserId={UserId}, TripId={TripId}, DetailId={DetailId}, SpotId={SpotId}, DayNumber={DayNumber}, SortOrder={SortOrder}",
                userId,
                trip.TripId,
                detail.DetailId,
                detail.SpotId,
                detail.DayNumber,
                detail.SortOrder);
            return await GetTripDetailItemAsync(userId, trip.TripId, detail.DetailId);
        }

        public async Task<PlanDetailItemDTO> UpdateTripDetailAsync(
            string userId,
            int tripId,
            int detailId,
            TripDetailUpdateRequestDTO request)
        {
            var trip = await GetOwnedTripAsync(userId, tripId);
            ValidateTripDetailRequest(trip, request);
            ValidateTripDetailTextFields(request);

            var detail = await _context.TTripDetails
                .FirstOrDefaultAsync(d => d.TripId == trip.TripId && d.DetailId == detailId && d.IsDeleted == false);

            if (detail == null)
            {
                throw new AppNotFoundException("找不到指定行程明細");
            }

            var spotId = await ResolveTripDetailSpotIdAsync(request);
            var oldDayNumber = detail.DayNumber;
            ApplyTripDetailRequest(detail, request, spotId, DateTime.Now, isCreate: false);

            if (oldDayNumber != detail.DayNumber)
            {
                await ReorderTripDetailsAsync(trip.TripId, oldDayNumber);
            }

            await ReorderTripDetailsAsync(trip.TripId, detail.DayNumber, detail.DetailId, request.SortOrder);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Trip detail updated. UserId={UserId}, TripId={TripId}, DetailId={DetailId}, SpotId={SpotId}, DayNumber={DayNumber}, SortOrder={SortOrder}",
                userId,
                trip.TripId,
                detail.DetailId,
                detail.SpotId,
                detail.DayNumber,
                detail.SortOrder);
            return await GetTripDetailItemAsync(userId, trip.TripId, detail.DetailId);
        }

        public async Task<bool> DeleteTripDetailAsync(string userId, int tripId, int detailId)
        {
            var trip = await GetOwnedTripAsync(userId, tripId);
            var detail = await _context.TTripDetails
                .FirstOrDefaultAsync(d => d.TripId == trip.TripId && d.DetailId == detailId && d.IsDeleted == false);

            if (detail == null)
            {
                throw new AppNotFoundException("找不到指定行程明細");
            }

            var dayNumber = detail.DayNumber;
            detail.IsDeleted = true;
            detail.TransportTime = null;
            detail.PolylineEncoded = string.Empty;
            detail.UpdateAt = DateTime.Now;

            await ReorderTripDetailsAsync(trip.TripId, dayNumber);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Trip detail deleted. UserId={UserId}, TripId={TripId}, DetailId={DetailId}, DayNumber={DayNumber}", userId, trip.TripId, detailId, dayNumber);
            return true;
        }

        public async Task<List<PlanDetailItemDTO>> SyncTripDetailsAsync(
            string userId,
            int tripId,
            TripDetailSyncRequestDTO request)
        {
            if (request == null)
            {
                throw new AppBadRequestException("請提供行程明細同步資料");
            }

            request.Created ??= new List<TripDetailCreateRequestDTO>();
            request.Updated ??= new List<TripDetailSyncUpdateRequestDTO>();
            request.DeletedDetailIds ??= new List<int>();

            var trip = await GetOwnedTripAsync(userId, tripId);
            var affectedDays = new HashSet<byte>();

            await using var transaction = _context.Database.IsRelational()
                ? await _context.Database.BeginTransactionAsync()
                : null;

            if (request.DeletedDetailIds.Any(id => id < 1))
            {
                throw new AppBadRequestException("刪除行程明細時 DetailID 必須大於 0");
            }

            var deletedDetailIds = request.DeletedDetailIds
                .Distinct()
                .ToList();

            if (deletedDetailIds.Count > 0)
            {
                var detailsToDelete = await _context.TTripDetails
                    .Where(d => d.TripId == trip.TripId
                             && deletedDetailIds.Contains(d.DetailId)
                             && d.IsDeleted == false)
                    .ToListAsync();

                if (detailsToDelete.Count != deletedDetailIds.Count)
                {
                    throw new AppNotFoundException("找不到指定行程明細");
                }

                foreach (var detail in detailsToDelete)
                {
                    affectedDays.Add(detail.DayNumber);
                    detail.IsDeleted = true;
                    detail.TransportTime = null;
                    detail.PolylineEncoded = string.Empty;
                    detail.UpdateAt = DateTime.Now;
                }
            }

            foreach (var update in request.Updated)
            {
                if (update.DetailId < 1)
                {
                    throw new AppBadRequestException("更新行程明細時 DetailID 必須大於 0");
                }

                ValidateTripDetailRequest(trip, update);
                ValidateTripDetailTextFields(update);

                var detail = await _context.TTripDetails
                    .FirstOrDefaultAsync(d => d.TripId == trip.TripId
                                           && d.DetailId == update.DetailId
                                           && d.IsDeleted == false);

                if (detail == null)
                {
                    throw new AppNotFoundException("找不到指定行程明細");
                }

                var oldDayNumber = detail.DayNumber;
                var spotId = await ResolveTripDetailSpotIdAsync(update);
                ApplyTripDetailRequest(detail, update, spotId, DateTime.Now, isCreate: false);
                affectedDays.Add(oldDayNumber);
                affectedDays.Add(detail.DayNumber);
            }

            foreach (var create in request.Created)
            {
                ValidateTripDetailRequest(trip, create);
                ValidateTripDetailTextFields(create);

                var spotId = await ResolveTripDetailSpotIdAsync(create);
                var detail = new TTripDetail
                {
                    TripId = trip.TripId,
                };

                ApplyTripDetailRequest(detail, create, spotId, DateTime.Now, isCreate: true);
                _context.TTripDetails.Add(detail);
                affectedDays.Add(detail.DayNumber);
            }

            await _context.SaveChangesAsync();

            foreach (var dayNumber in affectedDays.OrderBy(day => day))
            {
                await ReorderTripDetailsAsync(trip.TripId, dayNumber);
            }

            await _context.SaveChangesAsync();
            if (transaction != null)
            {
                await transaction.CommitAsync();
            }

            _logger.LogInformation(
                "Trip details synced. UserId={UserId}, TripId={TripId}, CreatedCount={CreatedCount}, UpdatedCount={UpdatedCount}, DeletedCount={DeletedCount}",
                userId,
                trip.TripId,
                request.Created.Count,
                request.Updated.Count,
                deletedDetailIds.Count);

            return await GetTripDetailItemsAsync(trip);
        }

        public async Task<List<HomeRecommendPlanDTO>> GetRecommendedPlansAsync()
        {
            _logger.LogInformation("取得推薦行程中...");

            // 1. 取得符合條件的 TripIds（每個作者最新的一筆，並依時間排序取前 20 筆）
            var topTripIds = await _context.TTrips
                .Where(t => t.OfficeOper == 1 && t.IsDeleted == false && t.DemoSessionId == null)
                .GroupBy(t => t.OwnerId)
                .Select(g => new
                {
                    OwnerId = g.Key,
                    MaxCreateAt = g.Max(t => t.CreateAt),
                    TripId = g.OrderByDescending(t => t.CreateAt).Select(t => t.TripId).FirstOrDefault()
                })
                .OrderByDescending(x => x.MaxCreateAt)
                .Take(20)
                .Select(x => x.TripId)
                .ToListAsync();

            _logger.LogInformation("成功取得 {count} 筆熱門行程 ID", topTripIds.Count);

            // 2. 撈出完整行程資料
            var trips = await _context.TTrips
                .Where(t => topTripIds.Contains(t.TripId))
                .OrderByDescending(t => t.CreateAt) // 確保最終呈現順序與最新時間一致
                .ToListAsync();

            _logger.LogInformation("成功取得 {count} 筆熱門行程", trips.Count);

            // 3. 取得關聯的作者名稱與資訊
            var ownerIds = trips.Select(t => t.OwnerId).Distinct().ToList();

            var members = await _context.TMembers
                .Where(m => ownerIds.Contains(m.MemberId))
                .ToDictionaryAsync(m => m.MemberId, m => m);

            _logger.LogInformation("取得對應的 memberId 數量: {count}", members.Count);

            // 4. 一次性取得所有相關行程的景點數量 (避免 N+1 查詢)
            var spotCounts = await _context.TTripDetails
                .Where(d => topTripIds.Contains(d.TripId) && d.IsDeleted == false)
                .GroupBy(d => d.TripId)
                .Select(g => new { TripId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.TripId, x => x.Count);

            // 5. 轉換為 DTO 並進行資料組裝
            var result = trips.Select(t =>
            {
                var dto = _mapper.Map<HomeRecommendPlanDTO>(t);

                // 計算天數 (包含起始日與結束日)
                dto.DurationDays = (t.EndDate.DayNumber - t.StartDate.DayNumber) + 1;

                // 補上作者資訊
                if (members.TryGetValue(t.OwnerId, out var member))
                {
                    dto.Author = member.Name;
                    dto.UserId = member.UserId;
                    dto.AvatarUrl = member.AvatarUrl;
                }

                // 補上景點數量
                dto.spotCount = spotCounts.GetValueOrDefault(t.TripId, 0);

                return dto;
            }).ToList();

            _logger.LogInformation("成功取得推薦 {count} 筆", result.Count);

            return result;
        }
    }
}
