using System;
using System.Collections.Generic;
using System.Data;
using IdentidadeDigital.Infra.Domain;
using IdentidadeDigital.Infra.Domain.Enums;
using IdentidadeDigital.Infra.Model.IdDigital;
using IdentidadeDigital.Infra.Repository.Base;
using Microsoft.EntityFrameworkCore;

namespace IdentidadeDigital.Infra.Repository
{
    public class ProvaVidaRepository : RepositoryBase<ProvaVida, IdDigitalDbContext>
    {
        public bool InserirProvaVida(List<ImagemProvaVida> listaImagemProvaVida, string idTransacao)
        {
            try
            {
                var dadosPid = new PedidosRepository().ConsultarPedidoIdTransacao(idTransacao);

                using (var db = new IdDigitalDbContext())
                {
                    foreach (var imagemProvaVida in listaImagemProvaVida)
                    {
                        int sqProvaVida;
                        using (var command = db.Database.GetDbConnection().CreateCommand())
                        {
                            var querySequence = "select id_digital.sq_prova_vida.nextval from dual";

                            command.CommandText = querySequence;
                            command.CommandType = CommandType.Text;
                            db.Database.OpenConnection();

                            sqProvaVida = Convert.ToInt32(command.ExecuteScalar());
                        }

                        var provaVida = new ProvaVida();
                        provaVida.SqProvaVida = sqProvaVida;
                        provaVida.SqTransacao = dadosPid.Transacao;
                        provaVida.TpImagem = imagemProvaVida.TpProvavida;
                        provaVida.ImFoto = Convert.FromBase64String(imagemProvaVida.ImProvavida);

                        db.ProvaVida.Add(provaVida);
                        db.SaveChanges();
                    }
                }
                return true;
            }
            catch (Exception e)
            {
                new LogRepository().InserirLog(idTransacao, "InserirProvaVida -> " + e.InnerException == null ? e.Message : e.InnerException.Message);
                throw new Exception(EnumHelper.GetDescriptionFromEnumValue(TipoErroEnum.InserirProvaVida));
            }
        }
    }
}
