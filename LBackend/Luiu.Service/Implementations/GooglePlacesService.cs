using AutoMapper;
using Luiu.Domain.Exceptions;
using Luiu.Domain.Models;
using Luiu.Service.DTOs.V1.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Luiu.Service.Implementations
{
    public class GooglePlacesService : BaseService<GooglePlacesService>
    {
        private readonly HttpClient _httpClient;
        private readonly IMemoryCache _cache;
        private readonly string _apiKey;
        private const string BaseUrl = "https://places.googleapis.com/v1/places";
        private static readonly TimeSpan TextSearchCacheDuration = TimeSpan.FromMinutes(10);

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        // RegionID 對照表（對應 tRegions 實際資料）
        private static readonly Dictionary<string, int> RegionMap = new()
        {
            { "高雄", 1 },
            { "屏東", 4 },
            { "台南", 5 },
            { "臺南", 5 },   // 繁體兩種寫法都對應
            { "嘉義", 6 },
            { "雲林", 7 }
        };

        public GooglePlacesService(
            HttpClient httpClient,
            IOptions<GoogleMapsOptions> options,
            IMemoryCache cache,
            LuiuDbContext context,
            ILogger<GooglePlacesService> logger,
            IMapper mapper) : base(context, logger, mapper)
        {
            _httpClient = httpClient;
            _cache = cache;
            _apiKey = options.Value.ApiKey;
        }

        // ── 文字搜尋（回傳清單，不存DB，讓使用者選） ──────────────
        public async Task<List<PlaceResultDTO>> TextSearchAsync(string query)
        {
            query = query.Trim();
            var cacheKey = $"GooglePlaces:TextSearch:{query.ToUpperInvariant()}";
            if (_cache.TryGetValue(cacheKey, out List<PlaceResultDTO>? cachedResults) && cachedResults != null)
            {
                _logger.LogInformation("Google Places text search memory cache hit. ResultCount={ResultCount}", cachedResults.Count);
                return cachedResults;
            }

            var requestBody = new
            {
                textQuery = query,
                languageCode = "zh-TW",
                regionCode = "TW"
            };

            _logger.LogInformation("Google Places text search started. HasQuery={HasQuery}", !string.IsNullOrWhiteSpace(query));

            var result = await SendGoogleAsync<GoogleTextSearchResponseDTO>(
                HttpMethod.Post,
                $"{BaseUrl}:searchText",
                requestBody,
                "places.id,places.displayName,places.formattedAddress," +
                "places.location,places.rating,places.userRatingCount,places.nationalPhoneNumber," +
                "places.regularOpeningHours,places.googleMapsUri,places.photos,places.priceLevel",
                "PlacesTextSearch");

            var places = result?.Places ?? new List<GooglePlaceResultDTO>();
            var mapped = places
                .Where(IsValidPlace)
                .Select(MapToDto)
                .ToList();

            if (mapped.Count == 0)
            {
                _logger.LogInformation("Google Places text search returned no usable results.");
            }
            else
            {
                _logger.LogInformation("Google Places text search completed. ResultCount={ResultCount}", mapped.Count);
            }

            _cache.Set(cacheKey, mapped, TextSearchCacheDuration);
            return mapped;
        }

        // ── Autocomplete 建議（回傳候選，不存DB） ──────────────
        public async Task<List<GoogleMapPlaceAutocompleteDTO>> AutocompleteAsync(string input, string? sessionToken)
        {
            input = input.Trim();
            var requestBody = new Dictionary<string, object>
            {
                ["input"] = input,
                ["languageCode"] = "zh-TW",
                ["regionCode"] = "TW",
                ["includedRegionCodes"] = new[] { "TW" }
            };

            if (!string.IsNullOrWhiteSpace(sessionToken))
            {
                requestBody["sessionToken"] = sessionToken.Trim();
            }

            _logger.LogInformation("Google Places autocomplete started. HasInput={HasInput}", !string.IsNullOrWhiteSpace(input));

            var result = await SendGoogleAsync<GoogleAutocompleteResponseDTO>(
                HttpMethod.Post,
                $"{BaseUrl}:autocomplete",
                requestBody,
                "suggestions.placePrediction.placeId," +
                "suggestions.placePrediction.text.text," +
                "suggestions.placePrediction.structuredFormat.mainText.text," +
                "suggestions.placePrediction.structuredFormat.secondaryText.text",
                "PlacesAutocomplete");

            var suggestions = result?.Suggestions ?? new List<GoogleAutocompleteSuggestionDTO>();
            var mapped = suggestions
                .Select(s => s.PlacePrediction)
                .Where(p => !string.IsNullOrWhiteSpace(p?.PlaceId))
                .Select(p => MapAutocompleteToDto(p!))
                .ToList();

            _logger.LogInformation("Google Places autocomplete completed. ResultCount={ResultCount}", mapped.Count);
            return mapped;
        }

        // ── 取得地點詳情（先查DB，沒有才打API並存DB）──────────────
        public async Task<PlaceResultDTO?> GetPlaceDetailsAsync(string googleMapId)
        {
            // 1. 查DB快取
            var cached = await _context.TSpots
                .FirstOrDefaultAsync(s => s.GoogleMapId == googleMapId);

            if (cached != null)
            {
                _logger.LogInformation("Google Place detail cache hit. GoogleMapId={GoogleMapId}, SpotId={SpotId}", googleMapId, cached.SpotId);
                if (ShouldRefreshCachedSpot(cached))
                {
                    _logger.LogInformation(
                        "Google Place cached detail needs refresh. GoogleMapId={GoogleMapId}, SpotId={SpotId}, HasChineseAddress={HasChineseAddress}, HasPhoto={HasPhoto}, HasOpeningHours={HasOpeningHours}, HasPriceLevel={HasPriceLevel}, HasRating={HasRating}, HasUserRatingCount={HasUserRatingCount}",
                        googleMapId,
                        cached.SpotId,
                        HasChineseText(cached.Address),
                        HasUsablePhoto(cached),
                        !string.IsNullOrWhiteSpace(cached.OpeningHoursJson),
                        !string.IsNullOrWhiteSpace(cached.PriceLevel),
cached.Rating.HasValue,
cached.UserRatingCount.HasValue
                        );
                    var refreshedPlace = await FetchPlaceDetailsFromGoogleAsync(googleMapId);
                    await UpdateExistingSpot(cached, refreshedPlace);
                    _logger.LogInformation("Google Place cached detail refreshed. GoogleMapId={GoogleMapId}, SpotId={SpotId}", googleMapId, cached.SpotId);
                }

                return MapEntityToDto(cached);
            }

            _logger.LogInformation("Google Place detail cache miss. GoogleMapId={GoogleMapId}", googleMapId);

            // 2. 打 Google API
            var place = await FetchPlaceDetailsFromGoogleAsync(googleMapId);

            // 3. 存DB並回傳
            var saved = await SaveToDb(place);
            _logger.LogInformation("Google Place detail saved. GoogleMapId={GoogleMapId}, SpotId={SpotId}", googleMapId, saved.SpotId);
            return MapEntityToDto(saved);
        }

        private async Task<GooglePlaceResultDTO> FetchPlaceDetailsFromGoogleAsync(string googleMapId)
        {
            var place = await SendGoogleAsync<GooglePlaceResultDTO>(
                HttpMethod.Get,
                $"{BaseUrl}/{googleMapId}?languageCode=zh-TW",
                null,
                "id,displayName,formattedAddress,location,rating," +
                "userRatingCount,nationalPhoneNumber,regularOpeningHours,googleMapsUri,photos,priceLevel",
                "PlaceDetails");

            if (place == null)
            {
                throw new AppExternalServiceException("Google 地點服務暫時無法使用");
            }

            if (!IsValidPlace(place))
            {
                _logger.LogWarning(
                    "Google Place detail missing required fields. GoogleMapId={GoogleMapId}, HasId={HasId}, HasName={HasName}, HasLocation={HasLocation}",
                    googleMapId,
                    !string.IsNullOrWhiteSpace(place.Id),
                    !string.IsNullOrWhiteSpace(place.DisplayName?.Text),
                    place.Location != null);
                throw new AppExternalServiceException("Google 地點資料格式不完整");
            }

            return place;
        }

        private async Task<T?> SendGoogleAsync<T>(
            HttpMethod method,
            string url,
            object? body,
            string fieldMask,
            string operationName)
        {
            using var request = new HttpRequestMessage(method, url);
            request.Headers.Add("X-Goog-Api-Key", _apiKey);
            request.Headers.Add("X-Goog-FieldMask", fieldMask);

            if (body != null)
            {
                request.Content = new StringContent(
                    JsonSerializer.Serialize(body, JsonOptions),
                    Encoding.UTF8,
                    "application/json");
            }

            using var response = await _httpClient.SendAsync(request);
            var json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Google API failed. Operation={Operation}, StatusCode={StatusCode}, BodySummary={BodySummary}",
                    operationName,
                    (int)response.StatusCode,
                    Truncate(json, 500));
                throw new AppExternalServiceException("Google 地點服務暫時無法使用");
            }

            try
            {
                return JsonSerializer.Deserialize<T>(json, JsonOptions);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Google API response deserialize failed. Operation={Operation}", operationName);
                throw new AppExternalServiceException("Google 地點資料格式錯誤");
            }
        }

        private static string Truncate(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
            {
                return value;
            }

            return value[..maxLength];
        }

        private static bool IsValidPlace(GooglePlaceResultDTO place)
        {
            return !string.IsNullOrWhiteSpace(place.Id)
                && !string.IsNullOrWhiteSpace(place.DisplayName?.Text)
                && place.Location != null;
        }

        private static bool HasChineseText(string? value)
        {
            return !string.IsNullOrWhiteSpace(value)
                && value.Any(c => c >= '\u4e00' && c <= '\u9fff');
        }

        private static bool HasUsablePhoto(TSpot entity)
        {
            return !string.IsNullOrWhiteSpace(entity.PhotoReference)
                && !string.IsNullOrWhiteSpace(entity.PhotoUrl);
        }

        private static bool ShouldRefreshCachedSpot(TSpot entity)
        {
            return !HasChineseText(entity.Address)
                || !HasUsablePhoto(entity)
                || string.IsNullOrWhiteSpace(entity.OpeningHoursJson)
                || string.IsNullOrWhiteSpace(entity.PriceLevel)
                || !entity.Rating.HasValue
                || !entity.UserRatingCount.HasValue;
        }

        private async Task UpdateExistingSpot(TSpot entity, GooglePlaceResultDTO place)
        {
            entity.SpotName = place.DisplayName.Text;
            entity.Address = place.FormattedAddress;
            entity.Rating = place.Rating.HasValue ? (decimal)place.Rating.Value : null;
            entity.UserRatingCount = place.UserRatingCount;
            entity.Tel = place.NationalPhoneNumber;
            entity.OpeningHoursJson = ResolveOpeningHoursJson(place);
            entity.GoogleMapUrl = place.GoogleMapsUri;
            var photoReference = ResolvePhotoReference(place);
            if (!string.IsNullOrWhiteSpace(photoReference))
            {
                entity.PhotoReference = photoReference;
                entity.PhotoUrl = BuildPhotoUrl(photoReference);
            }
            entity.PriceLevel = place.PriceLevel;
            entity.RegionId = await ResolveRegionId(place.FormattedAddress);
            entity.LastUpdated = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }

        // ── 存入 tSpots ────────────────────────────────────────────
        private async Task<TSpot> SaveToDb(GooglePlaceResultDTO place)
        {
            // 防止 concurrent request 重複寫入
            var existing = await _context.TSpots
                .FirstOrDefaultAsync(s => s.GoogleMapId == place.Id);
            if (existing != null)
            {
                _logger.LogInformation("Google Place already exists before save. GoogleMapId={GoogleMapId}, SpotId={SpotId}", place.Id, existing.SpotId);
                return existing;
            }

            var entity = new TSpot
            {
                GoogleMapId = place.Id,
                SpotName = place.DisplayName.Text,
                Address = place.FormattedAddress,
                Latitude = (decimal)place.Location.Latitude,
                Longitude = (decimal)place.Location.Longitude,
                Rating = place.Rating.HasValue ? (decimal)place.Rating.Value : null,
                UserRatingCount = place.UserRatingCount,
                Tel = place.NationalPhoneNumber,
                OpeningHoursJson = ResolveOpeningHoursJson(place),
                GoogleMapUrl = place.GoogleMapsUri,
                PhotoReference = ResolvePhotoReference(place),
                PhotoUrl = BuildPhotoUrl(ResolvePhotoReference(place)),
                PriceLevel = place.PriceLevel,
                RegionId = await ResolveRegionId(place.FormattedAddress),
                LastUpdated = DateTime.UtcNow,
                ViewCount = 0,
                FavoriteCount = 0,
                PlanCount = 0,
                RecordCount = 0,
                RefCount = 0
            };

            _context.TSpots.Add(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        // ── 從地址解析 RegionID，找不到就自動新增 tRegions ──────────
        private async Task<int> ResolveRegionId(string address)
        {
            // 台灣所有縣市（涵蓋全台擴展情境）
            var allRegions = new List<string>
            {
                "基隆", "台北", "臺北", "新北", "桃園", "新竹", "苗栗",
                "台中", "臺中", "彰化", "南投", "雲林", "嘉義",
                "台南", "臺南", "高雄", "屏東", "宜蘭", "花蓮", "台東", "臺東",
                "澎湖", "金門", "連江"
            };

            // 找出地址包含哪個縣市
            var matched = allRegions.FirstOrDefault(r => address.Contains(r));
            if (matched == null)
            {
                _logger.LogWarning("Region resolve fallback used. RegionId={RegionId}", RegionMap["高雄"]);
                return RegionMap["高雄"]; // 完全解析不到時 fallback 高雄
            }

            // 統一繁體寫法（臺→台）
            var normalized = matched.Replace("臺", "台");

            // 查 DB 有沒有這個縣市
            var existing = await _context.TRegions
                .FirstOrDefaultAsync(r => r.RegionName.Contains(normalized)
                                       || r.RegionName.Contains(matched));

            if (existing != null)
            {
                _logger.LogInformation("Region resolved from database. RegionName={RegionName}, RegionId={RegionId}", existing.RegionName, existing.RegionId);
                return existing.RegionId;
            }

            // 沒有就新增
            var newRegion = new TRegion
            {
                RegionName = normalized + (normalized.Length == 2 ? "市" : "") // 例：高雄市
            };

            _context.TRegions.Add(newRegion);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Region created by Google Place address resolve. RegionName={RegionName}, RegionId={RegionId}", newRegion.RegionName, newRegion.RegionId);
            return newRegion.RegionId;
        }

        // ── Google API 結果 → DTO ──────────────────────────────────
        private PlaceResultDTO MapToDto(GooglePlaceResultDTO place) => new()
        {
            GoogleMapID = place.Id,
            SpotID = null,   // 搜尋清單時尚未存DB，故無SpotID
            RegionID = null,
            SpotName = place.DisplayName.Text,
            Address = place.FormattedAddress,
            Latitude = place.Location.Latitude,
            Longitude = place.Location.Longitude,
            Rating = place.Rating,
            UserRatingCount = place.UserRatingCount,
            Tel = place.NationalPhoneNumber,
            OpeningHoursJson = ResolveOpeningHoursJson(place),
            GoogleMapURL = place.GoogleMapsUri,
            PhotoReference = ResolvePhotoReference(place),
            PhotoUrl = BuildPhotoUrl(ResolvePhotoReference(place)),
            PriceLevel = place.PriceLevel
        };

        private static GoogleMapPlaceAutocompleteDTO MapAutocompleteToDto(GooglePlacePredictionDTO prediction)
        {
            var mainText = prediction.StructuredFormat?.MainText?.Text
                           ?? prediction.Text?.Text
                           ?? string.Empty;

            return new GoogleMapPlaceAutocompleteDTO
            {
                PlaceId = prediction.PlaceId,
                MainText = mainText,
                SecondaryText = prediction.StructuredFormat?.SecondaryText?.Text,
                Description = prediction.Text?.Text ?? mainText
            };
        }

        // ── DB Entity → DTO ────────────────────────────────────────
        private PlaceResultDTO MapEntityToDto(TSpot entity) => new()
        {
            GoogleMapID = entity.GoogleMapId ?? string.Empty,
            SpotID = entity.SpotId,
            RegionID = entity.RegionId,
            SpotName = entity.SpotName,
            Address = entity.Address ?? string.Empty,
            Latitude = (double)(entity.Latitude ?? 0),
            Longitude = (double)(entity.Longitude ?? 0),
            Rating = (double?)entity.Rating,
            UserRatingCount = entity.UserRatingCount,
            Tel = entity.Tel,
            OpeningHoursJson = NormalizeOpeningHoursJsonForResponse(entity.OpeningHoursJson),
            GoogleMapURL = entity.GoogleMapUrl,
            PhotoUrl = entity.PhotoUrl,
            PhotoReference = entity.PhotoReference,
            PriceLevel = entity.PriceLevel
        };

        private static string? ResolveOpeningHoursJson(GooglePlaceResultDTO place)
        {
            var weekdayDescriptions = place.RegularOpeningHours?.WeekdayDescriptions
                ?? place.CurrentOpeningHours?.WeekdayDescriptions;

            return FormatOpeningHoursJsonForResponse(weekdayDescriptions);
        }

        internal static string? FormatOpeningHoursJsonForResponse(IEnumerable<string>? weekdayDescriptions)
        {
            if (weekdayDescriptions == null)
            {
                return null;
            }

            var normalized = weekdayDescriptions
                .Where(description => !string.IsNullOrWhiteSpace(description))
                .Select(NormalizeOpeningHourDescription)
                .ToList();

            return normalized.Count == 0
                ? null
                : JsonSerializer.Serialize(normalized);
        }

        internal static string? NormalizeOpeningHoursJsonForResponse(string? openingHoursJson)
        {
            if (string.IsNullOrWhiteSpace(openingHoursJson))
            {
                return openingHoursJson;
            }

            try
            {
                var weekdayDescriptions = JsonSerializer.Deserialize<List<string>>(openingHoursJson, JsonOptions);
                return weekdayDescriptions == null
                    ? openingHoursJson
                    : FormatOpeningHoursJsonForResponse(weekdayDescriptions);
            }
            catch (JsonException)
            {
                return openingHoursJson;
            }
        }

        private static string NormalizeOpeningHourDescription(string description)
        {
            var trimmed = description.Trim();
            var match = Regex.Match(trimmed, @"^\s*(?<day>[^:：]+)\s*[:：]\s*(?<hours>.+?)\s*$");
            if (!match.Success)
            {
                return trimmed;
            }

            var weekday = NormalizeWeekdayName(match.Groups["day"].Value);
            if (weekday == null)
            {
                return trimmed;
            }

            var hours = NormalizeOpeningHourTimes(match.Groups["hours"].Value);
            return $"{weekday}: {hours}";
        }

        private static string? NormalizeWeekdayName(string value)
        {
            var normalized = value.Trim().ToLowerInvariant();
            return normalized switch
            {
                "monday" or "mon" or "星期一" or "週一" or "周一" => "星期一",
                "tuesday" or "tue" or "tues" or "星期二" or "週二" or "周二" => "星期二",
                "wednesday" or "wed" or "星期三" or "週三" or "周三" => "星期三",
                "thursday" or "thu" or "thur" or "thurs" or "星期四" or "週四" or "周四" => "星期四",
                "friday" or "fri" or "星期五" or "週五" or "周五" => "星期五",
                "saturday" or "sat" or "星期六" or "週六" or "周六" => "星期六",
                "sunday" or "sun" or "星期日" or "星期天" or "週日" or "週天" or "周日" or "周天" => "星期日",
                _ => null
            };
        }

        private static string NormalizeOpeningHourTimes(string value)
        {
            var normalized = value.Trim()
                .Replace("—", "–")
                .Replace("-", "–");

            normalized = Regex.Replace(normalized, @"\s*–\s*", "–");
            normalized = Regex.Replace(
                normalized,
                @"(?i)(?<prefix>上午|下午)?\s*(?<hour>\d{1,2})(?::(?<minute>\d{2}))?\s*(?<suffix>am|pm)?",
                NormalizeTimeMatch);

            return normalized;
        }

        private static string NormalizeTimeMatch(Match match)
        {
            var prefix = match.Groups["prefix"].Value;
            var suffix = match.Groups["suffix"].Value.ToLowerInvariant();
            if (string.IsNullOrEmpty(prefix) && string.IsNullOrEmpty(suffix) && !match.Groups["minute"].Success)
            {
                return match.Value;
            }

            var hour = int.Parse(match.Groups["hour"].Value);
            var minute = match.Groups["minute"].Success
                ? int.Parse(match.Groups["minute"].Value)
                : 0;

            if (prefix == "下午" || suffix == "pm")
            {
                if (hour < 12)
                {
                    hour += 12;
                }
            }
            else if ((prefix == "上午" || suffix == "am") && hour == 12)
            {
                hour = 0;
            }

            return $"{hour:00}:{minute:00}";
        }

        private static string? ResolvePhotoReference(GooglePlaceResultDTO place)
        {
            return place.Photos
                .FirstOrDefault(photo => !string.IsNullOrWhiteSpace(photo.Name))
                ?.Name;
        }

        private string? BuildPhotoUrl(string? photoReference)
        {
            return string.IsNullOrWhiteSpace(photoReference)
                ? null
                : $"https://places.googleapis.com/v1/{photoReference}/media?maxWidthPx=800&key={Uri.EscapeDataString(_apiKey)}";
        }
    }

    public class GoogleMapsOptions
    {
        public string ApiKey { get; set; } = string.Empty;
    }
}
