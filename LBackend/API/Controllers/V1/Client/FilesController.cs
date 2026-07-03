using Luiu.Domain.Conmon;
using Luiu.Domain.DTOs;
using Luiu.Service.Implementations;
using Luiu.Service.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers.V1.Client
{
    public class FilesController : BaseController<FilesController>
    {
        private readonly IStorageService _storageService;
        public FilesController(ILogger<FilesController> logger, IStorageService storageService) : base(logger)
        {
            _storageService = storageService;
        }

        // POST: api/v1/files
        [HttpPost]
        public async Task<ActionResult<ResultDTO<string>>> Upload([FromForm] IFormFile file, [FromForm] string type)
        {
            if (file == null) return BadRequestFail("沒有收到檔案，請重新上傳");

            if (!CDictionary.UploadPolicies.TryGetValue(type, out var policy))
            {
                return BadRequestFail("不支援上傳類型");
            }

            string relativePath = await _storageService.SaveFileAsync(file, policy);

            return Success(relativePath, "上傳成功");
        }

        // PUT: api/v1/Files/multiple
        [HttpPost("multiple")]
        public async Task<ActionResult<ResultDTO<List<string>>>> UploadMultiple([FromForm] List<IFormFile> files, [FromForm] string type)
        {
            if (files == null || !files.Any()) return BadRequestFail("請選擇檔案");

            if (!CDictionary.UploadPolicies.TryGetValue(type, out var policy))
            {
                return BadRequestFail("不支援上傳類型");
            }

            var filePaths = await _storageService.SaveFilesAsync(files, policy);

            return Success(filePaths, "上傳成功");
        }
    }
}
