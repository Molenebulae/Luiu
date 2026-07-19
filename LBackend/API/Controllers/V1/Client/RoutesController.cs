using Asp.Versioning;
using Luiu.Domain.DTOs;
using Luiu.Service.DTOs.V1.Client;
using Luiu.Service.Implementations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers.V1.Client
{
    [ApiVersion("1.0")]
    [Authorize]
    [Route("api/v{version:apiVersion}/{userId}/plan-list/{tripId:int}/routes")]
    [Route("{userId}/plan-list/{tripId:int}/routes")]
    public class RoutesController : BaseController<RoutesController>
    {
        private readonly GoogleRoutesService _routesService;
        private readonly DemoSessionService _demoSessionService;

        public RoutesController(
            ILogger<RoutesController> logger,
            GoogleRoutesService routesService,
            DemoSessionService demoSessionService)
            : base(logger)
        {
            _routesService = routesService;
            _demoSessionService = demoSessionService;
        }

        // POST: api/v1/{userId}/plan-list/{tripId}/routes
        /// <summary>
        /// 計算指定行程日程的路線，並更新每段交通時間與 Polyline。
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<ResultDTO<RouteResultDTO>>> Compute(
            string userId,
            int tripId,
            [FromBody] RouteRequestDTO request)
        {
            _demoSessionService.EnsureRouteUserMatches(userId);

            if (request == null)
                return BadRequestFail("請提供路線計算資料");

            request.UserId = userId;
            request.TripId = tripId;

            _logger.LogInformation(
                "Route compute requested. UserId={UserId}, TripId={TripId}, DayNumber={DayNumber}, StopCount={StopCount}, TravelModeCount={TravelModeCount}, TravelModes={TravelModes}",
                userId,
                tripId,
                request.DayNumber,
                request.Stops?.Count ?? 0,
                request.TravelMode?.Count ?? 0,
                string.Join(",", request.TravelMode ?? new List<string>()));

            var result = await _routesService.ComputeRouteAsync(request);

            _logger.LogInformation(
                "Route compute completed. UserId={UserId}, TripId={TripId}, DayNumber={DayNumber}, LegCount={LegCount}",
                userId,
                tripId,
                request.DayNumber,
                result.Legs.Count);

            return Success(result, "路線規劃成功");
        }
    }
}
