using Asp.Versioning;
using Luiu.Domain.DTOs;
using Luiu.Service.DTOs.V1.Client;
using Luiu.Service.Implementations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers.V1.Client
{
    [ApiVersion("1.0")]
    //[Authorize]
    [Route("api/v{version:apiVersion}/places")]
    public class PlacesController : BaseController<PlacesController>
    {
        private readonly GooglePlacesService _placesService;

        public PlacesController(ILogger<PlacesController> logger, GooglePlacesService placesService)
            : base(logger)
        {
            _placesService = placesService;
        }

        // GET: api/v1/places/search?query=台南美食
        /// <summary>
        /// 文字搜尋地點（回傳清單，不存DB）
        /// </summary>
        [HttpGet("search")]
        public async Task<ActionResult<ResultDTO<List<PlaceResultDTO>>>> Search([FromQuery] string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return BadRequestFail("請輸入搜尋關鍵字");

            query = query.Trim();
            if (query.Length < 2 || query.Length > 30)
                return BadRequestFail("搜尋關鍵字長度需介於 2 到 30 字");

            _logger.LogInformation("Place search requested. HasQuery={HasQuery}", !string.IsNullOrWhiteSpace(query));
            var results = await _placesService.TextSearchAsync(query);
            _logger.LogInformation("Place search completed. ResultCount={ResultCount}", results.Count);

            if (results.Count == 0)
                return Success(new List<PlaceResultDTO>(), "查無結果");

            return Success(results, "搜尋成功");
        }

        // GET: api/v1/google-maps/places/search?query=台南美食
        /// <summary>
        /// Google Maps 文字搜尋地點（回傳清單，不存DB）
        /// </summary>
        [HttpGet("/api/v{version:apiVersion}/google-maps/places/search")]
        public async Task<ActionResult<ResultDTO<List<PlaceResultDTO>>>> GoogleMapSearch([FromQuery] string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return BadRequestFail("請輸入搜尋關鍵字");

            query = query.Trim();
            if (query.Length < 2 || query.Length > 30)
                return BadRequestFail("搜尋關鍵字長度需介於 2 到 30 字");

            _logger.LogInformation("Google map place search requested. HasQuery={HasQuery}", !string.IsNullOrWhiteSpace(query));
            var results = await _placesService.TextSearchAsync(query);
            _logger.LogInformation("Google map place search completed. ResultCount={ResultCount}", results.Count);

            if (results.Count == 0)
                return Success(new List<PlaceResultDTO>(), "查無結果");

            return Success(results, "搜尋成功");
        }

        // GET: api/v1/google-maps/places/autocomplete?input=台南&sessionToken=...
        /// <summary>
        /// Google Maps Autocomplete 候選建議（回傳候選，不存DB）
        /// </summary>
        [HttpGet("/api/v{version:apiVersion}/google-maps/places/autocomplete")]
        public async Task<ActionResult<ResultDTO<List<GoogleMapPlaceAutocompleteDTO>>>> GoogleMapAutocomplete(
            [FromQuery] string input,
            [FromQuery] string? sessionToken)
        {
            if (string.IsNullOrWhiteSpace(input))
                return BadRequestFail("請輸入搜尋關鍵字");

            input = input.Trim();
            if (input.Length < 1 || input.Length > 80)
                return BadRequestFail("搜尋關鍵字長度需介於 1 到 80 字");

            if (!string.IsNullOrWhiteSpace(sessionToken) && sessionToken.Length > 36)
                return BadRequestFail("Session token 長度不可超過 36 字");

            _logger.LogInformation("Google map place autocomplete requested. HasInput={HasInput}", !string.IsNullOrWhiteSpace(input));
            var results = await _placesService.AutocompleteAsync(input, sessionToken);
            _logger.LogInformation("Google map place autocomplete completed. ResultCount={ResultCount}", results.Count);

            if (results.Count == 0)
                return Success(new List<GoogleMapPlaceAutocompleteDTO>(), "查無結果");

            return Success(results, "取得成功");
        }

        // GET: api/v1/places/{googleMapId}
        /// <summary>
        /// 取得地點詳情（先查DB快取，沒有才打Google API並存DB）
        /// </summary>
        [HttpGet("{googleMapId}")]
        public async Task<ActionResult<ResultDTO<PlaceResultDTO>>> GetDetails(string googleMapId)
        {
            if (string.IsNullOrWhiteSpace(googleMapId))
                return BadRequestFail("地點ID不可為空");

            _logger.LogInformation("Place detail requested. GoogleMapId={GoogleMapId}", googleMapId);
            var result = await _placesService.GetPlaceDetailsAsync(googleMapId);

            if (result == null)
                return NotFoundFail("找不到該地點");

            _logger.LogInformation("Place detail completed. GoogleMapId={GoogleMapId}, SpotId={SpotId}", googleMapId, result.SpotID);
            return Success(result, "取得成功");
        }

        // GET: api/v1/google-maps/places/{placeId}
        /// <summary>
        /// 取得前端地圖用地點詳情。
        /// </summary>
        [HttpGet("/api/v{version:apiVersion}/google-maps/places/{placeId}")]
        public async Task<ActionResult<ResultDTO<GoogleMapPlaceDetailDTO>>> GetGoogleMapPlace(string placeId)
        {
            if (string.IsNullOrWhiteSpace(placeId))
                return BadRequestFail("地點ID不可為空");

            _logger.LogInformation("Google map place detail requested. PlaceId={PlaceId}", placeId);
            var result = await _placesService.GetPlaceDetailsAsync(placeId);

            if (result == null)
                return NotFoundFail("找不到該地點");

            var response = new GoogleMapPlaceDetailDTO
            {
                SpotId = result.SpotID,
                PlaceId = result.GoogleMapID,
                Name = result.SpotName,
                Address = result.Address,
                Lat = result.Latitude,
                Lng = result.Longitude,
                Phone = result.Tel,
                OpeningHoursJson = result.OpeningHoursJson,
                Rating = result.Rating,
                UserRatingCount = result.UserRatingCount,
                GoogleMapUrl = result.GoogleMapURL,
                PhotoUrl = result.PhotoUrl,
                PhotoReference = result.PhotoReference,
                PriceLevel = result.PriceLevel
            };

            return Success(response, "取得成功");
        }
    }
}
