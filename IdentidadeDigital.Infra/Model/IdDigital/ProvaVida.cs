using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IdentidadeDigital.Infra.Model.IdDigital
{
    [Table("PROVA_VIDA", Schema = "ID_DIGITAL")]
    public class ProvaVida
    {
        [Key]
        [Column("SQ_PROVA_VIDA")]
        public int SqProvaVida { get; set; }

        [Column("SQ_TRANSACAO")]
        public long SqTransacao { get; set; }

        [Column("TP_IMAGEM")]
        public short TpImagem { get; set; }

        [Column("IM_FOTO")]
        public byte[] ImFoto { get; set; }
    }
}
