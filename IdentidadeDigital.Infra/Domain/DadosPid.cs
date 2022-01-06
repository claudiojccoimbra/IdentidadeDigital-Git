using System;
using System.Collections.Generic;
using System.Text;

namespace IdentidadeDigital.Infra.Domain
{
    public class DadosPid
    {
        public long Pid { get; set; }
        public string Ric { get; set; }
        public int Vias { get; set; }
        public int Sexo { get; set; }
        public int? Corpo { get; set; }
        public int? Ano { get; set; }
        public string Erro { get; set; }
        public long Transacao { get; set; }
    }
}
