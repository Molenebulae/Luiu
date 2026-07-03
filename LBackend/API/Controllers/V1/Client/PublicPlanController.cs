using Asp.Versioning;
using Luiu.Domain.DTOs;
using Luiu.Service.DTOs.V1.Client;
using Luiu.Service.Implementations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers.V1.Client
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/plans")]
    public class PublicPlanController : BaseController<PublicPlanController>
    {
        private readonly PlanService _planService;
        public PublicPlanController(ILogger<PublicPlanController> logger, PlanService planService) : base(logger)
        {
            _planService = planService;
        }

        [HttpGet("recommended")]
        public async Task<ActionResult<ResultDTO<List<HomeRecommendPlanDTO>>>> GetHomeRecommended()
        {
            var data = await _planService.GetRecommendedPlansAsync();

            return Success(data, "成功取得推薦回憶");
        }
    }
}
