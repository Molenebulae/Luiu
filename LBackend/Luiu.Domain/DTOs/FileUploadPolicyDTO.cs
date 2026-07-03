using Luiu.Domain.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Luiu.Domain.DTOs
{
    public class FileUploadPolicyDTO
    {
        public string SubFolrer { get; set; } = string.Empty;
        public long MaxSize { get; set; }
        public string[] AllowedExtensions { get; set; } = Array.Empty<string>();
    }
}
