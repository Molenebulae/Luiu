using Luiu.Domain.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class BaseController<T> : Controller
    {
        protected readonly ILogger<T> _logger;

        protected BaseController(ILogger<T> logger)
        {
            _logger = logger;
        }

        [NonAction]
        protected ActionResult<ResultDTO<TData>> Success<TData>(TData data, string message = "成功")
        {
            return Ok(new ResultDTO<TData>{
                Success = true,
                Message = message,
                Data = data
            });
        }

        [NonAction]
        protected ActionResult<ResultDTO<TData>> CreatedSuccess<TData>(TData data, string message = "建立成功")
        {
            return StatusCode(201, new ResultDTO<TData>
            {
                Success = true,
                Message = message,
                Data = data
            });
        }

        private string GetDefaultMessage(int statusCode) => statusCode switch
        {
            400 => "請求內容有誤",
            401 => "身份驗證失敗",
            403 => "您沒有權限執行此操作",
            404 => "找不到指定資源",
            409 => "資料已存在或發生衝突",
            422 => "欄位驗證失敗",
            _ => "發生錯誤"
        };

        [NonAction]
        private ObjectResult Fail<TData>(TData? data, string? message, int statusCode)
        {
            return StatusCode(statusCode, new ResultDTO<TData>
            {
                Success = false,
                Message = message ?? GetDefaultMessage(statusCode),
                Data = data
            });
        }

        // 400
        /// <summary>回傳 400 BadRequest 錯誤</summary>
        /// <param name="message">錯誤訊息 (預設為: "請求內容有誤")</param>
        /// <remarks>預設訊息：請求內容有誤</remarks>
        [NonAction]
        protected ObjectResult BadRequestFail(string message = null)
            => Fail<object>(null, message, StatusCodes.Status400BadRequest);
        [NonAction]
        protected ObjectResult BadRequestFail<TData>(TData data, string message = null)
            => Fail(data, message, StatusCodes.Status400BadRequest);

        // 401
        /// <summary>回傳 401 Unauthorized 錯誤</summary>
        /// <param name="message">錯誤訊息 (預設為: "身份驗證失敗")</param>
        /// <remarks>預設訊息：身份驗證失敗</remarks>
        [NonAction]
        protected ObjectResult UnauthorizedFail(string message = null)
            => Fail<object>(null, message, StatusCodes.Status401Unauthorized);
        [NonAction]
        protected ObjectResult UnauthorizedFail<TData>(TData data, string message = null)
            => Fail(data, message, StatusCodes.Status401Unauthorized);

        //// 403
        /// <summary>回傳 403 Forbid 錯誤</summary>
        /// <param name="message">錯誤訊息 (預設為: "您沒有權限執行此操作")</param>
        /// <remarks>預設訊息：您沒有權限執行此操作</remarks>
        [NonAction]
        protected ObjectResult ForbiddenFail(string message = null)
            => Fail<object>(null, message, StatusCodes.Status403Forbidden);
        [NonAction]
        protected ObjectResult ForbiddenFail<TData>(TData data, string message = null)
            => Fail(data, message, StatusCodes.Status403Forbidden);

        // 404
        /// <summary>回傳 404 NotFound 錯誤</summary>
        /// <param name="message">錯誤訊息 (預設為: "找不到指定資源")</param>
        /// <remarks>預設訊息：找不到指定資源</remarks>
        [NonAction]
        protected ObjectResult NotFoundFail(string message = null)
            => Fail<object>(null, message, StatusCodes.Status404NotFound);
        [NonAction]
        protected ObjectResult NotFoundFail<TData>(TData data, string message = null)
            => Fail(data, message, StatusCodes.Status404NotFound);


        // 409
        /// <summary>回傳 409 Conflict 錯誤</summary>
        /// <param name="message">錯誤訊息 (預設為: "資料已存在或發生衝突")</param>
        /// <remarks>預設訊息：資料已存在或發生衝突</remarks>
        [NonAction]
        protected ObjectResult ConflictFail(string message = null)
            => Fail<object>(null, message, StatusCodes.Status409Conflict);
        [NonAction]
        protected ObjectResult ConflictFail<TData>(TData data, string message = null)
            => Fail(data, message, StatusCodes.Status409Conflict);

        // 422
        /// <summary>回傳 422 Conflict 錯誤</summary>
        /// <param name="message">錯誤訊息 (預設為: "欄位驗證失敗")</param>
        /// <remarks>預設訊息：欄位驗證失敗</remarks>
        [NonAction]
        protected ObjectResult UnprocessableEntityFail(string message = null)
            => Fail<object>(null, message, StatusCodes.Status422UnprocessableEntity);
        [NonAction]
        protected ObjectResult UnprocessableEntityFail<TData>(TData data, string message = null)
            => Fail(data, message, StatusCodes.Status422UnprocessableEntity);
    }
}
