using AutoMapper;
using Luiu.Domain.Models;
using Microsoft.Extensions.Logging;

namespace Luiu.Service.Implementations
{
    public abstract class BaseService<T>
    {
        protected readonly LuiuDbContext _context;
        protected readonly ILogger<T> _logger;
        protected readonly IMapper _mapper;

        protected BaseService(LuiuDbContext context, ILogger<T> logger, IMapper mapper)
        {
            _context = context;
            _logger = logger;
            _mapper = mapper;
        }
    }
}
