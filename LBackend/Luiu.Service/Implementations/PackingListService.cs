using AutoMapper;
using Luiu.Domain.Exceptions;
using Luiu.Domain.Models;
using Luiu.Service.DTOs.V1.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Luiu.Service.Implementations
{
    public class PackingListService : BaseService<PackingListService>
    {
        private const int NameMaxLength = 20;

        public PackingListService(
            LuiuDbContext context,
            ILogger<PackingListService> logger,
            IMapper mapper) : base(context, logger, mapper)
        {
        }

        private static string NormalizeName(string? name, string fieldName)
        {
            var normalizedName = name?.Trim();
            if (string.IsNullOrWhiteSpace(normalizedName))
            {
                throw new AppBadRequestException($"請輸入{fieldName}");
            }

            if (normalizedName.Length > NameMaxLength)
            {
                throw new AppBadRequestException($"{fieldName}不可超過 {NameMaxLength} 個字");
            }

            return normalizedName;
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
            var trip = await _context.TTrips
                .FirstOrDefaultAsync(t => t.OwnerId == member.MemberId
                                       && t.TripId == tripId
                                       && t.IsDeleted == false);

            if (trip == null)
            {
                throw new AppNotFoundException("找不到指定行程");
            }

            return trip;
        }

        private async Task<TUserPackingList> GetActivePackingListAsync(TTrip trip)
        {
            if (!trip.ListId.HasValue)
            {
                throw new AppNotFoundException("找不到指定行李清單");
            }

            var list = await _context.TUserPackingLists
                .FirstOrDefaultAsync(l => l.ListId == trip.ListId.Value && l.IsDeleted == false);

            if (list == null)
            {
                throw new AppNotFoundException("找不到指定行李清單");
            }

            return list;
        }

        private async Task<TPackingCategory> GetActiveCategoryAsync(TUserPackingList list, int categoryId)
        {
            var category = await _context.TPackingCategories
                .FirstOrDefaultAsync(c => c.ListId == list.ListId
                                       && c.CategoryId == categoryId
                                       && c.IsDeleted == false);

            if (category == null)
            {
                throw new AppNotFoundException("找不到指定行李分類");
            }

            return category;
        }

        private async Task<TPackingItem> GetActiveItemAsync(TUserPackingList list, int itemId)
        {
            var item = await _context.TPackingItems
                .Join(
                    _context.TPackingCategories,
                    item => item.CategoryId,
                    category => category.CategoryId,
                    (item, category) => new { item, category })
                .Where(x => x.category.ListId == list.ListId
                         && x.category.IsDeleted == false
                         && x.item.ItemId == itemId
                         && x.item.IsDeleted == false)
                .Select(x => x.item)
                .FirstOrDefaultAsync();

            if (item == null)
            {
                throw new AppNotFoundException("找不到指定行李項目");
            }

            return item;
        }

        private async Task<PackingListResponseDTO> MapPackingListResponseAsync(TUserPackingList list)
        {
            var userName = await _context.TMembers
                .Where(m => m.MemberId == list.UserId && m.IsDelete == false)
                .Select(m => m.Name)
                .FirstOrDefaultAsync();

            var categories = await _context.TPackingCategories
                .Where(c => c.ListId == list.ListId && c.IsDeleted == false)
                .OrderBy(c => c.CreateAt)
                .ThenBy(c => c.CategoryId)
                .ToListAsync();

            var categoryIds = categories.Select(c => c.CategoryId).ToList();
            var items = await _context.TPackingItems
                .Where(i => categoryIds.Contains(i.CategoryId) && i.IsDeleted == false)
                .OrderBy(i => i.CreateAt)
                .ThenBy(i => i.ItemId)
                .ToListAsync();

            var itemsByCategory = items
                .GroupBy(i => i.CategoryId)
                .ToDictionary(g => g.Key, g => g.ToList());

            return new PackingListResponseDTO
            {
                ListId = list.ListId,
                ListName = list.ListName,
                UserId = list.UserId,
                UserName = userName ?? string.Empty,
                IsDeleted = list.IsDeleted,
                CreateAt = list.CreateAt,
                UpdateAt = list.UpdateAt,
                Categories = categories
                    .Select(category => new PackingCategoryResponseDTO
                    {
                        CategoryId = category.CategoryId,
                        CategoryName = category.CategoryName,
                        ListId = category.ListId,
                        IsDeleted = category.IsDeleted,
                        CreateAt = category.CreateAt,
                        UpdateAt = category.UpdateAt,
                        Items = itemsByCategory.TryGetValue(category.CategoryId, out var categoryItems)
                            ? categoryItems.Select(MapPackingItemResponse).ToList()
                            : new List<PackingItemResponseDTO>()
                    })
                    .ToList()
            };
        }

        private static PackingCategoryResponseDTO MapPackingCategoryResponse(TPackingCategory category)
        {
            return new PackingCategoryResponseDTO
            {
                CategoryId = category.CategoryId,
                CategoryName = category.CategoryName,
                ListId = category.ListId,
                IsDeleted = category.IsDeleted,
                CreateAt = category.CreateAt,
                UpdateAt = category.UpdateAt
            };
        }

        private static PackingItemResponseDTO MapPackingItemResponse(TPackingItem item)
        {
            return new PackingItemResponseDTO
            {
                ItemId = item.ItemId,
                ItemName = item.ItemName,
                IsCheck = item.IsCheck ?? false,
                CategoryId = item.CategoryId,
                IsDeleted = item.IsDeleted,
                CreateAt = item.CreateAt,
                UpdateAt = item.UpdateAt
            };
        }

        public async Task<PackingListResponseDTO?> GetPackingListAsync(string userId, int tripId)
        {
            var trip = await GetOwnedTripAsync(userId, tripId);
            if (!trip.ListId.HasValue)
            {
                return null;
            }

            var list = await _context.TUserPackingLists
                .FirstOrDefaultAsync(l => l.ListId == trip.ListId.Value && l.IsDeleted == false);

            if (list == null)
            {
                return null;
            }

            return await MapPackingListResponseAsync(list);
        }

        public async Task<List<PackingListSummaryResponseDTO>> GetPackingListSummariesAsync(string userId)
        {
            var member = await ResolveMemberAsync(userId);

            return await _context.TUserPackingLists
                .Where(list => list.UserId == member.MemberId && list.IsDeleted == false)
                .OrderByDescending(list => list.CreateAt)
                .ThenByDescending(list => list.ListId)
                .Select(list => new PackingListSummaryResponseDTO
                {
                    ListId = list.ListId,
                    ListName = list.ListName
                })
                .ToListAsync();
        }

        public async Task<PackingListResponseDTO> CreatePackingListAsync(
            string userId,
            int tripId,
            PackingListCreateRequestDTO request)
        {
            var member = await ResolveMemberAsync(userId);
            var trip = await _context.TTrips
                .FirstOrDefaultAsync(t => t.OwnerId == member.MemberId
                                       && t.TripId == tripId
                                       && t.IsDeleted == false);

            if (trip == null)
            {
                throw new AppNotFoundException("找不到指定行程");
            }

            if (trip.ListId.HasValue)
            {
                var existingList = await _context.TUserPackingLists
                    .FirstOrDefaultAsync(l => l.ListId == trip.ListId.Value && l.IsDeleted == false);
                if (existingList != null)
                {
                    throw new AppConflictException("此行程已存在行李清單");
                }
            }

            var now = DateTime.Now;
            var list = new TUserPackingList
            {
                ListName = NormalizeName(request.ListName, "清單名稱"),
                UserId = member.MemberId,
                IsDeleted = false,
                CreateAt = now,
                UpdateAt = now
            };

            _context.TUserPackingLists.Add(list);
            await _context.SaveChangesAsync();

            trip.ListId = list.ListId;
            trip.UpdateAt = now;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Packing list created. UserId={UserId}, TripId={TripId}, ListId={ListId}", userId, tripId, list.ListId);
            return await MapPackingListResponseAsync(list);
        }

        public async Task<PackingListResponseDTO> UpdatePackingListAsync(
            string userId,
            int tripId,
            PackingListUpdateRequestDTO request)
        {
            var trip = await GetOwnedTripAsync(userId, tripId);
            var list = await GetActivePackingListAsync(trip);

            list.ListName = NormalizeName(request.ListName, "清單名稱");
            list.UpdateAt = DateTime.Now;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Packing list updated. UserId={UserId}, TripId={TripId}, ListId={ListId}", userId, tripId, list.ListId);
            return await MapPackingListResponseAsync(list);
        }

        public async Task<bool> DeletePackingListAsync(string userId, int tripId)
        {
            var trip = await GetOwnedTripAsync(userId, tripId);
            var list = await GetActivePackingListAsync(trip);

            var categories = await _context.TPackingCategories
                .Where(c => c.ListId == list.ListId && c.IsDeleted == false)
                .ToListAsync();
            var categoryIds = categories.Select(c => c.CategoryId).ToList();
            var items = await _context.TPackingItems
                .Where(i => categoryIds.Contains(i.CategoryId) && i.IsDeleted == false)
                .ToListAsync();

            var now = DateTime.Now;
            list.IsDeleted = true;
            list.UpdateAt = now;
            foreach (var category in categories)
            {
                category.IsDeleted = true;
                category.UpdateAt = now;
            }

            foreach (var item in items)
            {
                item.IsDeleted = true;
                item.UpdateAt = now;
            }

            trip.ListId = null;
            trip.UpdateAt = now;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Packing list deleted. UserId={UserId}, TripId={TripId}, ListId={ListId}", userId, tripId, list.ListId);
            return true;
        }

        public async Task<PackingCategoryResponseDTO> CreateCategoryAsync(
            string userId,
            int tripId,
            PackingCategoryCreateRequestDTO request)
        {
            var trip = await GetOwnedTripAsync(userId, tripId);
            var list = await GetActivePackingListAsync(trip);
            var now = DateTime.Now;
            var category = new TPackingCategory
            {
                CategoryName = NormalizeName(request.CategoryName, "分類名稱"),
                ListId = list.ListId,
                IsDeleted = false,
                CreateAt = now,
                UpdateAt = now
            };

            _context.TPackingCategories.Add(category);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Packing category created. UserId={UserId}, TripId={TripId}, CategoryId={CategoryId}", userId, tripId, category.CategoryId);
            return MapPackingCategoryResponse(category);
        }

        public async Task<PackingCategoryResponseDTO> UpdateCategoryAsync(
            string userId,
            int tripId,
            int categoryId,
            PackingCategoryUpdateRequestDTO request)
        {
            var trip = await GetOwnedTripAsync(userId, tripId);
            var list = await GetActivePackingListAsync(trip);
            var category = await GetActiveCategoryAsync(list, categoryId);

            category.CategoryName = NormalizeName(request.CategoryName, "分類名稱");
            category.UpdateAt = DateTime.Now;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Packing category updated. UserId={UserId}, TripId={TripId}, CategoryId={CategoryId}", userId, tripId, category.CategoryId);
            return MapPackingCategoryResponse(category);
        }

        public async Task<bool> DeleteCategoryAsync(string userId, int tripId, int categoryId)
        {
            var trip = await GetOwnedTripAsync(userId, tripId);
            var list = await GetActivePackingListAsync(trip);
            var category = await GetActiveCategoryAsync(list, categoryId);
            var items = await _context.TPackingItems
                .Where(i => i.CategoryId == category.CategoryId && i.IsDeleted == false)
                .ToListAsync();

            var now = DateTime.Now;
            category.IsDeleted = true;
            category.UpdateAt = now;
            foreach (var item in items)
            {
                item.IsDeleted = true;
                item.UpdateAt = now;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Packing category deleted. UserId={UserId}, TripId={TripId}, CategoryId={CategoryId}", userId, tripId, category.CategoryId);
            return true;
        }

        public async Task<PackingItemResponseDTO> CreateItemAsync(
            string userId,
            int tripId,
            int categoryId,
            PackingItemCreateRequestDTO request)
        {
            var trip = await GetOwnedTripAsync(userId, tripId);
            var list = await GetActivePackingListAsync(trip);
            var category = await GetActiveCategoryAsync(list, categoryId);
            var now = DateTime.Now;
            var item = new TPackingItem
            {
                ItemName = NormalizeName(request.ItemName, "項目名稱"),
                IsCheck = request.IsCheck ?? false,
                CategoryId = category.CategoryId,
                IsDeleted = false,
                CreateAt = now,
                UpdateAt = now
            };

            _context.TPackingItems.Add(item);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Packing item created. UserId={UserId}, TripId={TripId}, ItemId={ItemId}", userId, tripId, item.ItemId);
            return MapPackingItemResponse(item);
        }

        public async Task<PackingItemResponseDTO> UpdateItemAsync(
            string userId,
            int tripId,
            int itemId,
            PackingItemUpdateRequestDTO request)
        {
            var trip = await GetOwnedTripAsync(userId, tripId);
            var list = await GetActivePackingListAsync(trip);
            var item = await GetActiveItemAsync(list, itemId);

            if (request.ItemName != null)
            {
                item.ItemName = NormalizeName(request.ItemName, "項目名稱");
            }
            if (request.IsCheck.HasValue)
            {
                item.IsCheck = request.IsCheck.Value;
            }
            if (request.ItemName == null && !request.IsCheck.HasValue)
            {
                throw new AppBadRequestException("請提供要更新的行李項目資料");
            }
            item.UpdateAt = DateTime.Now;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Packing item updated. UserId={UserId}, TripId={TripId}, ItemId={ItemId}", userId, tripId, item.ItemId);
            return MapPackingItemResponse(item);
        }

        public async Task<bool> DeleteItemAsync(string userId, int tripId, int itemId)
        {
            var trip = await GetOwnedTripAsync(userId, tripId);
            var list = await GetActivePackingListAsync(trip);
            var item = await GetActiveItemAsync(list, itemId);

            item.IsDeleted = true;
            item.UpdateAt = DateTime.Now;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Packing item deleted. UserId={UserId}, TripId={TripId}, ItemId={ItemId}", userId, tripId, item.ItemId);
            return true;
        }
    }
}
