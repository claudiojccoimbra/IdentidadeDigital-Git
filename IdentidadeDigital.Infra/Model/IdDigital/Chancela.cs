using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IdentidadeDigital.Infra.Model.IdDigital
{
    [Table("CHANCELA", Schema = "ID_DIGITAL")]
    public class Chancela
    {
        [Key]
        [Column("SQ_CHANCELA")]
        public int SqChancela { get; set; }

        [Column("NU_RIC")]
        public int? NuRic { get; set; }

        [Column("NU_VIAS")]
        public int? NuVias { get; set; }

        [Column("DT_INICIO")]
        public DateTime? DtInicio { get; set; }

        [Column("DT_FIM")]
        public DateTime? DtFim { get; set; }

        [Column("DT_INCLUSAO")]
        public DateTime? DtInclusao { get; set; }

        [Column("IM_CHANCELA")]
        public byte[] ImChancela { get; set; }
    }
}
