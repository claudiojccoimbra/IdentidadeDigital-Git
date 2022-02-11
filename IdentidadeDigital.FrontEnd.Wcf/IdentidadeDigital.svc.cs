using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;
using System.Threading.Tasks;
using IdentidadeDigital.FrontEnd.Wcf.DataContracts;
using IdentidadeDigital.FrontEnd.Wcf.Interface;
using Montreal.Biometric.ZFace;
using Newtonsoft.Json;

namespace IdentidadeDigital.FrontEnd.Wcf
{
    public class ServiceIdentidadeDigital : IServiceIdentidadeDigital
    {
        private static readonly HttpClient HttpClient = new HttpClient();
        private readonly string _dataBaseWebApi = ConfigurationManager.AppSettings["IdentidadeDigitalDataBase"];

        public async Task<StatusCarteira> ChecarStatusCarteira(string idTransacao)
        {
            var statusCarteira = new StatusCarteira();

            try
            {

                HttpResponseMessage response = await HttpClient.GetAsync(_dataBaseWebApi + "ChecarStatusCarteira/" + idTransacao);

                if (response.IsSuccessStatusCode)
                {
                    statusCarteira = await response.Content.ReadAsAsync<StatusCarteira>();
                }
                return statusCarteira;
            }
            catch (TimeoutException e)
            {
                statusCarteira.Erro = "Serviço temporariamente indisponível. Tente novamente";
                return statusCarteira;
            }
            catch (CommunicationException e)
            {
                statusCarteira.Erro = "Serviço temporariamente indisponível. Tente novamente";
                return statusCarteira;
            }
            catch (Exception e)
            {
                statusCarteira.Erro = e.Message;
                return statusCarteira;
            }
        }

        public async Task<VersaoApp> ChecarVersao(string versao)
        {
            var versaoApp = new VersaoApp();

            try
            {
                HttpResponseMessage response = await HttpClient.GetAsync(_dataBaseWebApi + "checarversao/" + versao);

                if (response.IsSuccessStatusCode)
                {
                    versaoApp = await response.Content.ReadAsAsync<VersaoApp>();
                }
                return versaoApp;
            }
            catch (TimeoutException e)
            {
                versaoApp.Erro = "Serviço temporariamente indisponível. Tente novamente";
                return versaoApp;
            }
            catch (CommunicationException e)
            {
                versaoApp.Erro = "Serviço temporariamente indisponível. Tente novamente";
                return versaoApp;
            }
            catch (Exception e)
            {
                versaoApp.Erro = e.Message;
                return versaoApp;
            }
        }

        public async Task<QrCode> ValidarQrCode(clsSolicitacao cls)
        {
            var qrCode = new QrCode();

            var sessionSqTransacao = string.Empty;
            var sessionIdTransacao = string.Empty;

            try
            {
                HttpResponseMessage response = await HttpClient.GetAsync(_dataBaseWebApi + "GetSession");

                if (response.IsSuccessStatusCode)
                {
                    var sessionId = await response.Content.ReadAsStringAsync();
                    var id = sessionId.Split(';');

                    if (id.Length > 0)
                    {
                        sessionSqTransacao = id[0];
                        sessionIdTransacao = id[1];
                    }

                    StreamWriter sw = File.CreateText(ConfigurationManager.AppSettings["CertificaCarteiraPath"] + sessionIdTransacao + ".txt");
                    sw.Write(cls.DeQrCode);
                    sw.Close();

                    using (Process pd = Process.GetCurrentProcess())
                    {
                        string sarg = ConfigurationManager.AppSettings["CertificaCarteiraPath"] + sessionIdTransacao + ".txt " + ConfigurationManager.AppSettings["CertificaCarteiraPath"] +
                                      sessionIdTransacao + ".json " + ConfigurationManager.AppSettings["CertificaCarteiraPath"] + sessionIdTransacao + ".jpg";

                        pd.StartInfo.UseShellExecute = false;
                        pd.StartInfo.FileName = ConfigurationManager.AppSettings["CertificaCarteiraPath"] + "CertificaCarteira2.exe";
                        pd.StartInfo.Arguments = sarg;

                        pd.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                        pd.StartInfo.RedirectStandardError = false;
                        pd.StartInfo.RedirectStandardInput = false;
                        pd.StartInfo.RedirectStandardOutput = false;
                        pd.PriorityClass = ProcessPriorityClass.High;

                        pd.Start();
                        pd.WaitForExit();
                        pd.Dispose();
                    }

                    var permissaoAcesso = await VerificarPermissoesQrCodeAcessoCarteira(sessionIdTransacao);

                    if (bool.TryParse(permissaoAcesso, out _))
                    {
                        // atribuição para que o método de inserir tenha somente uma classe
                        cls.SqTransacao = sessionSqTransacao;
                        cls.IdTransacao = sessionIdTransacao;

                        var inserirPedidoConfeccaoGeracaoCarteira = await InserirPedidoQrCode(cls);

                        if (!bool.TryParse(inserirPedidoConfeccaoGeracaoCarteira, out _))
                            throw new Exception(inserirPedidoConfeccaoGeracaoCarteira);
                    }
                    else
                        throw new Exception(permissaoAcesso);

                    qrCode.Codigo = sessionIdTransacao;
                }

                return qrCode;
            }
            catch (TimeoutException e)
            {
                qrCode.Erro = "Serviço temporariamente indisponível. Tente novamente";
                return qrCode;
            }
            catch (CommunicationException e)
            {
                qrCode.Erro = "Serviço temporariamente indisponível. Tente novamente";
                return qrCode;
            }
            catch (Exception e)
            {
                qrCode.Erro = e.Message;
                return qrCode;
            }
        }

        public async Task<BarCode> ValidarBarCode(clsSolicitacao cls)
        {
            var barcode = new BarCode();

            try
            {
                cls.Nulinha = string.IsNullOrEmpty(cls.Nulinha) ? "0" : cls.Nulinha;
                cls.NuImei = string.IsNullOrEmpty(cls.NuImei) ? "0" : cls.NuImei;
                cls.DeIp = string.IsNullOrEmpty(cls.DeIp) ? "0" : cls.DeIp;
                cls.DeFabricante = string.IsNullOrEmpty(cls.DeFabricante) ? "0" : cls.DeFabricante;
                cls.DeModelo = string.IsNullOrEmpty(cls.DeModelo) ? "0" : cls.DeModelo;
                cls.DeSerie = string.IsNullOrEmpty(cls.DeSerie) ? "0" : cls.DeSerie;
                cls.DeSo = string.IsNullOrEmpty(cls.DeSo) ? "0" : cls.DeSo;
                cls.DeSoVersao = string.IsNullOrEmpty(cls.DeSoVersao) ? "0" : cls.DeSoVersao;
                cls.NuGpsLat = string.IsNullOrEmpty(cls.NuGpsLat) ? "0" : cls.NuGpsLat;
                cls.NuGpsLong = string.IsNullOrEmpty(cls.NuGpsLong) ? "0" : cls.NuGpsLong;

                barcode = await InserirPedidoBarCode(cls);
                return barcode;
            }
            catch (TimeoutException e)
            {
                barcode.Erro = "Serviço temporariamente indisponível. Tente novamente";
                return barcode;
            }
            catch (CommunicationException e)
            {
                barcode.Erro = "Serviço temporariamente indisponível. Tente novamente";
                return barcode;
            }
            catch (Exception e)
            {
                barcode.Erro = e.Message;
                return barcode;
            }
        }

        public async Task<QrCode> ValidarProvaVida(string idTransacao, List<clsVida> lstVida)
        {
            var qrCode = new QrCode();

            try
            {
                HttpResponseMessage response = await HttpClient.GetAsync(_dataBaseWebApi + "VerificarIdTransacaoProvaVida/" + idTransacao);

                if (response.IsSuccessStatusCode)
                {
                    response = await HttpClient.PostAsJsonAsync(_dataBaseWebApi + "InserirProvaVida", lstVida);

                    if (response.IsSuccessStatusCode)
                    {
                        response = await HttpClient.GetAsync(_dataBaseWebApi + "ConsultarFotoCarteira/" + idTransacao);

                        var fotoCarteira = response.Content.ReadAsStringAsync().ToString();

                        var provaVida = Convert.ToInt16(MatchProvaVida(Convert.FromBase64String(lstVida[1].ImProvavida), Convert.FromBase64String(fotoCarteira), idTransacao));

                        if (provaVida >= Convert.ToInt16(ConfigurationManager.AppSettings["Score"]))
                            qrCode.Codigo = idTransacao;
                        else
                            qrCode.Erro = "Erro na validação da face. Tente novamente";
                    }
                    else
                        qrCode.Erro = "Erro ao inserir prova de vida";
                }

                return qrCode;
            }
            catch (TimeoutException e)
            {
                qrCode.Erro = "Serviço temporariamente indisponível. Tente novamente";
                return qrCode;
            }
            catch (CommunicationException e)
            {
                qrCode.Erro = "Serviço temporariamente indisponível. Tente novamente";
                return qrCode;
            }
            catch (Exception e)
            {
                qrCode.Erro = e.Message;
                return qrCode;
            }
        }

        public async Task<QrCode> AutenticarFace(string idTransacao, List<clsVida> lstVida)
        {
            var qrCode = new QrCode();
            string sFotoCarteira = "";

            try
            {
                HttpResponseMessage response = await HttpClient.GetAsync(_dataBaseWebApi + "ConsultarFotoCarteira/" + idTransacao);

                if (response.IsSuccessStatusCode)
                {
                    sFotoCarteira = response.Content.ReadAsStringAsync().Result;

                    if (MatchAutenticarFace(Convert.FromBase64String(lstVida[1].ImProvavida), Convert.FromBase64String(sFotoCarteira), idTransacao) >= Convert.ToInt16(ConfigurationManager.AppSettings["Score"]))
                        qrCode.Codigo = idTransacao;
                    else
                        qrCode.Erro = "Erro na validação da face. Tente novamente.";
                }

                return qrCode;
            }
            catch (TimeoutException e)
            {
                qrCode.Erro = "Serviço temporariamente indisponível. Tente novamente";
                return qrCode;
            }
            catch (CommunicationException e)
            {
                qrCode.Erro = "Serviço temporariamente indisponível. Tente novamente";
                return qrCode;
            }
            catch (Exception e)
            {
                qrCode.Erro = e.Message;
                return qrCode;
            }
        }

        public async Task<Carteira> ConsultarIdentidade(string idTransacao)
        {
            var carteira = new Carteira();

            try
            {
                HttpResponseMessage response = await HttpClient.GetAsync(_dataBaseWebApi + "ConsultarIdentidade/" + idTransacao);

                if (response.IsSuccessStatusCode)
                {
                    carteira.Escore = carteira.Escore;
                    carteira.Ric = carteira.Ric;
                    carteira.Transacao = carteira.Transacao;
                    carteira.CarteiraFrente = carteira.CarteiraFrente;
                    carteira.CarteiraVerso = carteira.CarteiraVerso;
                }
                return carteira;
            }
            catch (TimeoutException e)
            {
                carteira.Erro = "Serviço temporariamente indisponível. Tente novamente";
                return carteira;
            }
            catch (CommunicationException e)
            {
                carteira.Erro = "Serviço temporariamente indisponível. Tente novamente";
                return carteira;
            }
            catch (Exception e)
            {
                carteira.Erro = e.Message;
                return carteira;
            }
        }

        #region Métodos Privados

        private async Task<string> VerificarPermissoesQrCodeAcessoCarteira(string sessionIdTransacao)
        {
            try
            {
                // verificar permissões de acesso a carteira de identidade digital
                clsQrCodeRG qrcd = new clsQrCodeRG();

                using (StreamReader r = new StreamReader(ConfigurationManager.AppSettings["CertificaCarteiraPath"] + sessionIdTransacao + ".json"))
                {
                    string json = r.ReadToEnd();
                    qrcd = JsonConvert.DeserializeObject<clsQrCodeRG>(json);
                }

                qrcd.Rg = qrcd.Rg.Replace(".", "");
                qrcd.Rg = qrcd.Rg.Replace("-", "");

                // testar quando houver erro
                var date = DateTime.Now.ToString("dd-MM-yyyy");
                HttpResponseMessage response = await HttpClient.GetAsync(_dataBaseWebApi + "VerificarPermissoesQrCodeAcessoCarteira/" + date + "/" + qrcd.Rg);

                if (response.IsSuccessStatusCode)
                    return "true";

                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        /// <summary>
        /// método antigo Ler_Rg_QrCode
        /// </summary>
        /// <param name="sessionSqTransacao">sequencial da transação</param>
        /// <param name="sessionIdTransacao">identificação da transação</param>
        /// <param name="cls">classe Mobile</param>
        private async Task<string> InserirPedidoQrCode(clsSolicitacao cls)
        {
            try
            {
                cls.Nulinha = string.IsNullOrEmpty(cls.Nulinha) ? "0" : cls.Nulinha;
                cls.NuImei = string.IsNullOrEmpty(cls.NuImei) ? "0" : cls.NuImei;
                cls.DeIp = string.IsNullOrEmpty(cls.DeIp) ? "0" : cls.DeIp;
                cls.DeFabricante = string.IsNullOrEmpty(cls.DeFabricante) ? "0" : cls.DeFabricante;
                cls.DeModelo = string.IsNullOrEmpty(cls.DeModelo) ? "0" : cls.DeModelo;
                cls.DeSerie = string.IsNullOrEmpty(cls.DeSerie) ? "0" : cls.DeSerie;
                cls.DeSo = string.IsNullOrEmpty(cls.DeSo) ? "0" : cls.DeSo;
                cls.DeSoVersao = string.IsNullOrEmpty(cls.DeSoVersao) ? "0" : cls.DeSoVersao;
                cls.NuGpsLat = string.IsNullOrEmpty(cls.NuGpsLat) ? "0" : cls.NuGpsLat;
                cls.NuGpsLong = string.IsNullOrEmpty(cls.NuGpsLong) ? "0" : cls.NuGpsLong;

                clsQrCodeRG qrcd;
                using (StreamReader r = new StreamReader(ConfigurationManager.AppSettings["CertificaCarteiraPath"] + cls.IdTransacao + ".json"))
                {
                    string json = r.ReadToEnd();
                    qrcd = JsonConvert.DeserializeObject<clsQrCodeRG>(json);
                }
                cls.Pid = qrcd.Pid;
                qrcd.Rg = qrcd.Rg.Replace(".", "");
                qrcd.Rg = qrcd.Rg.Replace("-", "");

                HttpResponseMessage response = await HttpClient.PostAsJsonAsync(ConfigurationManager.AppSettings["IdentidadeDigitalDataBase"] + "InserirPedidoQrCode", cls);

                if (response.IsSuccessStatusCode)
                {
                    var dadosCidadao = await CarregarDadosCidadao(cls.IdTransacao);
                    var dadosCarteira = ConfeccionarImagemCarteira(dadosCidadao);
                    // atribuindo idTransação para eliminar o parametro na chamada do método
                    dadosCarteira.IdTransacao = cls.IdTransacao;

                    response = await HttpClient.PostAsJsonAsync(ConfigurationManager.AppSettings["IdentidadeDigitalDataBase"] + "InserirImagemCarteira", dadosCarteira);
                }

                return qrcd.Rg;
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        private async Task<BarCode> InserirPedidoBarCode(clsSolicitacao cls)
        {
            try
            {
                HttpResponseMessage response = await HttpClient.PostAsJsonAsync(ConfigurationManager.AppSettings["IdentidadeDigitalDataBase"] + "InserirPedidoBarCode", cls);

                if (response.IsSuccessStatusCode)
                {
                    var dadosCidadao = await CarregarDadosCidadao(cls.IdTransacao);
                    var dadosCarteira = ConfeccionarImagemCarteira(dadosCidadao);
                    // atribuindo idTransação para eliminar o parametro na chamada do método
                    dadosCarteira.IdTransacao = cls.IdTransacao;

                    response = await HttpClient.PostAsJsonAsync(ConfigurationManager.AppSettings["IdentidadeDigitalDataBase"] + "InserirImagemCarteira", dadosCarteira);
                }

                return response.IsSuccessStatusCode ? new BarCode { Codigo = response.Content.ReadAsStringAsync().Result } : new BarCode { Erro = response.Content.ReadAsStringAsync().Result };
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        /// <summary>
        /// método antigo ConsultaDadosRic
        /// </summary>
        /// <param name="idTransacao"></param>
        private async Task<Cidadao> CarregarDadosCidadao(string idTransacao)
        {
            try
            {
                HttpResponseMessage response = await HttpClient.GetAsync(_dataBaseWebApi + "ConsultarDadosRic/" + idTransacao);

                if (response.IsSuccessStatusCode)
                    return await response.Content.ReadAsAsync<Cidadao>();

                return null;
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        /// <summary>
        /// método antigo HtmlToImage
        /// </summary>
        /// <param name="idTransacao"></param>
        private Carteira ConfeccionarImagemCarteira(Cidadao cidadao)
        {
            var carteira = new Carteira();

            try
            {
                GerarHtml(cidadao);
                GerarPdf(cidadao);
                GerarImagem(cidadao.NuRic);

                string arquivo = ConfigurationManager.AppSettings["HtmlPath"] + "Frente" + cidadao.NuRic + ".png";
                carteira.CarteiraFrente = ConverterImagem(arquivo, false);

                arquivo = ConfigurationManager.AppSettings["HtmlPath"] + "Verso" + cidadao.NuRic + ".png";
                carteira.CarteiraVerso = ConverterImagem(arquivo, false);

                arquivo = ConfigurationManager.AppSettings["HtmlPath"] + cidadao.NuRic + ".pdf";
                carteira.CarteiraPdf = ConverterImagem(arquivo, false);

                return carteira;
            }
            catch (Exception e)
            {
                throw new Exception(carteira.Erro = e.Message);
            }
        }

        private void GerarHtml(Cidadao cidadao)
        {
            try
            {
                //caso venha separar frente e verso em cada html, criar cada file.create para frente e verso
                StreamWriter swFrente = File.CreateText(ConfigurationManager.AppSettings["HtmlPath"] + "Frente" + cidadao.NuRic + ".html");
                StreamWriter swVerso = File.CreateText(ConfigurationManager.AppSettings["HtmlPath"] + "Verso" + cidadao.NuRic + ".html");

                string sHtmlIni = "";
                string sHtmlFim = "";
                string sHtmlFrente = "";
                string sHtmlVerso = "";

                //inicio do html
                sHtmlIni = "<!DOCTYPE html PUBLIC '-//W3C//DTD XHTML 1.0 Transitional//EN' 'http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd'> " + Environment.NewLine;
                sHtmlIni += "<html xmlns='http://www.w3.org/1999/xhtml'> " + Environment.NewLine;
                sHtmlIni += "<head> " + Environment.NewLine;
                sHtmlIni += "    <meta http-equiv='Content-Type' content='text/html charset=utf-8' /> " + Environment.NewLine;
                sHtmlIni += "    <title>RG - Digital - RJ</title> " + Environment.NewLine;
                sHtmlIni += "    <link rel='stylesheet' type='text/css' href='../css/default.css' /> " + Environment.NewLine;
                sHtmlIni += "    <link rel='stylesheet' type='text/css' href='../fonts/opensans/css/opensans.css' /> " + Environment.NewLine;

                sHtmlIni += "</head> " + Environment.NewLine;
                sHtmlIni += " " + Environment.NewLine;
                sHtmlIni += "<body> " + Environment.NewLine;

                string snomesocial = "";

                if (!string.IsNullOrEmpty(cidadao.NoSocial))
                    snomesocial = cidadao.NoSocial;
                else
                    snomesocial = cidadao.NoCidadao;

                //frente da carteira
                sHtmlFrente = "<div id='frente' class='canvas'> " + Environment.NewLine;
                sHtmlFrente += "	<div class='bg'><img src='../img/frente-print-size.png' class='frente' /></div> " + Environment.NewLine;

                sHtmlFrente += " <h1 id='labelTitulo' class='title line-simple'>Estado do Rio de Janeiro<br/><small class=' text-condensed'>GOVERNO DO ESTADO DO RIO DE JANEIRO<br/>DETRAN - DIRETORIA DE IDENTIFICAÇÃO CIVIL</small></h1> " + Environment.NewLine;

                if (!string.IsNullOrEmpty(cidadao.Pcd))
                {
                    sHtmlFrente += "<div>";

                    var imgsPcd = "";

                    for (int x = 0; x <= cidadao.Pcd.Length - 1; x++)
                    {
                        var result = Convert.ToInt16(cidadao.Pcd.Substring(x, 1));

                        if (result == 1)
                        {
                            imgsPcd += "<img class='imgPcd' src='../img/Auditivo.png' style='left: 31em;'> ";
                        }
                        if (result == 2)
                        {
                            imgsPcd += cidadao.Pcd.Contains("1")
                                ? "<img class='imgPcd' src='../img/Fisico.png' style='left: 33.6em;'> "
                                : "<img class='imgPcd' src='../img/Fisico.png' style='left: 31em;'> ";
                        }
                        if (result == 3)
                        {
                            if (cidadao.Pcd.Contains("2"))
                            {
                                imgsPcd += "<img class='imgPcd' src='../img/Visual.png' style='left: 36.4em;'> ";
                            }
                            else
                            {
                                if (cidadao.Pcd.Contains("1"))
                                {
                                    imgsPcd += "<img class='imgPcd' src='../img/Visual.png' style='left: 33.6em;'> ";
                                }
                                else
                                {
                                    imgsPcd += "<img class='imgPcd' src='../img/Visual.png' style='left: 31em;'> ";
                                }
                            }
                        }
                        if (result == 4 || result == 5)
                        {
                            if (cidadao.Pcd.Contains("3"))
                                imgsPcd += "<img class='imgPcd' src='../img/Mental.png' style='left: 39em;'> ";
                            else
                            {
                                if (cidadao.Pcd.Contains("2"))
                                {
                                    imgsPcd += "<img class='imgPcd' src='../img/Mental.png' style='left: 36.3em;'> ";
                                }
                                else
                                {
                                    if (cidadao.Pcd.Contains("1"))
                                    {
                                        imgsPcd += "<img class='imgPcd' src='../img/Mental.png' style='left: 33.6em;'> ";
                                    }
                                    else
                                    {
                                        imgsPcd += "<img class='imgPcd' src='../img/Mental.png' style='left: 31em;'> ";
                                    }
                                }
                            }
                        }
                        if (result == 6)
                        {
                            if (cidadao.Pcd.Length == 4)
                                imgsPcd += "<img class='imgPcd' src='../img/Autismo.png' style='left: 39em;'> ";
                            else
                            {
                                if (cidadao.Pcd.Length == 3)
                                {
                                    imgsPcd += "<img class='imgPcd' src='../img/Autismo.png' style='left: 36.3em;'> ";
                                }
                                else
                                {
                                    if (cidadao.Pcd.Length == 2)
                                    {
                                        imgsPcd += "<img class='imgPcd' src='../img/Autismo.png' style='left: 33.6em;'> ";
                                    }
                                    else
                                    {
                                        imgsPcd += "<img class='imgPcd' src='../img/Autismo.png' style='left: 31em;'> ";
                                    }
                                }
                            }
                        }
                    }

                    sHtmlFrente += imgsPcd + "</div>";
                }

                sHtmlFrente += !string.IsNullOrEmpty(cidadao.NoSocial)
                    ? "	<div id='labelNome' class='label'>Nome Social</div> " + Environment.NewLine
                    : "	<div id='labelNome' class='label'>Nome</div> " + Environment.NewLine;

                sHtmlFrente += "    <div id='valueNome' class='value text-large text-condensed text-bold line-simple'>" + snomesocial + "</div> " + Environment.NewLine;

                sHtmlFrente += "	<div id='valueFoto' class='value'>  <img src='data:image/jpeg;base64, " + Convert.ToBase64String(cidadao.FotoCivil64) + " '/> </div> " + Environment.NewLine;

                sHtmlFrente += "	<div id='labelFiliacao' class='label'>Filiação</div> " + Environment.NewLine;

                if (!string.IsNullOrEmpty(cidadao.MultiParental1))
                    sHtmlFrente += "	<div id='valueFiliacao' class='value line-simple text-condensed'>" + cidadao.NoPaiCidadao + " / " + cidadao.NoMaeCidadao + " </br> " + cidadao.MultiParental1 + "</div> " + Environment.NewLine;
                else
                    sHtmlFrente += "	<div id='valueFiliacao' class='value line-simple text-condensed'>" + cidadao.NoPaiCidadao + " </br></br> " + cidadao.NoMaeCidadao + "</div> " + Environment.NewLine;

                sHtmlFrente += "	<div id='labelDtNasc' class='label'>Data nasc.</div> " + Environment.NewLine;
                sHtmlFrente += "    <div id='valueDtNasc' class='value'>" + cidadao.DtNascimento.Substring(0, 10) + "</div> " + Environment.NewLine;

                sHtmlFrente += "	<div id='labelNaturalidade' class='label'>Naturalidade</div> " + Environment.NewLine;
                sHtmlFrente += "	<div id='valueNaturalidade' class='value line-simple text-condensed'>" + cidadao.Naturalidade + "</div> " + Environment.NewLine;

                sHtmlFrente += "	<div id='labelObs' class='label'>Observação</div> " + Environment.NewLine;

                if (!string.IsNullOrEmpty(cidadao.Observacao))
                    sHtmlFrente += "	<div id='valueObs' class='value line-simple text-condensed'>" + cidadao.Observacao + "</div> " + Environment.NewLine;
                else
                    sHtmlFrente += "	<div id='valueObs' class='value line-simple text-condensed'>NÃO HÁ</div> " + Environment.NewLine;

                sHtmlFrente += "	<div id='labelFRH' class='label'>Fator RH</div> " + Environment.NewLine;

                if (!string.IsNullOrEmpty(cidadao.TipoSanguineo))
                    sHtmlFrente += "    <div id='valueFRH' class='value text-bold text-red'>" + cidadao.TipoSanguineo + cidadao.FatorRh + "</div> " + Environment.NewLine;
                else
                    sHtmlFrente += "    <div id='valueFRH' class='value' style='left:37.8em !important'>XXXX</div> " + Environment.NewLine;

                byte[] buffer = Convert.FromBase64String(cidadao.Assinatura);

                FileStream oFS = new FileStream(ConfigurationManager.AppSettings["HtmlPath"] + "ass" + cidadao.NuRic + ".jpg", FileMode.OpenOrCreate, FileAccess.ReadWrite);
                oFS.Write(buffer, 0, buffer.Length);
                oFS.Close();

                Bitmap source = new Bitmap(ConfigurationManager.AppSettings["HtmlPath"] + "ass" + cidadao.NuRic + ".jpg");
                source.MakeTransparent();
                for (int x = 0; x < source.Width; x++)
                {
                    for (int y = 0; y < source.Height; y++)
                    {
                        Color currentColor = source.GetPixel(x, y);
                        if (currentColor.R >= 220 && currentColor.G >= 220 && currentColor.B >= 220)
                        {
                            source.SetPixel(x, y, Color.Transparent);
                        }
                    }
                }

                source.Save(ConfigurationManager.AppSettings["HtmlPath"] + "ass" + cidadao.NuRic + ".png", ImageFormat.Png);

                sHtmlFrente += "    <div id='valueAssinatura' class='value text-center'><img style='width:99%;' src='C:\\Inetpub\\wwwroot\\RgDigitalWs\\HtmlToImage\\html\\ass" + cidadao.NuRic + ".png' /></div> " + Environment.NewLine;

                //VERSO DA CARTEIRA
                sHtmlVerso = "<div id='verso' class='canvas'> " + Environment.NewLine;
                sHtmlVerso += "	<div class='bg'><img src='../img/verso-print-size.png' class='verso' /></div> " + Environment.NewLine;

                sHtmlVerso += "    <div id='labelCpf' class='label text-medium'>CPF</div> " + Environment.NewLine;

                if (!string.IsNullOrEmpty(cidadao.Cpf))
                    sHtmlVerso += "    <div id='valueCpf' class='value text-medium'>" + Convert.ToUInt64(cidadao.Cpf.PadLeft(11, '0')).ToString(@"000\.000\.000\-00") + "</div> " + Environment.NewLine;
                else
                    sHtmlVerso += "    <div id='valueCpf' class='value text-medium'> 000.000.000-00</div> " + Environment.NewLine;

                sHtmlVerso += "    <div id='labelDni' class='label text-medium'>DNI</div> " + Environment.NewLine;

                sHtmlVerso += !string.IsNullOrEmpty(cidadao.Observacao)
                    ? "    <div id='valueDni' class='value text-medium'>" + cidadao.Dni + "</div> " + Environment.NewLine
                    : "    <div id='valueDni' class='value text-medium'> 000000000000000</div> " + Environment.NewLine;

                sHtmlVerso += "    <div id='labelRegGeral' class='label text-medium'>Registro Geral</div> " + Environment.NewLine;
                sHtmlVerso += "    <div id='valueRegGeral' class='value text-medium'> &nbsp; " + Convert.ToUInt64(cidadao.NuRic.PadLeft(10, '0')).ToString(@"00\.000\.000\-0") + "</div> " + Environment.NewLine;

                sHtmlVerso += "    <div id='labelDtExp' class='label text-medium'>Data expedição</div> " + Environment.NewLine;
                sHtmlVerso += "    <div id='valueDtExp' class='value text-medium'> &nbsp; " + cidadao.DtExpedicao.Substring(0, 10) + "</div> " + Environment.NewLine;

                sHtmlVerso += "    <div id='labelRegCivil' class='label text-medium'>Registro Civil</div> " + Environment.NewLine;

                if (!string.IsNullOrEmpty(cidadao.NoSocial))
                {
                    sHtmlVerso += "    <div id='valueRegCivil' class='value text-medium line-simple' style='line-height:1.1em'>" + cidadao.NoCidadao;

                    // nova regra 17/08/21
                    if (!string.IsNullOrEmpty(cidadao.NuMatriculaCertidao))
                    {
                        sHtmlVerso += " <br>";
                        sHtmlVerso += "MATRICULA NÚMERO:" + " <br>";
                        sHtmlVerso += cidadao.NuMatriculaCertidao;
                        sHtmlVerso += "</div> " + Environment.NewLine;
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(cidadao.DescricaoCertidao))
                        {
                            sHtmlVerso += " <br>";

                            if (string.IsNullOrEmpty(cidadao.NuCertidaoCircunscricao))
                                sHtmlVerso += cidadao.DescricaoCertidao + " &nbsp; LIV " + cidadao.NuCertidaoLivro + " FLS " + cidadao.NuCertidaoFolha + " &nbsp; TERM " + cidadao.NuCertidaoTermo;
                            else
                                sHtmlVerso += cidadao.DescricaoCertidao + " &nbsp; LIV " + cidadao.NuCertidaoLivro + " FLS " + cidadao.NuCertidaoFolha + " &nbsp; TERM " + cidadao.NuCertidaoTermo + " C " + cidadao.NuCertidaoCircunscricao.PadLeft(3, '0');

                            sHtmlVerso += " <br> " + cidadao.NoMunicipioNascimento + "&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;" + cidadao.SgUfNascimento + "</div> " + Environment.NewLine;
                        }
                        else
                        {
                            if (!string.IsNullOrEmpty(cidadao.NoMunicipioNascimento))
                                sHtmlVerso += " <br> " + cidadao.NoMunicipioNascimento + "&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;" + cidadao.SgUfNascimento.ToUpper() + "</div> " + Environment.NewLine;
                            else
                                sHtmlVerso += "</div> " + Environment.NewLine;
                        }
                    }
                }
                else
                {
                    // nova regra 17/08/21
                    if (!string.IsNullOrEmpty(cidadao.NuMatriculaCertidao))
                    {
                        sHtmlVerso += "    <div id='valueRegCivil' class='value text-medium'>";
                        sHtmlVerso += "MATRICULA NÚMERO:" + " <br>";
                        sHtmlVerso += cidadao.NuMatriculaCertidao;
                        sHtmlVerso += "</div> " + Environment.NewLine;
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(cidadao.DescricaoCertidao))
                        {
                            sHtmlVerso += "    <div id='valueRegCivil' class='value text-medium line-simple'>";

                            if (string.IsNullOrEmpty(cidadao.NuCertidaoCircunscricao))
                                sHtmlVerso += cidadao.DescricaoCertidao + " &nbsp; LIV " + cidadao.NuCertidaoLivro + " FLS " + cidadao.NuCertidaoFolha + " &nbsp; TERM " + cidadao.NuCertidaoTermo;
                            else
                                sHtmlVerso += cidadao.DescricaoCertidao + " &nbsp; LIV " + cidadao.NuCertidaoLivro + " FLS " + cidadao.NuCertidaoFolha + " &nbsp; TERM " + cidadao.NuCertidaoTermo + " C " + cidadao.NuCertidaoCircunscricao.PadLeft(3, '0');

                            sHtmlVerso += " <br> " + cidadao.NoMunicipioNascimento + "&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;" + cidadao.SgUfNascimento + "</div> " + Environment.NewLine;
                        }
                        else
                        {
                            if (!string.IsNullOrEmpty(cidadao.NoMunicipioNascimento))
                                sHtmlVerso += " <br> " + cidadao.NoMunicipioNascimento + "&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;" + cidadao.SgUfNascimento.ToUpper() + "</div> " + Environment.NewLine;
                            else
                                sHtmlVerso += "</div> " + Environment.NewLine;
                        }
                    }
                }

                if (!string.IsNullOrEmpty(cidadao.DtValidade))
                {
                    sHtmlVerso += "    <div id='labelRgVal' class='label text-medium text-red'>Validade</div> " + Environment.NewLine;
                    sHtmlVerso += "    <div id='valueRgVal' class='value text-medium text-red'>" + cidadao.DtValidade + "</div> " + Environment.NewLine;
                }

                sHtmlVerso += "    <div id='labelTEleitor' class='label text-medium'>T. Eleitor</div> " + Environment.NewLine;

                sHtmlVerso += !string.IsNullOrEmpty(cidadao.TituloEleitor)
                    ? "    <div id='valueTEleitor' class='value text-medium'>" + cidadao.TituloEleitor + "</div> " + Environment.NewLine
                    : "    <div id='valueTEleitor' class='value text-medium'>NÃO INFORMADO</div> " + Environment.NewLine;

                sHtmlVerso += "    <div id='labelCtps' class='label text-medium'>CTPS / SÉRIE / UF</div> " + Environment.NewLine;

                sHtmlVerso += !string.IsNullOrEmpty(cidadao.NuCtps)
                    ? "    <div id='valueCtps' class='value text-medium'>" + cidadao.NuCtps + "&nbsp;" + cidadao.SerieCtps + "&nbsp;" + cidadao.Ufctps + "</div> " + Environment.NewLine
                    : "    <div id='valueCtps' class='value text-medium'>NÃO INFORMADO</div> " + Environment.NewLine;

                sHtmlVerso += "    <div id='labelPis' class='label text-medium'>Nis / pis / pasep</div> " + Environment.NewLine;

                sHtmlVerso += !string.IsNullOrEmpty(cidadao.Nispispasep)
                    ? "    <div id='valuePis' class='value text-medium'>" + cidadao.Nispispasep + "</div> " + Environment.NewLine
                    : "    <div id='valuePis' class='value text-medium'>NÃO INFORMADO</div> " + Environment.NewLine;

                sHtmlVerso += "    <div id='labelIdPro' class='label text-medium'>IDENTIDADE PROFISSIONAL</div> " + Environment.NewLine;

                if (!string.IsNullOrEmpty(cidadao.IdentProfissional3))
                {
                    sHtmlVerso += "  <div id='valueIdPro' class='value text-medium line-simple text-condensed'>" + cidadao.IdentProfissional1 +
                                     "<br>" + cidadao.IdentProfissional2 +
                                     "<br>" + cidadao.IdentProfissional3 + "</div> " + Environment.NewLine;
                }
                else
                {
                    if (!string.IsNullOrEmpty(cidadao.IdentProfissional2))
                    {
                        sHtmlVerso += "  <div id='valueIdPro' class='value text-medium line-simple text-condensed'>" + cidadao.IdentProfissional1 +
                                      "<br>" + cidadao.IdentProfissional2 + "</div> " + Environment.NewLine;
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(cidadao.IdentProfissional1))
                            sHtmlVerso += "  <div id='valueIdPro' class='value text-medium line-simple text-condensed'>" + cidadao.IdentProfissional1 + "</div> " + Environment.NewLine;
                        else
                            sHtmlVerso += "  <div id='valueIdPro' class='value text-medium line-simple text-condensed'>NÃO INFORMADO</div> " + Environment.NewLine;
                    }
                }

                sHtmlVerso += "    <div id='labelCertMil' class='label text-medium'>Cert. militar</div> " + Environment.NewLine;

                if (!string.IsNullOrEmpty(cidadao.CertificadoMilitar))
                    sHtmlVerso += "    <div id='valueCertMil' class='value text-medium'>" + cidadao.CertificadoMilitar + "</div> " + Environment.NewLine;
                else
                    sHtmlVerso += "    <div id='valueCertMil' class='value text-medium'>NÃO INFORMADO</div> " + Environment.NewLine;

                sHtmlVerso += "    <div id='labelCnh' class='label text-medium'>CNH</div> " + Environment.NewLine;

                if (!string.IsNullOrEmpty(cidadao.Cnh))
                    sHtmlVerso += "    <div id='valueCnh' class='value text-medium'>" + cidadao.Cns + "</div> " + Environment.NewLine;
                else
                    sHtmlVerso += "    <div id='valueCnh' class='value text-medium'>NÃO INFORMADO</div> " + Environment.NewLine;

                sHtmlVerso += "    <div id='labelCns' class='label text-medium'>CNS</div> " + Environment.NewLine;

                if (!string.IsNullOrEmpty(cidadao.Cns))
                    sHtmlVerso += "    <div id='valueCns' class='value text-medium'>" + cidadao.Cns + "</div> " + Environment.NewLine;
                else
                    sHtmlVerso += "    <div id='valueCns' class='value text-medium'>NÃO INFORMADO</div> " + Environment.NewLine;

                sHtmlVerso += "    <div id='labelVia' class='label text-medium'>Via</div> " + Environment.NewLine;

                if (!string.IsNullOrEmpty(cidadao.NuVia))
                {
                    var via = Convert.ToByte(cidadao.NuVia);
                    if (via > 2)
                        cidadao.NuVia = "2";
                }

                sHtmlVerso += "    <div id='valueVia' class='value text-medium'>" + cidadao.NuVia + "</div> " + Environment.NewLine;

                //IMG CHANCELA
                sHtmlVerso += " <div id='valueChancela' class='value'>";

                sHtmlVerso += " <img src='data:image/jpeg;base64, " + Convert.ToBase64String(cidadao.Chancela64) + " '/>   </div> " + Environment.NewLine;

                sHtmlVerso += "    <div id='valuePosto' class='value text-medium'>" + cidadao.NuPosto.PadLeft(4, '0') + "</div> " + Environment.NewLine;

                sHtmlVerso += "    <div id='labelDigital' class='label text-medium text-center text-expanded'>POLEGAR DIREITO</div> " + Environment.NewLine;

                sHtmlVerso += "    <div id='valueDigital' class='value'> " + Environment.NewLine; // bg-white //style='width:98px; height:119px;'

                // IMG DIGITAL
                sHtmlVerso += "    <img src='data:image/jpeg;base64, " + Convert.ToBase64String(cidadao.PolegarDireito64) + " '/> </div> " + Environment.NewLine;

                sHtmlVerso += Environment.NewLine;

                //final do html
                sHtmlFim = "</body> " + Environment.NewLine;
                sHtmlFim += "</html> " + Environment.NewLine;

                //juntando o corpo do html, fiz assim para eventual geração de html separado da frente e verso
                swFrente.Write(sHtmlIni + sHtmlFrente + sHtmlFim);
                swVerso.Write(sHtmlIni + sHtmlVerso + sHtmlFim);

                swFrente.Close();
                swVerso.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        private void GerarPdf(Cidadao cidadao)
        {
            try
            {
                StreamWriter swFrente = File.CreateText(ConfigurationManager.AppSettings["HtmlPath"] + "Pdf_" + cidadao.NuRic + ".html");

                string sHtmlIni = "";
                string sHtmlFim = "";
                string sHtmlFrente = "";
                string sHtmlVerso = "";

                //inicio do html
                sHtmlIni = "<!DOCTYPE html PUBLIC '-//W3C//DTD XHTML 1.0 Transitional//EN' 'http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd'> " + Environment.NewLine;
                sHtmlIni += "<html xmlns='http://www.w3.org/1999/xhtml'> " + Environment.NewLine;
                sHtmlIni += "<head> " + Environment.NewLine;
                sHtmlIni += "    <meta http-equiv='Content-Type' content='text/html charset=utf-8' /> " + Environment.NewLine;
                sHtmlIni += "    <title>RG - Digital - RJ</title> " + Environment.NewLine;
                sHtmlIni += "    <link rel='stylesheet' type='text/css' href='../css/default.css' /> " + Environment.NewLine;
                sHtmlIni += "    <link rel='stylesheet' type='text/css' href='../fonts/opensans/css/opensans.css' /> " + Environment.NewLine;

                sHtmlIni += "</head> " + Environment.NewLine;
                sHtmlIni += " " + Environment.NewLine;
                sHtmlIni += "<body> " + Environment.NewLine;

                string snomesocial = "";

                if (!string.IsNullOrEmpty(cidadao.NoSocial))
                    snomesocial = cidadao.NoSocial;
                else
                    snomesocial = cidadao.NoCidadao;


                sHtmlFrente = " <div>" + Environment.NewLine;
                sHtmlFrente += " <h2 style='position:absolute; margin-left:20px;' > Identidade Digital </h2>" + Environment.NewLine;
                sHtmlFrente += " <h4 style='position:absolute; margin-left:20px; top:17px; font-weight: normal;' > Diretoria de Identificação Civil </h4>" + Environment.NewLine;
                sHtmlFrente += " <div style='position:absolute; border-bottom: 1px solid gray; width:740px; top:40px; left:20px;'> </div>" + Environment.NewLine;
                sHtmlFrente += " </div> " + Environment.NewLine;


                //frente da carteira
                sHtmlFrente += "<div id='frente' class='canvas' style=' margin-top: 50px; margin-left: 20px '> " + Environment.NewLine;
                sHtmlFrente += "	<div class='bg'><img src='../img/frente-print-size.png' class='frente' /></div> " + Environment.NewLine;

                sHtmlFrente += " <h1 id='labelTitulo' class='title line-simple'>Estado do Rio de Janeiro<br/><small class=' text-condensed'>GOVERNO DO ESTADO DO RIO DE JANEIRO<br/>DETRAN - DIRETORIA DE IDENTIFICAÇÃO CIVIL</small></h1> " + Environment.NewLine;

                if (!string.IsNullOrEmpty(cidadao.Pcd))
                {
                    sHtmlFrente += "<div>";

                    var imgsPcd = "";

                    for (int x = 0; x <= cidadao.Pcd.Length - 1; x++)
                    {
                        var result = Convert.ToInt16(cidadao.Pcd.Substring(x, 1));

                        if (result == 1)
                        {
                            imgsPcd += "<img class='imgPcd' src='../img/Auditivo.png' style='left: 31em;'> ";
                        }
                        if (result == 2)
                        {
                            imgsPcd += cidadao.Pcd.Contains("1")
                                ? "<img class='imgPcd' src='../img/Fisico.png' style='left: 33.6em;'> "
                                : "<img class='imgPcd' src='../img/Fisico.png' style='left: 31em;'> ";
                        }
                        if (result == 3)
                        {
                            if (cidadao.Pcd.Contains("2"))
                            {
                                imgsPcd += "<img class='imgPcd' src='../img/Visual.png' style='left: 36.4em;'> ";
                            }
                            else
                            {
                                if (cidadao.Pcd.Contains("1"))
                                {
                                    imgsPcd += "<img class='imgPcd' src='../img/Visual.png' style='left: 33.6em;'> ";
                                }
                                else
                                {
                                    imgsPcd += "<img class='imgPcd' src='../img/Visual.png' style='left: 31em;'> ";
                                }
                            }
                        }
                        if (result == 4 || result == 5)
                        {
                            if (cidadao.Pcd.Contains("3"))
                                imgsPcd += "<img class='imgPcd' src='../img/Mental.png' style='left: 39em;'> ";
                            else
                            {
                                if (cidadao.Pcd.Contains("2"))
                                {
                                    imgsPcd += "<img class='imgPcd' src='../img/Mental.png' style='left: 36.3em;'> ";
                                }
                                else
                                {
                                    if (cidadao.Pcd.Contains("1"))
                                    {
                                        imgsPcd += "<img class='imgPcd' src='../img/Mental.png' style='left: 33.6em;'> ";
                                    }
                                    else
                                    {
                                        imgsPcd += "<img class='imgPcd' src='../img/Mental.png' style='left: 31em;'> ";
                                    }
                                }
                            }
                        }
                        if (result == 6)
                        {
                            if (cidadao.Pcd.Length == 4)
                                imgsPcd += "<img class='imgPcd' src='../img/Autismo.png' style='left: 39em;'> ";
                            else
                            {
                                if (cidadao.Pcd.Length == 3)
                                {
                                    imgsPcd += "<img class='imgPcd' src='../img/Autismo.png' style='left: 36.3em;'> ";
                                }
                                else
                                {
                                    if (cidadao.Pcd.Length == 2)
                                    {
                                        imgsPcd += "<img class='imgPcd' src='../img/Autismo.png' style='left: 33.6em;'> ";
                                    }
                                    else
                                    {
                                        imgsPcd += "<img class='imgPcd' src='../img/Autismo.png' style='left: 31em;'> ";
                                    }
                                }
                            }
                        }
                    }

                    sHtmlFrente += imgsPcd + "</div>";
                }

                sHtmlFrente += !string.IsNullOrEmpty(cidadao.NoSocial)
                    ? "	<div id='labelNome' class='label'>Nome Social</div> " + Environment.NewLine
                    : "	<div id='labelNome' class='label'>Nome</div> " + Environment.NewLine;

                sHtmlFrente += "    <div id='valueNome' class='value text-large text-condensed text-bold line-simple'>" + snomesocial + "</div> " + Environment.NewLine;

                sHtmlFrente += "	<div id='valueFoto' class='value'>  <img src='data:image/jpeg;base64, " + Convert.ToBase64String(cidadao.FotoCivil64) + " '/> </div> " + Environment.NewLine;

                sHtmlFrente += "	<div id='labelFiliacao' class='label'>Filiação</div> " + Environment.NewLine;

                if (!string.IsNullOrEmpty(cidadao.MultiParental1))
                    sHtmlFrente += "	<div id='valueFiliacao' class='value line-simple text-condensed'>" + cidadao.NoPaiCidadao + " / " + cidadao.NoMaeCidadao + " </br> " + cidadao.MultiParental1 + "</div> " + Environment.NewLine;
                else
                    sHtmlFrente += "	<div id='valueFiliacao' class='value line-simple text-condensed'>" + cidadao.NoPaiCidadao + " </br></br> " + cidadao.NoMaeCidadao + "</div> " + Environment.NewLine;

                sHtmlFrente += "	<div id='labelDtNasc' class='label'>Data nasc.</div> " + Environment.NewLine;
                sHtmlFrente += "    <div id='valueDtNasc' class='value'>" + cidadao.DtNascimento.Substring(0, 10) + "</div> " + Environment.NewLine;

                sHtmlFrente += "	<div id='labelNaturalidade' class='label'>Naturalidade</div> " + Environment.NewLine;
                sHtmlFrente += "	<div id='valueNaturalidade' class='value line-simple text-condensed'>" + cidadao.Naturalidade + "</div> " + Environment.NewLine;

                sHtmlFrente += "	<div id='labelObs' class='label'>Observação</div> " + Environment.NewLine;

                if (!string.IsNullOrEmpty(cidadao.Observacao))
                    sHtmlFrente += "	<div id='valueObs' class='value line-simple text-condensed'>" + cidadao.Observacao + "</div> " + Environment.NewLine;
                else
                    sHtmlFrente += "	<div id='valueObs' class='value line-simple text-condensed'>NÃO HÁ</div> " + Environment.NewLine;

                sHtmlFrente += "	<div id='labelFRH' class='label'>Fator RH</div> " + Environment.NewLine;

                if (!string.IsNullOrEmpty(cidadao.TipoSanguineo))
                    sHtmlFrente += "    <div id='valueFRH' class='value text-bold text-red'>" + cidadao.TipoSanguineo + cidadao.FatorRh + "</div> " + Environment.NewLine;
                else
                    sHtmlFrente += "    <div id='valueFRH' class='value' style='left:37.8em !important'>XXXX</div> " + Environment.NewLine;

                sHtmlFrente += "    <div id='valueAssinatura' class='value text-center'><img style='width:99%;' src='C:\\Inetpub\\wwwroot\\RgDigitalWs\\HtmlToImage\\html\\ass" + cidadao.NuRic + ".png' /></div> " + Environment.NewLine;

                //VERSO DA CARTEIRA
                sHtmlVerso = "<div id='verso' class='canvas'> " + Environment.NewLine;
                sHtmlVerso += "	<div class='bg'><img src='../img/verso-print-size.png' class='verso' /></div> " + Environment.NewLine;

                sHtmlVerso += "    <div id='labelCpf' class='label text-medium'>CPF</div> " + Environment.NewLine;

                if (!string.IsNullOrEmpty(cidadao.Cpf))
                    sHtmlVerso += "    <div id='valueCpf' class='value text-medium'>" + Convert.ToUInt64(cidadao.Cpf.PadLeft(11, '0')).ToString(@"000\.000\.000\-00") + "</div> " + Environment.NewLine;
                else
                    sHtmlVerso += "    <div id='valueCpf' class='value text-medium'> 000.000.000-00</div> " + Environment.NewLine;

                sHtmlVerso += "    <div id='labelDni' class='label text-medium'>DNI</div> " + Environment.NewLine;

                sHtmlVerso += !string.IsNullOrEmpty(cidadao.Observacao)
                    ? "    <div id='valueDni' class='value text-medium'>" + cidadao.Dni + "</div> " + Environment.NewLine
                    : "    <div id='valueDni' class='value text-medium'> 000000000000000</div> " + Environment.NewLine;

                sHtmlVerso += "    <div id='labelRegGeral' class='label text-medium'>Registro Geral</div> " + Environment.NewLine;
                sHtmlVerso += "    <div id='valueRegGeral' class='value text-medium'> &nbsp; " + Convert.ToUInt64(cidadao.NuRic.PadLeft(10, '0')).ToString(@"00\.000\.000\-0") + "</div> " + Environment.NewLine;

                sHtmlVerso += "    <div id='labelDtExp' class='label text-medium'>Data expedição</div> " + Environment.NewLine;
                sHtmlVerso += "    <div id='valueDtExp' class='value text-medium'> &nbsp; " + cidadao.DtExpedicao.Substring(0, 10) + "</div> " + Environment.NewLine;

                sHtmlVerso += "    <div id='labelRegCivil' class='label text-medium'>Registro Civil</div> " + Environment.NewLine;

                if (!string.IsNullOrEmpty(cidadao.NoSocial))
                {
                    sHtmlVerso += "    <div id='valueRegCivil' class='value text-medium line-simple' style='line-height:1.1em'>" + cidadao.NoCidadao;

                    if (!string.IsNullOrEmpty(cidadao.NuMatriculaCertidao))
                    {
                        sHtmlVerso += " <br>";
                        sHtmlVerso += "MATRICULA NÚMERO:" + " <br>";
                        sHtmlVerso += cidadao.NuMatriculaCertidao;
                        sHtmlVerso += "</div> " + Environment.NewLine;
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(cidadao.DescricaoCertidao))
                        {
                            sHtmlVerso += " <br>";

                            if (string.IsNullOrEmpty(cidadao.NuCertidaoCircunscricao))
                                sHtmlVerso += cidadao.DescricaoCertidao + " &nbsp; LIV " + cidadao.NuCertidaoLivro + " FLS " + cidadao.NuCertidaoFolha + " &nbsp; TERM " + cidadao.NuCertidaoTermo;
                            else
                                sHtmlVerso += cidadao.DescricaoCertidao + " &nbsp; LIV " + cidadao.NuCertidaoLivro + " FLS " + cidadao.NuCertidaoFolha + " &nbsp; TERM " + cidadao.NuCertidaoTermo + " C " + cidadao.NuCertidaoCircunscricao.PadLeft(3, '0');

                            sHtmlVerso += " <br> " + cidadao.NoMunicipioNascimento + "&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;" + cidadao.SgUfNascimento + "</div> " + Environment.NewLine;
                        }
                        else
                        {
                            if (!string.IsNullOrEmpty(cidadao.NoMunicipioNascimento))
                                sHtmlVerso += " <br> " + cidadao.NoMunicipioNascimento + "&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;" + cidadao.SgUfNascimento.ToUpper() + "</div> " + Environment.NewLine;
                            else
                                sHtmlVerso += "</div> " + Environment.NewLine;
                        }
                    }
                }
                else
                {
                    if (!string.IsNullOrEmpty(cidadao.NuMatriculaCertidao))
                    {
                        sHtmlVerso += "    <div id='valueRegCivil' class='value text-medium'>";
                        sHtmlVerso += "MATRICULA NÚMERO:" + " <br>";
                        sHtmlVerso += cidadao.NuMatriculaCertidao;
                        sHtmlVerso += "</div> " + Environment.NewLine;
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(cidadao.DescricaoCertidao))
                        {
                            sHtmlVerso += "    <div id='valueRegCivil' class='value text-medium line-simple'>";

                            if (string.IsNullOrEmpty(cidadao.NuCertidaoCircunscricao))
                                sHtmlVerso += cidadao.DescricaoCertidao + " &nbsp; LIV " + cidadao.NuCertidaoLivro + " FLS " + cidadao.NuCertidaoFolha + " &nbsp; TERM " + cidadao.NuCertidaoTermo;
                            else
                                sHtmlVerso += cidadao.DescricaoCertidao + " &nbsp; LIV " + cidadao.NuCertidaoLivro + " FLS " + cidadao.NuCertidaoFolha + " &nbsp; TERM " + cidadao.NuCertidaoTermo + " C " + cidadao.NuCertidaoCircunscricao.PadLeft(3, '0');

                            sHtmlVerso += " <br> " + cidadao.NoMunicipioNascimento + "&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;" + cidadao.SgUfNascimento + "</div> " + Environment.NewLine;
                        }
                        else
                        {
                            if (!string.IsNullOrEmpty(cidadao.NoMunicipioNascimento))
                                sHtmlVerso += " <br> " + cidadao.NoMunicipioNascimento + "&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;" + cidadao.SgUfNascimento.ToUpper() + "</div> " + Environment.NewLine;
                            else
                                sHtmlVerso += "</div> " + Environment.NewLine;
                        }
                    }
                }

                if (!string.IsNullOrEmpty(cidadao.DtValidade))
                {
                    sHtmlVerso += "    <div id='labelRgVal' class='label text-medium text-red'>Validade</div> " + Environment.NewLine;
                    sHtmlVerso += "    <div id='valueRgVal' class='value text-medium text-red'>" + cidadao.DtValidade + "</div> " + Environment.NewLine;
                }

                sHtmlVerso += "    <div id='labelTEleitor' class='label text-medium'>T. Eleitor</div> " + Environment.NewLine;

                sHtmlVerso += !string.IsNullOrEmpty(cidadao.TituloEleitor)
                    ? "    <div id='valueTEleitor' class='value text-medium'>" + cidadao.TituloEleitor + "</div> " + Environment.NewLine
                    : "    <div id='valueTEleitor' class='value text-medium'>NÃO INFORMADO</div> " + Environment.NewLine;

                sHtmlVerso += "    <div id='labelCtps' class='label text-medium'>CTPS / SÉRIE / UF</div> " + Environment.NewLine;

                sHtmlVerso += !string.IsNullOrEmpty(cidadao.NuCtps)
                    ? "    <div id='valueCtps' class='value text-medium'>" + cidadao.NuCtps + "&nbsp;" + cidadao.SerieCtps + "&nbsp;" + cidadao.Ufctps + "</div> " + Environment.NewLine
                    : "    <div id='valueCtps' class='value text-medium'>NÃO INFORMADO</div> " + Environment.NewLine;

                sHtmlVerso += "    <div id='labelPis' class='label text-medium'>Nis / pis / pasep</div> " + Environment.NewLine;

                sHtmlVerso += !string.IsNullOrEmpty(cidadao.Nispispasep)
                    ? "    <div id='valuePis' class='value text-medium'>" + cidadao.Nispispasep + "</div> " + Environment.NewLine
                    : "    <div id='valuePis' class='value text-medium'>NÃO INFORMADO</div> " + Environment.NewLine;

                sHtmlVerso += "    <div id='labelIdPro' class='label text-medium'>IDENTIDADE PROFISSIONAL</div> " + Environment.NewLine;

                if (!string.IsNullOrEmpty(cidadao.IdentProfissional3))
                {
                    sHtmlVerso += "  <div id='valueIdPro' class='value text-medium line-simple text-condensed'>" + cidadao.IdentProfissional1 +
                                     "<br>" + cidadao.IdentProfissional2 +
                                     "<br>" + cidadao.IdentProfissional3 + "</div> " + Environment.NewLine;
                }
                else
                {
                    if (!string.IsNullOrEmpty(cidadao.IdentProfissional2))
                    {
                        sHtmlVerso += "  <div id='valueIdPro' class='value text-medium line-simple text-condensed'>" + cidadao.IdentProfissional1 +
                                      "<br>" + cidadao.IdentProfissional2 + "</div> " + Environment.NewLine;
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(cidadao.IdentProfissional1))
                            sHtmlVerso += "  <div id='valueIdPro' class='value text-medium line-simple text-condensed'>" + cidadao.IdentProfissional1 + "</div> " + Environment.NewLine;
                        else
                            sHtmlVerso += "  <div id='valueIdPro' class='value text-medium line-simple text-condensed'>NÃO INFORMADO</div> " + Environment.NewLine;
                    }
                }

                sHtmlVerso += "    <div id='labelCertMil' class='label text-medium'>Cert. militar</div> " + Environment.NewLine;

                if (!string.IsNullOrEmpty(cidadao.CertificadoMilitar))
                    sHtmlVerso += "    <div id='valueCertMil' class='value text-medium'>" + cidadao.CertificadoMilitar + "</div> " + Environment.NewLine;
                else
                    sHtmlVerso += "    <div id='valueCertMil' class='value text-medium'>NÃO INFORMADO</div> " + Environment.NewLine;

                sHtmlVerso += "    <div id='labelCnh' class='label text-medium'>CNH</div> " + Environment.NewLine;

                if (!string.IsNullOrEmpty(cidadao.Cnh))
                    sHtmlVerso += "    <div id='valueCnh' class='value text-medium'>" + cidadao.Cnh + "</div> " + Environment.NewLine;
                else
                    sHtmlVerso += "    <div id='valueCnh' class='value text-medium'>NÃO INFORMADO</div> " + Environment.NewLine;

                sHtmlVerso += "    <div id='labelCns' class='label text-medium'>CNS</div> " + Environment.NewLine;

                if (!string.IsNullOrEmpty(cidadao.Cns))
                    sHtmlVerso += "    <div id='valueCns' class='value text-medium'>" + cidadao.Cns + "</div> " + Environment.NewLine;
                else
                    sHtmlVerso += "    <div id='valueCns' class='value text-medium'>NÃO INFORMADO</div> " + Environment.NewLine;

                sHtmlVerso += "    <div id='labelVia' class='label text-medium'>Via</div> " + Environment.NewLine;

                if (!string.IsNullOrEmpty(cidadao.NuVia))
                {
                    var via = Convert.ToByte(cidadao.NuVia);
                    if (via > 2)
                        cidadao.NuVia = "2";
                }

                sHtmlVerso += "    <div id='valueVia' class='value text-medium'>" + cidadao.NuVia + "</div> " + Environment.NewLine;

                //IMG CHANCELA
                sHtmlVerso += " <div id='valueChancela' class='value'>";
                sHtmlVerso += " <img src='data:image/jpeg;base64, " + Convert.ToBase64String(cidadao.Chancela64) + " '/>   </div> " + Environment.NewLine;
                sHtmlVerso += "    <div id='valuePosto' class='value text-medium'>" + cidadao.NuPosto.PadLeft(4, '0') + "</div> " + Environment.NewLine;
                sHtmlVerso += "    <div id='labelDigital' class='label text-medium text-center text-expanded'>POLEGAR DIREITO</div> " + Environment.NewLine;
                sHtmlVerso += "    <div id='valueDigital' class='value'> " + Environment.NewLine; // bg-white //style='width:98px; height:119px;'

                // IMG DIGITAL
                sHtmlVerso += "    <img src='data:image/jpeg;base64, " + Convert.ToBase64String(cidadao.PolegarDireito64) + " '/> </div> " + Environment.NewLine;

                sHtmlVerso += Environment.NewLine;
                sHtmlVerso += "</div> </div> " + Environment.NewLine;


                sHtmlVerso += " <label style='position:absolute; left:410px; margin-top:53px'><strong>QR-CODE</strong></label> " + Environment.NewLine;
                sHtmlVerso += " <div id = 'QrCodeCertifica' style = 'position:absolute; border: 1px solid gray; width: 310px; height: 308px; left: 410px; margin-top: 70px' > " + Environment.NewLine;
                sHtmlVerso += " <img style = 'width:99%;' src='C:\\Inetpub\\wwwroot\\RgDigitalWs\\HtmlToImage\\html\\qrcodeTeste.jpg' /> " + Environment.NewLine;
                sHtmlVerso += " </div> " + Environment.NewLine;


                sHtmlVerso += " <p style='position:absolute; top:382px; left:416px; font-size:11px' > " + Environment.NewLine;
                sHtmlVerso += " Documento assinado com certificado digital em conformidade<br> " + Environment.NewLine;
                sHtmlVerso += "com a medida provisória nº xxxx-xx / xxxx.Sua validade poderá<br> " + Environment.NewLine;
                sHtmlVerso += "ser confirmada por meio do programa Assinador. " + Environment.NewLine;
                sHtmlVerso += " </p> " + Environment.NewLine;


                //final do html
                sHtmlFim = "</body> " + Environment.NewLine;
                sHtmlFim += "</html> " + Environment.NewLine;

                swFrente.Write(sHtmlIni + sHtmlFrente + sHtmlVerso + sHtmlFim);
                swFrente.Close();
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        private void GerarImagem(string rg)
        {
            try
            {
                StreamWriter swbat = File.CreateText(ConfigurationManager.AppSettings["HtmlPath"] + rg + ".bat");

                swbat.NewLine = Environment.NewLine;


                string strEx = ConfigurationManager.AppSettings["HtmlPath"] + "wkhtmltoimage.exe --enable-local-file-access --width 432 --height 300 --zoom 1.20   " + ConfigurationManager.AppSettings["HtmlPath"] + "Frente" + rg + ".html " + ConfigurationManager.AppSettings["HtmlPath"] + "Frente" + rg + ".png" + swbat.NewLine;
                strEx += ConfigurationManager.AppSettings["HtmlPath"] + "wkhtmltoimage.exe --enable-local-file-access --width 432 --height 300 --zoom 1.20   " + ConfigurationManager.AppSettings["HtmlPath"] + "Verso" + rg + ".html " + ConfigurationManager.AppSettings["HtmlPath"] + "Verso" + rg + ".png" + swbat.NewLine;

                strEx += ConfigurationManager.AppSettings["HtmlPath"] + "wkhtmltopdf.exe --enable-local-file-access " + ConfigurationManager.AppSettings["HtmlPath"] + "pdf_" + rg + ".html " + ConfigurationManager.AppSettings["HtmlPath"] + rg + ".pdf" + swbat.NewLine;

                swbat.Write(strEx);
                swbat.Close();

                using (Process p = new Process())
                {
                    p.StartInfo.UseShellExecute = false;
                    p.StartInfo.FileName = ConfigurationManager.AppSettings["HtmlPath"] + rg + ".bat";
                    p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    p.StartInfo.RedirectStandardError = false;
                    p.StartInfo.RedirectStandardInput = false;
                    p.StartInfo.RedirectStandardOutput = false;
                    p.Start();

                    while (!p.HasExited)
                    {
                        //update UI
                    }
                    p.Dispose();
                }

                // rotação da imagem
                Bitmap bmp = new Bitmap(ConfigurationManager.AppSettings["HtmlPath"] + "Frente" + rg + ".png");
                bmp.RotateFlip(RotateFlipType.Rotate270FlipXY);
                bmp.Save(ConfigurationManager.AppSettings["HtmlPath"] + "Frente" + rg + ".png");

                Bitmap bmp1 = new Bitmap(ConfigurationManager.AppSettings["HtmlPath"] + "Verso" + rg + ".png");
                bmp1.RotateFlip(RotateFlipType.Rotate270FlipXY);
                bmp1.Save(ConfigurationManager.AppSettings["HtmlPath"] + "Verso" + rg + ".png");
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        private string ConverterImagem(string arquivo, bool apagar)
        {
            byte[] iRes = new byte[(int)1];

            if (!string.IsNullOrEmpty(arquivo) && File.Exists(arquivo))
            {
                FileStream oFS = new FileStream(arquivo, FileMode.Open, FileAccess.Read);
                iRes = new byte[(int)oFS.Length - 1 + 1];
                oFS.Read(iRes, 0, iRes.Length);
                oFS.Close();

                if (apagar)
                    System.IO.File.Delete(arquivo);
            }

            return Convert.ToBase64String(iRes);
        }

        private async Task<double> MatchProvaVida(byte[] fotoCelular, byte[] fotoCarteira, string idTransacao)
        {
            try
            {
                double dret = 0;

                ZFace zface = new ZFace();

                var modelsPath = ConfigurationManager.AppSettings["ZFaceModels"];

                zface.Config(modelsPath);

                MemoryStream ms = new MemoryStream(fotoCelular);
                Image imgFotoCelular = Image.FromStream(ms);

                ms = new MemoryStream(fotoCarteira);
                Image imgFotoCarteira = Image.FromStream(ms);

                var templateCelular = zface.GenerateFacecode(fotoCelular, imgFotoCelular.Width, imgFotoCelular.Height, ImageType.JPEG, FaceDetectorType.MTCNN);
                var templateCarteira = zface.GenerateFacecode(fotoCarteira, imgFotoCarteira.Width, imgFotoCarteira.Height, ImageType.JPEG, FaceDetectorType.MTCNN);

                dret = ZFace.CompareFacecodes(templateCelular.Facecode, templateCarteira.Facecode);

                HttpResponseMessage response = await HttpClient.GetAsync(_dataBaseWebApi + "AtualizarEscore/" + idTransacao + "/" + dret);
                response.EnsureSuccessStatusCode();

                if (dret >= Convert.ToInt16(ConfigurationManager.AppSettings["Score"]))
                {
                    // status 5 - VALIDO
                    response = await HttpClient.GetAsync(_dataBaseWebApi + "AtualizarStatusIdentidade/" + idTransacao + "/" + 5);
                    response.EnsureSuccessStatusCode();
                }
                else
                {
                    // status 3 - FINALIZADO (REPROVADO)
                    response = await HttpClient.GetAsync(_dataBaseWebApi + "AtualizarStatusIdentidade/" + idTransacao + "/" + 3);
                    response.EnsureSuccessStatusCode();
                }

                ms.Close();

                return dret;
            }
            catch (Exception e)
            {
                if (e.Message != "ErrorNoFacesFound") throw e;
                throw new Exception("Erro na validação da face.Tente novamente");
            }
        }

        private double MatchAutenticarFace(byte[] fotoCelular, byte[] fotoCarteira, string idTransacao)
        {
            try
            {
                double dret = 0;

                ZFace zface = new ZFace();

                var modelsPath = ConfigurationManager.AppSettings["ZFaceModels"];

                zface.Config(modelsPath);

                MemoryStream ms = new MemoryStream(fotoCelular);
                Image imgFotoCelular = Image.FromStream(ms);

                ms = new MemoryStream(fotoCarteira);
                Image imgFotoCarteira = Image.FromStream(ms);

                var templateCelular = zface.GenerateFacecode(fotoCelular, imgFotoCelular.Width, imgFotoCelular.Height, ImageType.JPEG, FaceDetectorType.MTCNN);
                var templateCarteira = zface.GenerateFacecode(fotoCarteira, imgFotoCarteira.Width, imgFotoCarteira.Height, ImageType.JPEG, FaceDetectorType.MTCNN);

                dret = ZFace.CompareFacecodes(templateCelular.Facecode, templateCarteira.Facecode);

                ms.Close();

                return dret;
            }
            catch (Exception e)
            {
                if (e.Message != "ErrorNoFacesFound") throw e;
                throw new Exception("Erro na validação da face.Tente novamente");
            }
        }

        #endregion


        public async Task<StatusCarteira> Teste()
        {
            var qrcodeGeorge =
                "AUHvP6Gwt1oPvY4HvJQ+TBq11wsw/hHTTiyMpedcPMOdnPIHQqjpEFXpeLjmT3q0aheBy/0BaisrlrThLW36scQOkY0wUwRvADUE9ATL1IGF3toVNsnQlXSMFJhV00/QekJJ1/Gz9jJgXbcQw+6drIbMABRDX7ZF6IE5hgGSEALbHjcAA4CxQbx0nENnIwC5MkKERA5TgoeQLSr8yG189oJGr4HnHsgR/ImEnFlXlWcxoV37I4eWko23nIiYdrBYIxVKr1dI/OfAbTkVBg0jj0UoZBbKdNyRwUd1x/7+8gpDLvue2uW0tAEab7eRquHZ8x0mSwdaego5q97AzyRLMC973WF4dfWN0jKZIeBi3OTWglA5qDLNv1faGDXTh4OcRTjGVMvFOJEBUC8+L4ydTLcfxJWgusR0lY6y/bq/QCXJ+Hyc6+/3rME8/AV12fwy1gJTNCF4g/lHnlVICqJyoYWq/ohUPLBBLZRR2TxCk+G4KvxlD44m4dyRXW+IiCNKnJC8RHneZh4C1+6hBBbvFd2kz30S9S+dQx7fhs27Q7bot4cl+IdbT9+cH/Ax1/+ET4HFGtpV8UOFwzS/+6m9yfpg70S4O4+JeNJHMOCWdPgABcJmt6LlbDmE1U3ASQMr9WftbxsgRlGb/0RSIf/IDC0Okc45kCFCkEHuiz6Toe2NE2LXjZGo/nf5mGuYBgK64ptfj34YTyWxrLRb0EGFLfJsZWtYoyfTZ5ZK/JEf/BE0aBxb+y7YWhXCnqScobo7I/keHC9CPQdxy1bAkgr9hLW/bkfEmCY5I5+/mi/A1khtj0G3ZZCTJBGtqU/C9shW2lK/uvjqkJHYrNEHFH4tSpVYQXpo43wrqBt4cT178b/IV1pbDIlDwA8mvhTD0ewvs8h8qkwe9F+ceDe2Y8QEldSbqA+dq3AdATweFKKhEYbIB6RrEmLWE/F1vWHri/4mHGvPCr/uoXlJGKyMlyadihOo2ztGtkhxkacWrRhv9yJ0UTru3W+inTFNd5AZ54B45dKxOsPYMXMT6riHoVz7sw8lwWwOCvA4BAqij7DnImLSdT60gx7WYVNKJDv+E80HmQuKxEA5aeUE7XF+6jNgCTS4b0CBLmZizzNUtjwUsyDZOEXxuL3M02CPT6urSbMia2fhq0pZMUc7Zh2X38+syByr+w3AUfQxyINM1JBlRXDiBy6wVU1x4sWyXnkLZoTBiryuVxKON/RLqOUAqXctVQCpcjWR0pH7EE5yyntnsvQBIy2wJd+SvR4+XATgIT7sK/2ZvnQDqw9Sx/b+7DRIzh+K55FhaD4/8d1yovKlgCj35F9+XE5Mt0d+pgw4FWgg2X22XYHdsQYrlzIwAWjCZ4ykzC5HmsK5WnlnSf0BfcMMVXvFk8zkT3+6frxMDUBowehUT3z4CoA6oLyh61rusyKUqFeSSvVhE6uDV1js76XmExLDEZP0hj2Xcdm5gC3Jrxc7IuSd7254LhM8UfNWeoiFGWmcMlqB/1dBSg7cR3d2ogOT63GkxB4XvRl1vEWoKtFvctKBkgAAs5yXNgC/ml6sEFmELYYOmjpg8oqyefwnQYzQS6TbnYLsimMTUUDttkixK6cWsn64i9KbXAVsc6PuthLxAIm6HHcv6RH705sklVf2vl1cVTJVtVJp2yul79y7/biG5AhKZmuYcfnPFwhg7teM4HquTaQVdsBrW5jOwoijH38z+3JJEx8ufOyBdmR23HvDo4pvycziVfI0hqus22Wufr9PrFVLY9f99AFd06M0gsNL9iV9UUUrQ+jX6ZXVzD5yeKdXIsKrZu0DPjWwdT24gPU/RvExLr3yAqio/oT2A51T5W/y3llEqEUWFTkAAg==";

            var clsSolicitacao = new clsSolicitacao();

            clsSolicitacao.Nulinha = "0";
            clsSolicitacao.NuImei = "0C89E943-CA10-413E-8576-DA1F6E71C1B0";
            clsSolicitacao.DeIp = "0";
            clsSolicitacao.DeFabricante = "Apple";
            clsSolicitacao.DeModelo = "iPhone12,8";
            clsSolicitacao.DeSerie = null;
            clsSolicitacao.DeSo = "iOS";
            clsSolicitacao.DeSoVersao = "14.0.1";
            clsSolicitacao.NuGpsLat = "-22.90266270117286";
            clsSolicitacao.NuGpsLong = "-43.183094148785095";
            clsSolicitacao.DeTpGrafico = "AK05958210"; //AJ08338981 -- pcd //"AK05958210"; marquinho  //"RJ18576762E"; paulo //AM05497841 presidente //AM04907779 diretor dic // AM08372608 governador
            clsSolicitacao.DeQrCode = qrcodeGeorge;


            StatusCarteira result = await ChecarStatusCarteira("1ED021A05EF5089233379BE996F7BBDD");
            return result;

            //var result = ValidarQrCode(clsSolicitacao);
            //var result = await CarregarDadosCidadao("1ED021A05EF5089233379BE996F7BBDD");

        }



    }
}
