using System;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using IdentidadeDigital.Infra.Domain;
using IdentidadeDigital.Infra.Domain.Enums;
using IdentidadeDigital.Infra.Model;
using IdentidadeDigital.Infra.Model.IdDigital;
using IdentidadeDigital.Infra.Repository.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace IdentidadeDigital.Infra.Repository
{
    public class PedidosRepository : RepositoryBase<Pedidos, IdDigitalDbContext>
    {
        public string GetSession()
        {
            try
            {
                string sequencePedidos;
                string idTransacao;

                using (var db = new IdDigitalDbContext())
                {
                    using (var command = db.Database.GetDbConnection().CreateCommand())
                    {
                        var querySequence = "select id_digital.sq_pedidos.nextval from dual";

                        command.CommandText = querySequence;
                        command.CommandType = CommandType.Text;
                        db.Database.OpenConnection();

                        sequencePedidos = command.ExecuteScalar().ToString();
                        idTransacao = Md5ComputeHash(sequencePedidos);
                    }
                }
                return sequencePedidos + ";" + idTransacao;
            }
            catch (Exception e)
            {
                throw new Exception(EnumHelper.GetDescriptionFromEnumValue(TipoErroEnum.GetSession));
            }
        }

        public string Md5ComputeHash(string strSenha)
        {
            string strSenhacrypt = "";
            string[] tabStringHexa = new string[16];
            MD5 md5 = new MD5CryptoServiceProvider();

            byte[] arrSenhacrypt = md5.ComputeHash(Encoding.ASCII.GetBytes(strSenha));

            for (int i = 0; i < arrSenhacrypt.Length; i++)
            {
                if (arrSenhacrypt[i] > 15)
                    tabStringHexa[i] = (arrSenhacrypt[i]).ToString("X");
                else
                    tabStringHexa[i] = '0' + (arrSenhacrypt[i]).ToString("X");

                strSenhacrypt += tabStringHexa[i];
            }
            return strSenhacrypt;
        }

        public DadosPid ConsultarPedidoIdTransacao(string idTransacao)
        {
            try
            {
                DadosPid dadosPid = new DadosPid();

                using (var db = new IdDigitalDbContext())
                {
                    using (var command = db.Database.GetDbConnection().CreateCommand())
                    {
                        var query = $@"SELECT ci.nu_pid, ci.nu_ric, ci.nu_vias, id.tp_sexo, ci.id_corpo, ci.nu_ano, dp.sq_transacao 
                                       FROM imagem.corpos_index ci, civil.identificacoes id, id_digital.pedidos dp 
                                         where ci.nu_ric = id.nu_ric 
                                          and ci.nu_vias = id.nu_vias 
                                          and ci.nu_ric = dp.nu_ric 
                                          and ci.nu_vias = dp.nu_vias 
                                          and dp.id_transacao = '{idTransacao}'
                                          ORDER BY ci.id_corpo DESC";

                        command.CommandText = query;
                        command.CommandType = CommandType.Text;

                        db.Database.OpenConnection();

                        using (var resultPid = command.ExecuteReader())
                        {
                            if (resultPid.Read())
                            {
                                dadosPid.Ric = resultPid["nu_ric"].ToString();
                                dadosPid.Vias = Convert.ToByte(resultPid["nu_vias"].ToString());
                                dadosPid.Corpo = Convert.ToInt32(resultPid["id_corpo"].ToString());
                                dadosPid.Ano = Convert.ToInt16(resultPid["nu_ano"].ToString());
                                dadosPid.Sexo = Convert.ToInt16(resultPid["tp_sexo"].ToString());
                                dadosPid.Pid = Convert.ToInt64(resultPid["nu_pid"].ToString());
                                dadosPid.Transacao = Convert.ToInt64(resultPid["sq_transacao"].ToString());
                            }
                        }
                    }
                }
                return dadosPid;
            }
            catch (Exception e)
            {
                throw new Exception(EnumHelper.GetDescriptionFromEnumValue(TipoErroEnum.SemDadosPid));
            }
        }

        /// <summary>
        /// Pega_DadosTpGrafico
        /// </summary>
        /// <param name="pid"></param>
        /// <param name="solicitacao"></param>
        /// <returns></returns>
        public DadosTipoGrafico ConsultarDadosTipoGrafico(string pid, Solicitacao solicitacao)
        {
            try
            {
                using (var db = new IdDigitalDbContext())
                {
                    Identificacoes query;
                    if (string.IsNullOrEmpty(pid))
                    {
                        //tipografico fisico ex. AH12048330
                        if (short.TryParse(solicitacao.DeTpGrafico.Substring(solicitacao.DeTpGrafico.Length - 1, 1), out _))
                        {
                            query = (from i in db.Identificacoes
                                     where i.NuTipoGraficoFisico == solicitacao.DeTpGrafico
                                     select i).FirstOrDefault();
                        }
                        else
                        {
                            //espelho ex. RJ15008570E
                            var espelho = Convert.ToInt64(solicitacao.DeTpGrafico.Substring(2, solicitacao.DeTpGrafico.Length - 3));

                            query = (from i in db.Identificacoes
                                     where i.SgUfTipoGrafico == solicitacao.DeTpGrafico.Substring(0, 2) &&
                                           i.NuEspelho == espelho &&
                                           i.NuSerie == solicitacao.DeTpGrafico.Substring(solicitacao.DeTpGrafico.Length - 1, 1)
                                     select i).FirstOrDefault();
                        }
                    }
                    else
                    {
                        var pidParse = Convert.ToInt64(pid);

                        query = (from i in db.Identificacoes
                                 where i.NuPid == pidParse
                                 select i).FirstOrDefault();
                    }

                    if (query != null)
                    {
                        return new DadosTipoGrafico
                        {
                            DeTpGraficoFisico = query.NuTipoGraficoFisico,
                            DeTpGraficoNumero = query.NuEspelho,
                            DeTpGraficoSerie = query.NuSerie,
                            DeTpGraficoUf = query.SgUfTipoGrafico,
                            NuRic = query.NuRic,
                            NuVias = query.NuVias,
                            NuPid = query.NuPid
                        };
                    }
                    return null;
                }
            }
            catch (Exception)
            {
                throw new Exception(EnumHelper.GetDescriptionFromEnumValue(TipoErroEnum.BuscarDadosPid));
            }
        }

        public bool InserirPedidoQrCode(string pid, int sqTransacao, string idTransacao, Solicitacao solicitacao)
        {
            try
            {
                using (var db = new IdDigitalDbContext())
                {
                    var dadosTipoGrafico = ConsultarDadosTipoGrafico(pid, null);

                    var pedido = new Pedidos();
                    pedido.SqTransacao = sqTransacao;
                    pedido.IdTrasacao = idTransacao;
                    pedido.ImQrCode = new[] { Convert.ToByte(solicitacao.DeQrCode) };
                    pedido.NuCelLinha = Convert.ToInt32(solicitacao.Nulinha);
                    pedido.NuCelImei = solicitacao.NuImei;
                    pedido.DeCelIp = solicitacao.DeIp;
                    pedido.DeCelFabricante = solicitacao.DeFabricante;
                    pedido.DeCelModelo = solicitacao.DeModelo;
                    pedido.DeCelSerie = solicitacao.DeSerie;
                    pedido.DeCelSoVersao = solicitacao.DeSoVersao;
                    pedido.NuGpsLat = solicitacao.NuGpsLat;
                    pedido.NuGpsLong = solicitacao.NuGpsLong;
                    pedido.TpStatus = 1;
                    pedido.DeTipograficoUf = dadosTipoGrafico.DeTpGraficoUf;
                    pedido.DeTipograficoNumero = dadosTipoGrafico.DeTpGraficoNumero;

                    db.Pedidos.Add(pedido);
                    return db.SaveChanges() > 0;
                }
            }
            catch (Exception e)
            {
                new LogRepository().InserirLog(idTransacao, "InserirPedidoQrCode -> " + e.Message);

                throw new Exception(EnumHelper.GetDescriptionFromEnumValue(TipoErroEnum.InserirIdentificacao));
            }
        }

        public bool InserirPedidoBarCode(Solicitacao solicitacao)
        {
            string idTransacao = null;

            try
            {
                using (var db = new IdDigitalDbContext())
                {
                    var dadosTipoGrafico = ConsultarDadosTipoGrafico(null, solicitacao);

                    int sqTransacao;
                    using (var command = db.Database.GetDbConnection().CreateCommand())
                    {
                        var querySequence = "select id_digital.sq_pedidos.nextval from dual";

                        command.CommandText = querySequence;
                        command.CommandType = CommandType.Text;
                        db.Database.OpenConnection();

                        sqTransacao = Convert.ToInt32(command.ExecuteScalar());
                    }

                    // verificação de novas regras
                    VerificarPermissoesAcessoCarteira(DateTime.Now, dadosTipoGrafico.NuRic);

                    idTransacao = Md5ComputeHash(sqTransacao.ToString());

                    var pedido = new Pedidos();
                    pedido.SqTransacao = sqTransacao;
                    pedido.IdTrasacao = idTransacao;
                    pedido.ImQrCode = new[] {Convert.ToByte(solicitacao.DeQrCode)};
                    pedido.NuCelLinha = Convert.ToInt32(solicitacao.Nulinha);
                    pedido.NuCelImei = solicitacao.NuImei;
                    pedido.DeCelIp = solicitacao.DeIp;
                    pedido.DeCelFabricante = solicitacao.DeFabricante;
                    pedido.DeCelModelo = solicitacao.DeModelo;
                    pedido.DeCelSerie = solicitacao.DeSerie;
                    pedido.DeCelSoVersao = solicitacao.DeSoVersao;
                    pedido.NuGpsLat = solicitacao.NuGpsLat;
                    pedido.NuGpsLong = solicitacao.NuGpsLong;
                    pedido.TpStatus = 1;
                    pedido.DeTipograficoUf = dadosTipoGrafico.DeTpGraficoUf;
                    pedido.DeTipograficoNumero = dadosTipoGrafico.DeTpGraficoNumero;

                    db.Pedidos.Add(pedido);
                    db.SaveChanges();
                    return true;
                }
            }
            catch (Exception e)
            {
                new LogRepository().InserirLog(idTransacao, "InserirPedidoQrCode -> " + e.Message);

                throw new Exception(EnumHelper.GetDescriptionFromEnumValue(TipoErroEnum.InserirIdentificacao));
            }
        }

        public bool AtualizarEscore(string idTransacao, int score)
        {
            try
            {
                var dadosPid = ConsultarPedidoIdTransacao(idTransacao);

                using (var db = new IdDigitalDbContext())
                {
                    var query = (from i in db.Pedidos
                        where i.SqTransacao == dadosPid.Transacao
                        select i).FirstOrDefault();

                    if (query != null)
                    {
                        query.NuScore = score;
                        db.SaveChanges();
                        return true;
                    }
                }
                return false;
            }
            catch (Exception)
            {
                throw new Exception(EnumHelper.GetDescriptionFromEnumValue(TipoErroEnum.InserirImagemCarteira));
            }
        }

        public bool VerificarPermissoesAcessoCarteira(DateTime dtExpedicaoIdentidade, long nuRg)
        {
            try
            {
                using (var db = new IdDigitalDbContext())
                {
                    using (var command = db.Database.GetDbConnection().CreateCommand())
                    {
                        var query = $@"SELECT dt_nascimento, dt_validade_carteira, dt_expedicao_carteira, nu_vias 
                                    FROM civil.IDENTIFICACOES WHERE nu_vias = (select max(nu_vias) 
                                    FROM civil.IDENTIFICACOES where nu_ric = {nuRg} ) and NU_RIC = {nuRg} ";

                        command.CommandText = query;
                        command.CommandType = CommandType.Text;

                        db.Database.OpenConnection();

                        DateTime? dtNascimento = null;
                        DateTime? dtValidade = null;
                        DateTime? dtExpedicao = null;
                        int via = 1;

                        using (var result = command.ExecuteReader())
                        {
                            if (result.Read())
                            {
                                dtNascimento = result["dt_nascimento"] == DBNull.Value
                                    ? (DateTime?)null
                                    : Convert.ToDateTime(result["dt_nascimento"]);

                                dtValidade = result["dt_validade_carteira"] == DBNull.Value
                                    ? (DateTime?)null
                                    : Convert.ToDateTime(result["dt_validade_carteira"]);

                                dtExpedicao = result["dt_expedicao_carteira"] == DBNull.Value
                                    ? (DateTime?)null
                                    : Convert.ToDateTime(result["dt_expedicao_carteira"]);

                                via = Convert.ToInt16(result["nu_vias"]);
                            }

                            if (dtNascimento != null)
                            {
                                var anoNascimento = Convert.ToDateTime(dtNascimento).Year;
                                var anoInclusaoCarteira = dtExpedicaoIdentidade.Year;

                                if (anoInclusaoCarteira - anoNascimento <= 18)
                                {
                                    if (anoInclusaoCarteira - anoNascimento == 18)
                                    {
                                        var mesNascimento = Convert.ToDateTime(dtNascimento).Month;
                                        var mesInclusaoCarteira = dtExpedicaoIdentidade.Month;

                                        if (mesNascimento > mesInclusaoCarteira)
                                            throw new ArgumentException("Identidade Digital indiponível para menores de 18 anos");
                                    }
                                    else
                                        throw new ArgumentException("Identidade Digital indiponível para menores de 18 anos");
                                }
                            }

                            if (dtValidade != null)
                            {
                                var validadeCarteira = Convert.ToDateTime(dtValidade);

                                if (validadeCarteira < DateTime.Now)
                                {
                                    throw new ArgumentException(
                                        "Identidade Digital indisponível para carteira com validade vencida! Para ter acesso ao documento digital solicite a segunda via da sua Carteira de Identidade física.");
                                }
                            }

                            //Bloquear a Identidade Digital após 30 dias de emissão de uma nova carteira física;
                            //    · Após a emissão da nova carteira e até o 30º dia:
                            //Sua identidade digital será bloqueada em XX / XX / XXXX(indicar o 31º dia a contar da emissão da nova via).Efetue a sua substituição a partir da nova via da sua carteira física, emitida em XX/ XX / XXXX(indicar a data de emissão da nova via).
                            //    · A partir do 31º dia de emissão da nova via:
                            //Sua identidade digital foi bloqueada!Efetue a sua substituição a partir da nova via de sua carteira física, emitida em XX/ XX / XXXX(indicar a data de emissão da nova via).

                            if (dtExpedicao != null)
                            {
                                var expedicaoCarteira = Convert.ToDateTime(dtExpedicao);

                                if (expedicaoCarteira > dtExpedicaoIdentidade)
                                {
                                    throw new ArgumentException(
                                        "Identidade Digital bloqueada. Efetue a sua substituição a partir da nova via de sua carteira física, emitida em " +
                                        expedicaoCarteira.ToString("dd/MM/yyyy"));
                                }
                            }
                        }

                        //verifica duplicidade de RG
                        query = $@" select distinct p.nu_cric_pesq, p.nu_ric, d.nu_ric, 
                                    case when p.st_prevalecido = 1 then p.nu_ric 
                                        when d.st_prevalecido = 1 then d.nu_ric else 0 end as ricprevalecido, 
                                    case when p.nu_status = 30 then 'Prevalecimento RG' 
                                        when p.nu_status = 40 then 'Aguardando unificação' 
                                        when p.nu_status = 40 then 'Processo reaberto' 
                                        when p.nu_status > 89 then 'Processo Finalizado' end as status  
                                        from civil.cric_pesquisado p, civil.cric_duplicidade d
                                        where p.nu_cric_pesq = d.nu_cric_pesq
                                            and (p.nu_ric = {nuRg} or d.nu_ric = {nuRg}) 
                                            and (p.st_prevalecido = 1 or d.st_prevalecido = 1) 
                                            and d.nu_status > 20";

                        command.CommandText = query;
                        command.CommandType = CommandType.Text;

                        using (var result = command.ExecuteReader())
                        {
                            if (result.Read())
                            {
                                var status = result["status"];

                                if (status != DBNull.Value && (string)status != "Processo Finalizado")
                                {
                                    throw new ArgumentException(
                                        "Identidade Digital indisponível. Para concluí-la efetue o agendamento para solicitação da segunda via da sua carteira física.");
                                }
                            }

                        }

                        // carteira roubada/extraviada
                        query = $@"SELECT distinct P.NU_RIC, p.nu_vias, trunc(P.DT_EXPEDICAO_CARTEIRA) as DT_EXPEDICAO_CARTEIRA, p.DT_RETIRADA, E.DT_INCINERACAO, 
                                case when E.DT_INCINERACAO is not null and p.DT_RETIRADA is null then 'Incinerada' 
                                        when p.DT_RETIRADA is not null then 'Devolvida' 
                                        when TP_REGISTRO = 'E' then 'Extraviada' 
                                        when TP_REGISTRO = 'N' then 'Extraviada' 
                                        when TP_REGISTRO = 'F' then 'Furtada' 
                                        when TP_REGISTRO = 'C' then 'Inutilizada' 
                                        else 'Ativa' 
                                end SIT 
                                FROM CIVIL.CARTEIRAS_PERDIDAS P, CIVIL.ESPELHO_INCINERADO E, CIVIL.SITUACAO_VIA V 
                                WHERE p.NU_RIC = e.nu_ric(+) 
                                    and p.nu_vias = e.nu_vias(+) 
                                    and p.nu_ric = V.NU_RG(+) 
                                    and p.dt_expedicao_carteira = v.dt_expedicao(+) 
                                    and p.NU_RIC = {nuRg}
                                    and p.nu_vias = {via}";


                        command.CommandText = query;
                        command.CommandType = CommandType.Text;

                        using (var result = command.ExecuteReader())
                        {
                            if (result.Read())
                            {
                                var situacao = result["sit"];
                                throw new ArgumentException("Identidade Digital indisponível. Sua carteira foi " +
                                                    situacao.ToString().ToLower() +
                                                    ". Para obter a sua Identidade Digital solicite a segunda via de sua carteira física.");
                            }
                        }

                        // óbito
                        query = $@"SELECT nu_ric AS rg FROM civil.OBITOS WHERE nu_ric = {nuRg}
                                                    union
                                                    SELECT nu_rg AS rg FROM ifprj.OBITOS WHERE NU_RG = {nuRg}";
                        command.CommandText = query;
                        command.CommandType = CommandType.Text;

                        using (var result = command.ExecuteReader())
                        {
                            if (result.Read())
                            {
                                throw new ArgumentException(
                                    "Identidade Digital indisponível. Para concluir a sua solicitação compareça à Diretoria de Identificação Civil na sede do Detran–RJ.");
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                throw e;
            }
            return true;
        }

        public bool VerificarIdTransacaoProvaVida(string idTransacao)
        {
            try
            {
                using (var db = new IdDigitalDbContext())
                {
                    return (from i in db.Pedidos
                        where i.IdTrasacao == idTransacao
                        select i).Any();
                }
            }
            catch (Exception e)
            {
                throw new Exception(EnumHelper.GetDescriptionFromEnumValue(TipoErroEnum.VerificarIdTransacaoProvaVida));
            }
        }
    }
}
