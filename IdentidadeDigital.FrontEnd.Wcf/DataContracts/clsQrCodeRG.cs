using System.Runtime.Serialization;

namespace IdentidadeDigital.FrontEnd.Wcf.DataContracts
{
    [DataContract]
    public class clsQrCodeRG
    {
        [DataMember]
        public string Rg { get; set; }
        [DataMember]
        public string Data { get; set; }
        [DataMember]
        public string Pid { get; set; }
        [DataMember]
        public string Erro { get; set; }
    }
}