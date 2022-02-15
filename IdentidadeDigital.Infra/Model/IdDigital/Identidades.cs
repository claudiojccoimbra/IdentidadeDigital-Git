using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection.Metadata;
using Oracle.ManagedDataAccess.Types;

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

        [Column("IM_FRENTE", TypeName = "BLOB")]
        public byte[] ImFrente { get; set; }

        [Column("IM_VERSO", TypeName = "BLOB")]
        public byte[] ImVerso { get; set; }

        [Column("IM_QRCODE", TypeName = "BLOB")]
        public byte[] ImQrCode { get; set; }

        [Column("IM_PDF", TypeName = "BLOB")]
        public byte[] ImPdf { get; set; }

        [Column("IM_ASSINATURA", TypeName = "BLOB")]
        public byte[] ImAssinatura { get; set; }

        [Column("NU_SCORE")]
        public int NuScore { get; set; }
    }
}
