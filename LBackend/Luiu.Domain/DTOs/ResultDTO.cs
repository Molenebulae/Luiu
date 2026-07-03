using System.Net.NetworkInformation;

namespace Luiu.Domain.DTOs
{
    public class ResultDTO<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public T? Data { get; set; }
    }
}
