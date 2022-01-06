using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IdentidadeDigital.Infra.Model.IdDigital
{
    [Table("VERSAO_APP", Schema = "ID_DIGITAL")]
    public class VersaoApp
    {
        [Key]
        [Column("SQ_VERSAO")]
        public int SqVersao { get; set; }

        [Column("NU_VERSAO")]
        public short NuVersao { get; set; }

        [Column("DT_INICIO")]
        public DateTime? DtInicio { get; set; }

        [Column("DT_FIM")]
        public DateTime? DtFim { get; set; }

        [Column("DT_INCLUSAO")]
        public DateTime? DtInclusao { get; set; }

        [Column("TP_STATUS")]
        public short TpStatus { get; set; }

        [Column("DS_VERSAO")]
        public string DsVersao { get; set; }

        [Column("DS_STATUS")]
        public string DsStatus { get; set; }

        [Column("DS_MSG_USUARIO")]
        public string DsMsgUsuario { get; set; }
    }
}
