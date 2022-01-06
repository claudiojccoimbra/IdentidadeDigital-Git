using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using IdentidadeDigital.Infra.Domain;
using IdentidadeDigital.Infra.Domain.Enums;
using IdentidadeDigital.Infra.Helper;
using IdentidadeDigital.Infra.Model;
using IdentidadeDigital.Infra.Model.IdDigital;
using IdentidadeDigital.Infra.Repository.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace IdentidadeDigital.Infra.Repository
{
    public class IdentidadesRepository : RepositoryBase<Identidades, IdDigitalDbContext>
    {
        private readonly IConfiguration _configuration;

        public IdentidadesRepository(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public Cidadao ConsultarDadosRic(string idTransacao)
        {
            var cidadao = new Cidadao();
            var dadosPid = new DadosPid();
            //string nuPid = "33001912370";
            //string nuRic = "200457760";

            try
            {
                var repository = new PedidosRepository();
                dadosPid = repository.ConsultarPedidoIdTransacao(idTransacao);


                using (var db = new IdDigitalDbContext())
                {
                    using (var command = db.Database.GetDbConnection().CreateCommand())
                    {
                        var query = string.Format(@"Select
                                ID.NU_PID,
                                ID.TP_VALIDADE_CARTEIRA,
                                ID.Nu_Ric,
                                ID.tp_sexo,
                                to_char(id.dt_expedicao_carteira,'dd/mm/yyyy') dt_expedicao_carteira,
                                 ID.co_profissao,
                                 ID.tp_doador,
                                 ID.de_fatorrh,
                                 upper(ID.de_tiposanguineo) de_tiposanguineo,
                                 ID.Nu_Vias,
                                 ID.Nu_PostoOrigem,
                                 ID.Tp_Pedido,
                                 TO_CHAR(ID.Dt_Validade_Carteira,'DD/MM/YYYY') Dt_Validade_Carteira,
                                 ID.Nu_CIS,
                                 upper(ID.No_Social) No_Social,
                                 upper(ID.No_Cidadao) No_Cidadao,
                                 upper(ID.No_PaiCidadao) No_PaiCidadao,
                                 upper(ID.No_MaeCidadao) No_MaeCidadao,
                                 DECODE(ID.Dt_Nascimento, NULL, DECODE(ID.Dt_NascimentoAprox, NULL, 'XXXXXXXXXX', ID.Dt_NascimentoAprox), TO_CHAR(ID.Dt_Nascimento,'DD/MM/YYYY')) Dt_Nascimento,
                                 ID.Tp_Nacionalidade,
                                 MU.NO_MUNICIPIO_CI Naturalidade,
                                 (SELECT no_municipio_ci FROM detran.Municipios WHERE co_municipio = id.co_municipionascimento) Naturalidade_CI,
                                 upper(CA.Nu_CertidaoLivro) Nu_CertidaoLivro,
                                 upper(CA.Nu_CertidaoFolha) Nu_CertidaoFolha,
                                 upper(CA.Nu_CertidaoTermo) Nu_CertidaoTermo,
                                 upper(CA.Nu_CertidaoCircunscricao) Nu_CertidaoCircunscricao,
                                 upper(CA.NU_CERTIDAODISTRITO) Nu_CertidaoDistrito,
                                 upper(CA.NU_CERTIDAOSUBDISTRITO) Nu_CertidaoSubDistrito,
                                 upper(CA.NU_CERTIDAOZONA) Nu_CertidaoZona,
                                 upper(CA.Sg_UFCertidao) Sg_UFCertidao,
                                 CA.nu_matriculacertidao,
                                 lpad(DECODE(OO.Co_Orgao, 21, OO.Nu_RGOutroOrgao, null), 10, '0') Nu_RGOutroOrgao,
                                 NA.Co_Nacionalidade,
                                 NA.Nu_Portaria,
                                 NA.AA_Portaria,
                                 CA.Co_MunicipioCertidao,
                                 DECODE(SIGN(CA.Co_MunicipioCertidao - 9972), -1, MU.No_Municipio, NULL) Municipio_Certidao,
                                 DECODE(CA.Tp_Certidao, 1, '1', 2, '2', 3, '3', 4, '4',NULL, DECODE(NA.Nu_Portaria, NULL, '6', '5')) Certidao,
                                 ID.co_municipionascimento,
                                 ID.co_paisnascimento,
                                 ID.id_corpo,
                                 TO_CHAR(ID.dt_identificacao,'DDMMYYYY') dt_identificacao,
                                 ID.co_impossassinat,
                                 ID.tp_idoso,
                                 ID.CO_PNE,
                                 PID.no_maquina_scan,
                                 nvl(PID.EQ_CAMERADIGITAL,0) EQ_CAMERADIGITAL,
                                 nvl(PID.EQ_PADASSINATURA,0) EQ_PADASSINATURA,
                                 nvl(PID.EQ_LIVESCANNER,0)   EQ_LIVESCANNER,
                                 nvl(PID.FL_CONVERTE_TP_PEDIDO,0) FL_CONVERTE_TP_PEDIDO,
                                 nvl(ID.ST_CPF,1) ST_CPF,
                                 nvl(ID.ST_PISPASEP,1) ST_PISPASEP,
                                 (select upper(no_filiacao) from civil.fonema_via_multiparental where nu_ric = ID.nu_ric and nu_vias = ID.nu_vias and nu_seq = 1) no_filiacao1,
                                 (select upper(no_filiacao) from civil.fonema_via_multiparental where nu_ric = ID.nu_ric and nu_vias = ID.nu_vias and nu_seq = 2) no_filiacao2,
                                 (select upper(co_cid) from pid.cracha_pcd where nu_pid = {0} and nu_seq = 1) CID1,
                                 (select upper(co_cid) from pid.cracha_pcd where nu_pid = {0} and nu_seq = 2) CID2,
                                 (select upper(co_cid) from pid.cracha_pcd where nu_pid = {0} and nu_seq = 3) CID3,
                                 (select upper(co_cid) from pid.cracha_pcd where nu_pid = {0} and nu_seq = 4) CID4,
                                 ID.Nu_CPF,
                                 ID.Nu_PisPasep,
                                 ID.Tp_PisPasep,
                                 ID.NU_CNS,
                                 ID.NU_CNH,
                                 ID.NU_TITULOELEITOR,
                                 ID.NU_DNI,
                                 ID.NU_CTPS,
                                 ID.NU_SERIE_CTPS,
                                 ID.SG_UF_CTPS,
                                 ID.NU_CERT_MILITAR,
                                 ID.FL_CRACHA,
                                 ID.DE_IDENTPROFISSIONAL_1,
                                 ID.DE_IDENTPROFISSIONAL_2,
                                 ID.DE_IDENTPROFISSIONAL_3,
                                 ID.NU_ESPELHO,
                                 ID.NU_TIPOGRAFICO_FISICO 
                                From Civil.Identificacoes ID, Civil.Certidao_Apresentada CA, Civil.Identidades_Outro_Orgao OO,
                                     Civil.Nacionalidades NA, Detran.Municipios MU, Pid.Pedidos PID 
                                Where ID.Nu_RIC           = {1}
                                   AND ID.NU_PID          = {2}
                                   AND ID.Nu_PID          = PID.Nu_PID
                                   AND OO.Nu_Ric(+)       = ID.Nu_Ric
                                   AND OO.Nu_Vias(+)      = ID.Nu_Vias
                                   AND NA.Nu_Ric(+)       = ID.Nu_Ric
                                   AND NA.Nu_Vias(+)      = ID.Nu_Vias
                                   AND ID.Nu_Ric          = CA.Nu_Ric(+)
                                   AND ID.Nu_Vias         = CA.Nu_Vias(+)
                                   AND MU.Co_Municipio(+) = CA.Co_MunicipioCertidao
                                   AND id.nu_vias = (select max(nu_vias) from civil.identificacoes where nu_ric = {3}) ", dadosPid.Pid, dadosPid.Ric, dadosPid.Pid, dadosPid.Ric);

                        command.CommandText = query;
                        command.CommandType = CommandType.Text;

                        db.Database.OpenConnection();

                        using (var result = command.ExecuteReader())
                        {
                            if (result.Read())
                            {
                                cidadao.NuRic = Convert.ToString(result["nu_ric"]);
                                cidadao.NuVia = Convert.ToString(result["nu_vias"]);
                                cidadao.NuPid = Convert.ToString(result["nu_pid"]);
                                cidadao.NoCidadao = Convert.ToString(result["no_cidadao"]).ToUpper().Trim();

                                cidadao.NoSocial = "";
                                if (!string.IsNullOrEmpty(Convert.ToString(result["no_social"])))
                                    cidadao.NoSocial = Convert.ToString(result["no_social"]).ToUpper().Trim();

                                cidadao.NoPaiCidadao = "";
                                if (!string.IsNullOrEmpty(Convert.ToString(result["no_paicidadao"])))
                                    cidadao.NoPaiCidadao = Convert.ToString(result["no_paicidadao"]).ToUpper().Trim();

                                cidadao.NoMaeCidadao = "";
                                if (!string.IsNullOrEmpty(Convert.ToString(result["no_maecidadao"])))
                                    cidadao.NoMaeCidadao = Convert.ToString(result["no_maecidadao"]).ToUpper().Trim();

                                cidadao.DtNascimento = "";
                                if (!string.IsNullOrEmpty(Convert.ToString(result["dt_nascimento"])))
                                    cidadao.DtNascimento = Convert.ToString(result["dt_nascimento"]);

                                cidadao.Naturalidade = "";
                                if (!string.IsNullOrEmpty(Convert.ToString(result["Naturalidade_CI"])))
                                    cidadao.Naturalidade = Convert.ToString(result["Naturalidade_CI"]);

                                cidadao.TipoSanguineo = "";
                                if (!string.IsNullOrEmpty(Convert.ToString(result["DE_TIPOSANGUINEO"])))
                                    cidadao.TipoSanguineo = Convert.ToString(result["DE_TIPOSANGUINEO"]);

                                cidadao.FatorRh = "";
                                if (!string.IsNullOrEmpty(Convert.ToString(result["de_fatorrh"])))
                                    cidadao.FatorRh = Convert.ToString(result["de_fatorrh"]);

                                cidadao.Cpf = "";
                                if (!string.IsNullOrEmpty(Convert.ToString(result["nu_cpf"])))
                                    cidadao.Cpf = Convert.ToString(result["nu_cpf"]);

                                cidadao.Dni = "";
                                if (!string.IsNullOrEmpty(Convert.ToString(result["NU_DNI"])))
                                    cidadao.Dni = Convert.ToString(result["NU_DNI"]);

                                cidadao.DtExpedicao = "";
                                if (!string.IsNullOrEmpty(Convert.ToString(result["dt_expedicao_carteira"])))
                                    cidadao.DtExpedicao = Convert.ToString(result["dt_expedicao_carteira"]);

                                cidadao.NuMatriculaCertidao = "";
                                if (!string.IsNullOrEmpty(Convert.ToString(result["nu_matriculacertidao"])))
                                    cidadao.NuMatriculaCertidao = Convert.ToString(result["nu_matriculacertidao"]);

                                cidadao.TpCertidao = "";
                                if (!string.IsNullOrEmpty(Convert.ToString(result["certidao"])))
                                    cidadao.TpCertidao = Convert.ToString(result["certidao"]);

                                switch (cidadao.TpCertidao)
                                {
                                    case "1":
                                        cidadao.DescricaoCertidao = "C.NASC";
                                        break;
                                    case "2":
                                        cidadao.DescricaoCertidao = "C.CASM";
                                        break;
                                    default:
                                        cidadao.DescricaoCertidao = "";
                                        break;
                                }

                                cidadao.NuCertidaoLivro = "";
                                if (!string.IsNullOrEmpty(Convert.ToString(result["nu_certidaolivro"])))
                                    cidadao.NuCertidaoLivro = Convert.ToString(result["nu_certidaolivro"]);

                                cidadao.NuCertidaoFolha = "";
                                if (!string.IsNullOrEmpty(Convert.ToString(result["nu_certidaofolha"])))
                                    cidadao.NuCertidaoFolha = Convert.ToString(result["nu_certidaofolha"]);

                                cidadao.NuCertidaoTermo = "";
                                if (!string.IsNullOrEmpty(Convert.ToString(result["nu_certidaotermo"])))
                                    cidadao.NuCertidaoTermo = Convert.ToString(result["nu_certidaotermo"]);

                                cidadao.NuCertidaoCircunscricao = "";
                                if (!string.IsNullOrEmpty(Convert.ToString(result["nu_certidaocircunscricao"])))
                                    cidadao.NuCertidaoCircunscricao = Convert.ToString(result["nu_certidaocircunscricao"]);

                                cidadao.NuCertidaoDistrito = "";
                                if (!string.IsNullOrEmpty(Convert.ToString(result["nu_certidaodistrito"])))
                                    cidadao.NuCertidaoDistrito = Convert.ToString(result["nu_certidaodistrito"]);

                                cidadao.NuCertidaoSubDistrito = "";
                                if (!string.IsNullOrEmpty(Convert.ToString(result["nu_certidaosubdistrito"])))
                                    cidadao.NuCertidaoSubDistrito = Convert.ToString(result["nu_certidaosubdistrito"]);

                                cidadao.NuCertidaoZona = "";
                                if (!string.IsNullOrEmpty(Convert.ToString(result["nu_certidaozona"])))
                                    cidadao.NuCertidaoZona = Convert.ToString(result["nu_certidaozona"]);

                                cidadao.NoMunicipioNascimento = "";
                                if (!string.IsNullOrEmpty(Convert.ToString(result["Municipio_Certidao"])))
                                    cidadao.NoMunicipioNascimento = Convert.ToString(result["Municipio_Certidao"]);

                                cidadao.SgUfNascimento = "";
                                if (!string.IsNullOrEmpty(Convert.ToString(result["Sg_UFCertidao"])))
                                    cidadao.SgUfNascimento = Convert.ToString(result["Sg_UFCertidao"]);

                                cidadao.NuPortaria = "";
                                if (!string.IsNullOrEmpty(Convert.ToString(result["nu_portaria"])))
                                    cidadao.NuPortaria = Convert.ToString(result["nu_portaria"]);

                                cidadao.AnoPortaria = "";
                                if (!string.IsNullOrEmpty(Convert.ToString(result["aa_portaria"])))
                                    cidadao.AnoPortaria = Convert.ToString(result["aa_portaria"]);

                                cidadao.Nacionalidade = "";
                                if (!string.IsNullOrEmpty(Convert.ToString(result["co_nacionalidade"])))
                                    cidadao.Nacionalidade = Convert.ToString(result["co_nacionalidade"]);

                                cidadao.DtValidade = "";
                                if (!string.IsNullOrEmpty(Convert.ToString(result["dt_validade_carteira"])))
                                    cidadao.DtValidade = Convert.ToString(result["dt_validade_carteira"]);

                                cidadao.TituloEleitor = "";
                                if (!string.IsNullOrEmpty(Convert.ToString(result["NU_TITULOELEITOR"])))
                                    cidadao.TituloEleitor = Convert.ToString(result["NU_TITULOELEITOR"]);

                                cidadao.NuCtps = "";
                                if (!string.IsNullOrEmpty(Convert.ToString(result["NU_CTPS"])))
                                    cidadao.NuCtps = Convert.ToString(result["NU_CTPS"]);

                                cidadao.SerieCtps = "";
                                if (!string.IsNullOrEmpty(Convert.ToString(result["NU_SERIE_CTPS"])))
                                    cidadao.SerieCtps = Convert.ToString(result["NU_SERIE_CTPS"]);

                                cidadao.Ufctps = "";
                                if (!string.IsNullOrEmpty(Convert.ToString(result["SG_UF_CTPS"])))
                                    cidadao.Ufctps = Convert.ToString(result["SG_UF_CTPS"]);

                                cidadao.Nispispasep = "";
                                if (!string.IsNullOrEmpty(Convert.ToString(result["Nu_PisPasep"])))
                                    cidadao.Nispispasep = Convert.ToString(result["Nu_PisPasep"]);

                                cidadao.IdentProfissional1 = "";
                                if (!string.IsNullOrEmpty(Convert.ToString(result["DE_IDENTPROFISSIONAL_1"])))
                                    cidadao.IdentProfissional1 = Convert.ToString(result["DE_IDENTPROFISSIONAL_1"]);

                                cidadao.IdentProfissional2 = "";
                                if (!string.IsNullOrEmpty(Convert.ToString(result["DE_IDENTPROFISSIONAL_2"])))
                                    cidadao.IdentProfissional2 = Convert.ToString(result["DE_IDENTPROFISSIONAL_2"]);

                                cidadao.IdentProfissional3 = "";
                                if (!string.IsNullOrEmpty(Convert.ToString(result["DE_IDENTPROFISSIONAL_3"])))
                                    cidadao.IdentProfissional3 = Convert.ToString(result["DE_IDENTPROFISSIONAL_3"]);

                                cidadao.CertificadoMilitar = "";
                                if (!string.IsNullOrEmpty(Convert.ToString(result["NU_CERT_MILITAR"])))
                                    cidadao.CertificadoMilitar = Convert.ToString(result["NU_CERT_MILITAR"]);

                                cidadao.Cnh = "";
                                if (!string.IsNullOrEmpty(Convert.ToString(result["NU_CNH"])))
                                    cidadao.Cnh = Convert.ToString(result["NU_CNH"]);

                                cidadao.Cns = "";
                                if (!string.IsNullOrEmpty(Convert.ToString(result["NU_CNS"])))
                                    cidadao.Cns = Convert.ToString(result["NU_CNS"]);

                                cidadao.Pcd = "";
                                if (!string.IsNullOrEmpty(Convert.ToString(result["CO_PNE"])))
                                    cidadao.Pcd = Convert.ToString(result["CO_PNE"]);

                                cidadao.Cid1 = "";
                                if (!string.IsNullOrEmpty(Convert.ToString(result["CID1"])))
                                    cidadao.Cid1 = Convert.ToString(result["CID1"]);

                                cidadao.Cid2 = "";
                                if (!string.IsNullOrEmpty(Convert.ToString(result["CID2"])))
                                    cidadao.Cid2 = Convert.ToString(result["CID2"]);

                                cidadao.Cid3 = "";
                                if (!string.IsNullOrEmpty(Convert.ToString(result["CID3"])))
                                    cidadao.Cid3 = Convert.ToString(result["CID3"]);

                                cidadao.Cid4 = "";
                                if (!string.IsNullOrEmpty(Convert.ToString(result["CID4"])))
                                    cidadao.Cid4 = Convert.ToString(result["CID4"]);

                                cidadao.NuPosto = "";
                                if (!string.IsNullOrEmpty(Convert.ToString(result["Nu_PostoOrigem"])))
                                    cidadao.NuPosto = Convert.ToString(result["Nu_PostoOrigem"]);

                                cidadao.NuEspelho = "";
                                if (!string.IsNullOrEmpty(Convert.ToString(result["nu_espelho"])))
                                    cidadao.NuEspelho = Convert.ToString(result["nu_espelho"]);

                                cidadao.NuTipografico = "";
                                if (!string.IsNullOrEmpty(Convert.ToString(result["NU_TIPOGRAFICO_FISICO"])))
                                    cidadao.NuTipografico = Convert.ToString(result["NU_TIPOGRAFICO_FISICO"]);

                                cidadao.MultiParental1 = "";
                                if (!string.IsNullOrEmpty(Convert.ToString(result["no_filiacao1"])))
                                    cidadao.MultiParental1 = Convert.ToString(result["no_filiacao1"]);

                                cidadao.MultiParental2 = "";
                                if (!string.IsNullOrEmpty(Convert.ToString(result["no_filiacao2"])))
                                    cidadao.MultiParental2 = Convert.ToString(result["no_filiacao2"]);

                                cidadao.ImpossAssinar = "";
                                if (!string.IsNullOrEmpty(Convert.ToString(result["co_impossassinat"])))
                                    cidadao.ImpossAssinar = Convert.ToString(result["co_impossassinat"]);

                                //carteiras permitidas a partir de 5 abril 2019
                                if (!string.IsNullOrEmpty(cidadao.DtExpedicao))
                                {
                                    var dtEmissao = Convert.ToDateTime(cidadao.DtExpedicao);
                                    var limiteEmissao = new DateTime(2019, 4, 5);

                                    if (dtEmissao < limiteEmissao)
                                    {
                                        throw new Exception(EnumHelper.GetDescriptionFromEnumValue(TipoErroEnum.CarteiraDigitalIndiponivel));
                                    }
                                }
                            }
                            // método duplicado?
                            // Método antigo SelectPedido(string nuRg)
                            //var queryPid = $@"Select ci.nu_pid, ci.nu_ric, ci.nu_vias, id.tp_sexo, ci.id_corpo, ci.nu_ano 
                            //                    FROM imagem.corpos_index ci, civil.identificacoes id 
                            //                    where ci.nu_ric = id.nu_ric 
                            //                     and ci.nu_vias = id.nu_vias 
                            //                     and ci.nu_ric = {dadosPid.Ric}
                            //                     ORDER BY ci.id_corpo DESC";

                            //command.CommandText = queryPid;
                            //command.CommandType = CommandType.Text;

                            //try
                            //{
                            //    using (var resultPid = command.ExecuteReader())
                            //    {
                            //        if (resultPid.Read())
                            //        {
                            //            dadosPid.Ric = resultPid["nu_ric"].ToString();
                            //            dadosPid.Vias = Convert.ToByte(resultPid["nu_vias"].ToString());
                            //            dadosPid.Corpo = Convert.ToInt32(resultPid["id_corpo"].ToString());
                            //            dadosPid.Ano = Convert.ToInt16(resultPid["nu_ano"].ToString());
                            //            dadosPid.Sexo = Convert.ToInt16(resultPid["tp_sexo"].ToString());
                            //            dadosPid.Pid = Convert.ToInt64(resultPid["nu_pid"].ToString());
                            //        }
                            //    }
                            //}
                            //catch (Exception)
                            //{
                            //    throw new Exception(EnumHelper.GetDescriptionFromEnumValue(TipoErroEnum.SemDadosPid));
                            //}

                            try
                            {
                                // Método antigo selectImage(int? corpo, int? ano)
                                var queryImage = $@"Select im_foto from imagem.corpos_{dadosPid.Ano} where id_corpo = {dadosPid.Corpo}";

                                command.CommandText = queryImage;
                                command.CommandType = CommandType.Text;

                                byte[] byteImg;
                                object image = command.ExecuteScalar();

                                if (image == null || ReferenceEquals(image, DBNull.Value))
                                {
                                    byteImg = new byte[1];
                                    byteImg[0] = 1;
                                }
                                else
                                    byteImg = (byte[])image;

                                cidadao.FotoCivil64 = byteImg;
                                cidadao.FotoCivil = Convert.ToBase64String(byteImg);
                            }
                            catch (Exception)
                            {
                                throw new Exception(EnumHelper.GetDescriptionFromEnumValue(TipoErroEnum.CarregarImagemFoto));
                            }

                            try
                            {
                                // Método antigo LerPolegarDireito(string corpo, string ano, string srg)
                                var queryImage = $@"Select Im_Dedo_01 from imagem.corpos_{dadosPid.Ano} where id_corpo = {dadosPid.Corpo}";

                                command.CommandText = queryImage;
                                command.CommandType = CommandType.Text;

                                byte[] byteImg;
                                object image = command.ExecuteScalar();

                                if (image == null || ReferenceEquals(image, DBNull.Value))
                                {
                                    byteImg = new byte[1];
                                    byteImg[0] = 1;
                                }
                                else
                                    byteImg = (byte[])image;

                                if (byteImg.Length > 1)
                                {
                                    WsqDecoder wsq = new WsqDecoder();
                                    var bitmap = wsq.Decode(byteImg);

                                    using (var stream = new MemoryStream())
                                    {
                                        bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Jpeg);
                                        byteImg = stream.ToArray();
                                    }
                                }
                                else
                                    byteImg = File.ReadAllBytes(_configuration.GetSection("Diretorio").GetSection("path").Value + "\\semfoto.jpg");

                                cidadao.PolegarDireito64 = byteImg;
                                cidadao.PolegarDireito = Convert.ToBase64String(byteImg);
                            }
                            catch (Exception)
                            {
                                throw new Exception(EnumHelper.GetDescriptionFromEnumValue(TipoErroEnum.CarregarImagemFoto));
                            }

                            try
                            {
                                // Método antigo LerAssinatura(string corpo, string ano, string srg)
                                var queryImage = $@"Select im_assinatura from imagem.corpos_{dadosPid.Ano} where id_corpo = {dadosPid.Corpo}";

                                command.CommandText = queryImage;
                                command.CommandType = CommandType.Text;

                                byte[] byteImg;
                                object image = command.ExecuteScalar();

                                if (image == null || ReferenceEquals(image, DBNull.Value))
                                    byteImg = File.ReadAllBytes(_configuration.GetSection("Diretorio").GetSection("path").Value + "\\semfoto.jpg");
                                else
                                    byteImg = (byte[])image;

                                cidadao.Assinatura64 = byteImg;
                                cidadao.Assinatura = Convert.ToBase64String(byteImg);
                            }
                            catch (Exception)
                            {
                                throw new Exception(EnumHelper.GetDescriptionFromEnumValue(TipoErroEnum.CarregarAssinatura));
                            }

                            var chancela = new ChancelaRepository().CarregarChancela(Convert.ToDateTime(cidadao.DtExpedicao));

                            cidadao.Chancela64 = chancela;
                            cidadao.Chancela = Convert.ToBase64String(chancela);
                        }
                    }
                    return cidadao;
                }
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        public StatusCarteira ChecarStatusCarteira(string idTransacao)
        {
            try
            {
                using (var db = new IdDigitalDbContext())
                {
                    var query = (from i in db.Identidades
                                 join p in db.Pedidos on i.SqTransacao equals p.SqTransacao
                                 where p.IdTrasacao == idTransacao
                                 select new StatusCarteira
                                 {
                                     DtInclusao = p.DtInclusao,
                                     TpStatusId = i.TpStatusId,
                                     NuRic = i.NuRic,
                                     SqTransacao = i.SqTransacao,
                                     DeStatus = EnumHelper.GetDescriptionFromEnumValue((TipoStatusIdEnum)Enum.ToObject(typeof(TipoStatusIdEnum), i.TpStatusId))
                                 }).FirstOrDefault();

                    return query ?? new StatusCarteira{ Erro = "Dados não encontrados" };
                }
            }
            catch (Exception)
            {
                throw new Exception(EnumHelper.GetDescriptionFromEnumValue(TipoErroEnum.VerificarStatusCarteira));
            }
        }

        public bool InserirIdentidade(Carteira carteira, string idTransacao)
        {
            try
            {
                var dadosPid = new PedidosRepository().ConsultarPedidoIdTransacao(idTransacao);

                using (var db = new IdDigitalDbContext())
                {
                    int sqIdentidade;

                    using (var command = db.Database.GetDbConnection().CreateCommand())
                    {
                        var querySequence = "select id_digital.sq_identidade.nextval from dual";

                        command.CommandText = querySequence;
                        command.CommandType = CommandType.Text;
                        db.Database.OpenConnection();

                        sqIdentidade = Convert.ToInt32(command.ExecuteScalar());
                    }

                    var identidade = new Identidades();

                    identidade.SqIdentidade = sqIdentidade;
                    identidade.NuRic = Convert.ToInt32(dadosPid.Ric);
                    identidade.NuVias = dadosPid.Vias;
                    identidade.NuPid = dadosPid.Pid;
                    identidade.SqTransacao = dadosPid.Transacao;
                    identidade.TpStatusId = (int)TipoStatusIdEnum.Solicitado;
                    identidade.ImFrente = new[] { Convert.ToByte(carteira.CarteiraFrente)}; // Convert.FromBase64String
                    identidade.ImVerso = new[] { Convert.ToByte(carteira.CarteiraVerso)};

                    db.Identidades.Add(identidade);
                    db.SaveChanges();
                    return true;
                }
            }
            catch (Exception)
            {
                throw new Exception(EnumHelper.GetDescriptionFromEnumValue(TipoErroEnum.InserirImagemCarteira));
            }
        }

        public byte[] ConsultarImagem(string idTransacao)
        {
            try
            {
                var dadosPid = new PedidosRepository().ConsultarPedidoIdTransacao(idTransacao);

                using (var db = new IdDigitalDbContext())
                {
                    using (var command = db.Database.GetDbConnection().CreateCommand())
                    {
                        // Método antigo selectImage(int? corpo, int? ano)
                        var queryImage =
                            $@"Select im_foto from imagem.corpos_{dadosPid.Ano} where id_corpo = {dadosPid.Corpo}";

                        command.CommandText = queryImage;
                        command.CommandType = CommandType.Text;
                        db.Database.OpenConnection();

                        byte[] byteImg;
                        object image = command.ExecuteScalar();

                        if (image == null || ReferenceEquals(image, DBNull.Value))
                        {
                            byteImg = new byte[1];
                            byteImg[0] = 1;
                        }
                        else
                            byteImg = (byte[]) image;

                        return byteImg;
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception(EnumHelper.GetDescriptionFromEnumValue(TipoErroEnum.ConsultarFotoCarteira));
            }
        }

        public Carteira ConsultarIdentidade(string idTransacao)
        {
            var carteira = new Carteira();

            try
            {
                var dadoPid = new PedidosRepository().ConsultarPedidoIdTransacao(idTransacao);

                using (var db = new IdDigitalDbContext())
                {
                    var query = (from i in db.Identidades
                        where i.SqTransacao == dadoPid.Transacao
                        select i).FirstOrDefault();

                    if (query != null)
                    {
                        carteira.Transacao = idTransacao;
                        carteira.Ric = query.NuRic.ToString();
                        carteira.Escore = query.NuScore.ToString();
                        carteira.CarteiraFrente = Convert.ToBase64String(query.ImFrente);
                        carteira.CarteiraVerso = Convert.ToBase64String(query.ImVerso);
                    }
                }

                return carteira;
            }
            catch (Exception e)
            {
                throw new Exception(EnumHelper.GetDescriptionFromEnumValue(TipoErroEnum.CarregamentoDadosCidadao));
            }
        }

        public bool AtualizarStatusIdentidade(string idTransacao, int statusId)
        {
            try
            {
                var dadosPid = new PedidosRepository().ConsultarPedidoIdTransacao(idTransacao);

                using (var db = new IdDigitalDbContext())
                {
                    var query = (from i in db.Identidades
                        where i.SqTransacao == dadosPid.Transacao
                        select i).FirstOrDefault();

                    if (query != null)
                    {
                        query.TpStatusId = statusId;
                        db.SaveChanges();
                        return true;
                    }
                    return false;
                }
            }
            catch (Exception)
            {
                throw new Exception(EnumHelper.GetDescriptionFromEnumValue(TipoErroEnum.AtualizarStatusIdentidade));
            }
        }

    }
}
