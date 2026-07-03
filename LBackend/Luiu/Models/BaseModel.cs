using System;
using System.Reflection;

namespace Luiu.Models
{
    public class BaseModel
    {
        public override string ToString()
        {
            System.Type type = this.GetType();
            // 取得所有實例變數 (包含公開與非公開)
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            var values = fields.Select(f => $"{f.Name}: {f.GetValue(this)}");

            return $"[{type.Name}]\n" + string.Join("\n", values);
        }
    }
}
