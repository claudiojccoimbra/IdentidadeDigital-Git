using IdentidadeDigital.Infra.Model;
using IdentidadeDigital.Infra.Model.IdDigital;
using Microsoft.EntityFrameworkCore;
using VersaoApp = IdentidadeDigital.Infra.Model.IdDigital.VersaoApp;

namespace IdentidadeDigital.Infra
{
    public class IdDigitalDbContext : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                //PROD
                optionsBuilder.UseOracle(@"User Id=fac;Password=fac;Data Source=(DESCRIPTION=(ADDRESS_LIST= (LOAD_BALANCE=on))(ADDRESS=(PROTOCOL=tcp)(HOST=10.200.96.225)(PORT=1521))(ADDRESS=(PROTOCOL=tcp)(HOST=10.200.96.226)(PORT=1521))(ADDRESS=(PROTOCOL=tcp)(HOST=10.200.96.227)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=dic)))");
                //HOMOLOG
               // optionsBuilder.UseOracle(@"User Id=fac;Password=fac;Data Source=(DESCRIPTION=(ADDRESS_LIST= (LOAD_BALANCE=on))(ADDRESS=(PROTOCOL=tcp)(HOST=10.200.96.222)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=seid)))");
            }
        }

        public virtual DbSet<Chancela> Chancela { get; set; }
        public virtual DbSet<Identidades> Identidades { get; set; }
        public virtual DbSet<Log> Log { get; set; }
        public virtual DbSet<Pedidos> Pedidos { get; set; }
        public virtual DbSet<ProvaVida> ProvaVida { get; set; }
        public virtual DbSet<CorposIndex> CorposIndex { get; set; }
        public virtual DbSet<Identificacoes> Identificacoes { get; set; }
        public virtual DbSet<VersaoApp> VersaoApp { get; set; }
    }
}
