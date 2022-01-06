using System;

namespace IdentidadeDigital.Infra.Domain
{
   public class StatusCarteira
    {
        public DateTime? DtInclusao { get; set; }
        public int TpStatusId { get; set; }
        public int NuRic { get; set; }
        public long SqTransacao { get; set; }
        public string DeStatus { get; set; }
        public string DsMsgStatus { get; set; }
        public string Erro { get; set; }
    }
}
