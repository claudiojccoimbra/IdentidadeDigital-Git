using System;
using System.Collections.Generic;
using System.Text;

namespace IdentidadeDigital.Infra.Domain
{
    public class Carteira
    {
        public string Escore { get; set; }
        public string Ric { get; set; }
        public string Transacao { get; set; }
        public string CarteiraFrente { get; set; }
        public string CarteiraVerso { get; set; }
        public string Erro { get; set; }
    }

}
