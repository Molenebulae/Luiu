using AutoMapper;
using Luiu.Domain.Exceptions;
using Luiu.Domain.Models;
using Luiu.Service.DTOs.V1.Client;
using Luiu.Service.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Luiu.Service.Implementations
{
    public class GoogleRoutesService : BaseService<GoogleRoutesService>
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly LuiuDbContext _db;
        private readonly DemoSessionService _demoSessionService;

        private const string RoutesUrl = "https://routes.googleapis.com/directions/v2:computeRoutes";
        private static readonly TimeSpan RouteCacheSlidingExpiration = TimeSpan.FromDays(7);
        private static readonly TimeSpan RouteCacheMaxAge = TimeSpan.FromDays(30);

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        private sealed record RouteStopPoint(
            int? DetailId,
            int? SpotId,
            string? GoogleMapId,
            string Name,
            decimal Latitude,
            decimal Longitude,
            byte SortOrder);

        public GoogleRoutesService(
            HttpClient httpClient,
            IOptions<GoogleMapsOptions> options,
            LuiuDbContext db,
            ILogger<GoogleRoutesService> logger,
            IMapper mapper,
            DemoSessionService demoSessionService) : base(db, logger, mapper)
        {
            _httpClient = httpClient;
            _apiKey = options.Value.ApiKey;
            _db = db;
            _demoSessionService = demoSessionService;
        }

        // ── 主要入口：計算多點路線 ─────────────────────────────────
        public async Task<RouteResultDTO> ComputeRouteAsync(RouteRequestDTO request)
        {
            await _demoSessionService.IncrementQuotaAsync(DemoQuotaType.RouteCompute);

            request.TravelMode = NormalizeTravelModes(request.TravelMode);

            if (request.DayNumber < 1)
                throw new AppBadRequestException("天數必須大於 0");

            var member = await _db.TMembers
                .FirstOrDefaultAsync(m => m.UserId == request.UserId && !m.IsDelete);

            if (member == null)
                throw new AppBadRequestException("找不到指定行程，或此行程不屬於該使用者");

            var demoSession = await _demoSessionService.GetCurrentSessionAsync(throwIfInvalid: true);
            var tripQuery = _db.TTrips
                .Where(t => t.TripId == request.TripId
                         && t.OwnerId == member.MemberId
                         && !t.IsDeleted);

            tripQuery = demoSession == null
                ? tripQuery.Where(t => t.DemoSessionId == null)
                : tripQuery.Where(t => t.DemoSessionId == demoSession.DemoSessionId);

            var tripExists = await tripQuery.AnyAsync();

            if (!tripExists)
                throw new AppBadRequestException("找不到指定行程，或此行程不屬於該使用者");

            var stops = await ResolveRouteStopsAsync(request);

            if (stops.Count < 2)
                throw new AppBadRequestException("至少需要兩個地點才能計算路線");

            if (stops.Count > 12)
                throw new AppBadRequestException("單天地點數上限為 12 個");

            var expectedLegCount = stops.Count - 1;
            if (request.TravelMode.Count != expectedLegCount)
                throw new AppBadRequestException($"交通模式數量必須為 {expectedLegCount} 筆");

            // 1. 逐段計算路線，讓每個相鄰地點可使用不同交通方式。
            var legs = new List<RouteLegDTO>();
            for (var i = 0; i < expectedLegCount; i++)
            {
                var fromStop = stops[i];
                var toStop = stops[i + 1];
                var travelMode = request.TravelMode[i];
                var cacheKey = GenerateCacheKey(fromStop, toStop, travelMode);

                var leg = await GetCachedRouteLegAsync(cacheKey);
                if (leg == null || leg.Steps == null || leg.Steps.Count == 0)
                {
                    _logger.LogInformation(
                        "Route cache miss. CacheKey={CacheKey}, FromSpotId={FromSpotId}, ToSpotId={ToSpotId}, TravelMode={TravelMode}",
                        cacheKey,
                        fromStop.SpotId,
                        toStop.SpotId,
                        travelMode);

                    await _demoSessionService.IncrementQuotaAsync(DemoQuotaType.RouteExternalLeg);
                    leg = await CallRouteLegApiAsync(fromStop, toStop, travelMode);
                    await SaveRouteCacheAsync(cacheKey, fromStop, toStop, travelMode, leg);
                }
                else
                {
                    var effectiveTravelMode = string.IsNullOrWhiteSpace(leg.TravelMode)
                        ? travelMode
                        : leg.TravelMode;
                    ApplyLegStopMetadata(leg, fromStop, toStop, effectiveTravelMode);
                    _logger.LogInformation(
                        "Route cache hit. CacheKey={CacheKey}, FromSpotId={FromSpotId}, ToSpotId={ToSpotId}, TravelMode={TravelMode}",
                        cacheKey,
                        fromStop.SpotId,
                        toStop.SpotId,
                        travelMode);
                }

                legs.Add(leg);
            }

            var result = new RouteResultDTO
            {
                TotalDistanceMeters = legs.Sum(l => l.DistanceMeters),
                TotalDurationSeconds = legs.Sum(l => l.DurationSeconds),
                OverviewPolyline = string.Empty,
                Legs = legs
            };

            // 2. 把每段 TransportTime 存回 tTripDetails
            await UpdateTransportTimeAsync(request, stops, result.Legs);

            return result;
        }

        // ── 從使用者擁有的行程明細產生路線序列，避免任意 SpotIds 污染明細 ─────────────
        private async Task<List<RouteStopPoint>> ResolveRouteStopsAsync(RouteRequestDTO request)
        {
            if (request.Stops != null)
            {
                return NormalizeRequestStops(request.Stops);
            }

            return await GetTripDayRouteStopsAsync(request.TripId, request.DayNumber);
        }

        private static List<RouteStopPoint> NormalizeRequestStops(List<RouteStopDTO> stops)
        {
            var normalized = stops
                .OrderBy(s => s.SortOrder)
                .Select(s =>
                {
                    if (s.SortOrder < 1)
                        throw new AppBadRequestException("路線停靠點排序必須大於 0");

                    if (string.IsNullOrWhiteSpace(s.Name))
                        throw new AppBadRequestException("路線停靠點名稱不可為空");

                    if (!s.Latitude.HasValue || !s.Longitude.HasValue)
                        throw new AppValidationException("部分地點缺少經緯度，無法計算路線");

                    var latitude = s.Latitude.Value;
                    var longitude = s.Longitude.Value;
                    if (latitude < -90 || latitude > 90 || longitude < -180 || longitude > 180)
                        throw new AppBadRequestException("路線停靠點經緯度格式錯誤");

                    return new RouteStopPoint(
                        s.DetailId,
                        s.SpotId,
                        s.GoogleMapId?.Trim(),
                        s.Name.Trim(),
                        latitude,
                        longitude,
                        s.SortOrder);
                })
                .ToList();

            return normalized;
        }

        private async Task<List<RouteStopPoint>> GetTripDayRouteStopsAsync(int tripId, byte dayNumber)
        {
            var details = await _db.TTripDetails
                .Where(d => d.TripId == tripId
                         && d.DayNumber == dayNumber
                         && !d.IsDeleted)
                .OrderBy(d => d.SortOrder)
                .ThenBy(d => d.DetailId)
                .ToListAsync();

            var spotIds = details
                .Select(d => d.SpotId)
                .Distinct()
                .ToList();

            var spots = await _db.TSpots
                .Where(s => spotIds.Contains(s.SpotId))
                .ToDictionaryAsync(s => s.SpotId);

            var result = new List<RouteStopPoint>();
            foreach (var detail in details)
            {
                if (!spots.TryGetValue(detail.SpotId, out var spot))
                    throw new AppBadRequestException("部分地點在DB中找不到，請確認SpotID是否正確");

                if (spot.Latitude == null || spot.Longitude == null)
                    throw new AppValidationException("部分地點缺少經緯度，無法計算路線");

                result.Add(new RouteStopPoint(
                    detail.DetailId,
                    spot.SpotId,
                    spot.GoogleMapId,
                    spot.SpotName,
                    spot.Latitude.Value,
                    spot.Longitude.Value,
                    detail.SortOrder));
            }

            return result;
        }

        // ── 呼叫 Google Routes API ─────────────────────────────────
        private async Task<RouteLegDTO> CallRouteLegApiAsync(RouteStopPoint origin, RouteStopPoint destination, string travelMode)
        {
            var body = new
            {
                origin = new
                {
                    location = new
                    {
                        latLng = new
                        {
                            latitude = origin.Latitude,
                            longitude = origin.Longitude
                        }
                    }
                },
                destination = new
                {
                    location = new
                    {
                        latLng = new
                        {
                            latitude = destination.Latitude,
                            longitude = destination.Longitude
                        }
                    }
                },
                travelMode = travelMode,
                computeAlternativeRoutes = false,
                languageCode = "zh-TW"
            };

            _logger.LogInformation(
                "Google Routes API request started. FromSpotId={FromSpotId}, ToSpotId={ToSpotId}, TravelMode={TravelMode}",
                origin.SpotId,
                destination.SpotId,
                travelMode);

            var googleResponse = await SendGoogleAsync<GoogleRouteResponse>(
                HttpMethod.Post,
                RoutesUrl,
                body,
                "routes.legs.distanceMeters," +
                "routes.legs.duration," +
                "routes.legs.polyline.encodedPolyline," +
                "routes.legs.steps.distanceMeters," +
                "routes.legs.steps.staticDuration," +
                "routes.legs.steps.travelMode," +
                "routes.legs.steps.polyline.encodedPolyline," +
                "routes.legs.steps.navigationInstruction.instructions," +
                "routes.legs.steps.transitDetails.stopDetails.departureStop.name," +
                "routes.legs.steps.transitDetails.stopDetails.arrivalStop.name," +
                "routes.legs.steps.transitDetails.transitLine.name," +
                "routes.legs.steps.transitDetails.transitLine.nameShort," +
                "routes.legs.steps.transitDetails.transitLine.vehicle.type",
                "RoutesCompute");

            var route = googleResponse?.Routes?.FirstOrDefault();
            if (route == null)
            {
                if (travelMode == "TRANSIT")
                {
                    _logger.LogInformation(
                        "Google Routes API returned no transit route. Falling back to walk. FromSpotId={FromSpotId}, ToSpotId={ToSpotId}",
                        origin.SpotId,
                        destination.SpotId);

                    await _demoSessionService.IncrementQuotaAsync(DemoQuotaType.RouteExternalLeg);
                    return await CallRouteLegApiAsync(origin, destination, "WALK");
                }

                _logger.LogWarning(
                    "Google Routes API returned no route. FromSpotId={FromSpotId}, ToSpotId={ToSpotId}, TravelMode={TravelMode}",
                    origin.SpotId,
                    destination.SpotId,
                    travelMode);
                throw new AppExternalServiceException("Google 路線服務未回傳路線資料");
            }

            if (route.Legs.Count != 1)
            {
                _logger.LogWarning(
                    "Google Routes API leg count mismatch. FromSpotId={FromSpotId}, ToSpotId={ToSpotId}, ExpectedLegCount={ExpectedLegCount}, ActualLegCount={ActualLegCount}",
                    origin.SpotId,
                    destination.SpotId,
                    1,
                    route.Legs.Count);
                throw new AppExternalServiceException("Google 路線資料格式不完整");
            }

            _logger.LogInformation(
                "Google Routes API request completed. FromSpotId={FromSpotId}, ToSpotId={ToSpotId}, TravelMode={TravelMode}",
                origin.SpotId,
                destination.SpotId,
                travelMode);
            return MapToLegDto(route.Legs.First(), origin, destination, travelMode);
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
                throw new AppExternalServiceException("Google 路線服務暫時無法使用");
            }

            try
            {
                return JsonSerializer.Deserialize<T>(json, JsonOptions);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Google API response deserialize failed. Operation={Operation}", operationName);
                throw new AppExternalServiceException("Google 路線資料格式錯誤");
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

        // ── Google 回傳 → RouteLegDto ─────────────────────────────
        private RouteLegDTO MapToLegDto(GoogleRouteLeg leg, RouteStopPoint origin, RouteStopPoint destination, string travelMode)
        {
            var result = new RouteLegDTO
            {
                DistanceMeters = leg.DistanceMeters,
                DurationSeconds = ParseDuration(leg.Duration),
                Polyline = leg.Polyline?.EncodedPolyline ?? string.Empty,
                Steps = (leg.Steps ?? new List<GoogleRouteLegStep>())
                    .Select(MapToStepDto)
                    .ToList()
            };

            ApplyLegStopMetadata(result, origin, destination, travelMode);
            return result;
        }

        private static RouteStepDTO MapToStepDto(GoogleRouteLegStep step)
        {
            var transitLine = step.TransitDetails?.TransitLine;
            var stopDetails = step.TransitDetails?.StopDetails;
            var transitMode = NormalizeTransitMode(transitLine?.Vehicle?.Type);
            var type = NormalizeStepTravelMode(step.TravelMode);

            return new RouteStepDTO
            {
                Type = type,
                TransitMode = type == "TRANSIT" ? transitMode : null,
                LineName = string.IsNullOrWhiteSpace(transitLine?.NameShort)
                    ? transitLine?.Name
                    : transitLine?.NameShort,
                DepartureStop = stopDetails?.DepartureStop?.Name,
                ArrivalStop = stopDetails?.ArrivalStop?.Name,
                Instruction = step.NavigationInstruction?.Instructions ?? string.Empty,
                DurationSeconds = ParseDuration(step.StaticDuration),
                DistanceMeters = step.DistanceMeters
            };
        }

        private static int ParseDuration(string? duration)
        {
            return int.TryParse(duration?.TrimEnd('s'), out var sec) ? sec : 0;
        }

        private static string NormalizeStepTravelMode(string? travelMode)
        {
            var normalized = string.IsNullOrWhiteSpace(travelMode)
                ? string.Empty
                : travelMode.Trim().ToUpperInvariant();

            return normalized switch
            {
                "DRIVE" or "WALK" or "BICYCLE" or "TRANSIT" or "TWO_WHEELER" => normalized,
                _ => normalized
            };
        }

        private static string? NormalizeTransitMode(string? vehicleType)
        {
            var normalized = string.IsNullOrWhiteSpace(vehicleType)
                ? string.Empty
                : vehicleType.Trim().ToUpperInvariant();

            return normalized switch
            {
                "SUBWAY" => "SUBWAY",
                "BUS" => "BUS",
                "FERRY" => "FERRY",
                "RAIL" => "RAIL",
                "HEAVY_RAIL" or "LONG_DISTANCE_TRAIN" or "HIGH_SPEED_TRAIN" => "TRAIN",
                "TRAM" or "LIGHT_RAIL" => "TRAM",
                _ => normalized
            };
        }

        private static void ApplyLegStopMetadata(RouteLegDTO leg, RouteStopPoint origin, RouteStopPoint destination, string travelMode)
        {
            leg.FromSpotId = origin.SpotId;
            leg.ToSpotId = destination.SpotId;
            leg.FromDetailId = origin.DetailId;
            leg.ToDetailId = destination.DetailId;
            leg.FromGoogleMapId = origin.GoogleMapId;
            leg.ToGoogleMapId = destination.GoogleMapId;
            leg.FromName = origin.Name;
            leg.ToName = destination.Name;
            leg.TravelMode = travelMode;
        }

        private async Task<RouteLegDTO?> GetCachedRouteLegAsync(string cacheKey)
        {
            var now = DateTime.UtcNow;
            var cached = await _db.TRouteCaches
                .FirstOrDefaultAsync(r => r.CacheKey == cacheKey);

            if (cached == null || cached.CreatedAt <= now.Subtract(RouteCacheMaxAge))
            {
                return null;
            }

            var cachedLeg = JsonSerializer.Deserialize<RouteLegDTO>(
                cached.ResultJson, JsonOptions);

            if (cachedLeg == null)
            {
                _logger.LogWarning("Route cache deserialize failed. CacheKey={CacheKey}", cacheKey);
                throw new AppExternalServiceException("路線快取資料格式錯誤");
            }

            cached.ExpiredAt = now.Add(RouteCacheSlidingExpiration);
            await _db.SaveChangesAsync();

            return cachedLeg;
        }

        // ── 存入 tRouteCache ───────────────────────────────────────
        private async Task SaveRouteCacheAsync(
            string cacheKey,
            RouteStopPoint origin,
            RouteStopPoint destination,
            string travelMode,
            RouteLegDTO leg)
        {
            // 防止 concurrent request 重複寫入
            var now = DateTime.UtcNow;
            var existing = await _db.TRouteCaches.FirstOrDefaultAsync(r => r.CacheKey == cacheKey);
            if (existing != null)
            {
                existing.SpotIdsJson = JsonSerializer.Serialize(new[] { ResolveStopKey(origin), ResolveStopKey(destination) });
                existing.TravelMode = travelMode;
                existing.ResultJson = JsonSerializer.Serialize(leg, JsonOptions);
                existing.CreatedAt = now;
                existing.ExpiredAt = now.Add(RouteCacheSlidingExpiration);
                await _db.SaveChangesAsync();
                _logger.LogInformation("Route cache refreshed. CacheKey={CacheKey}", cacheKey);
                return;
            }

            _db.TRouteCaches.Add(new TRouteCache
            {
                CacheKey = cacheKey,
                SpotIdsJson = JsonSerializer.Serialize(new[] { ResolveStopKey(origin), ResolveStopKey(destination) }),
                TravelMode = travelMode,
                ResultJson = JsonSerializer.Serialize(leg, JsonOptions),
                CreatedAt = now,
                ExpiredAt = now.Add(RouteCacheSlidingExpiration)
            });

            await _db.SaveChangesAsync();
            _logger.LogInformation(
                "Route cache saved. CacheKey={CacheKey}, FromSpotId={FromSpotId}, ToSpotId={ToSpotId}, TravelMode={TravelMode}",
                cacheKey,
                origin.SpotId,
                destination.SpotId,
                travelMode);
        }

        // ── 把每段交通時間存回 tTripDetails ───────────────────────
        private async Task UpdateTransportTimeAsync(RouteRequestDTO request, List<RouteStopPoint> stops, List<RouteLegDTO> legs)
        {
            var sourceDetailIds = stops
                .Take(Math.Max(stops.Count - 1, 0))
                .Where(s => s.DetailId.HasValue)
                .Select(s => s.DetailId!.Value)
                .Distinct()
                .ToList();

            if (sourceDetailIds.Count == 0)
            {
                _logger.LogInformation(
                    "Trip detail transport time skipped. TripId={TripId}, DayNumber={DayNumber}, LegCount={LegCount}",
                    request.TripId,
                    request.DayNumber,
                    legs.Count);
                return;
            }

            var details = await _db.TTripDetails
                .Where(d => d.TripId == request.TripId
                         && d.DayNumber == request.DayNumber
                         && sourceDetailIds.Contains(d.DetailId)
                         && !d.IsDeleted)
                .ToDictionaryAsync(d => d.DetailId);

            for (int i = 0; i < legs.Count && i < stops.Count - 1; i++)
            {
                var sourceDetailId = stops[i].DetailId;
                if (!sourceDetailId.HasValue || !details.TryGetValue(sourceDetailId.Value, out var detail))
                {
                    continue;
                }

                detail.TransportTime = (short)legs[i].DurationMinutes;
                detail.PolylineEncoded = legs[i].Polyline; //更新 Polyline
                detail.UpdateAt = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync();
            _logger.LogInformation(
                "Trip detail transport time updated. TripId={TripId}, DayNumber={DayNumber}, LegCount={LegCount}, DetailCount={DetailCount}",
                request.TripId,
                request.DayNumber,
                legs.Count,
                details.Count);
        }

        // ── 產生快取 Key ───────────────────────────────────────────
        private string GenerateCacheKey(RouteStopPoint origin, RouteStopPoint destination, string travelMode)
        {
            var raw = $"{ResolveStopKey(origin)}_{ResolveStopKey(destination)}_{travelMode}";
            var hash = MD5.HashData(Encoding.UTF8.GetBytes(raw));
            return Convert.ToHexString(hash).ToLower();  // 32 字元，符合 nvarchar(50)
        }

        private static string ResolveStopKey(RouteStopPoint stop)
        {
            if (stop.SpotId.HasValue)
                return $"spot:{stop.SpotId.Value}";

            if (!string.IsNullOrWhiteSpace(stop.GoogleMapId))
                return $"google:{stop.GoogleMapId.Trim().ToLowerInvariant()}";

            return string.Format(CultureInfo.InvariantCulture, "coord:{0:0.######},{1:0.######}", stop.Latitude, stop.Longitude);
        }

        private static List<string> NormalizeTravelModes(List<string>? travelModes)
        {
            if (travelModes == null || travelModes.Count == 0)
                throw new AppBadRequestException("請提供每段交通模式");

            var supportedModes = new HashSet<string>
            {
                "DRIVE",
                "WALK",
                "BICYCLE",
                "TRANSIT",
                "TWO_WHEELER"
            };

            var normalized = travelModes
                .Select(mode => string.IsNullOrWhiteSpace(mode)
                    ? string.Empty
                    : mode.Trim().ToUpperInvariant())
                .ToList();

            if (normalized.Any(mode => !supportedModes.Contains(mode)))
                throw new AppBadRequestException("交通模式只支援 DRIVE、WALK、BICYCLE、TRANSIT、TWO_WHEELER");

            return normalized;
        }
    }
}
