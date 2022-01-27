using System;
using System.Collections.Generic;
using System.Text;

namespace IdentidadeDigital.Infra.Domain
{
    public class Solicitacao
    {
        public string Nulinha { get; set; }
        public string NuImei { get; set; }
        public string DeIp { get; set; }
        public string DeFabricante { get; set; }
        public string DeModelo { get; set; }
        public string DeSerie { get; set; }
        public string DeSo { get; set; }
        public string DeSoVersao { get; set; }
        public string NuGpsLat { get; set; }
        public string NuGpsLong { get; set; }
        public string DeTpGrafico { get; set; }
        public string DeQrCode { get; set; }
 
        public string Pid { get; set; }
        public string SqTransacao { get; set; }
        public string IdTransacao { get; set; }
    }
}
