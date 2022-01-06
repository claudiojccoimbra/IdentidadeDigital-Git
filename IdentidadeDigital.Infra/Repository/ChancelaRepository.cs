using System;
using System.Linq;
using IdentidadeDigital.Infra.Model.IdDigital;
using IdentidadeDigital.Infra.Repository.Base;

namespace IdentidadeDigital.Infra.Repository
{
    public class ChancelaRepository : RepositoryBase<Chancela, IdDigitalDbContext>
    {
        public byte[] CarregarChancela(DateTime dtExpedicao)
        {
            try
            {
                using (var db = new IdDigitalDbContext())
                {
                    var query = (from i in db.Chancela
                        where (dtExpedicao >= i.DtInicio && dtExpedicao <= i.DtFim) || i.DtFim == null
                        select i.ImChancela).FirstOrDefault();

                    return query;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
