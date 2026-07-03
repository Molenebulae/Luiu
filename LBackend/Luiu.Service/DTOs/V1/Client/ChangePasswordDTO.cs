using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Luiu.Service.DTOs.V1.Client
{
    public class ChangePasswordDTO
    {
        [Required(ErrorMessage = "必須輸入目前使用的舊密碼")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "必須輸入欲變更的新密碼")]
        //[MinLength(8, ErrorMessage = "新密碼長度至少需要 8 碼")]
        //[MaxLength(20, ErrorMessage = "新密碼長度最多不可超過 20 碼")]
        public string NewPassword { get; set; } = string.Empty;
    }
}
