using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Luiu.Domain.DTOs;
using Luiu.Domain.Exceptions;
using Luiu.Service.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace Luiu.Service.Implementations
{
    public class AzureStorageService : IStorageService
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly ILogger<LocalStorageService> _logger;
        private const string ContainerName = "luiu-media";

        public AzureStorageService(BlobServiceClient blobServiceClient, ILogger<LocalStorageService> logger)
        {
            _blobServiceClient = blobServiceClient;
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

            var containerClient = _blobServiceClient.GetBlobContainerClient(ContainerName);

            // 建立檔名
            string blobPath = $"uploads/{policy.SubFolrer}/{Guid.NewGuid()}{extension}".Replace("//", "/");
            var blobClient = containerClient.GetBlobClient(blobPath);

            // 串流上傳
            using (var stream = file.OpenReadStream())
            {
                var blobHttpHeader = new BlobHttpHeaders { ContentType = file.ContentType };
                await blobClient.UploadAsync(stream, new BlobUploadOptions { HttpHeaders = blobHttpHeader });
            }

            _logger.LogInformation("檔案已儲存到 Azure Blob: {path}", blobClient.Uri.ToString());

            // 回傳實體位置
            return blobPath;
        }

        public async Task<string> SaveFileAsync(byte[] fileBytes, string extension, FileUploadPolicyDTO policy)
        {
            if (fileBytes == null || fileBytes.Length == 0) throw new AppBadRequestException("檔案不可為空");

            // 判斷大小 413
            if (fileBytes.Length > policy.MaxSize) throw new AppRequestEntityTooLargeException($"檔案超過限制，最大允許 {policy.MaxSize / 1024 / 1024}MB");

            // 判斷副檔名 415 (確保傳進來的副檔名有帶點，例如 ".jpg")
            var fixedExtension = extension.StartsWith(".") ? extension.ToLower() : $".{extension.ToLower()}";
            if (!policy.AllowedExtensions.Contains(fixedExtension)) throw new AppUnsupportedMediaTypeException();

            var containerClient = _blobServiceClient.GetBlobContainerClient(ContainerName);

            string blobPath = $"uploads/{policy.SubFolrer}/{Guid.NewGuid()}{fixedExtension}".Replace("//", "/");
            var blobClient = containerClient.GetBlobClient(blobPath);

            // 判斷常見副檔名的 ContentType
            string contentType = fixedExtension switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".webp" => "image/webp",
                _ => "application/octet-stream"
            };

            // 寫入 byte[] 資料
            using (var stream = new MemoryStream(fileBytes))
            {
                var blobHttpHeader = new BlobHttpHeaders { ContentType = contentType };
                await blobClient.UploadAsync(stream, new BlobUploadOptions { HttpHeaders = blobHttpHeader });
            }

            _logger.LogInformation("大頭貼已儲存至 Azure Blob: {path}", blobClient.Uri.ToString());
            return blobClient.Uri.ToString();
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
