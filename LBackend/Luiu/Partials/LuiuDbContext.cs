using Microsoft.EntityFrameworkCore;

namespace Luiu.Models 
{
    public partial class LuiuDbContext : DbContext
    {

        public static string ConnectionString { get; set; }
        public LuiuDbContext() {}

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if(!optionsBuilder.IsConfigured && !string.IsNullOrEmpty(ConnectionString))
            {
                optionsBuilder.UseSqlServer(ConnectionString);
            }
        }
    }

}

