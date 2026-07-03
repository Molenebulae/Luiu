using Microsoft.EntityFrameworkCore;

namespace Luiu.Domain.Models
{
    public partial class LuiuDbContext : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if(!optionsBuilder.IsConfigured)
            {
                throw new Exception("請使用注入方式");
                //optionsBuilder.UseSqlServer(ConnectionString);
            }
        }
    }

}

