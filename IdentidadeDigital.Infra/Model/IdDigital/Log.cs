using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IdentidadeDigital.Infra.Model.IdDigital
{
    [Table("LOG", Schema = "ID_DIGITAL")]
    public class Log
    {
        [Key]
        [Column("SQ_LOG")]
        public long SqLog { get; set; }

        [Column("SQ_TRANSACAO")]
        public long SqTransacao { get; set; }

        [Column("TP_STATUS_PEDIDO")]
        public int TpStatusPedido { get; set; }

        [Column("DE_LOG")]
        public string DeLog { get; set; }

        [Column("DT_INCLUSAO")]
        public DateTime? DtInclusao { get; set; }
    }
}
