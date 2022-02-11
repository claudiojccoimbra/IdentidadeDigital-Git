using System.Runtime.Serialization;

namespace IdentidadeDigital.FrontEnd.Wcf.DataContracts
{
    [DataContract]
    public class BarCode
    {
        [DataMember]
        public string Codigo { get; set; }
        [DataMember]
        public string Erro { get; set; }
    }
}