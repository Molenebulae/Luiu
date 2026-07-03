using Luiu.Domain.DTOs;
using Microsoft.EntityFrameworkCore.SqlServer.Query.Internal;
using System;
using System.Collections.Generic;
using System.Text;

namespace Luiu.Domain.Conmon
{
    public static class CDictionary
    {
        //    public static readonly string[] ImageExts = { ".jpg", ".jpge", ".png", ".gif" };
        //    public const long MAXIMAGESIZE = 2 * 1024 * 1024;


        public static readonly Dictionary<string, FileUploadPolicyDTO> UploadPolicies = new()
        {
            {
                "avatar", new FileUploadPolicyDTO
                {
                    SubFolrer = "avatars",
                    MaxSize = 2 * 1024 * 1024,
                    AllowedExtensions = new [] { ".jpg", ".jpge", ".png", ".gif"}
                }
            },
            {
                "video", new FileUploadPolicyDTO
                {
                    SubFolrer = "videos",
                    MaxSize = 50 * 1024 * 1024,
                    AllowedExtensions = new[] { ".mp4", ".mov"}
                }
            },
            {
                "memory", new FileUploadPolicyDTO
                {
                    SubFolrer = "memories",
                    MaxSize = 5 * 1024 * 1024, // 5MB limit
                    AllowedExtensions = new [] { ".jpg", ".jpeg", ".png", ".gif", ".webp"}
                }
            },

            {
                "trips", new FileUploadPolicyDTO
                {
                    SubFolrer = "trips",
                    MaxSize = 10 * 1024 * 1024,
                    AllowedExtensions = new [] { ".jpg", ".jpeg", ".png", ".gif"}
                }
            },
        };

    }
}

