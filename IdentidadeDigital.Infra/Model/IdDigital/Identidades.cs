using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IdentidadeDigital.Infra.Model.IdDigital
{
    [Table("IDENTIDADES", Schema = "ID_DIGITAL")]
    public class Identidades
    {
        [Key]
        [Column("SQ_IDENTIDADE")]
        public long SqIdentidade { get; set; }

        [Column("NU_RIC")]
        public int NuRic { get; set; }

        [Column("NU_VIAS")]
        public int NuVias { get; set; }

        [Column("NU_PID")]
        public long NuPid { get; set; }

        [Column("DT_ATUALIZA")]
        public DateTime? DtAtualiza { get; set; }

        [Column("SQ_TRANSACAO")]
        public long SqTransacao { get; set; }

        [Column("TP_STATUS_ID")]
        public int TpStatusId { get; set; }

        [Column("IM_FRENTE")]
        public byte[] ImFrente { get; set; }

        [Column("IM_VERSO")]
        public byte[] ImVerso { get; set; }

        [Column("IM_QRCODE")]
        public byte[] ImQrCode { get; set; }

        [Column("IM_PDF")]
        public byte[] ImPdf { get; set; }

        [Column("IM_ASSINATURA")]
        public byte[] ImAssinatura { get; set; }

        [Column("NU_SCORE")]
        public int NuScore { get; set; }
    }
}
