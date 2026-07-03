using System;
using System.Collections.Generic;
using System.Text;

namespace Luiu.Domain.Exceptions
{
    public class AppUnsupportedMediaTypeException : BaseAppException
    {
        private const string DefaultMessage = "不支援的檔案格式";
        public AppUnsupportedMediaTypeException(string message = DefaultMessage) : base(null, message, 415) { }
        public AppUnsupportedMediaTypeException(object data, string message = DefaultMessage) : base(data, message, 415) { }
    }
}
