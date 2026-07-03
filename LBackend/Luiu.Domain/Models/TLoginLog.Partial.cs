using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Luiu.Domain.Models
{
    public partial class TLoginLog
    {
        public override string ToString()
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true // 讓排版變漂亮（有縮排）
            };
            return JsonSerializer.Serialize(this, options);
        }
    }
}
