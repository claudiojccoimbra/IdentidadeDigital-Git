using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IdentidadeDigital.Infra.Model
{
    [Table("CORPOS_INDEX", Schema = "IMAGEM")]
    public class CorposIndex
    {
        [Key]
        [Column("ID_CORPO")]
        public long IdCorpo { get; set; }

        [Column("NU_PID")]
        public long NuPid { get; set; }

        [Column("NU_RIC")]
        public int NuRic { get; set; }

        [Column("NU_VIAS")]
        public int NuVias { get; set; }
    }
}
