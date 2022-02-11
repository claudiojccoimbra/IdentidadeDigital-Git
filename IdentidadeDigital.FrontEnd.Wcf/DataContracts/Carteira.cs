using System.Runtime.Serialization;

namespace IdentidadeDigital.FrontEnd.Wcf.DataContracts
{
    [DataContract]
    public class Carteira
    {
        [DataMember]
        public string Escore { get; set; }
        [DataMember]
        public string Ric { get; set; }
        [DataMember]
        public string Transacao { get; set; }
        [DataMember]
        public string IdTransacao { get; set; }
        [DataMember]
        public string CarteiraFrente { get; set; }
        [DataMember]
        public string CarteiraVerso { get; set; }
        [DataMember]
        public string CarteiraPdf { get; set; }
        [DataMember]
        public string Erro { get; set; }
    }

}
