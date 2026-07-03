using Luiu.Domain.Conmon;
using Luiu.Domain.DTOs;
using Luiu.Domain.Exceptions;
using Luiu.Service.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Luiu.Service.Implementations
{
    public class LocalStorageService : IStorageService
    {
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<LocalStorageService> _logger;
        public LocalStorageService(IWebHostEnvironment env, ILogger<LocalStorageService> logger)
        {
            _env = env;
            _logger = logger;
        }

        public async Task<string> SaveFileAsync(IFormFile file, FileUploadPolicyDTO policy)
        {
            // 檔案檢查 400
            if (file.Length == 0) throw new AppBadRequestException("檔案不可為空");

            // 判斷大小 413
            if (file.Length > policy.MaxSize) throw new AppRequestEntityTooLargeException($"檔案超過限制，最大允許 {policy.MaxSize / 1024 / 1024}MB");

            // 判斷副檔名 415
            var extension = Path.GetExtension(file.FileName).ToLower();
            if (!policy.AllowedExtensions.Contains(extension)) throw new AppUnsupportedMediaTypeException();

            // 儲存路徑
            string rootPath = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            string uploadPath = Path.Combine(rootPath, "uploads", policy.SubFolrer);
            if (!Directory.Exists(uploadPath))
            {
                Directory.CreateDirectory(uploadPath);
            }

            // 完整檔名含路徑
            string fileName = $"{Guid.NewGuid()}{extension}";
            string fullPath = Path.Combine(uploadPath, fileName);

            // 儲存檔案
            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            _logger.LogInformation("檔案已儲存至: {path}", fullPath);

            return $"uploads/{policy.SubFolrer}/{fileName}";
        }

        public async Task<string> SaveFileAsync(byte[] fileBytes, string extension, FileUploadPolicyDTO policy)
        {
            if (fileBytes == null || fileBytes.Length == 0) throw new AppBadRequestException("檔案不可為空");

            // 判斷大小 413
            if (fileBytes.Length > policy.MaxSize) throw new AppRequestEntityTooLargeException($"檔案超過限制，最大允許 {policy.MaxSize / 1024 / 1024}MB");

            // 判斷副檔名 415 (確保傳進來的副檔名有帶點，例如 ".jpg")
            var fixedExtension = extension.StartsWith(".") ? extension.ToLower() : $".{extension.ToLower()}";
            if (!policy.AllowedExtensions.Contains(fixedExtension)) throw new AppUnsupportedMediaTypeException();

            // 儲存路徑
            string rootPath = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            string uploadPath = Path.Combine(rootPath, "uploads", policy.SubFolrer);
            if (!Directory.Exists(uploadPath))
            {
                Directory.CreateDirectory(uploadPath);
            }

            // 完整檔名含路徑
            string fileName = $"{Guid.NewGuid()}{fixedExtension}";
            string fullPath = Path.Combine(uploadPath, fileName);

            // 儲存檔案 (寫入 byte[])
            await File.WriteAllBytesAsync(fullPath, fileBytes);

            _logger.LogInformation("大頭貼已儲存至: {path}", fullPath);
            return $"/uploads/{policy.SubFolrer}/{fileName}";
        }

        public async Task<List<string>> SaveFilesAsync(List<IFormFile> files, FileUploadPolicyDTO policy)
        {
            var paths = new List<string>();

            foreach (var file in files)
            {
                var path = await SaveFileAsync(file, policy);
                paths.Add(path);
            }

            return paths;
        }
    }
}
