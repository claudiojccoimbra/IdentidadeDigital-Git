using System;
using System.Runtime.Serialization;

namespace IdentidadeDigital.FrontEnd.Wcf.DataContracts
{
    [DataContract]
    public class VersaoApp
    {
        [DataMember]
        public int SqVersao { get; set; }
        [DataMember]
        public short NuVersao { get; set; }
        [DataMember]
        public DateTime? DtInicio { get; set; }
        [DataMember]
        public DateTime? DtFim { get; set; }
        [DataMember]
        public DateTime? DtInclusao { get; set; }
        [DataMember]
        public short TpStatus { get; set; }
        [DataMember]
        public string DsVersao { get; set; }
        [DataMember]
        public string DsStatus { get; set; }
        [DataMember]
        public string DsMsgUsuario { get; set; }
        [DataMember]
        public string Erro { get; set; }
    }
}