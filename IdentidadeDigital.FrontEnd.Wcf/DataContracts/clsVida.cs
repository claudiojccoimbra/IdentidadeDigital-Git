using System.Runtime.Serialization;

namespace IdentidadeDigital.FrontEnd.Wcf.DataContracts
{
    [DataContract]
    public class clsVida
    {
        [DataMember]
        public string IdTransacao { get; set; }
        [DataMember]
        public string ImProvavida { get; set; }
        [DataMember]
        public string TpProvavida { get; set; }
    }
}