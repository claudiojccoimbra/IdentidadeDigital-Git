using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IdentidadeDigital.Infra.Model
{
    [Table("IDENTIFICACOES", Schema = "CIVIL")]
    public class Identificacoes
    {
        [Key]
        [Column("NU_RIC")]
        public long NuRic { get; set; }

        [Column("TP_SEXO")]
        public short TpSexo { get; set; }

        [Column("NU_VIAS")]
        public short NuVias { get; set; }

        [Column("SG_UFTIPOGRAFICO")]
        public string SgUfTipoGrafico { get; set; }

        [Column("NU_ESPELHO")]
        public int NuEspelho { get; set; }

        [Column("NU_SERIE")]
        public string NuSerie { get; set; }

        [Column("NU_TIPOGRAFICO_FISICO")]
        public string NuTipoGraficoFisico { get; set; }

        [Column("NU_PID")]
        public long NuPid { get; set; }
    }
}
