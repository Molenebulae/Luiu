using System;
using System.Collections.Generic;
using System.Text;

namespace Luiu.Domain.Exceptions
{
    public class AppBadRequestException : BaseAppException
    {
        private const string DefaultMessage = "請求內容有誤";
        public AppBadRequestException(string message = DefaultMessage) : base(null, message, 400) { }
        public AppBadRequestException(object data, string message = DefaultMessage) : base(data, message, 400) { }
    }
}
