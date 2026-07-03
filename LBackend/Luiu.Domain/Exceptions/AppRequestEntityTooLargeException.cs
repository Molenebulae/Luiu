using System;
using System.Collections.Generic;
using System.Text;

namespace Luiu.Domain.Exceptions
{
    public class AppRequestEntityTooLargeException : BaseAppException
    {
        private const string DefaultMessage = "檔案大小超過限制";
        public AppRequestEntityTooLargeException(string message = DefaultMessage) : base(null, message, 413) { }
        public AppRequestEntityTooLargeException(object data, string message = DefaultMessage) : base(data, message, 413) { }
    }

}
