using Asp.Versioning;
using AutoMapper;
using Azure.Storage.Blobs.Models;
using Luiu.Domain.DTOs;
using Luiu.Domain.Enums;
using Luiu.Service.Implementations;
using Luiu.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers.V1.Client
{
    [Authorize]
    [ApiVersion("1.0")]
    public class AdminController : BaseController<AdminController>
    {
        private readonly IAdminService _adminService;
        public AdminController(ILogger<AdminController> logger, IAdminService adminService, IConfiguration config, IMapper mapper) : base(logger)
        {
            _adminService = adminService;
        }

        [Authorize(Roles = nameof(AppEnums.RoleType.OfficialManager))]
        [HttpPost("{type}/{id}/recommendation")]
        public async Task<ActionResult<ResultDTO<object>>> UpdateRecommendation(string type, int id, [FromBody] bool isRecommended)
        {
            if (string.IsNullOrEmpty(type))
            {
                _logger.LogWarning("推薦請求失敗: 類型為空");
                return BadRequestFail("類型不能為空");
            }

            await _adminService.SetRecommendationAsync(type, id, isRecommended);
            _logger.LogInformation("推薦成功");
            return Success<object>("推薦成功");
        }
    }
}
