using System;

namespace IdentidadeDigital.Infra.Domain
{

    public partial class VersaoApp
    {
        public int SqVersao { get; set; }

        public short NuVersao { get; set; }

        public DateTime? DtInicio { get; set; }

        public DateTime? DtFim { get; set; }

        public DateTime? DtInclusao { get; set; }

        public short TpStatus { get; set; }

        public string DsVersao { get; set; }

        public string DsStatus { get; set; }

        public string DsMsgUsuario { get; set; }
        public string Erro { get; set; }
    }
}
