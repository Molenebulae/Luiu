using System;
using System.Collections.Generic;
using System.Text;

namespace Luiu.Service.Interfaces
{
    public interface IAdminService
    {
        Task SetRecommendationAsync(string type, int id, bool isRecommended);
    }
}
