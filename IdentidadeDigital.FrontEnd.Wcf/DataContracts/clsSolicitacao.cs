using System.Runtime.Serialization;

namespace IdentidadeDigital.FrontEnd.Wcf.DataContracts
{
    public class clsSolicitacao
    {
        [DataMember]
        public string Nulinha { get; set; }
        [DataMember]
        public string NuImei { get; set; }
        [DataMember]
        public string DeIp { get; set; }
        [DataMember]
        public string DeFabricante { get; set; }
        [DataMember]
        public string DeModelo { get; set; }
        [DataMember]
        public string DeSerie { get; set; }
        [DataMember]
        public string DeSo { get; set; }
        [DataMember]
        public string DeSoVersao { get; set; }
        [DataMember]
        public string NuGpsLat { get; set; }
        [DataMember]
        public string NuGpsLong { get; set; }
        [DataMember]
        public string DeTpGrafico { get; set; }
        [DataMember]
        public string DeQrCode { get; set; }

        [DataMember]
        public string Pid { get; set; }
        [DataMember]
        public string SqTransacao { get; set; }
        [DataMember]
        public string IdTransacao { get; set; }
    }
}