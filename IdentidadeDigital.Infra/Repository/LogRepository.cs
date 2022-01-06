using System;
using System.Data;
using IdentidadeDigital.Infra.Model.IdDigital;
using IdentidadeDigital.Infra.Repository.Base;
using Microsoft.EntityFrameworkCore;

namespace IdentidadeDigital.Infra.Repository
{
    public class LogRepository : RepositoryBase<Log, IdDigitalDbContext>
    {
        public void InserirLog(string idTransacao, string descLog)
        {
            try
            {
                using (var db = new IdDigitalDbContext())
                {
                    using (var command = db.Database.GetDbConnection().CreateCommand())
                    {
                        var querySequence = "select id_digital.sq_digital_log.nextval from dual";

                        command.CommandText = querySequence;
                        command.CommandType = CommandType.Text;

                        db.Database.OpenConnection();

                        long sqLog = Convert.ToInt64(command.ExecuteScalar());

                        var dadosPid = new PedidosRepository().ConsultarPedidoIdTransacao(idTransacao);

                        var log = new Log
                        {
                            SqLog = sqLog,
                            SqTransacao = dadosPid.Transacao,
                            TpStatusPedido = 1, // valor fixo?
                            DeLog = descLog,
                            DtInclusao = DateTime.Now
                        };

                        db.Log.Add(log);
                        db.SaveChanges();
                    }
                }
            }
            catch (Exception e)
            {
                throw e;
            }
        }
    }
}
