using AutoMapper;
using AutoMapper.Execution;
using AutoMapper.QueryableExtensions;
using Luiu.Domain.Exceptions;
using Luiu.Domain.Models;
using Luiu.Service.DTOs.V1.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text.Json;
using System.Threading.Tasks;

namespace Luiu.Service.Implementations
{
    public class MemoryService : BaseService<MemoryService>
    {
        private readonly GooglePlacesService _googlePlacesService;

        public MemoryService(LuiuDbContext context, ILogger<MemoryService> logger, IMapper mapper, GooglePlacesService googlePlacesService) : base(context, logger, mapper) 
        { 
            _googlePlacesService = googlePlacesService;
        }

        public async Task<List<MemoryListResponseDTO>> GetMemoriesAsync(MemoryQueryParameters parameters)
        {
            _logger.LogInformation("開始查詢 Memory 列表...");

            // STEP 1: 子查詢篩選條件
            var query = _context.TMemories.Where(x => x.IsDelete == false).AsQueryable();

            if (!string.IsNullOrEmpty(parameters.Keyword))
            {
                query = query.Where(x => x.Title.Contains(parameters.Keyword));
            }

            if (parameters.UserId.HasValue)
            {
                query = query.Where(x => x.UserId == parameters.UserId.Value);
            }

            // STEP 2: 取出 Memory 主列表 (1 支 SQL)
            var memories = await query
                .OrderByDescending(mem => mem.MemoryId)
                .ToListAsync();

            _logger.LogInformation("Memory 主查詢完成，共 {Count} 筆", memories.Count);

            // STEP 3: 先判斷是否有效，防空集合提前返回
            if (!memories.Any())
            {
                _logger.LogInformation("Memory 列表為空，提前返回。");
                return new List<MemoryListResponseDTO>();
            }

            // STEP 4: 子查詢一次取出所有相關作者 (1 支 SQL，不使用 JOIN)
            var userIds = memories.Select(m => m.UserId).Distinct().ToList();
            var members = await _context.TMembers
                .Where(u => userIds.Contains(u.MemberId))
                .Select(u => new { u.MemberId, u.Name, u.AvatarUrl, u.UserId })
                .ToListAsync();

            var memberDict = members.ToDictionary(m => m.MemberId);
            _logger.LogInformation("作者資料子查詢完成，共取得 {Count} 位作者", members.Count);

            // STEP 5: 在記憶體中組合結果，不再打資料庫
            var result = memories.Select(mem => new MemoryListResponseDTO
            {
                MemoryId = mem.MemoryId,
                UserId = mem.UserId,
                AuthorUserId = memberDict.TryGetValue(mem.UserId, out var m3) ? m3.UserId : string.Empty,
                AuthorName = memberDict.TryGetValue(mem.UserId, out var m) ? m.Name : "無名旅人",
                AuthorAvatarUrl = memberDict.TryGetValue(mem.UserId, out var m2) ? m2.AvatarUrl : null,
                Title = mem.Title,
                CoverImage = mem.CoverImage,
                StartDate = mem.StartDate,
                EndDate = mem.EndDate,
                ViewCount = mem.ViewCount,
                LikeCount = mem.LikeCount,
                FavoriteCount = mem.FavoriteCount,
                SourceTripId = mem.SourceTripId
            }).ToList();

            _logger.LogInformation("Memory 列表組合完成，返回 {Count} 筆。", result.Count);
            return result;
        }


        public async Task<MemoryDetailResponseDTO> GetMemoryDetailAsync(int id)
        {
            _logger.LogInformation($"開始查詢 Memory 詳情，MemoryId: {id}");

            // 1. 先判斷是否有效
            var memoryExists = await _context.TMemories.AnyAsync(m => m.MemoryId == id && m.IsDelete == false);
            if (!memoryExists)
            {
                _logger.LogWarning($"查詢失敗：找不到指定的回憶 (MemoryId: {id})");
                throw new AppNotFoundException("找不到指定的回憶");
            }

            // 2. 子查詢寫法，不使用 join
            var memory = await _context.TMemories
                .Where(mem => mem.MemoryId == id && mem.IsDelete == false)
                .Select(mem => new MemoryDetailResponseDTO
                {
                    MemoryId = mem.MemoryId,
                    UserId = mem.UserId,
                    AuthorUserId = _context.TMembers.Where(u => u.MemberId == mem.UserId).Select(u => u.UserId).FirstOrDefault(),
                    // 利用子查詢撈取關聯的使用者資料
                    AuthorName = _context.TMembers.Where(u => u.MemberId == mem.UserId).Select(u => u.Name).FirstOrDefault(),
                    AuthorAvatarUrl = _context.TMembers.Where(u => u.MemberId == mem.UserId).Select(u => u.AvatarUrl).FirstOrDefault(),
                    Title = mem.Title,
                    CoverImage = mem.CoverImage,
                    StartDate = mem.StartDate,
                    EndDate = mem.EndDate,
                    ReviewStatus = mem.ReviewStatus,
                    ViewCount = mem.ViewCount,
                    LikeCount = mem.LikeCount,
                    FavoriteCount = mem.FavoriteCount,
                    SourceTripId = mem.SourceTripId
                }).FirstOrDefaultAsync();

            // 取得天數資料
            var days = await _context.TMemoryDays
                .Where(d => d.MemoryId == id)
                .OrderBy(d => d.DayNumber)
                .ToListAsync();

            // 防呆：過濾資料庫中可能存在的重複測試資料
            days = days.GroupBy(d => d.DayNumber).Select(g => g.First()).ToList();

            // 取得對應景點資料
            var dayIds = days.Select(d => d.DayId).ToList();
            var stops = await _context.TMemoryStops
                .Where(s => dayIds.Contains(s.DayId))
                .OrderBy(s => s.ArrivalTime)
                .ToListAsync();

            // 組裝巢狀結構
            foreach (var day in days)
            {
                var dayDto = new MemoryDayDTO
                {
                    DayId = day.DayId,
                    DayNumber = day.DayNumber,
                    DayDate = day.DayDate,
                    Stops = stops.Where(s => s.DayId == day.DayId).Select(s => new MemoryStopDTO
                    {
                        StopId = s.StopId,
                        PlaceName = s.PlaceName,
                        Latitude = s.Latitude,
                        Longitude = s.Longitude,
                        ArrivalTime = s.ArrivalTime,
                        MemoryText = s.MemoryText,
                        VideoEmbedUrl = s.VideoEmbedUrl,
                        Duration = s.Duration,
                        Expense = s.Expense,
                        Rating = s.Rating,
                        ImageUrls = string.IsNullOrEmpty(s.ImageUrls) ? new List<string>() : JsonSerializer.Deserialize<List<string>>(s.ImageUrls)
                    }).ToList()
                };
                memory.Days.Add(dayDto);
            }

            _logger.LogInformation($"查詢 Memory 詳情完成，包含 {memory.Days.Count} 天的行程。");
            return memory;
        }

        public async Task<MemoryDetailResponseDTO> CreateMemoryAsync(MemoryCreateRequestDTO request, string currentUserIdStr)
        {
            _logger.LogInformation($"開始建立 Memory，要求建立的 UserId(String): {currentUserIdStr}");

            // 1. 先判斷是否有效 (防呆檢查)
            if (string.IsNullOrWhiteSpace(request.Title))
            {
                _logger.LogWarning("建立失敗：回憶標題不能為空。");
                throw new AppNotFoundException("回憶標題不能為空"); // 暫時代替 BadRequest
            }

            if (request.StartDate.HasValue && request.EndDate.HasValue && request.StartDate > request.EndDate)
            {
                _logger.LogWarning("建立失敗：開始日期不能大於結束日期。");
                throw new AppNotFoundException("開始日期不能大於結束日期"); // 暫時代替 BadRequest
            }

            // 安全性加固：利用 Token 中的字串 UserId 尋找資料庫的會員
            var member = await _context.TMembers.FirstOrDefaultAsync(m => m.UserId == currentUserIdStr && m.IsDelete == false);
            if (member == null)
            {
                _logger.LogWarning($"建立失敗：找不到指定會員或會員已被刪除 (String UserId: {currentUserIdStr})");
                throw new AppNotFoundException("找不到目前登入的會員資料，無法發文");
            }

            // 如果前端有傳入 SourceTripId，進行嚴格防呆驗證 (不使用 JOIN，以 AnyAsync 子查詢驗證)
            if (request.SourceTripId.HasValue)
            {
                var isValidTrip = await _context.TTrips.AnyAsync(t => t.TripId == request.SourceTripId.Value && t.OwnerId == member.MemberId);
                if (!isValidTrip)
                {
                    _logger.LogWarning($"建立失敗：找不到指定的行程或權限不足 (TripId: {request.SourceTripId.Value}, MemberId: {member.MemberId})");
                    throw new AppNotFoundException("指定的行程不存在或無權限匯入");
                }
                _logger.LogInformation($"準備從行程 (TripId: {request.SourceTripId.Value}) 建立回憶貼文");
            }

            // 導入資料庫交易保護機制
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var newMemory = new TMemory
                {
                    UserId = member.MemberId, // 直接使用從資料庫查得的整數 MemberId，不再透過 request.UserId 中轉
                    Title = request.Title,
                    CoverImage = request.CoverImage,
                    StartDate = request.StartDate,
                    EndDate = request.EndDate,
                    ReviewStatus = 0, // 假設 0 代表草稿或待審核
                    ViewCount = 0,
                    LikeCount = 0,
                    FavoriteCount = 0,
                    SourceTripId = request.SourceTripId
                };

                _context.TMemories.Add(newMemory);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Memory 主檔建立成功，MemoryId: {newMemory.MemoryId}");

                // 新增天數與景點 (巢狀寫入)
                if (request.Days != null && request.Days.Any())
                {
                    _logger.LogInformation($"準備寫入 {request.Days.Count} 天的行程資料...");
                    
                    foreach (var dayReq in request.Days)
                    {
                        var newDay = new TMemoryDay
                        {
                            MemoryId = newMemory.MemoryId,
                            DayNumber = dayReq.DayNumber,
                            DayDate = newMemory.StartDate.HasValue ? newMemory.StartDate.Value.AddDays(dayReq.DayNumber - 1) : null
                        };
                        _context.TMemoryDays.Add(newDay);
                        await _context.SaveChangesAsync(); // 先儲存以取得 DayId

                        if (dayReq.Stops != null && dayReq.Stops.Any())
                        {
                            foreach (var stopReq in dayReq.Stops)
                            {
                                DateTime? arrivalTime = null;
                                if (!string.IsNullOrEmpty(stopReq.Time) && TimeSpan.TryParse(stopReq.Time, out var parsedTime))
                                {
                                    var baseDate = newDay.DayDate.HasValue ? newDay.DayDate.Value.ToDateTime(new TimeOnly(0, 0)) : new DateTime(2000, 1, 1);
                                    arrivalTime = baseDate + parsedTime;
                                }

                                decimal lat = 0;
                                decimal lng = 0;

                                // 使用 Google API 根據景點名稱自動搜尋經緯度
                                if (!string.IsNullOrEmpty(stopReq.Title))
                                {
                                    try
                                    {
                                        var searchResult = await _googlePlacesService.TextSearchAsync(stopReq.Title);
                                        var firstPlace = searchResult?.FirstOrDefault();
                                        if (firstPlace != null)
                                        {
                                            lat = (decimal)firstPlace.Latitude;
                                            lng = (decimal)firstPlace.Longitude;
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger.LogWarning(ex, $"無法取得景點 '{stopReq.Title}' 的經緯度，將使用預設值。");
                                    }
                                }

                                var newStop = new TMemoryStop
                                {
                                    DayId = newDay.DayId,
                                    PlaceName = string.IsNullOrEmpty(stopReq.Title) ? "未命名景點" : stopReq.Title,
                                    Latitude = lat,
                                    Longitude = lng,
                                    ArrivalTime = arrivalTime,
                                    MemoryText = stopReq.Description,
                                    VideoEmbedUrl = stopReq.VideoUrl,
                                    Duration = stopReq.Duration,
                                    Expense = stopReq.Expense,
                                    Rating = stopReq.Rating,
                                    ImageUrls = stopReq.ImageUrls != null && stopReq.ImageUrls.Any() ? JsonSerializer.Serialize(stopReq.ImageUrls, new JsonSerializerOptions { WriteIndented = false }) : null
                                };
                                _context.TMemoryStops.Add(newStop);
                            }
                        }
                    }
                    await _context.SaveChangesAsync(); // 儲存所有景點
                    _logger.LogInformation("天數與景點資料寫入完成。");
                }

                await transaction.CommitAsync(); // 提交交易
                _logger.LogInformation($"Memory {newMemory.MemoryId} 完整建立流程結束。交易已提交。");
                
                return await GetMemoryDetailAsync(newMemory.MemoryId);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(); // 發生異常時 Rollback
                _logger.LogError(ex, $"建立 Memory 時發生未預期的錯誤: {ex.Message}，已執行 Rollback。");
                throw;
            }
        }

        public async Task<MemoryDetailResponseDTO> UpdateMemoryAsync(int memoryId, MemoryCreateRequestDTO request, string currentUserIdStr)
        {
            _logger.LogInformation($"開始更新 Memory，要求更新的 UserId(String): {currentUserIdStr}, MemoryId: {memoryId}");

            if (string.IsNullOrWhiteSpace(request.Title))
            {
                _logger.LogWarning("更新失敗：回憶標題不能為空。");
                throw new AppNotFoundException("回憶標題不能為空");
            }

            if (request.StartDate.HasValue && request.EndDate.HasValue && request.StartDate > request.EndDate)
            {
                _logger.LogWarning("更新失敗：開始日期不能大於結束日期。");
                throw new AppNotFoundException("開始日期不能大於結束日期");
            }

            var member = await _context.TMembers.FirstOrDefaultAsync(m => m.UserId == currentUserIdStr && m.IsDelete == false);
            if (member == null)
            {
                _logger.LogWarning($"更新失敗：找不到指定會員或會員已被刪除 (String UserId: {currentUserIdStr})");
                throw new AppNotFoundException("找不到目前登入的會員資料，無法修改");
            }

            var memory = await _context.TMemories.FirstOrDefaultAsync(m => m.MemoryId == memoryId && m.IsDelete == false);
            if (memory == null)
            {
                throw new AppNotFoundException("找不到該回憶貼文");
            }

            if (memory.UserId != member.MemberId)
            {
                throw new AppNotFoundException("無權限修改此回憶貼文");
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 更新主檔
                memory.Title = request.Title;
                memory.CoverImage = request.CoverImage;
                memory.StartDate = request.StartDate;
                memory.EndDate = request.EndDate;
                
                if (request.SourceTripId.HasValue)
                {
                    memory.SourceTripId = request.SourceTripId;
                }

                // 移除舊的天數與景點
                var existingDays = await _context.TMemoryDays.Where(d => d.MemoryId == memoryId).ToListAsync();
                if (existingDays.Any())
                {
                    var dayIds = existingDays.Select(d => d.DayId).ToList();
                    var existingStops = await _context.TMemoryStops.Where(s => dayIds.Contains(s.DayId)).ToListAsync();
                    
                    _context.TMemoryStops.RemoveRange(existingStops);
                    _context.TMemoryDays.RemoveRange(existingDays);
                }

                // 新增新的天數與景點
                if (request.Days != null && request.Days.Any())
                {
                    foreach (var dayReq in request.Days)
                    {
                        var newDay = new TMemoryDay
                        {
                            MemoryId = memory.MemoryId,
                            DayNumber = dayReq.DayNumber,
                            DayDate = memory.StartDate.HasValue ? memory.StartDate.Value.AddDays(dayReq.DayNumber - 1) : null
                        };
                        _context.TMemoryDays.Add(newDay);
                        await _context.SaveChangesAsync();

                        if (dayReq.Stops != null && dayReq.Stops.Any())
                        {
                            foreach (var stopReq in dayReq.Stops)
                            {
                                DateTime? arrivalTime = null;
                                if (!string.IsNullOrEmpty(stopReq.Time) && TimeSpan.TryParse(stopReq.Time, out var parsedTime))
                                {
                                    var baseDate = newDay.DayDate.HasValue ? newDay.DayDate.Value.ToDateTime(new TimeOnly(0, 0)) : new DateTime(2000, 1, 1);
                                    arrivalTime = baseDate + parsedTime;
                                }

                                decimal lat = 0;
                                decimal lng = 0;

                                if (!string.IsNullOrEmpty(stopReq.Title))
                                {
                                    try
                                    {
                                        var searchResult = await _googlePlacesService.TextSearchAsync(stopReq.Title);
                                        var firstPlace = searchResult?.FirstOrDefault();
                                        if (firstPlace != null)
                                        {
                                            lat = (decimal)firstPlace.Latitude;
                                            lng = (decimal)firstPlace.Longitude;
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger.LogWarning(ex, $"無法取得景點 '{stopReq.Title}' 的經緯度，將使用預設值。");
                                    }
                                }

                                var newStop = new TMemoryStop
                                {
                                    DayId = newDay.DayId,
                                    PlaceName = string.IsNullOrEmpty(stopReq.Title) ? "未命名景點" : stopReq.Title,
                                    Latitude = lat,
                                    Longitude = lng,
                                    ArrivalTime = arrivalTime,
                                    MemoryText = stopReq.Description,
                                    VideoEmbedUrl = stopReq.VideoUrl,
                                    Duration = stopReq.Duration,
                                    Expense = stopReq.Expense,
                                    Rating = stopReq.Rating,
                                    ImageUrls = stopReq.ImageUrls != null && stopReq.ImageUrls.Any() ? JsonSerializer.Serialize(stopReq.ImageUrls, new JsonSerializerOptions { WriteIndented = false }) : null
                                };
                                _context.TMemoryStops.Add(newStop);
                            }
                        }
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                
                _logger.LogInformation($"Memory {memory.MemoryId} 更新成功。");
                return await GetMemoryDetailAsync(memory.MemoryId);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, $"更新 Memory 時發生未預期的錯誤: {ex.Message}，已執行 Rollback。");
                throw;
            }
        }

        // 更新按讚數：isLike 為 true 時 +1，為 false 時 -1 (最小為 0)
        public async Task<bool> UpdateLikeCountAsync(int memoryId, bool isLike)
        {
            var memory = await _context.TMemories.FirstOrDefaultAsync(m => m.MemoryId == memoryId && m.IsDelete == false);
            if (memory == null) return false;

            if (isLike)
            {
                memory.LikeCount += 1;
            }
            else
            {
                memory.LikeCount = Math.Max(0, memory.LikeCount - 1);
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteMemoryAsync(int memoryId, string currentUserIdStr)
        {
            _logger.LogInformation($"開始軟刪除 Memory，ID: {memoryId}，使用者: {currentUserIdStr}");

            var member = await _context.TMembers.FirstOrDefaultAsync(m => m.UserId == currentUserIdStr && m.IsDelete == false);
            if (member == null)
            {
                throw new AppNotFoundException("找不到目前登入的會員資料，無法刪除");
            }

            var memory = await _context.TMemories.FirstOrDefaultAsync(m => m.MemoryId == memoryId && m.IsDelete == false);
            if (memory == null)
            {
                throw new AppNotFoundException("找不到該回憶貼文");
            }

            if (memory.UserId != member.MemberId)
            {
                throw new AppNotFoundException("無權限刪除此回憶貼文");
            }

            memory.IsDelete = true;
            var rowsAffected = await _context.SaveChangesAsync();

            _logger.LogInformation($"成功軟刪除 Memory {memoryId}");
            return rowsAffected > 0;
        }
        
        public async Task<List<MemberProfileMemoriesDTO>> GetMemoriesByUserAsync(string userId)
        {
            int? memberId = await _context.TMembers
                .Where(m => m.UserId == userId)
                .Select(m => (int?)m.MemberId)
                .FirstOrDefaultAsync();

            if (memberId == null) throw new AppNotFoundException("查無此會員");

            _logger.LogInformation("取得回憶中...");

            return await _context.TMemories
                .Where(m => m.UserId == memberId)
                .ProjectTo<MemberProfileMemoriesDTO>(_mapper.ConfigurationProvider)
                .ToListAsync();
        }

        //public async Task<List<HomeHotMemoryDTO>> GetHotMemoriesAsync()
        //{
        //    _logger.LogInformation("取得推薦回憶中...");

        //    var memories = await _context.TMemories
        //        .Where(m => m.OfficeOper == 1)
        //        .GroupBy(m => m.UserId)
        //        .Select(g => g.OrderByDescending(m => m.LikeCount).FirstOrDefault())
        //        .OrderByDescending(m => m.LikeCount)
        //        .Take(20)
        //        .ToListAsync();
        //    _logger.LogInformation("成功取得 {count} 筆熱門回憶", memories.Count);

        //    var MemberIds = memories.Select(m => m.MemoryId).Distinct().ToList();
        //    var members = await _context.TMembers
        //        .Where(m => MemberIds.Contains(m.MemberId))
        //        .ToDictionaryAsync(m => m.MemberId, m => m);
        //    _logger.LogInformation("取得對應的 memberId 數量: {count}", members.Count);

        //    //var result = memories.Select(m =>
        //    //{
        //    //    var dto = _mapper.Map<HomeHotMemoryDTO>(m);
        //    //    if (members.TryGetValue(m.UserId, out var member))
        //    //    {
        //    //        dto.Author = member.Name;
        //    //        dto.AvatarUrl = member.AvatarUrl;
        //    //    }
        //    //    return dto;
        //    //}).ToList();
        //    var result = new List<HomeHotMemoryDTO>();
        //    _logger.LogInformation("成功取得推薦 {count} 筆", result.Count);

        //    return result;
        //}

        public async Task<List<HomeHotMemoryDTO>> GetHotMemoriesAsync()
        {
            _logger.LogInformation("取得推薦回憶中...");
            var topGroupIds = await _context.TMemories
                .Where(m => m.OfficeOper == 1)
                .GroupBy(m => m.UserId)
                .Select(g => new
                {
                    UserId = g.Key,
                    MaxLike = g.Max(m => m.LikeCount),
                    MemoryId = g.OrderByDescending(m => m.LikeCount).Select(m => m.MemoryId).FirstOrDefault()
                })
                .OrderByDescending(x => x.MaxLike)
                .Take(20)
                .Select(x => x.MemoryId)
                .ToListAsync();
            _logger.LogInformation("成功取得 {count} 筆熱門回憶", topGroupIds.Count);

            var memories = await _context.TMemories
                .Where(m => topGroupIds.Contains(m.MemoryId))
                .OrderByDescending(m => m.LikeCount)
                .ToListAsync();

            _logger.LogInformation("成功取得 {count} 筆熱門回憶", memories.Count);

            var userIds = memories.Select(m => m.UserId).Distinct().ToList();

            var members = await _context.TMembers
                .Where(m => userIds.Contains(m.MemberId))
                .ToDictionaryAsync(m => m.MemberId, m => m);
            _logger.LogInformation("取得對應的 memberId 數量: {count}", members.Count);

            var result = memories.Select(m =>
            {
                var dto = _mapper.Map<HomeHotMemoryDTO>(m);
                if (members.TryGetValue(m.UserId, out var member))
                {
                    dto.Author = member.Name;
                    dto.AvatarUrl = member.AvatarUrl;
                }
                return dto;
            }).ToList();

            _logger.LogInformation("成功取得推薦 {count} 筆", result.Count);

            return result;
        }
    }
}
