using Luiu.Domain.DTOs;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;

namespace Luiu.Service.Interfaces
{
    public interface IStorageService
    {
        Task<string> SaveFileAsync(IFormFile file, FileUploadPolicyDTO policy);
        Task<string> SaveFileAsync(byte[] fileBytes, string extension, FileUploadPolicyDTO policy);
        Task<List<string>> SaveFilesAsync(List<IFormFile> files, FileUploadPolicyDTO policy);
    }
}
