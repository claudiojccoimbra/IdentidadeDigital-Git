using System;
using System.Runtime.Serialization;

namespace IdentidadeDigital.FrontEnd.Wcf.DataContracts
{
    [DataContract]
    public class StatusCarteira
    {
        [DataMember]
        public DateTime DtInclusao { get; set; }
        [DataMember]
        public string TpStatusId { get; set; }
        [DataMember]
        public string NuRic { get; set; }
        [DataMember]
        public string SqTransacao { get; set; }
        [DataMember]
        public string DeStatus { get; set; }
        [DataMember]
        public string DsMsgStatus { get; set; }
        [DataMember]
        public string Erro { get; set; }
    }
}