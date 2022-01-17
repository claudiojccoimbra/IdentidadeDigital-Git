using Microsoft.EntityFrameworkCore;

namespace IdentidadeDigital.WebApi
{
    public class OracleDbContext : DbContext
    {
        public OracleDbContext(DbContextOptions options) : base(options) { }
    }
}
