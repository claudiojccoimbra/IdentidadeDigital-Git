using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IdentidadeDigital.Infra.Model.IdDigital
{
    [Table("PEDIDOS", Schema = "ID_DIGITAL")]
    public class Pedidos
    {
        [Key]
        [Column("SQ_TRANSACAO")]
        public int SqTransacao { get; set; }

        [Column("ID_TRANSACAO")]
        public string IdTrasacao { get; set; }

        [Column("DE_TIPOGRAFICO_UF")]
        public string DeTipograficoUf { get; set; }

        [Column("DE_TIPOGRAFICO_NUMERO")]
        public int DeTipograficoNumero { get; set; }

        [Column("DE_TIPOGRAFICO_SERIE")]
        public string DeTipograficoSerie { get; set; }

        [Column("DE_TIPOGRAFICO_FISICO")]
        public string DeTipograficoFisico { get; set; }

        [Column("NU_RIC")]
        public int NuRic { get; set; }

        [Column("NU_VIAS")]
        public string NuVias { get; set; }

        [Column("DE_QRCODE")]
        public byte[] ImQrCode { get; set; }

        [Column("NU_CEL_LINHA")]
        public int NuCelLinha { get; set; }

        [Column("NU_CEL_IMEI")]
        public string NuCelImei { get; set; }

        [Column("DE_CEL_IP")]
        public string DeCelIp { get; set; }

        [Column("DE_CEL_FABRICANTE")]
        public string DeCelFabricante { get; set; }

        [Column("DE_CEL_MODELO")]
        public string DeCelModelo { get; set; }

        [Column("DE_CEL_SERIE")]
        public string DeCelSerie { get; set; }

        [Column("DE_CEL_SO")]
        public string DeCelSo { get; set; }

        [Column("DE_CEL_SO_VERSAO")]
        public string DeCelSoVersao { get; set; }

        [Column("NU_GPS_LAT")]
        public string NuGpsLat { get; set; }

        [Column("NU_GPS_LONG")]
        public string NuGpsLong { get; set; }

        [Column("TP_STATUS")]
        public int TpStatus { get; set; }

        [Column("DT_INCLUSAO")]
        public DateTime? DtInclusao { get; set; }

        [Column("NU_PID")]
        public int NuPid { get; set; }

        [Column("NU_SCORE")]
        public int NuScore { get; set; }
    }
}
