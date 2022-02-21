using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net.Http;
using System.ServiceModel;
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
                    lstVida[0].IdTransacao = idTransacao;
                    response = await HttpClient.PostAsJsonAsync(_dataBaseWebApi + "InserirProvaVida", lstVida);

                    if (response.IsSuccessStatusCode)
                    {
                        response = await HttpClient.GetAsync(_dataBaseWebApi + "ConsultarFotoCarteira/" + idTransacao);

                        var fotoCarteira = response.Content.ReadAsStringAsync().Result;

                        var score = MatchAutenticarFace(Convert.FromBase64String(lstVida[1].ImProvavida), Convert.FromBase64String(fotoCarteira), idTransacao);

                        await AtualizarRegistroMatchProvaVida(idTransacao, score);

                        if (score >= Convert.ToInt16(ConfigurationManager.AppSettings["Score"]))
                            qrCode.Codigo = idTransacao;
                        else
                            qrCode.Erro = "Erro na validação da face. Tente novamente";
                    }
                    else
                        qrCode.Erro = response.Content.ReadAsStringAsync().Result; // = "Erro ao inserir prova de vida";
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

                    response.EnsureSuccessStatusCode();
                }

                return "true";
            }
            catch (Exception e)
            {
                return e.Message;
            }
        }

        private async Task<BarCode> InserirPedidoBarCode(clsSolicitacao cls)
        {
            var idTransacao = string.Empty;

            try
            {
                HttpResponseMessage response = await HttpClient.PostAsJsonAsync(ConfigurationManager.AppSettings["IdentidadeDigitalDataBase"] + "InserirPedidoBarCode", cls);

                if (response.IsSuccessStatusCode)
                {
                    idTransacao = response.Content.ReadAsStringAsync().Result;
                    var dadosCidadao = await CarregarDadosCidadao(idTransacao);
                    var dadosCarteira = ConfeccionarImagemCarteira(dadosCidadao);
                    // atribuindo idTransação para eliminar o parametro na chamada do método
                    dadosCarteira.IdTransacao = idTransacao;

                    response = await HttpClient.PostAsJsonAsync(ConfigurationManager.AppSettings["IdentidadeDigitalDataBase"] + "InserirImagemCarteira", dadosCarteira);
                }

                return response.IsSuccessStatusCode ? new BarCode { Codigo = idTransacao } : new BarCode { Erro = response.Content.ReadAsStringAsync().Result };
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

        private async Task<string> AtualizarRegistroMatchProvaVida(string idTransacao, double score)
        {
            HttpResponseMessage response;
            try
            {
                response = await HttpClient.GetAsync(_dataBaseWebApi + "AtualizarEscore/" + idTransacao + "/" + Math.Truncate(score).ToString());
                response.EnsureSuccessStatusCode();

                if (score >= Convert.ToInt16(ConfigurationManager.AppSettings["Score"]))
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
                return "true";
            }
            catch(Exception e)
            {
                return e.InnerException != null ? e.InnerException.Message : e.Message;
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


        public async Task<QrCode> Teste()
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
            clsSolicitacao.DeTpGrafico = "AM08652465"; //AJ08338981 -- pcd //"AK05958210"; marquinho  //"RJ18576762E"; paulo //AM05497841 presidente //AM04907779 diretor dic // AM08372608 governador
            clsSolicitacao.DeQrCode = qrcodeGeorge;


            var listaProvaVida = new List<clsVida>();
            var clsVida = new clsVida();
            var clsVida1 = new clsVida();
            //img do marquinho
            clsVida.ImProvavida =
                "/9j/4AAQSkZJRgABAAEBkAGQAAD//gAcQ3JlYXRlZCBieSBBY2N1U29mdCBDb3JwLgD/wAARCAJ1AdgDASEAAhEBAxEB/9sAhAAHBQUGBQQHBgYGCAcHCAoRCwoJCQoUDw8MERgVGRkXFRcXGh0mIBocJBwXFyEsISQnKCoqKhkgLjEuKTEmKSooAQcICAoJChMLCxMoGxcbGygoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCj/xAAfAAABBQEBAQEBAQAAAAAAAAAAAQIDBAUGBwgJCgv/xAC1EAACAQMDAgQDBQUEBAAAAX0BAgMABBEFEiExQQYTUWEHInEUMoGRoQgjQrHBFVLR8CQzYnKCCQoWFxgZGiUmJygpKjQ1Njc4OTpDREVGR0hJSlNUVVZXWFlaY2RlZmdoaWpzdHV2d3h5eoOEhYaHiImKkpOUlZaXmJmaoqOkpaanqKmqsrO0tba3uLm6wsPExcbHyMnK0tPU1dbX2Nna4eLj5OXm5+jp6vHy8/T19vf4+fr/xAAfAQADAQEBAQEBAQEBAAAAAAAAAQIDBAUGBwgJCgv/xAC1EQACAQIEBAMEBwUEBAABAncAAQIDEQQFITEGEkFRB2FxEyIygQgUQpGhscEJIzNS8BVictEKFiQ04SXxFxgZGiYnKCkqNTY3ODk6Q0RFRkdISUpTVFVWV1hZWmNkZWZnaGlqc3R1dnd4eXqCg4SFhoeIiYqSk5SVlpeYmZqio6Slpqeoqaqys7S1tre4ubrCw8TFxsfIycrS09TV1tfY2dri4+Tl5ufo6ery8/T19vf4+fr/2gAMAwEAAhEDEQA/APpGigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKZmgAyfSk8wBSSwAHrQBQHiLRzd/Zf7UtPPIzs80Vc+1wkcTR/8AfYoAPtcAwDNGC3QbxzUuaADdRuoAXNFAC0UAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFNY4FACbqytX8VaHoMbPqerWtoE+8HkGR+FAHGa98dPCejwyfZ5pNRk/5Z+Sv7uQ/71clffH2O4uxLpn+j2qw/vYp4wSH9qQHB3fxl8YXl/I0GrtHyDtCLxjnpXOXXie71nULq+vdZvEnm4aJZmAb/AID6UgMhYLmKPEaxSB+yTcr9anh1XVrWP93JuVfl2eZz+VIY221q8lxbj7W5U5A3tkH2rp7HxbrkI/4lmvX1u8fEimbfs/OgDcg+L3j60+zC81TdaK/zypaxb2H5V2EP7Qd1FqL2N7ocIlVdwZJvlceoouI7PRPjF4Z1HCXl0NPl2eYfO+4Pxrura+tLyNJLa5ilSQbkKNnIqgLFFMBaKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigApm8jtQA151iQtIQijqWOK8t8U/Hfw/pM/2PSZBqlz8yu0any0OODn+IZ9KQHj3in4teLNdmnH9ofYLKUf6mJ9vFcl9plvrhYneaWZvuzSqd0o+pqbgGoW7x7ort3tQnzhWhb95+VZq6hcR3sEkhXbEu3YvQjvTASIwO0hWaKTj/lr8rU2SSBYwGsYt395HNMCJrjTjE4MVxFL2IxipolxbI1vKlyyj5lHFAyydZvTHDa3v7sW+ZIiBtYZ9+9XLPRbieBpbG88oxr88Uh27z2AHegQW/iG8h36dqKEA/IC/H5H0ov2223lTEjyG8uJwNy8/3G9KmwGxa3Nle2EEWrR/a/IwieUAGA7E/wB76UkP2+y1We40DU55zbDh8mGaJPpSGd54f+Nnii1s2iuI49QEMQVZ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";
            clsVida.TpProvavida = "0";

            clsVida1.ImProvavida =
                "/9j/4AAQSkZJRgABAAEBkAGQAAD//gAcQ3JlYXRlZCBieSBBY2N1U29mdCBDb3JwLgD/wAARCAJ1AdgDASEAAhEBAxEB/9sAhAAHBQUGBQQHBgYGCAcHCAoRCwoJCQoUDw8MERgVGRkXFRcXGh0mIBocJBwXFyEsISQnKCoqKhkgLjEuKTEmKSooAQcICAoJChMLCxMoGxcbGygoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCj/xAAfAAABBQEBAQEBAQAAAAAAAAAAAQIDBAUGBwgJCgv/xAC1EAACAQMDAgQDBQUEBAAAAX0BAgMABBEFEiExQQYTUWEHInEUMoGRoQgjQrHBFVLR8CQzYnKCCQoWFxgZGiUmJygpKjQ1Njc4OTpDREVGR0hJSlNUVVZXWFlaY2RlZmdoaWpzdHV2d3h5eoOEhYaHiImKkpOUlZaXmJmaoqOkpaanqKmqsrO0tba3uLm6wsPExcbHyMnK0tPU1dbX2Nna4eLj5OXm5+jp6vHy8/T19vf4+fr/xAAfAQADAQEBAQEBAQEBAAAAAAAAAQIDBAUGBwgJCgv/xAC1EQACAQIEBAMEBwUEBAABAncAAQIDEQQFITEGEkFRB2FxEyIygQgUQpGhscEJIzNS8BVictEKFiQ04SXxFxgZGiYnKCkqNTY3ODk6Q0RFRkdISUpTVFVWV1hZWmNkZWZnaGlqc3R1dnd4eXqCg4SFhoeIiYqSk5SVlpeYmZqio6Slpqeoqaqys7S1tre4ubrCw8TFxsfIycrS09TV1tfY2dri4+Tl5ufo6ery8/T19vf4+fr/2gAMAwEAAhEDEQA/APpGigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKZmgAyfSk8wBSSwAHrQBQHiLRzd/Zf7UtPPIzs80Vc+1wkcTR/8AfYoAPtcAwDNGC3QbxzUuaADdRuoAXNFAC0UAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFNY4FACbqytX8VaHoMbPqerWtoE+8HkGR+FAHGa98dPCejwyfZ5pNRk/5Z+Sv7uQ/71clffH2O4uxLpn+j2qw/vYp4wSH9qQHB3fxl8YXl/I0GrtHyDtCLxjnpXOXXie71nULq+vdZvEnm4aJZmAb/AID6UgMhYLmKPEaxSB+yTcr9anh1XVrWP93JuVfl2eZz+VIY221q8lxbj7W5U5A3tkH2rp7HxbrkI/4lmvX1u8fEimbfs/OgDcg+L3j60+zC81TdaK/zypaxb2H5V2EP7Qd1FqL2N7ocIlVdwZJvlceoouI7PRPjF4Z1HCXl0NPl2eYfO+4Pxrura+tLyNJLa5ilSQbkKNnIqgLFFMBaKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigApm8jtQA151iQtIQijqWOK8t8U/Hfw/pM/2PSZBqlz8yu0any0OODn+IZ9KQHj3in4teLNdmnH9ofYLKUf6mJ9vFcl9plvrhYneaWZvuzSqd0o+pqbgGoW7x7ort3tQnzhWhb95+VZq6hcR3sEkhXbEu3YvQjvTASIwO0hWaKTj/lr8rU2SSBYwGsYt395HNMCJrjTjE4MVxFL2IxipolxbI1vKlyyj5lHFAyydZvTHDa3v7sW+ZIiBtYZ9+9XLPRbieBpbG88oxr88Uh27z2AHegQW/iG8h36dqKEA/IC/H5H0ov2223lTEjyG8uJwNy8/3G9KmwGxa3Nle2EEWrR/a/IwieUAGA7E/wB76UkP2+y1We40DU55zbDh8mGaJPpSGd54f+Nnii1s2iuI49QEMQVZJeHz/wCzV12ifH/TLifyNb0+bTzgYlT5wT9KpMR6RYeKtB1PAs9Ys5Sf4VmGa1wc96oB9LQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABSUARvLsXe3Cjkk9hXkPjz456fpkTWfhqRb26YH/AEr/AJZx/wCNIDwXVfGmsa/NjVtXvbiPzPM8nzflB+grKmuZpbkkgxOq/d2bMf8AxJpARWFx5LN5Ns0ly6kbnXdtH0rVl1G+lgtvMuI4FgX935y/dpDM681GW92/aL7eR8v+zWfHBPc3G2BPPk/hVT1piK2Yy2ZIznvjtSqYc43nNAFhbVZ/vSsp+lKba5gbzI1ZdnSVOlAy4t295kajbNKgGd8Q5Wp7Yajp8Sx2i/arWX52tw2/f78cigDoZ7I61oYNxpkttMDiO4Y5QJ3U1ipHcWmgskhmMCNiKFx+7f157GgCO3ljOnrNNG0sO7krw8P+Iq/K1tcyr9o352/uL2L7v/A6kCSe5T/R7i9vw93OfLRl/wBWgHr6Ump6fLNvFvdQ3TxffeFsqB9aYGO8v2a4QxCSYg5LxOUdTXoGgfFlNPtZVuFZdRlZf+JhYOVkx6S54amI9x8DfEe112wQXWoW9w/3WuYk2KG7K4/hNd+KoBaWgAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooASsnxH4m0rwrpD6jq1yIIF4Hq59APWgDwXxz4/17xfDNbWl3a6Vo+3PkiX97cezn0NeQzWryOY5pI7eLzMIitywqQLF/rMGkk2enxJBImRNLsBY56rz/OsZdVnk5SPeQeGc/zpASPq12u3M43jhRD2/GqnnMRyGkP8Rc1QCRrFnO3cKurexWU8c9krRlOQ3ekAye7DT+fJGMP12e9BsEUx4kV0lHQfpQBUnkuIZmjOGxxvXvTAPOH/AB9uMdnNAy9El6MPG2QO4NTi7lZvMybObHLQnbn8qAOmtteubzRLexlbcIPM3BflUL6+5rT0UfYtAv7Ir9unupQYy3+qggxyakRo+HdO0SCefT9pOoRrgrKP3D5BJX8B0rmtUtrGJXksoiLKRljEu7Lwn0aoGQarYxWxQgCS1l+XzBxyPVe1UbnQxCsZU8YyVtX3Y+vrWgDFnXzsNLFc+YOJj+7b3XNJc2GnmN1WVVlLL5bRyZ2j+LI70hFvRLm+8M3M1zaf6ZYt/rLfPEnpuFfRfwu8bXGopBpsvn3tnNF5trqEnX/ajb/d6VYHqYp1MAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigBKMigDi/HXxN0TwTbFJphcag3CWkfJHu3oK+X/FfxC1XxhrP2y/CySxZSKLrFEvqBSAxGRtWjd5765a4ii3KmeGqj5/2GPIXdJ91Xb+AelIDMkYuSVUt9aupbrcWyscxAfeVP50ATDTLNJMf2isvy/wACEVDJaQAbEkGe+80AVdkcD/OOv92mqTyN2fSgCQYeLMf3k7UsExWQK2VB6UAaltbDULjyAh+0txGY+M1WvtPNndOvm+Zs9U/pQBJbxwf2VdSRpMblHQ/L9wL3qV7iS9gKsobZ0+TBAoAn065+zsjSM3ycEdRn0rqUhmFnJbJIvk3EZ3rH1I6jH41mMkmsmhf+0LafzLl0WVFuemejg/pVjVtTuPslxYNYWgcQI0srRbDv9EHTvSGXdXtXayWK6lgs7i3s4zDDcRnhP4646K0ubHyZoViS2OQsm7Cygfe+lUImv1sfEs8uooRaTooaaIRfcx6Y60R2hmhkhkmgFwoDLNHh4sN2J/hb2oAz5Ybiwle2kBV1IYo/D4rqfDOoyaNqWmz3mqXOmQyD7ZaltxhYZ+4Qvrj6VSA+qvD2vWfiHRotQtGOHHzofvI3oa1asQtFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUANNeQfFL4s3GiWckGhR4QZjk1Fugb+7H6n3oA+Z5bkXM/21jcSM7nfLMd3mUkTwzXHkRww26OmcnPUVAFq1lgs1Hmlo36lUjGfzrJnc3E7Nh9vUZFMCDztisu7A9KTzGPzFzkf3aAHKYEj3M28/3RVqK6s2iKTweYeqyZ+YUATz26vBiBIWdAMsH+ZvwrMnt5IpCCjRuP4H60ARwymOT/AFfzHj6VqSw+XCkpkFzbOn+uQf6s+h96ABLQJcwMbjMJI3kferVaOa53QzNvBb5ZvVfegCSwhvNKvQh06V1uhsKj7rD1pLqzWzmeOCy3ImW84MzNj/aFAyO3iTPlywGGSXDI3VSP8a6TTZobu8k0K5uI7aLzf9Fn2421mxmrdWd/4clN7JbG73MqmYjI9MflU/iqCMXkD+ab3zuknQIcdvWkBF4mF6tjpustFHJdLGPKSI5JhUbSMdTWNY3en2t7BB9l86zv124kfb5f/wBegCO/8N3+iyfajGt9o0R+c2/3qrSWun3rLfRXiFSeNqncv++P0pgQnULi1WS11Swi1f8Aht5f4l960PBStf8Aii20YXtotpdAp5l5GJPs3tg9/SmhHvvw21W7e5urG4tmigtCLa1b7vnJ/fx0FeloMCtBD6KYBRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAcj428SWekRRWlxffYUnz5twsm2SNR/d9yeK+TvEmo3GratcLcSfuxJ+7i/hI7fjUgZj2kZSJZrrbHIPNwvpRtsPI/0Z/JlxkM/YUgKOySNNjzl+/yDdiqiwylvvOR70wDyLdT+/n3E/wL/Wpo2ttjw4XY3JbHK/T2oAi8qKF/7wI5K9BTRZ7v3m5QPSgBY5Ch2NtdByrd1qxDc+XeNcHF2xUht/8AOgCnGxjff1XtU1jczWFx5sJxngjHyt9RTAmQI2AFMTH73+e9adv5vEW8SB/ur0JpAaRvtU0qTZsuElj43H5mA7gGtU3MWpj7dD5tjcDj918x+vuKm4yuLGRpjslS7bOf3T9vUD1pPslv5ouP7LOpQrw8Uj4c0hnUaF8SLGz8q11Lw60OnqdglF3v8sn7oZccitO48PQ6lar9luxBcagTK1u0u2JP9qI/04qbgZlkl4ky6BqMEem69AxexuT/AMt1x0J7ZHaqVrp0XiWae1+W2mSAiS1ccO+f4Pc0gMuxnhsFmt1knAgl2tFP95a0rR/Cer3M7yavLp14flQPaYH+9vz/AEpgVbzwnrmjwHU4/s1xaO33opdwI/vH3rnUuJ7eeKBI4c+eHWZF2zIfrTiI9p8FeKNQi0Vk1C4jjuZ7tF0648oFLghvmGc/fr3iIsR84wa1ESUUwCigAooAKKACigAooAKKACigAooAKKACigAooAKKACigBKwvEninTvC2kNqOoTBIxwozzIfRR3PtQB8w+J/EUHiS+udXvDL/AGpeDDWn8EK9FXPrtri5bprqaOyS3xGil2C9fU1AFe8vyszm3GxMfIp5xWVJdSMctTAcl/dW8beVIfm/uVBJdXdx99nNADcAY3NlqshBsG0bMetAEjY4IH1AqRVj8vduC+xoAryqh+bt0qEZjfIOaAHBUL4Y4B6VMjsOOCPSgC/Z20E1sUKTeafukONgqZ7G4XCTXAY9sP0FAHRaGwtXRk8R+RcD/lm6bhnt1qBvE0lpdytcfv5u8kW1T+HpSYyxa+NdEu4Dbarphgd5NxvbUYkXAwP/AK9acep6dJdxJ/bEWoEx/uWhTyZIz/t+oxWYGm95Fc77W6tLG+eMj5h98AfTp9arvLbxXRRJvsUW/MVtMvm4PZ/OHGDz8tSyjYi1C1vYz/bUYlhlYlX27JUOPvj+99arXOg3NuxubK5S8QYeOfq0f/16zvYEi1fWWleItRm/tGaWxvwu+S5EeRdjtJ6BqxH8DWMluvleMNPnh5cQSkebn0zWsXcbRZtNJm0m2bzLxLizv+Ps8UmX/wB9scYBrmrzSUkguggMl5FzFOOr5/h+hpfaJK9v4pjk1HTlu7dLXT9LQbEiP73ef48+ue9eweDPi++ifufEVybrTJnPk3nmb5Yv98elbok9xs7qG+tIrq2mSaCZd8ciHIYVYqgCigAooAKKACigAooAKKACigAooAKKACigAooAKKACkoAjkkWOJnlYLGoyxPYV80/FbxrbeLdZ8nSN13pdkm2eSL5o856g9vekBwcNro4vPs8up3Nm3aZVHlnPt1rOnWx0u6+0WWoXF1LG2Pli+RvxqAMo+bd+ZtuIY2k6iU7az5ICmVYowHcdDVAMAUgtCuO3NSqm4YP3qAK7Ntc/u9pHrT0kY+9MC3bj5/nf5scKKkddrqZ4dv4UgEMctycQQY7AY61VezeJlQ9W/h/pQBNBYTvyVWOJThpG7Vcs4pfLZYtgA6s8dTcpRLYhn2KuzzGHTC4p7aVczAGTj3xWTmjdQJl0C42cAsPWrdroBinzInDDawIySPao9oXyDLnwxuxsKyhuBgcEVXPhG8Rh+7/Oq9sTyGhpnhyUSZcPbPG3yOnauqtTqtwiPP5d5JE27zJUzu9Frn9oaez0BbeWBEuPs43h282+Zc+V7D2q1A2q2N4j2kq2rElkdV3eZnsR71PtENUzQivbKaGcX8CWlwh/eyD5n3f7I7VRm03TVsVd7CMaesv7wBsPKx6E7flpqqN0yO90m00aJtRGkMdOl2NG8E/+q9mFZLkz62000aSwSDfK78EZ6KPf0raMrnNONjnvF2iwaW1tCqMVnXzA8yYlQ5+69ZkN9JBayQMFkhZsNF7iulGB6v8ABb4lXPh6W30LUgX0m5k2wv3tmP8A7JX0oGO7BqwH0UwCigAooAKKACigAooAKKACigAooAKKACigAooAKQ9KAPKPjl4xvNA0C20nTlR59Uysq5+cRdyK8Ev7htJ02WzSOKzt7oL8kB3fnUsDmL25RNsEiZlXu33qlgkiWwghliYRtKWZlOGI7UhlPb5oIbGF/Oo5INuN3AboR1piInEakbm+bpUR+duDQAHH/LX95s704R5TzSCq9himBZQXNqvJGDzz1FWI764jTarHGc5bk0gF+1/aZMTn5/7461fjBnzCMpETk896zlI0jEvxaMssuFVmc+nPNb6eHHcx7wsZxwE/nXHOrY7YUjZt/D8anG3LCtaHwqC67FyevSvPlVPQjSNCLwsqLukVR/u1KPDnIOwMKz9qDpgPDcUMezaOudg7ZqYaDbnblBhf71PmDkJhpsbbV8uOTHb0pP8AhHgkZ2fKr9QlJzYchF/YtzaxkPCrRt/AP4qf9mt1mwqTxN0+ZcYqeYjkRHD4UiEkm1v+Bd6kHh4W1s2F+XGBF2ampj5EZ/8AZMttvzGrRjmRD02+4rI1bR9FVZf+Je0sabZvN87kEf3R6V30ZnDWgcD8SJGuPEDXz7jO+zec8YCjFczHL9m5f/UXHVsZP1r0o6nns3tOeNZltlueJf8AVyY6e1fR3wy+IkWoRWvh3Vn8vU4k8uN2fd5+33/vVYj07cKdVgFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUyQqsZZ22qOSaAPl/xf4v0/xL4s1TVHhka1/1NgWbJYLxujHo2Ca89eykvrX7ZAiqFOWB+8OazbAo3sEA1Se4+0IUf/lm33x9arySR3LK25kG3AB6UwK7okm77JnMYz83Wq8M7Z/fpvFMBxjRyvlAHttY1KtuoPlyh4cdpB1+lAELwwiT903ne3pUxnnB3sfm6cf0pFEsFnNeyBdvPUlqtfYDt8sDLdwKykzSMTd0zw/9oCfutv1rpNP0G0R9qgM2PlHYVxVah6NOjodBp2hbBvKf7+OM1s6fpZaTzBDtjH3TXmTqXO6EDfg0+NRgIPrWlBboPmYYrI1exaSOF/u9vUVIYYGz+7GTxnNVYx1IRBCH2qg9yaR7KEgccCpLIjYx7v8AVgf7VH2RflJ6VIyRvLQc5552+tCwpJ2Mnu1FybEsVuEB4/IUXFsrfdJXii5DMG5t3icO8u2Sc4RiOtZVzomXV4QggDYk3LuAPpiuulM56kTgdX8OXGuNdr9mg821ieUkHGFA9K83FsZtGuAxjjaHEi7m7e1e3Teh5E1qOS4/1Ei/IR/EPWulh1F7XxBb3dsWi84rLFN3hnHcVqZn1J8P/Fp8XeHxczwmC8t38i5GMIXHdfautrQAooAKKACigAooAKKACigAooAKKACigAooAKKAEJC9a4b4u+JX8O/D28Nqsz3l7/o1v5K5ILd/pjNAHzZpGjRQwQXd1qHktCcWsTHkjqR7VS1W+T7Q0FlIVDHd5oPasRmRdLbbG3+W02PlBGDK59x+dZLxYVVPM55Y56VohCeUVyEYt70n2R2UnO3ApDsRSW+0qM/NilO8YEj7/wDfoAvWUlrB+8lxhPuj1NIZWnk+WM/lSehotNjd0yC7uRhcLu49K67S/DcaBS4J2fn+NedXqHoUaZ0ttpAkX/UEpnh+lbEdlBb/AHIAnGOlebOR6SjYuxW3mbePlHatiC2VE+6K5jVFqPC4FWtoYbcVRLJMiNOR0piYcbjHimZisOnApSD3P4UAKY93cqfWmYfbgvz/AHqQxvkjdv8A4+x9KlVcHNIQu/BqU7SMmkSyvdRrInzqOuRmsOSO5s75JIoPN3dU7MtaQepEvhOP8W6H9ont7uOSRLOWTypZ2IG1OoU+leYyadL4fufMlgDQX3mwxydfkzjK171HY8WruZEt0sXhqbShDEpNyLhbj+PIG3y/p3q5aXG+2gh2keVwWz6/dOK6uhgew/BXxfs1+PTZmIhv02jeT8sq9hX0DVoAopgFFABRQAUUAFFABRQAUUAFFABRQAUUAFFADGGa+av2itSuZfHFjZR3gFvZ2m9o42wYyx/i+tAHJ3M2lGytohbea8cIJEZyBXOX1y8rEMiIzccDHFZgYzRSxq7+aVbP8a1LBbCZ1RT8zfMzGmxoe1tIu7bynUNTvs8ZsNxjfIPzuF3Cs+Y1K8scXTr6FKPshl2rGGuD16UcwuS5fs/DN3qEmWgMQz2rqdN8HJERujwP1rkq1ztoYfudZpnh5YhgRg/WtyLTFyc/zryak7nrQikacNvLgDfhPdasQ2wDfM5esGzUuwrn7oAq4ke4ZqRlhRhsn0pwY4qzIV2KL1Jpy/pTEOH0oJDYYjmgkM0lSMM8Uv0NIBM57/hSnLcZoENk+ZcVnJJ5d9H5m7Z9wjPrTjuRLYyfEEcZks4UUSWrytFFD2iKrwfrXlvimyMumq5S4WaEOrc/LEn97/gVe5R2PEqLU5LT1i1LT2WcfNEMMR/AexpFHmygDbgR+S+9cfjXcYjIruW21GCeOYpc28wk2fw5WvtbSNQ/tXRrK/jkRluIlf5envVok0aKoAooAKKACigAooAKKACigAooAKKACigAooAhuJFgheZvuxqWP4V8ReKtcfxZ40ubk3QEd3dfK0nUJnjNAGtNcxj/AEaJVQA+QxX+I+tUr3TJNPtfmmHmjn6N3rMDHj0xp7hla5Bcep61vadpyRRGXyhJ8u3msakrG8EEFlGLtVeJlUZz6H2rWTT/ACxts0XB5cDoa5JVbM7lBW2LEXhgSESSW6oP7orbsfD1tAuXjG72rnliGbwoxNRbGEYCDCetTxWUCuz4ye2a43K52WLsKdP5Vc8pST79/SsWaWJoIjFH5W4sP7zU/owFSFi1AN3XpmtDhYwAKdgYgP60v8QqiSTqpoQ+ucCmIfvy/wBe1KT29KRAnNH41BYzP/AqeccUhDfpTgMUwHZNQXVqjjeDyvIFMzZkXUEN3IPMmW2e6lXPyFwHX/GuR12wgudXl+yWkmobpTFMC2yNTXs4d6HkVl7x5lpujXX23Vo7pNn2bmRB0DdvyqlvbTdQt7ht7QMdkmepr0DlZFrEHk6mLjCwxS/vF2/d2+1fS3wP1i3vvBx06ORpGsn4ZlxlTWqIPUKKoAooAKKACigAooAKKACigAooAKKACigAooA4D4y+KW8K/Dq8mhd0u7vFrblR0Ldf0zXyPoNgdQ1USSbiIzu+WpYHVL5M2pzNArNtXMcO3kD3olgCwf6TL5v95WxubHpWYxtqsu1neDalxjb8vOB6Vs/ZzdIBsK9s9K4a8j0KMTRsNKMCBc7sdzWpFbKuCtebJ3O+MS2kZ35qXjp3rBo6Ionj6VYIHas7lMepwBxVqJh0FMssL830pZPvcUwJreUBPcVc89duOaAYu4K+aUuvHXNBJLk9cdqVWzkqOlBIpY49fwpAfkxmpAeOgoPQ80ARZ/DNPH50FDm7GkBG6mQPA546Up+YYagkxtWs0uM+dvQBDtkVtuP8axdSmZbbTi9tvkuY932iMfumkU/e9h7V6eEeh5WJWpz2uP8A2jrz2S24hiu41hnkjbG98Z3V5pexsmuGLpbXC/u16FscV6pwEOvErFJZ+WYvJYHBX7v1rvvgD4j/ALA8a/2Xdu6wavHiEMejj/GtExH1Dup1aCCigAooAKKACigAooAKKACigAooAKKACigD58/advrp5vD+jox+yyiW4kUD+JdoU/q1eX2cltaLbxRybwnzeR2f6+lQwJbrW4beNrdLjdJK2+SWIf6uoIbmPPmRDddSKI4oOuwHris+hcTpbCx+UmTa8sI2b8/KDjtW1DGteVXkerQJwmTxUyLziuFnaWVHOKckeD6Vnc0LCgAUeZtJqCxUYnL4q6hONuMUIZbjIx96lcgNgdKoQ6NvLl+7VoNxSGP9aVTj3oAk3EHNSM3VufwpkAuccnNOyNu7vUgAz+FN8ve/3eBQA4gDk0gPcflQMkHTpTeaZA7GTTzQIj8hJ42R45MN8pdD92sc6K1hpsisjRArvW4T5oj26fw/SvRwmx5eJPPTp9pput3MrM948Mi+QM8SEnnArz/xOw84x+XjyWIjV1/eKPSvWR57HaulvJYxSGVJJkQJJj/loateBbj/AIrPQHeD/j1u1+fPX0rREn2XT61EFFABRQAUUAFFABRQAUUAFFABRQAUUAFFAHgP7Teo28cGiWiTJ9sEhfbj5lX1rwmCS5zJDbYD/fO3+GpAliiTUr2Gzt1xGTl3/vGu5tNLisxcpn5/ueZjLY/u+wrKRcTobO3U2IKhESM7UwP85p8YPzNmvGrbnrUSQN3FTxHLAdq5WdpbBC9qkJBUc1kWhHJIFNU5bB7UjQsRx+/Bq0Bx96kIsJ8wHGKmUAv7UgHRoF79e9ToMd6pAWkHP1pvkBLjjoaZI8YHJxil7/eoAeQNwO78Kf34/WgkPRd2D7dKO9IYwx56sR9Kfg9aAEbmgg0CFzSZO3I61IiRZWWSOQD/AFZrX08CTzVLDE44DjqPcV6OFaWh5mJXU8Y13TpvCXja5IuTs5kgcjcd5/pXMeLY7OS8GoWwh8iT78hbc00nc/WvWR57OZ1K2kbR4p0+eJHz05H1rY+H9rbXfjbSJJ7iNLJn3bv7knYCtkSfXsakIoY7mA5NS1qSFFABRQAUUAFFABRQAUUAFFABRQAUUAFFAHyz+0VJP/wtG2BZT5NgrJx91Sx615QU2uixBw83HX7x/wAKQHW+GoW05PJi8uSWd1GF+9EK6nzINPWSTUFZY9wKIo+dhWEzSI63vZbqBFXMNuOQh++B2rUVspXi1dz1qIo+7VmLH41zM6yb+Lmpyfl4XiszQRVy/wDs1b8pT+NBRKtuAg9BU4XHQ1IydPu1NEOM0wBhxx1qVBTAsRtt4qQDrTIFAAfkU5QSW+agRJjanvTX+XCs2c+lBIvH3VHFGDng0FB8wpM44Y81mIKGOeKYho9qUnb6CgRJBO28MuNverHyoC43bD3z0ropPU5KyOQ8b6H/AG9Zs8ETJeWy5jlz95e4NeSSz3F/4evLVrb9/preYjbANg9T7V7dM8qSMGy1WawQXq4k3gCSPrHj/drXjvodP1HT5dLESrDOJ/JI/wBWx610EH19Yzi7sLe4VgwljDZFWa1JCimAUUAFFABRQAUUAFFABRQAUUAFFABRQB8j/HO5kPxOvSsjzD5Y/wB4OFwPuj2rzqNvKkzzvTgGpA63R7tNIXdeW482chjM3/LNvWtOEzXiW904kuLuRtmWPyoo/wA9K55mkdzetIi6CUtjd1CrgVeTpXjVdz2aWxLH0p2awOhEi5zV2KP5azZoTqmWAqwFwfWgRMg7/pTtuKViiRRj6etWIl7iqsBII+lOWiwEvOzAxmpFY07Ejmz1oX5e1OwifO4ZIxSbvl9hUkiBvM7Z+lPb72PanYY07fLyDS8lQcA0WJGSK2Rs+X1oMeDUFDCP4aX/AGakQ0fIfrSsBHgjGF5/+tVQepFRaFbUJftFhcFS8suMxvF/LFeN+LreK+/4mIfyb+2hy8MfIuAD6V7tE8aocURBLfzRxmIqYW2t/Dn0qxdWksWsRWEbbwkUfK9VJFdhgfZHhqOSHwtpkUwxIlrGrcd9ta1akhRTAKKACigAooAKKACigAooAKKACigAooA+QPjRqi658W79raF/Lt0SzIddu5x1Irh4I0+0vI0hljibC+rNUgbOlWI1K+WXUZd0veMeldxp2UZJPJ5Hywx9vL/pXLW2NKa1LfnPM+JPu/3R2NWY+ABXjTPbgrImHQ05F3N7VkalqMqvtVuM7unWnYLkivsPJGalE3PWiw7j/PEa9af54POcU+UYSTfu2xgZ/WrcNxhVBHTiixRZRhg5pQ555FIRLE4PJ6ilZsEHNMBVIxwc05XUseeaAF+0AAt1J4pZJ8xKTjHcilYgZHIrI3Vk7YoE8fmOqYd4xn5P5UwJjOixO8kqpt6N6VWF6j7JLecPGTt3HvRYzJ/tC7MyTAY+YjdjFZc+tFGaWF0kVOfvVXITzl2y1G2vJPKG/wA0Advlq1NtXOcc9KwcbGqkRH7vDflUZ5U9+9ZjZWju1tLpX3CBt3OTwa4/4meHnubOTXNJuB50DeYURcEfT616uFkeTXgeY3trpGr6huglaJr+1EhymBHd90/3TVCW6Et0JcNbXEkWCPQr/wDqr1ziZ9keFLlr3who1y7B5JLGFnPvsGa2a1EFFABRQAUUAFFABRQAUUAFFABRQAUUAFNoA+Pvi3eXEnxN1ZrnbvjbygFGOOxrjdOSC0064u7ndu/5YemfU1DA6nwrZPNbRyXCBPtBLFsfORXZjbbW4VpOp247iuKszopbjY1X+AY+tWRXlM9lD15OP5VHc6p9ki2iIyN7dqSiJysRQ6plv32c9l9KunVUVPvKn+1WnITzDk1a2mbYHGfXNTpfpH9+VQewajksNTI5NSic7hKvH3qk/tTcgVCkmT09KrlHzF77b/o4OPl7ccVcWXe5YMOn3fSoNFItwzAqAxpxmAfrkCpGTwy/u+lJKy8NupDI/P6ZIOfSkWYD6f3vSmMct5HsK7agmnPkbpXQIv5UEGTqPiKC1hZmnEhQfu1+7n2rnb74hW0cb2+myCLZ800h/kp7tW8IXOSdSxzLeOZLieaSWWXY/Ehkf5j7CoJPH149ww0pS3G3cx6D6V1qgcvtin/wkEu/9/dSO/dAd2BVyHxHNNapHHdvGN2Mbef++u1V7InmLcF1dfaXl/taRdvH+t+7Xf8AhvULWw05ppb6WTb8plmfJGfQVx1KZ0U5HRm5YGKWF/Ojbhm6Ee+KvckD+deZJHcjO1SzEqZRd2Kw7e9+y3AhJ/dMePNbCg+5rpw7tI58RH3Ty3XdE/snxHKm5ZI5szwohwF79azbjypP9PP7y1CZmij+8te/E8Vn1P8ACjZ/wrLR9jZBjJ65xk9K7OugQUUAFFABRQAUUAFFABRQAUUAFFABRQAUlAHxL43im/4TnVYpMNdNfSbwPujn5TmoYdJN1BbR7swWQyN/KyHnNQxo6hNTS02xWqbpXQNuJq1piTPGWm5c9cnmvPxB2YdamrGMYU8CpTII4843DPavMPSMfVtcFjDuhbORztP3a5yPxC8ty+9h5Z967qVM4pzItR8Sxxw7bVT/ALResF/EF9LHtEq+XXYqaOZ1Btrq91Z3XmiTkj0q23ii6l+dvmK+9U6SYKqx6eKrn7hZhGeqjvW9pniRdoRn+XHVv4amVLQuNXU67R9aE6YWVm9fSteXUBb+ZKDncQv0FcEoHfTkLp19vdI5JNzLxx0JrftvLMR8z5MetYS3OrdD7OaNi5i+fZ/tVOZF27Mbt/fPSsyjJM0sM21mjRAetRS6qLfbsdWLDv0FWkS2VJtbUzfZ1j3sSN2zmsrWfEXkWjQznzpD9xF4FbRgc0pnCa7f3cgVrwcquRB/d+tYCqDZpuy0h5dv7tejCOh5s5XZNZ6BdakVEMexS+yun0zwPId21TBFt25Pc0p1OUqNO5qx+AYEtFDMeD95un4CnXvhKOyiiiiZmz821uN1YKszZ0kjEfQltHUDdvbrmtfS9SS3nD3StJJEvPHFKbuhR0O20HWrWdVtJSv2m5ySfRK68bYxgcgV5VVWPQgxUI2szd643xZpggjmuDAJrcr+9B6KPWppv3iaq0OVb7NdaNFaXd0sjI26yuJicRr/AHM+lcXcW4fU5o7ZFLyfLILc5G32r6Klqjw5I+kPgQHT4V2Ucsflukki4J64avSa6iAooAKKACigAooAKKACigAooAKKACigApjvsQuei8mgD4d1XTrmw1+5hnQtJJcudxP8JJx/OtW3VLS1eFH+YREsT/DWchoboWJfK8sM/lHJnf8AkK7ZEAQdv7teZiWejhkPz8vNV7+7W2hLRlUcrxu6VyQV2dctEefardNPdv8AYmbyOiq4/OsxNOuZz97Z7Yr142SPMldsnj0G5lkCnp6etdBY+Dw2wPFjd0AqZVEgVN3H3XgjIP2Zt/P3c1hXHhi8giP7jp+FTGqN0mZP2d1YJ5Z/KpRFKpyPlGa6b3Rly2Zv6dNdxNH5D7R2YHkV1trfTiGSK4DHzV+Z8dv8a4ah2UWa1pJEVwPm6bR6VtJNLIV/e5yuWzXHM74Mv2CgIXUKD3onkUbvLCk455rJGpmtcndiUfvD0xXLXzXsjCcORGOfetomEmYNzrl1ApMYeJd23AHMnvWbNdzwBVOGuB85bsntXbFHDIzPOluZzDGrzzOfnz/WtTStCu725/eqBHn+EcZrSU7IyULs9L8O+GfskaKDv7n3rs4tOhWILt6V5dSZ6NOBMNNjBz2xj8Kgk0q3D/u1X2rLmNXAyrrQ7eNBHJGBuP3uuKxL7SLKyXL75N5644rSMzGUCpazWAvYsxxuQNvmYwwrvYLlJIElD/nUVgpluFtq72OVbtUeo26XdsYzwGXgjqK54nRLY8w1TSpdQs7zSph5flqXQ/wnH93/AArmIbaBLZFsoSk0+EiVeo9jX0FB6Hg1FZn0d8H7CbTfhvYwXMflzh5d6+h3mu6rtMQopgFFABRQAUUAFFABRQAUUAFFABRQAUmMjFAHyt8ZYEj+L0sEMPlrJFAoUDGTz81cUkDL4iuraWUrDv8A3ko58z2Ws5DRvaJHuvD5a+XEp/1P933NdQuce1eRiNz1MPsO39xiuW8Q3ZdeQDGG2n2qKW5pV2MiysHvH8wEMn/LM5rcttFDwHzOSnTHWuuUrHPCNzbs9HTcsk0YIxxjtWxHZQxrlFJQ8Mtcc53O2NMtpaReWFChU9qqTadG4b5cr6HtUplSRh3egQP92P8AOsm68Ng52bQ3oa6IzOaVO5Q/s77M/mbDk9TmtWO6kaNEzxTvcUYWNK1m804kXy2H3HrcjkCquBk4xxWEzrgWoJDu/e/ug3TFSvEs0Zj3g8gk461kalOdM2zJtwSTz6VzcyvES5kyfTsPpWkTnkjnruHMu9wzf3RRBp26XcRulJyfSunnMHE3tL0CNB80YyepHU10VpbQWkAZANw9K551LmsIWLf/AAkllaRDM6oc8jNQf8J7pxJ23QbHpWXKb3G/8LD04JzOat23jO0u/wDUuGHrnpScGXc0INSS6HzVFd2dtPBz8w75rG/KyJHNPaLFf5hwqj2rqNMnn8rBTnpjpWs9Uc8dzb3k46Z9BUnRRXKjpZz2qaJ9rmYr0bp6qfUVw6od8kb4jvLUkbCuM4/jr3MN8J41dHvPwxlim8BWDQz/AGjg5k9Tmuur00cgUUwCigAooAKKACigAooAKKACigAooAKKAPmn46WcMHxRjuSW3T2ifL6YzzXn94IYILUB/MnuV85z268YrKQ0dD4atgsOfU8mugKhR8p6141f4j16HwlW5GI8njI4Poa5i/iNzJ6Y+971VMcy1ptqq4KjIX24/CuhtP8AaXbRUZcIlr7ZDCu0kY+tUrjxTZWW1WutnsBWSjc257ES+LUlO2O3nmDdCFoHi0IHS40+4jx321fIZykQjxlp+/ZLuRj/AHhWguqafdRbo5EP0p8hKmUL6KCf/V496wLmRrU+i+1VFA2WLLUN/wAu/cPeuw09squM9KzqF02bEEXmsD2Vs9K3ha7gWjA+f26Vg2bmZf26RqcwhW/nXG6pEE3HNOO5DOVn1EefhSC3SrtlP5R3nmutowRJfeKobMY6v/dWsY67rGqHZFJ9kQ8VUadzNzsZh1fSdNfNyHv7hD0LcGmP4t1COOG6h0uKC1kz5bFOGxXZ7DQ45V7Mii8dTyTbZrKCRD6LV2PWNKvpsoHs5D6dPrSdEcMQbFl4gu9JkEV1N9oi7Tqa73StfjuoAqsrZrzq9Gx6NOpzItlY5Zt4C1oQ/IBuHvXG3oVY1beX5MCP8aUB9+c/L2rE1HTb1CvBJsda5/xtp/8AaATUItgaeH5w4wWx1XNeph9jycTud38JIo4/BSGByYXfcqn+D2Fd1XtnALRTAKKACigAooAKKACigAooAKKACigAooA+cfjfGb34hx74VMFtbrub+Nu+P92vPl0x7y+WW4ASGPA4GB/uisJMaOwsV27kCKipwB2q2eRnArxZ7ns09irdpvi2k/LWT9iG48bmqohItWqtEcE49qhvNUS2YhjjHSqtdgnZHMzahe61cNFat5cI+89Oa70LQ4Ruc3N13zzzXT7PsYOZUufiIUx9gtBDj+9Th8Qtbls5J5bJJLfcFaQR/KD6Z9a6FRMXWY3/AITXT9SxFd2SxH1qJoLcyZsLjy93oahwsCncsQapqNovlSkSj1XvU01+86j90236VnyWN1qJHaSDEyfLjtXb+G7wsoUmuatsdNJWO1sIDKeAc10sMICgMxx7VwXOsZe2yvHwB+NeX+MFMKyDI5OKum1zESWhwr6NdAGSAZx1qT7He/ZjIZPLT3r1fdZwamLPLb2TbmHny9zS6VaX3iG6WBZVt7bd8/PzVtHY5pXIvF3hP/hHr2OWNC1rIPlaubNzK4WCSaRooyfLj3fKua6ovQ5JbnReCdBn1PV/N25t4PvHFaviTRo3uf8ARYcPn5sCspTNqVPQzrOy1GKQW4gM3rHium0qz1GzIlEbLH/cYfdriryOylBnd6awuwMnGBziuiijfAxyorxpnfFF+1/edPujsasY2KRWZQxiHXYy0yUIkHkuivEc7EZvm3f4V6eFPNxSLPw51z7B4fe0NvK+Lhz9/Krz0HtXolpdx3ce+M8V6lKunLlOKdO2paorrMQooAKKACigAooAKKACigAooAKKACigD55+LtoY/HbNJk/aMFEQfMcAfpXLm0mt0Q3B4/5ZQ55jNclQtGlaIfJ/e9/WrRwFrxpbns09ijLGMEp8p71Htbs1O5VircloIs4rkNX1GIsUkft+IrqpHPU2Ofl1q5aL7NYo0UXfb1NbfhLQrC8uQ99lpBztavQvY4HcpeLvD39jaqXVNttLyrentXNeaxzHufZ1KZ4zXQpqxhZna+BvCiai7ahqgEVmi7dr8bqdr2h6dFeEaM0jN39KwmdNKJWtbC+gtwN0vmluQU+QDtj3rf0u5v4GVZLNJB6mueozsii8RlJJnUFR29K1fDUO2UYHHpXn1XodkEeh6dtyfm/Ct6Jh5eQDXCdDC5x5dcB440xriyEkafdOaIaSE9jm7Ty/K4jbJGCgP86zr/Q7nUHPnOdqj5Yl6V6aZxuJAnhiNFIL9hjcOlSnTIIpiAu3C/Ls4rfm0M2hxjtniMFwZJ4uytWeNL0Gac/6FwM9BWkZGTp3NS0klGn+TpFkYQD0ZeDWlYeHbrVZMs7Ie4rnczaEfdOu0DwgthFsuFEx/vHrW1faJC8I2gbx2rgqzNoIz49ICz/LiP14rUS028eYNvpiuWRuWUi96HwI/wDa70oiKLuc9a0vsKSph5LX7QuCMjpXo4Y4MSUrLbp3mWwRP3cmGra0PXIU1yOAzDdKduxf0oo3+sFTjfD8x3WaWvoTxwooAKKACigAooAKKACigAooAKKACquoX8Om2b3Exwq/rQB8++PtSPi7xVZyqHj8vMSeUccdapmCPzWuHiLIPulz1NcPPzJnTyWaHD5nJNO6c15DPUpjHTe+M1C37voKm5oVbjEsOCcFe9Y8ukxXzkbF6cnHJropyMpRM4eFHLZUGP6VMmjXVizMFZohzlq6va3Ob2dix5slzFFbX2THJyd46VFBY6VHHMws0ZDLsiwOc1rzi9mbQsGlmVAo8hPvVY+xIIETyR8p+9WTmdEYDPsPO48ipQyWw+Vdy+prNu5pYoxwNeXLIoxFnmum0a3WNwB90Vx1GbwR19rGkbgha27cbY9zHArmiayI7hc4HaqU9ulzCYpOlA0cdqPhhraczWp6c4rJfzon3ldq11U5XMZonQRz4BC/jVr+z0Z/kx+Nb3I5R/8AYMRX/VfitPi8MQq+7BVahzsHKXrXR1im2q/6VrQ2DQcjHXtXO5BFGnHcLH061E85Zq55MuxYMAZM96YImHWoYyVE2j1qKZsfJjmhCZnXPBz+VWvDiLNHLFdZ/wBGO+LJ5Y16OG3OPFfCcz491S58NzONwlubr7r+h9ar/B/T5LzxKs927vIP328nqa6YR/flP/dT37vTq9k8QKKACigAooAKKACigAooAKKACigArz/4rzzwaVZ7M+QzkP8AXtWdX4GXT+NHlttxd7hGHcjjNVJ5VN8nnyb3B+W2UcV5dP4TvrfETqWZm4ApyfMprjkddMAvzUHFZGpVn+70WksSgboKu5mzWCoVzUUln5i7+MelVz2HykD6WJnDvj7uzbjtUkek26hcxqzLzgVspi5S8LFs/wCr2ila2iSMl2qikY17KfnEPKpxVUQSzY/uVMnYcUX4rcQptAwa1NKbacVys3R09rJ+7rXgkUgfSoRo1oQXH7vC96SLmoe4h7WIeL61i3egpPC69MUJtPQnc43UbGbSLs8fL1zU1jq2/h/5V2phY6K01K3Ye9agurcx8ED61m0S4kRlj7c+9Kt18uM1gxWGeXLKmUfFXrW1kGPMrIDVTGMCl2AUmIic+XxVGZtzetAFK46Fe2M1S0mWONLoM2ZJF+Tnkc124fc5sT8Bz/jp5tQtdyFG8uQZbbXV/B6xP2ya4CnakWPxNelH+KiL/wCynr1FemeMFFABRQAUUAFFABRQAUUAFFABRQAVzvjrRBr3hG6tv40HmofcVMthx0Z836dqPlyf2dISMOSuTj9au7jCVkLr9ufP1UetefFe6dtZO6LcXy8VLkBcAV5szvp7Eg6ZHFR85rI1GGPd7VWERjc8ZqiTQtpicA1dGcYbgUGpMBGmKm80R7eijvTQEEupAfu8nPpWVNcPKDu6fzrdCGQWnmMDirO1YztzWMncq1iN23SYFaenw5UseKhlI3IF+XOeBV1SWG9eg9KyNug53Y8Hr1FIkpVulSIuLNkADih22pzznvUmZWvdPivrRlnQMpHFcZf+GJbJwFzt/hNbwkGxUS1uYT6VdiS6LAEkD1rcnnNe0sZckzOSDWvDYFRu2n5axkSi2EPynKgj0qSNstn7rVgUThlTrxnvUq1IEU+NvTmst1y2elMkr3jAp8o6cVhalPcWEJjtIgLmdSiTf88uPvCuzD/Ec9f4DKSItoWjWFxKFkQO0vPLtXrnw7sRZ6bOR/GR/KvQpyvWRhWVsMjsaK9U8kKKACigAooAKKACigAooAKKACigApCMqRQB8yeMdLXQ/G14giPlBsbT/tdDWRd7bSdrqHFyFbyg/rxzXnd0ejU2izRtmPkr61aQ8Yrz5o7IEn8NTrH0zXOaCogZzxTGiwvXpTRQ2JN3KMNvrV1DjG0eZ70mWTrC7c8YH92kePfzu6dBWiE0UZYCsmXfc5p8FgWOTk+gNEpFJEzxeUMdO9Z00o3s2MVCBjY243cZNdBYLiPmnMIl8b92EGRW3ZpiFUReT61ETST0HS2+0/MCTTDb7UzUyRKkRbsfKDSb/wBKzAtwuMqDU0sa3PyyYIqogZF1oMJk3DI/Go1tPswHlHdk42tWnMTylpP3LZXC5PSpZJ1yodx7AVLZSiL1XDEYPpUsKr0DdKzGSXUSTQ7Wk201JdihQ2akQ6Rvlz3qk33qZBBIvz+1Vr+yF9ZSIflYYZX/ALmDW8XYwqfCYOrwRnV47kIF83sPWvTPAkzN5kefk8sHFdeFd6w8XG2FR2dFe+fPhRQAUUAFFABRQAUUAFFABRQAUUAFFAHg3xbtjN4luxtOSiYI+lcNblZbMW8/7q8j+Qc9R2zXm/8ALxnpy/hxL1nv+z4c/N3q2vWvPmzqgTL92ra5NYs0Qu9ccD8Kd5XmB09qhG9iOO3CjCj8Ksx2p4x1x8tMqxeSMMjNyrU2WJkjXbyT3ocgsOt7ZM5f79SEbW4NSUUdUUtZb17d65e4m3tsDbq2gc8y5ApkmHHArpoU/wBHxnHFEiomnFgwjYN2a1LOSS0Vd65xUxWpczRMiTIdmKhJ3IVqpmaM2VQOOlZtzeNbuCo3Y/WsbFFu1u0kO4Pmr0M3GfWkiy1wRmoyoKEDFIRXePY4b0oSFPNJbB4yKlmgojAx8u3FN8wr8vAz3oEM8zjn5hSbt+F9OlQDJi2EqrJx+NUZjVfIxinp9/0FWYyOc1mEoqFl+ZJsBq9I+H9r/wAS6S7wRv8AlX3HrXoYBe+Rjp/uUjsKK948AKKACigAooAKKACigAooAKKACigAooA8i+LFq0Wsx3H8M0O1fqK81azEsn2hNuXb51PXivJqaVz06avQLY+RFFOztlrjmdSJ4+1WQ20881iaxH5zx0NNj3ryeeKg6iwivkfKB/F1q7EEVTuB5qRk69OAPanehNIByn061BIX87mrSJMzVrjyrdg5wpFcfpfzztLKcLnbXTBWOeep0lqoSUVqrLz7dKlouJo20/lIAvQd62mu08oYfPFUUxFuVj+ZT196cLkcndWMgFkZJBlazrm386P5B8y9KhDObsL5rfWGtm+VetdXFMBw3SqY4l6N1ePK0+NtvBrMoXbuXHc1Eu/nK5x+lSIXnbxUBBf6ikCIJFP3gOlMjjYN5nK0hsk83zB8pqJ5G39OKZmMD/rU/G1cmrRlLYq6lb/abUKE3AsK9T0i1Sy0m3t4/uogr18BE87Gy2Reor1jzQooAKKACigAooAKKACigAooAKKACigDg/ixZef4ZiuR1tpd2favIIYtrNJ/Djp715WJ0qXPVwr/AHdg3d6euCa4WdCJ4uEalVjt6cVBuiUFWZT6VMi7+azZsmXVRmCHIq6kQ55zUFi/xZpKBDQ22opTwTVoRyHiOUNEVAYH0qjpi7VRW6N82feutbHOzbL4AOamhvwvXG31rMpMuw3g4ANW/tyqB5nfpTNBGvEVPkOPalF20aKxPFZSDY0INSQnrV77cnldPmNQV0OR8SRPHqsN5FwhjIcVq6JqUd1DuPb1q3sRE6CLlsr0qbIJ+WsTQmAp24dMUiRNpxUfl/hSGRGPqO1Rfd+XNBLZXHCbR601uV460ElfPNWEf92uecVRnLY0NK/fXlsP4fOGRXpMYwgFe5gPhPIxbu0Por0TjCigAooAKKACigAooAKKACigAooAKKAOb8fxed4G1JR/zzzXhX/LDr1rzMZ8SO/C7Fcyqsixt1NTA1wHcieLljT/AJsGoNkPXMYBbvVu3/1/tWZoi/GPmxnGKn5X5R9ag1GjL0/+HikSQb8Piql5JwExjFWhHO6nEW+ZnwB0qjA/7rb3FdfQxJLq92W5P90Vy8Hj61W4aC4haNM4EwrelT5kYVJ8rOwgvFuYENtIGjPQir/nno5+70rJqzN1LQiF4u4nPK/xelcTrvxKa3umgtIPMRDtMh/irSnS52YVqvKafhnxvHf/ACS7oJ/7h6Guug1nczIrGsa1PlOmjPmiXNUmjuooEG/5fvAVRgZ7U/ICKx+yUviOlsrnzwOcECti1k684rCRqXU+7jNPPTlakljTmmsw2HoTQIg3/LVdpUz70iSAphtx4pCwbHGKYis33zzTlb24HWmZs2/DRQ6xAmON9ekCvcwHwHj4r4xaK9E5QooAKKACigAooAKKACigAooAKKACigDL8R2j33hu/to22tJCQDXz02ET17V5+LWx24YoyJumUt1HSp91ead6LkRxye1K2WePtnrWbNUWMDbg1YhYIag0RoRMrHzKnP3947ipNBnHWgN7fepFEDNt+ZqzriR2fJ6VcSXsULuHzB0zzxXIXs76ffMpO3dXZEwI7i6aayZPl+bvXONp1uwKZ+Y12U1Y46upa0m7l0GTylLeSf4T2rqP+EhgdODUVYDpz6FV9TQwsn8TdhWUdAhviAwxTj7gTSnuOXR7e1lHzbWHSt60uvs68yZqKvvHRS93Y7jwxaG4g+0zBvn4Ga37vRkmi4UD6V5+xvfUxoN9lN5T1u2ku7r36VkzY04224IOas7iTmoJE5zg0yRYt27o1BBCw2N8vSsucSRzl8fLSES8HpyDUGcB1NBLKvm88VMh3jAPAFWjM0NIkZNSt8HH7wAV6qK9vAfAeZjPjFor0TiCigAooAKKACigAooAKKACigAooAKKAGS/6l/92vmq7yty8a9FJFcWL2R1YbcpFwT05pysDXlnpIvR9ADTmJ3VkzREsEysuw9RU6r82QaRaLkPQj1q0p/h/u1mbDn57Uzvmgogn+YHIzVNU3thjj2qhDJCvPGAKxNQsoLuPDRBv51vBmbRgS+EoySVkmAPbNSWvhuG3lA2szCuz2mhxOOppy+Hbe5QZT5j3psPgy3ifnNS6pUKRbi8MxPOFhiCY6k961IfDKSQ5HDiocxyVhLjwQJ8FvmPTIqxafDrT7SRZiXYZ/iPWpctCoHcW8UNrEgQbVHAFWmA9K4jqZianYiTMgXa4/iqpayhcRnr6ikWjWikcrjgiratxUgSgjH9ajZfOxzQZsZjh6qtgjLdKRBAqtzj7vaqtwdoxSEUYz87bqlVvLU+9aEmjZSNHLA+OUbivVbG6W8tFlU9a9XAy6Hm4uPUsilr1jgCigAooAKKACigAooAKKACigAooAKKAGt9w/SvmjUW/wBOuONv71//AEI1xYv4Tpw+5nn7wPegHBryz0S6jFgOasM2QAD1qDSJHGPLYnqat7zx82B7VLLiXI5eVXNXBKNny9c1idBIeOppi/d4plledsE1B9zvyetUSQydcMc8VVeANmT7mzkVpEybI28x5OoA9qljTy/n7mtNjKRcjhHBc4HpV6G13Dd2pBcksYQm5v4gfyrXQqU6fhSIZMqoqjAKf7NEtwFjfj5U7Golca0E+0bvlGWzVqGYk4Y81PQ1UiV1E0RrEuLfyJdw/CoNUOt5Bz/C1Xlk4x1qSxPO4GTx6YqQTbcHOaDJgz5TatVmyF+akZlWZyvQ1TlkzGfWkBUil3YOPrVwSBiAcFWq2It2SlZ/K9+K7jRLn7JeeUW/dy/kDXThZ8szDER5oHVClr6M8UWigAooAKKACigAooAKKACigAooAKSgDhPF3j2Gy32VgPOk+67joK8duzvnL+pya8vEVOZ2PQoQ5VchON1Rke9cR1FmF/3eKkWTmkykSfxdKlQ4xWZcSykg4NXRMucevSoaOgl8zHOfzpDJgYpFlZ2ydwqD1MlWZyZExwynuelSlQYsY/OtEYNkSxqr/PViOxWZwArAdqGwRo/2bP52AB071aWN1iBKAN3xT50VYYHUTyLs59q0dPiEqh3XGO2eKlzQuUueU45KZqGQ7wSVqb8xAz7pXoG7H1pgby5cKfloYFlLgOPn+RhTbj99HtNZGtzHJ2zCLOG7VNb3Rb8DSZ0botlxIPwqOM7UwWyRRcyaJ1mKpVeSXctSQVZm5xVKWT+H86aASFEfI5UVa4C49OBVMks237iQHcT3zW5Fe48vPrTXxK3cPss9Gjz5a59KfX1J8+LRTAKKACigAooAKKACigAooAKKACsPxjdS2XhK+mhzvCcYpS2GtzwozZb5z81UZiMD614H2me3JaIiOO9RNSELEd3rVhHB6djTYi0CenapBL8/SsTVE2cD5amhfj5uKTN0T5wvy8riqssxZDjg+tJFD4vuDv6U6UHbgDmrRjN6DPlCgsPnHrTXl/dj0FWc6lcsRJtRJ5WCr71Wm8RQW85Ef7wetZHZTRpad4ltLuXy3cL/ALRrf8y3niHlyqQ3pU8hq4kGLPTlLSyrx3JrOk8X6ZCCsZBNLkGojbPx9HKClxCAvQGujt57K+t/3Dqcr0FFuQidMiktxEfnzwOKqRfxjzNxrQ4eox5cttH3jTwWCLnqOo9ahGhSvhvUnG1hVGGVlODmpNYMvxXfrn8KkE0YJKndmoLHPNlRmkfNBkU7iVF/dNx/telU0wrqN2auIixt+T5GqTfnGM8dRSAkkm8mzeXIrc8JabLq98sjIfJi5YnpXVhqXPMxrVOWB6lRX0R4gtFABRQAUUAFFABRQAUUAFFABRQAVS1fTo9W0m4sZfuzJtoA+e/E+j3vhvUfJvIm/wBl8fK341j+YGCYO7dXj1YcrPYhU5okxHrTGrjNSD7p4HFWIflOR0PatOhn1Lq8jrSeaPPKVkbInWQ1N5gTrSNkI87fcHAqHnzT1agUmX4D8mcYxTnJP8ePaqSMGymf3jY6496Lm4hsrI+bhlParJpxOevdXkv8RRs3lr2BqmroE+cYxW8KZ1cw4sOPLP5Ukd9qMHzQysqqeQTT5DRTHS3FzdQp55Lbud5PWrtokcWDJGPlpDcia4u7Zvl2AVDBqdzpLrdRMVXvzUSFHU7XRfFttrCeXN+7uB69xVzCRySSoSEbtXKYVIopRakvmk4zzitCJ14wce1BiSSYY/NWHcBonAV/epNYkkcv9zrUsbhW5pFslQmRc5qYSHoTz2FQSUDEpuOfnzTngHmb88elVcQbcNgUAyJ8jD/gVSNnT+G/DcOuuwuGbyI8EovevSbOzgsLZYLeNY0XsK+gwkLQueLXleVixRXac4UUAFFABRQAUUAFFABRQAUUAFFABRQBWvdPtdRg8q6hWVPRhXivxD8J2Phq4tTZReXDNmubER9y5vQfvo43j1pD0zXjnrDCuRTo2P5UzPqWI922n7VMgbFZGyLKfMOetScbdrfgaDR7Fdl2N8xJzUoPUd6DNjpJSsHGaqT33kLw+a2iZsjj1WC2snmkkVfrXGX3iRdTvD8x8teK2hC407FuxS8uA00NviMdHPFW3t7ww/vNskhFa35TeKuVrazvoYtzp8p6cVbntbny42FpI74x8tK5r7MrQw6tKfKjtX2rwoK09NM1xpcbWGfWndFxgaEHh/WYn3NAJD6Cq95a6vZwt5+nO0P+zzil7pm04mG1+0LLInmRt246V1WgfEFHtha3T/OvygtWU4djnbJpdcWS7HllD9DW/YanlkPrWM4GJt+eG571l36gvk/pXKzaJWSRl4/WrPOM44NBoTREouwGnBhxznbUiF4L7gOlB+dTQIaJEO5ajaTA6ZyNtMGep+BLMW+h+djmXvXUV9LQVqaPBqfEworYgKKACigAooAKKACigAooAKKACigAooAK89+L1l5/h2G5zj7PJk1lV+Bl09JHjb9Ny8Y7Uh+c14bPYWwf0py4znFPoSWc9xTselQbRLAzsKgDNC8t8y1JoRPy+1qmQcfNyB6VUSGR3dz5VocCuI1K9uryY21mPq/92umOjOaRVtdIN0dmoXbvjqo6Gt+y0qzsyDGqBT/D3rWUuw6UTThkEnyfdQHgVqWnlifeUGTxXO2ehBHRW8Vs64Owgc7dtXbE2rS5aJcZIGKnmZ0hbmGFSwjHLmo40hlLvsCpmpuNOxdhNpjKNyOtVJGjEz7SGXuO1S3qSyje6Zpd2nlG1jkQ98dK5jVvA2jtmOKPaexWtoVbbnLOFzj7jwlc2D+dbtJE8X8B6NWvpN3IwUN8rr2rVyUkcTjynT2F4ysDIanv5vnBFcUzSJXjbM24flVuObBw3OazNEWlb0xSghfmXFQMevLDYOepqOT5R9aYiuzkfcpq5kwTVoiR7V4Zha38N2cbjDCPmtavpYfCjwpbhRViCigAooAKKACigAooAKKACigAooAKKACsHxpam88H6hCqb2MXy1Mtho+dN2M557U0OCteHPc9hfCPDDbwaVOeewqegFlH+Tk04H56RqiVc9BVmNRjnrWZY24g/eBhmoJQy42nFVEiRmXDhk/1m5vQ1Fa2CoN2zl61bJSLMdhDIenNW4tGQwMFb5jWXMaxVim+nz2k7Apnj71XYpQuOvSqOlGzZXRjhYLJyo6f3q1oWiaO2Ksqlc5Wg2uSRSxxBS+QGkIqm10UcBV+Q9aRKepDDOoSRFYjdz9aet3LGRtHy9GFJltgRIY/L/hPzelRCH5sLJuHpWTZkxdRty9ru+8a5V7MLIWT5GFaU5nFVRahBbDbua1hGz2+T2pVCIDFRFOMk1IsSLjBOfesrmpYj56HGKUYzUjJoSVkYDunWoMgyAd+9NCIAS7lU4yK1fDVmL7WrW2K7g55/CumkrzRz1H7rPa1UIgUdBTq+iPGCigAooAKKACigAooAKKACigAooAKKACigAqK4iWa2kjYZDLigD5h1OzksNUuLWRdvkyEY9qq54rxJfEz1qXwgvH0p54GR0rPoUyx97jbSq+1uaRqiwH4Oami6Z71BRY+Yryaq3CAAeppJEyKhgydxTjtU5+VQqim2OI6MbDuxVgOVQ9u9Zl7C/a1kjyeagDRvNvXsK1ig9oM+1RplOd+PvCom1TyV+Vjv71fKy1WiTweIJvuE7gOlKdYEsuAJEIpWYe1j0JoLwKyfJlj3NWvtXPIxUsXtCc3YZh71ZjPPOMe1c0hxkWcIRyawdU0/wAhdyfMDSgRMp2sYB7gir8WfmG+t5GSJcI0I5+YUcfL16VlYsWNtuc1LvGzeRipLEM2w1A8p/OrRDIS3mSKE6LXa/Dez+1a614ekCnj6124dfvEcld+6z1WivcPLCigAooAKKACigAooAKKACigAooAKKACigAooA8B+Kem/YvGskyoVjnUEH1NcYOF6/NXkVlaZ6NF+6Ie2etTKTtOcVj0Nh8Evy/N0qbcpGUpFpjwxdeuKsROOPWoLJ8k9TUEgDc7qSExwYPIuT0HSjd0wpapZpEV2GfpTR8w71JTE28cdKqy5z04rVGTQnmDHoPSqs20xcit0Yk0S4AZakJJkHy0MqI/D4yvWrURkeJM81gy7GhCqquGPNTjefutxXPM2Ra3/IACM028TKhjmsYlS2OedZFumPVPWrqHKg9q6lscwE7fuHinxyDGRSKEWUY65Gak3ZPzHjtWdiribhjNQSP+968CtEiRsb4zjvXsXw80/wCyeHVnMe15+TXoYNXkcOJeh1tFescAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFAHmvxi0f7RosOoR8Nbt83uDXie4FfQ+tebiY6nbh9gb5eO4p0cn94VzHQShsU4fK3P3akosq4b7op29em3kVJdy1BMCh8z8DUAk2sc9KkY9THjeOtOWbAOeD7VLLTGtcb16UsTlnApFRLHlnmoJ0IUmgbMh5JnlCLHxVmK2vZGw0PFdBk4kqWl7GCBATmhbe8/jByKGxwJYUkLcoRWpDH8nTmueTLsSCJll3MtWFRxnPFYM0EJaMb+CahkkYj5ixpIiRn3DDccIVFMicluDXT0Ocn86M9D83oaaGzNtB2+1Iq5G7srDA471NCULBWz9aVhkmfkz71CWJJwuaZLJ7KIy3MUEYy8rgCvftKBj0+KPGAi4r1MGjgxJdpa9A5AooAKKACigAooAKKACigAooAKKACigAooAKToKAPC/it8RItRu/+Ee0seeN370r7V54T8wwuUx1rhxJ14cG4XH3v6UzzcNg81xI6WLuJYVYUkkMtIofGSrelS559c0gLHGzr+FI3O1OmaRaARP5uC3Ap2OOtQyloNEb846GrEcZicHFSa2NBMRrukORUbLG6eoNSWPgiQfJ5fBrSitn5bpV7DLsVqXXceD2xUyWAMu0pSuIstpkJiCbAGqKTSvLOWGB7VjIkje2VX27eMdarSxhjwCw9qzNCKSFSBgVTljIkP930q0YmdeDe+Izx6Vn5MZ+bpXQjG1hyfNz3pe9FhDecZzytSQSAgdj3oLJxN8+Acg1HvB3HOOaZBcsNZg0PU7O5uQW+fjivoHTLqC902G5tnDxSLkEV62FVoHn1/iLlLXWc4UUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFeV/Fvx5Npkcfh7RmLajd/I5T+DPagDyGPT44YZrKE+Zcuv+nXPcH/AJ5qfT3rPt76C7VjavvSNtmT/npXLiVodFF2Ji5PA69qiDfNzXnI7BYz8hqaKTC9aYE+56cGwwHU0hDw5L1L9o+ba3IpFxY938xwB0p6j+GsTctKRgVMU3LWRoiSKckhP7vXirFvHvj9QD0rVBc0oIkjx/KtGEKJflBx2zSYy2jIF296c7fOF6n1rIokxN5eOKsHaYwvJ4qSWUpl+fGOnSqvQfL8uazGV5Y0jYhF+Zqz7lTxjueRTTJMu72hMIELVk3ZbYMptWuuJhIpJIT/ABkelJ5x8zaxx71oZDlnViPmzVgTZk/urjn3qrBzEoO6EZPy+wqaHYFG3OPTFR1H0M77W8188uFntH/d8cjjv9a9R8A6nN4ZkhtbhzJpd1xG+c+U9ezS0icFU9aH50+tjAKKACigAooAKKACigAooAKKACigAooAKKAMDxh4ntfCugyXk74cjEQ9TXz7t1GXVHnnbzdV1L75frbRH/2agDmfE+tfZI5NCsGBhHE9wn8f+zmud0e5+yagbfZhJeuKiovdLi/eOoD5H0pG2jnFeTbU7+hEz8Ugk3d+BTEWo5HZfvUCQrL6kdKkCQOwX5jwant+u8HjoKTGiZSTIF3cHvV6JP4qwZ1RLSR/KemKkgYYPHNZGhMvGPerkO6AGPuTmtQSLUkgHG/a3Zqdb3LnJMvmY7UIZLJcuswwQe9W01HcuPLbB6tWZaLEM22Mnf8AgaU3XT5GzU2IbuKHbyST1/lUYXan97NYsZBcfIw45rPklH3th3VAMxL6VRjyxgVjzzZXHf3rvpnPIz3Iww7jrURlTgYy1bHOH8eecVYDn7+Pl6UAWk3KF/vVFreqDTNIluhkPjbHj+9Tj8ZT+E5HwXrC2GqhNQdvssx+Yj/lk/8Afr1XQ7qTTr82kEo+zuf9TJ8ynuVH17V7Njgvc9i8J6rDcWaRRtL5Z/1Ql+9H/sH3rpKoyHUUAFFABRQAUUAFFABRQAUUAFFABRQAVXvbyDT7KW7uXEcMS7mY0AeFeLLubV9TPiLU4DPbo23TtMb+L0kP061yWvavL4b094Xw2tXwy7n/AJYKf60Aee+RJcyxxwIzvKduP7xrr/7AGj6KyKEluj/r5/cfw/Qd6JAtzMtrlJAsg24fkFe5q1uyPevLmveO+OxHzk+1RsuCSOBUlD0kxxuqffuXK1IBnsDxVuD5VxnikwRaX24rQgnBi3HjtXOzqiW40zGc09fki4GSayNSUbk6/Mw7VJJNvUeZkOf0pooSO5ZIcnDnOBSS3RaUhzsYD7y1qT1HRXEkqBw4GKvQ3h8p84z/AHazsV0JYJQV3YIzVpZXmUF/lUdPes2BJJcMiYRuP4qdHeZ58s4rFgJM/wC6znH92sqe4EfG3GR1oSJZg39wsEbfMOtYU0m45Vjx613U0c82U5HR3/nTAN3TkCuhGA+L72eq1eWQsnOR/Ks2NE8fzD5d27+9WHrFzBeamNMlx5Spmfbyc/8A1q0oK8iaj0OYl02fS7poZgRkfIT/AMtF9a6rwj4gJtv7LfiTcBbTn9Afoa9g8/Y988EXsdzBNcrteaNxFfxxn/VzDv8AjXoqPuAP8qQ2S0UxBRQAUUAFFABRQAUUAFFABRQAUUAFeceN9Y+13sdt5mLSBsmPbnzXH97/AGR6d6APM9c1hIZG1rUEa4iU+VYwn5fMcd8f3RXmd1JLdyvcTvvllfc7GhAegeDfCwsbIanfQMbiUfuOf9Wn97Hqa5z4gaz9laTRbaONGTi5kTpnsg/rQxnK6Bc5hkg/ijOfwrdWXDY9a86qtTrpj8jBAHWm+xrJGgn4Uvm7EHOGpjGkjdipklHGKlgasEpkUE8Vfg+a22nHXNYNHREuoW28cE1YEbCbOe1Ys2RDOzLncSDnIIqIyu3zkmrSLEZ38sDHFMSRmV1ZNpNWZMsxz7EC7ama+KqSVGakss2d6C/zZC4pz6ijFlV+RWTQ7li3nmZRx71Z+0HbK2W57VkT1Ksk7gBS25cVnahcgrhW2etXBEM5O9vlabrkVVEhkyc13JaHJcgbvjimDOQM1ZJdhCmPcrH0xVyOOVmHOVzWLLRYeYWaPcfdEa7mrzbTNZZvEr6hc5CXUn7wR8HHtXZhFpcwrM9Yu9HtNd8OLbkorbd1ncj17f8AATXm0T3eg+IFfy9l1aybijjjjtivQORnu3hTWXivLDXBEkOl6l+5uIk/hbsfwbP4Yr2HTHHleX8xKk5JoGaVFAgooAKKACigAooAKKACigAooAKKAMjxBqw02wIQjz5B8gzXjWs6jvika6ll+zxjzbyQDoP7o9zQB5RrutT61q3nn/VINttD/CsfpW94K8Mi+f8AtW8hdrKN/kj/AOerf/EiqA6nxbrh0OxHlJu1C7/1XP8Aqx/f/wAK8fukMh2gmQtx6kmkA+PT5NI1AQTcSsvzVppINo45zXHVOqmTlucbuaYTu+tcpqh4IxzkGmeYehUcUFDXVW+Zc5qLe8YHpVWIuX7K+zgZyK07aVVzxkmspRLjIvwXLrNwvytVw3BJBzxXNJHVCQGWKbchb8KFdAojx06U0aXILi4KHbu4pv2jHB61qQ2MScZ9fajdIST92lYXQlF2jLjvSs78N8uPWpsFya21ExMV3Zz61YfVTGeDwanlFczrzXGRiIm4/i965/UNbDnIzuxWkIGE5mYJjJy1OVmxx3rptoZD0jcn71WRHI+AQHFQyi1t2rhflFTJKy8bsisijK8UXMkOhsVfAdtprkZLJreWCZk/dTDKNXoYVWgctbc7fwp4jayiXT7qbZbN/qZT/wAsG/wNV/Ftuy6ut8U+QkRSc9fTmuvYw3Oo8BaoFhk0K4G+J1aWAA/dbuPxr3jwneStaQefL/AF28nkd8/TihiOtzRQAUtABRQAUUAFFABRQAUUAFFAFDVtZsNDsjdahcLDH7968wvvivJrHiRNI0oGGB0Y+aPvkj+VAFLUNR8ld7lpJT8mXbP7zv8AlXlPinxAuq4tLGeSS0jOXduDM/8AexQN6FDQdF/te++zSyNFbxczzIudi/4npXq1xdWug6H5rwsLS3AWGDP3z/cH8zVCPLNWvptR1Ga+n/4+Je390egroNA8MfZrWLU7oN9onXMEWOI1/vn3Pahgcd4pkFp4vjtgPmA+df7tIG456dsVx1UdFMtDrjI56GlJxw3BrnaN0KOADu5oyrk4OKgohOegNI+cc96pEMqM7xcr0rTs9RTABOGolsJGwt2ku3Eg3dxU32pVXAkrCxutAN4m0YILCnNeb8eXjPelYvmKxvAW459ah+0fMWZuK0sK49btSMF8behFQyXcnJMmadhiRan/AA9/Wp/tvyfepWM+YQ3aFhum/CqU+pGNs+Zx6VSRLZQlvpbkfJmNfWoFTBzkmtUrGRYA6etP5XoKkZNGrbvm4qyu4Hg1myyfO4YzUiyxoPMYbtvaoGct4zud2mxgfLvkziuk8OaRa6z4ajt5QJElTKXBB3RH1wK9Kj8By1Xqc1qtrcabePZ3IHmRcDHQj1rpfDMH/CTabNp7SKs8a4VpBnpyK3ZgN0S5bS9RjuY/3c8E2VA9R2+lfR/h64W7laS3yYLhBc2/Pyrn7yihgUr/AMWz+HdYubeSItEfnWWSTI+ldPofiCLV9Mt7or5fmpuoA11dX+6wNPoAWigAooAKKACigAooAK5bWvGtnZX39nWrrLedW/upQB5J8RL1m0+1kmmklme7kRi7eig/gOayPhrpSXl1e6q7GNYR5KyjqpPU/lQBB411t9s0FsGRm+Qc/cT1/wB41wdpa3F9eLaQRl7qRgqIO9AM9H0uwj0O1WB/k8sGS4kz94/xN9B2rk/EXiL+3bsSKDFaxLtt4fQev1NNAavgfw0NRuP7Sv8AiygbAiI/4+H7D6Dqa3fFPiOHRdN8xZpLm9m3eRG6bfrI3+z/AHRVMDw7U5S2pJcuxaR5Ms5963kbctctU2pkq9AKl+8mO9cxuHQYqZMLUFCkDrSY/GgBjxLiqckOTlFqkSwWSRKkF02aoom+11LFcorZJIqLC6jopYFY7iT9Kl+0WrQsFVg1SaEXmw+VtPal+1RcsV6UxiG7tQv+rxUBuU6IDQTYhYM5qSG12tn+dMRKsXPyjAp5TaAEXLGk2JKwiDjcRU42E5ycDtSGLvA+7mhWXdndu96kCxu44qvJK547e1IZyvi6UyXsEPYLmt34e+Iv7EuTaXLf6JKeWb/li3r9K9Kn8JxT+I7fXtCi8S6Y3lrtvYAfJk/vf7B+vauV+Hty1vrs0YyrsuA/9xf4q1M2bF9pbaPrVxa8ugO6Ld/dPINem/DrVtuy0BLGE/I57IfvLT6AaPxJsVYWuoDv+4mw3Q+1VfB19OfCUFr85P2rYD2CBh/jVbgYVn41ntvEN6PtDxhbhwjdyM9K9h8O60dW0aO8l4ycZHepaA2kp1IAooAKKACigBKydS8SWGmjBk82T+5HzQB5d4++Ims21tbraulqlwGPycnFcH4G1OXUfHO6R/MkaNt26mBZ+IU7HTdNgwxaW6kwDzuJVRW2rR+HvCi2yH9zp6fvc9ZZ258v3FJgjzi98yaYtKTKxO5z61veHtF+x2Z1OYb3uhiEEcqnc0AjK8V66XY6ZGfkQ5uSvr/Co9qz/DmhT+I9WjtoVIHWSTtEvrVID1m5mtNC0AymJ47Cxh8tR3//AGnNePa3q13rF9Nf3Z/eP/D2iUdFHsKbAzr3RZj4dk1eZMK5/dA/eb/a+lR2km+3Rup2ZrmqmtMtL0qQDjrzXKdAOd2KUSY59aQEiSHn0pVqShx54NBjOM5+WgQww96b9mBppgiSO1R2xjp1oW0G4+lK5ZMtmIj/AEpy2R6/wmpuWhVsAPSm/ZBgqRwaVxifZY+oHIoEWeNmKCQEQzzxUvT3pXENIPYUjfuiD/FTERjOA4700z5X3zViHDczcPt9qmhixnP5VIxzOvQdaRh5Q+egRyU6/bvFEMLsSjnYNvap73TptHuZrOcYZfu7fuyLXp09jinuekfD3XGvbA6ZcTgXNuuY8/8ALSP/AOxrCvNtr8To5SCsUsvmSLF/EP8A65piPQ/F2j7dGttRQMLtMLcbvvbG+6T/AOg1n6GYdIMV/sbzYJBKOeGx1WrjsSen6vaR6lpN/DHtkieFL20b2/ixWL4SE76Fe20R4+04HH+qBTk/nQgPKvHsx0/xxdsh/d4SSJ1/i46/nmvafhvrnn+ENLt12s3lZk/EtVy2Ak8K63qcEjxqGu7MLv2dXT1xXdWmpW17D5kMox0IPG0+lZsC1nmlpALRQAUUAeWHxJqmrh2N2PI54Tjd7VyFtqdxN4nvdOnkzb2sfmoiD73Pc0Ac78TrpYL3TI4zgiIlvxrM+F6ef4rmb/nnFT6Adffxf2hqulYDM1p5kkaEc7ycL9elUPFN3HfahFYJJ/o1h/rGA/105+8xpIZS0Pw+viDVZN3FpCv74g43Hsv41teLdYGjaNJMAkVwR5UEKNwh9vpQxHj9sk9zeKq5knkOPdia9l8KaKdE082ywg3M3+vlL8MR2+gp9AOR8aeIRrd8LO12iztTwN3+ufu/+FUPDnhpvEWpFXRxaW43XEg7+ifU0+gGl498qz8Kz28kHlM37qFA2dv/ANYV5ro1xmLYfpWMzSBsJw2GqT6VyHT0Drg+tRdBSGPP7oDvmpI2bOTSAmeQNTlbI9qkB+3IpPLPapCxNAWxtFP8ppGOO3pUmyRPGOKf8n8T4FZl2EU7fujmkdT3ouFhnlrnFNK+XmmShp/SiPANWIiZj1XgUhb+9zTJIZHJ+lRp9/kVZJYi2qcjpUnm7jhe/rUjHKBnBGCO9Vr2YquSc9hSKMaCQaX4isrmZcoG/ee3vXeeLfDst/aLd2ypM0Hzxsp++vfFelT2OCZxlhcPpt3HeW/EqHcK3vEmyPWrXU4bnzJAok+Xt3FaGZ6vpl2niDQ4T5m23v4Nku5wAjH39c1zWnwCG6e1kJLQ5SWLsDTiM7zwrqiLp2nQSbT9md7dmPaBvWptKs20rxff6Oj7POiyDn+HOaBHlXxi02K01mwmTI82BkCnphD1/wDHq6H4aaxBB4FgX7QFuzcSnH+wFrToI0vCevPDNBPbyEbSMj+8PSus8T3siawklpm12oCCvRs81Mtxl7w9r+oT+Y00P7mEctF8y/l1ro7TXbacLvIjD/cfs9QBpiRWGQc/SnZpAFFMD5+0e8bSPAsOosPMVBLx7+ZxWP4YvjJ4oe5nIzcwyb/+AoWH8qAOZ+Ik/nXVrOe+5R+GK1fhVEVubq5bpmJM/Xd/hTA6C+vSkQUEZtPN2vjncT61g2FldXlylnZjzLm4lVef7xq4rQGemwadbaHpgtImAhi+d5dnP+1If6V474y1r+3NRaSNdkEfywwkfdUd6zAPCGntDjUZtocMUhBH/fR/wrY8S+ITp9udLtW/0iVP37J/yzT+4Pc0wOTs7KfULqG0tE3TyELGCeteqafYadomli0W5t1hgUyS3JJ+Y/xyY/8AQabA8r8RXc3i/WJmhYrDDHttUA/hHf8AGuRjQ2t0OMH0rGoaQNuH94M1Ij5PbI7VyM6uhKcjtTSmeMUgG7Pm5puGEvtQBKevIpyMEUdaQiwp96f0qCicj5V2kDNSLno3yGpZsh4BhOacGVu1ZMsnHl7erZph4FSMjyPvYyaaz7s7lwKaJK752D+dRZ2SZNaIljGlTtTMkpz1rRGQgX3pQDjjqaYCuhHANWYovLj/ANqs2Ugk+XrUFhZvqWqBf+WSctR0Gij4pt0h1dFwCCMdK7vwTdrqXh97Ir/plv8AuxvbGR/A39K9Cg70ziqfGc14t0g6ZqCmMK6X33PKHCt0K1oeJNA+xaVplxw0yr5c2PWtzE3/AISSrcpe2csh2Qyb0xn5QfT8RXSeI9JVvES6pHmIXUe+6lkfITZ96jYDzq7+JkV1qbQRH+z4bSUNZSxLxNzzvr2TxVqy29x4d1+KTes0YUp/z0XuTT3A5/442ovtB068SFdiTEI4P8OK4H4d3lnY6RrJut7XMYYW6/UVothF3wffyNexWjnG/wCXNe76vpcN5oju27zLUfJjvSmMb4Hyug3bhRnfWFqlxJaeJvtEGEkCKdv8H4ioA3dJurq9iE8I8i4aUriI/u/xU1rL4gW3uRa3uwy9MwHd/wCO9aQGpb3ttc48mZWPpnmrGaAPnjxQjaX4WtNPXasbuIZh/tKM5/GuK0+8f/hIUhTokcp+X/dNHQDN8bz7tRtoehTLfgcV2Xwutnm0u5dDhftcHJ7ACT/GqAxtJ1ptS8Varok7b2mYm1P3c7f8a9T8CeHntbIapdbPtMwIhkH/ACzi7n/gXSgDJ+KGpXDWy6NCSLmY+bN5XTHRYv615zpmgz6rqUcKfuMf6yVlJ8lP7xqUB0utzQ6FZGSCEyRwAJDxj5v4R/7NXnJeWeRnlk3PKdx9aEB6Z4D0RdOhe6vbYi+nXCrt/wBXF6fVqyfiN4gkmn/4R6xf90jeZdbFGWk/55/8B6fWqe4HA2d3eaH4h+1SRtHNEdkkTf3e61Z1+xH2x54RlHPmqR6H0rOsi6ZRgfacZ+WrrL5o+7yvpXGzqHmTeoz1p46bsVJQjJuOajzx86VICL704Hv+lUIep29/wqTzfl9R29qgokRlPp+NWYpN3GcioZoiwSDF96k3r/drIoXcD3xSjJ7bvc0DQ/gfeqB/v9eO9JAyu7fhVaV/m6ZrVGQ0HPGRinfxcdK0JHgEc09R8/y0h2LCqNvPWlztU1mOxSkkP+8x4GK7Hw/ops9N3y/62TrWdV2NIROQ8WW/m3gXHQ0zw9rg0jVrbUdjm2+5dRR/3P8APNelhfgOOurSOn+IH+qjCS/vPMWaMjrz1P5Yq7Z6t/bWiQzXFlshjPlTMvR5v4cfhmupHKReCdJvZPHdqog8qGSTa6fw+Wwzij45fEC1mtl8H6Lny4cSXM6ycbsf6sUAeX+H7N9W2WqQb5fMURgH+9wAa9Y1vVL648DaNDfLF50XnJtiXbtwelWgPQ/H+lN/wq3yk5S3it2wR045rxnwDeR2Xiu8ilt1lSS3bcT/AA4Bqo7AL4XnzrVoc8eaMfnX0jrV/wDZ9K1BzyIou3/XQCpqdAM7wXqsKafPZndulJdKyvEe3+3LkHjYoT64FHUDovBCeZpwkx0nb+S1yviaWSDxJdAAoN+N9StwNfwdDFd2TJ9n3fv2AO7pgCup/shP+fd/+/hpPcDwr4i3Hk26qTlftrbc+yVwnhW4aXxNLcuvyrEycf7QxTWwGZ4luhd+J5GXOEhSPnsRXovgyRtN+HU19bxJNI0u/ZN049qYHm/izTpNG1SPVLC9ldXIuIbhl2n1/nXsvhH4xaZrHh8z3P8Ao+uwxgPbn/VzMOFx6LUgc/Ks8l79rkmcTJJ5/mNz81d2J57vR01d7NLa8kizMkMfMydmA+tNgeVeK9YuNU1SS2lZlt7UlY48YIP94+9UvCWii+1cTT48i3OWX++3ZaEB3fijxD/YOlJIHZr+6z9nw/I9ZPw7V5h4auLO28TJ/aWTD080/wDLN/4TVAdL488PGWAavFh50XFxt/ujvXNaPdfbrIWsp3SW/wDqyf7npUVvhLp/EQXNh5Mu3sfumoo3ZG27vmrz0ztaLCqJevDCnLw/OaTESLz1owCMNUjGvCKjKECqATgHrTiPlxSFckVsAKy8VOvFSy4kyYz7Clz+NZmgqD5ulWPtO2PmpGiAzM/0prtt70ySI7up4FVmB3ZrREMkWEUuwbqBIfjIxUyR8Db+JqWzVEoHfvVWRu2eT2pIdjV8L+HpNRvPtbD/AEaI/L/tGu/mgRIOOP8AZFcNepdnRTR5n4r2+cwx81ZWqWEGk21jAWdZpYPNnX+4x7V7WC+E8zFbnSapbxXeh6W8qH7dLErnnOR0z+NeiXHhey8KeAIYr+5hjLKJxmTbl/b8DXatNzjPGtU+It19tubDRZBb2c37rzl6heh2/WqOo6Gr6ElzaQcQfO0p4Lr7etMDX+GVhLe398kWIJZbf91dP9yD+8W/DpXf+L2jd9I0+CJVit7FN7Y6k/xfWrEel+OLhv8AhFNStQPk+xKDjvgV86aO/leM7VPM8hJdysx/u0obAX/D3OvR+X90XH/s1fQniUfZfC+rJcHEkyTOn0U7hTqboZ5boXiExzANJ2DZB6V6p4n0z7XbR6vb8hkBkT8OtKXxAWPBtzt0ye2C/M0vyke6/wD1q5nxjDKviSdHAOUQr/3yBULcDX+Hrvi7jTn54z9Oua7/AJ9aUtwPl34r3yfaba2D/wB6Y/8AAulcz4HSX7Lc3KQlhM4cH/rmeaYHPzyi71a5uBwHlJUV6x4OtHm8K2MTHKy2km9Nv+23+FAHn3ijw9fWum2d55i3Gnszi28mXcIz3B9D7VV0/SJbW1SS2x9oK7zuHX2+lAHe/DvV7XxXPHY3V5FE9vJmWCVsNKq9l9a9X1y/+waRLeTJtmjwsMbZx5mOOPQCh6geDajYP9rd9+JpJdrsecljXd2trY6XoLM+yGKzXk7eT/8AXJoGea+dd+LPFv8AcMzbV44jSjxd4a/4RrUgYhI9nIMZlGGVu6tVIk6/wXrw1TSZNOuVWS5t0wu//ltH/wDWrh9c06Xwx4iwG+TG+Mjoy0S2KW5vRpHqdjncNpX5TWFeac6v/ddeme9eKnaZ6rXuEEE3OyUYcVaGCeDWzMBoUx8N8w9af05XmkMnVNyZpvlhh71AyGaI5woBpPKIXdRcViPcR2qZT60xofnuOKlUHHXmpLFD/wCyRTyTUjImIGDnmjjvTJE5NNUZbGKYiT7vB4qaO25D9BSHYeoEhO3oKlJWKDCjrWTKKzbidsf36uaPpEup3SiP/Vj70nvRJ2RpE9MsdPjsbZYolAQD9ah1Ehbc15stWdUUefajFHcazD9o4twd0jZxxSWXg7VfHV9e68q+RaxqSi4+ecD+6K+kwelM8XE/GXrfUtJ0ia3mvA01tCmHTPzLiuO8b+Prz4j6nHAU+z6dbnMUCnqema7Gcxyt5agakqRjAAGR2r0HRZX8VW0Vs7fZ1+beUXCqAPvU0I67wdp93p3w0vblYo0gnl8tp1GJHX/ComnbV9Qikk/5ZlIvwFaRA9e8SwR/8I7qSz/enhYx/QLXzBemOHUoJ5ifKDhnx3FRAZvabN5fimR7dNqmXeintX0bq+nz674buhdfu5JLXZF75TmiYHzfJM9jJu8so0fykGvpHSbvzPCkaNjd/ZwJz2+WiYGR4K1AQ3Hkb1UXHAb0NQePLbyNUtZo5cq0e3DdRio6gL4OnX+1WWObZ5sRXHvXafZLv/n4X86UlqB8jePLtrjXXiJ3G2iW3bd/eQYNdFoinw/4QS44MDWplB9DIpX+dUBw8cQSAscB9u6vbtG2WWl20MI/48sRysD/ANMiT+poYHlmm659is7rTL2EXlncESiEnAif+9VnxZfJb6VFPpjKkd9ujnx1jbqQPwoA8+hikW/t3glaKbeNkyHBB7V6FpnjzUfsz2GvzvcR7/MguJOx7n8aQHc6P4Xn1vX7GO8gkjg8sXsgA6oOV/PGKrfEmJIdJjAYF3lJki6MmOf60AchqOkx6VZaL4k06bzxuCyr/cP92vRZksfFnh82ysZ4Z04ll4dfb6p+oqwPHES68L+Iiu9fOs22k9pB6fQiu98RafF4l8Mrd2SBmBMtuPb+JaGI5Dwjcj95bMfu8xpXTXdiLmD7uTXhV/dmexR96BhXulbouU5HRhWeokhHzj5f746VcZXRDViwh5znORTNo7VRJNCdhwakT5iMcYqBjsZz60mznNADTbq3SofK5waB2EEJXhjxUgQ5z2pgOy/0pGWQjpQMQW5KZapfszbfSkTyieSq981KkQGc9aQWE4znqadh3xvk4/u0mXYmf5UCx/d71GsMlwD5Y/dL99z0qS7FzT7A3/7i23JbN/rJiOZfYV3ukWSWVokawhAOlcdaZvBGiT74rMv3yD81c0dzc477J/amtSW/K2MPNxJjIPon1rptQ8U6TpHhy+8PTbo3t/8AXy2X7vn+9jrX1uHj+6Pnq/8AEPBdc1ebU7mVASIU+76sPeodF8xdSjSJcvN+7GK0MTf8Q6Ommm1SJpHkYEXIfGFf0BHau98DaNfL8O5JmC20Ulx8kj/ekg/iUVoI9a8SfZV8GaBbWcXlae95CI0X+KPOK87uY/8ATrxYY1UCVig9hTgM9Z1e2/tbw/cxN8jR2WxPqVr5n1qPbD5YTHljIz7VEQLMruNchuCnlfaoEkAr6Z8ON/afhOyMt9u3WuxfUNjBpzA+e/FdqIridGziMn5vX3r1Dw74gW58MxMYGa5v7XBj7KuzrTkBg+HNWO3G7dIhr1DxKh1Xw3b364JTBLelS9GByHh5/sviS23xFvMk2oPrXp3P/PualgfFErXGu660uwm5vJ9zKD3Jr0DxTYnTvCE9t5hZZmWKJP8AgQb+lWwOOsLP7bqMFqOfMkC8V614gZtK03U/LfOYC+RxzgLSYHkMke/5N2MnqK7i98OW89x5MzeSxtt0eeEVwg61LA4+28NX2l6nO91ZfIvyNvH97+Jao+K1jht7e03iU/eL9wOwNUwOr8G/FqWxsjpWuW7Xe3mCVTyzdg5/uir97cL4zvQPN8kRKwXP/Lx36+uaSAwtPuDFYT6ZdD5Lj5ZR/wA85OzitnwXrcemX7abfsFtpuDIhwYX7MK0YGp8RfD4udO/tFIAtzaqIpUQcMv978PX0rF8B6qtvO2lXDKsUvzQOx/j9PxrMCn4m0c+G/E0F/F+7trxu3Plt3XNdJaESJ1ryscj0cGyOeyJ+bv6VlSaduBOwMo6xmvPhM65RuZTWJVj9lPmIOqt95aYFDfd6+9dkZHPykwTsaBD3Q0rhYMSA8r+VL/uUXHYkCZ6VJ5Y67aVzSxC4To1MHzLgdKVzEdxSeXIxyVz+NO5Q/BBx2pS2O9AxnzPjYMmpxGf+WjYqSrC7dnCc0x1I+p9KRpYtrZ7FEuoEiP+GEfx1estMm1PHmK1vZL91B1asZysWonX2OmRWsQwuAB8q1oJhBXA9WbWIXfn61nalN5EHypvkc+XGP8AaNaUVzVLEVPdhcy9ds7nw94SuotP2reRqJ7iYNnec8uBXLazYGbw5qWtbUla7sYjE7D971+ZjX2EVaCPnpO8jzOJftNzNt+XI6VoaM8tjqtnPB/rQ/yD68UIR6Vd+Eo5LS2l3+VIXLRxFeX5IP5YrS0/WL3WW8yWFYEgQRxxxjCBR3x61a3A9Quba4vNN8MtEwjgQo+PbH+NefOjfarrd181uV781MQPVdOuJNY8O2jH5XW23FvU4Za+c9etPK8yBiQ6fKwPUUo7gRagZmsNBvpPlWSIxJ9FbFe+fC3UWn8OxRx2Pm+Q7Rq+fX5v61ctgOE8ZaWkWrX1pGvCuyhm71c8FS3U3hZtPs9n2mCRreWV/wDlmr9P0zSewHBaVcy6fqk8DPuEUrJn1wa+ivCMh1TwbbwvjE1sSf8Avpv8KUwOLPn27GWNsXER4/2CKX/hItf/AOfn9aloDxvwHaNN4sgnC/LajzTkdq6bx7b+XaWVqsh2NKbhefwqpAYHhS2LeLrDYNxR9zHFdn40uBB4PMb4aeSXyGP61IHm+mQtNr2nw44e4XNd341eK48LXNxIP3n+pFJgcf4c8WagmswW2oSNdI6+UfNG7EYrG8TxbtYubuFNtiWxCGX+GmBL4d0sGCS9ZVeM/uxD/ER3YVZ1m8fw4lvDpzJJ5w33ETfMrL/D+NMRd0zU7PXIswoqmHBltrk847tu9KtfY4ZGZTi0Ocw7vlT/AL670Ad1pGoXFvpkdjqwclU2eZ/rIpI+3zV5fremzaRrVxaFgUR98Mic8dqYz0S1Nv448HywTGNbnbtK945ezfSsHQXkjtfIuYjHdxN5cqt2Irz8bH3TswejNwc8elMktvNw2BuA614R6pnyWUbHdjZJ/eFUZtMy23yyT3Za3jIlxIPsMyEfxL+tOW2KkhgRV3I5SaKIDOOR71Z/s+CRRtFS5ByiSaSicpwaaNOmxnIxRzodiGXSpG+tRf2dcR9I6rmQuUj+zSDnywv1pkltMx+Rx71VxWI47KQn5nwlWfskWd2GNMdh235dvSmshzlgQKgqxah024uI87lgj/vvWlYWiYP2SPOP+XiQVEpFF610OPz/ADJXadv75rcjgxgY6dK45yNUWPqQaevzD2rIZG0SdciqE1xZ6fbPrl/nyrZTHCgP+sc969LL6d6lzjxcvcsYHgy/uNf1PWIJ7gJLqllJB9olG5A2OAKNGS31Dwla2cu+bUHtp9OPbyEHIPv0r6dniHlPhe1hTU5rmXbN5B2KjevTmtrSfB0+p+MX01HVSXB83+GMdevakhnpFlPY6/8AEGS3066uJ4bKEwySykHzHVcZX/ZqpbR/Y7SWBfvPJtWnHcD2DQYftUKQOCtxpiJbyJ2+7mvJ7goZtxHzec+fzqIgeoeFXN34YsbdThzu5/2Q3/168W8bWXl69qnzjebqXcPbdRH4gMGe2lk8BQ3TSb1gujFGv91ep/nXq/wY1WFLa5s5J5YxsWVQBnoeat7AX/HlusPiZWCbUuY+EPp3rn/C9tF/xNrO5P8Ao2Vl+T7zn7ij/wAeqfsgcD42tv7D8ayg5jW4jE2wdYx02/pXunwmud2h2EQbci2I5PPJkkNKWwGOl0ZLy/WQZP2h12j/AHjTvLT/AJ4irA4T4axRW4ur2SLeZH+zZ/ujbuqL4gQ7dbt7fdua3tEjb69agCL4ewyz67eukZdktTtHuXX/AOvVr4iOqvbwKebhvtTDsP4f6UgOW8M27P4ojlOdtqpn/wC+a2/HxePT5f7k10Hj+mwf1pMDk/C0KzeJYEdcgxyn8fLNdPrWpw6fpseh6lEHtJC/lzoPmgcdPrmgC3p2kadeaXZz6Y/yWkW2UuRk7eS/0zgV5leSzXl3NPL/AKxic7en4VQjQ8O2DKhu1XZO7bYpD2x/Q12dtp41GxMkHmQsR+9UN88RP8OO3tQBzMmsa14X1GTTTOXVeVOPlb0bFdAuoafremwzapZtb+YMLe256euRTQD9Lt7zwnffb9PI1jSsf6TsGDt/2l6qa2dWsY2kh17TmEtlej5tvTd/SufEq9M6cO7SCJ1cDb1IzmrKGvmmrSPZHiFWNRPaehx/ShDEFrkbHGf9oULaEr8mH+vWnzFco37AGXcu3/gdIlusacjB9qLhYf8AZ8gYDN9Kd9k+UEeZ+IoCw0w/89OW7YqP7DKce9AWK8ti/SqQsyGHzgfhWiZNhz2sQwfMZ/VUFSfZCxASOQk9NtO4i7Do7OpxGqkf3uavQ6PBHzJ879t3SseYtIm+wQsd8p80jovZauw2mQGOEQdAKzci7FoIU/GpD05rIYIBjpT1jJzUvYkjnxja0ixxY3SOf4V9a808Q6pceKPEEVnatstEbZBtHGO8mK+hy2HuXPJxkvesdNd3cfg2XQINNQXUf2vEpZQG3A8n6GrfjC5s/Cz69MLn/SnaWSHzFA3lh04+teueeeVfDbR01K5v5rmdYVg2MxP1ziuh8Ualc6dDJdWUb6fFqynO1+sfTB9aYHW/A/T4f7Lnmnj2SxB2D/7OKjto4rrxHa27u0cXmGRmHoOacQPQPA15LcaBqGo+Y0kuo6gzKW67elcHq426zfLFjZ9pYLntUxA9F8G3St4PWCIf6RvaJX6V5h8QLHZ4s1LzB8xYfjxUr4gOc0ywEng3Wg8oSOGTCj8M1tfCXVntfEdh/pQiSX90+5M9a06Aer+P7dptHtrkTBvKkxJPj73sK4/w5BKPEcscBVZJ7ZsI3RnHTNR9kDk/ixpjb7a8GT5bGK4Y95D/AEro/g9rk8fhiKxtbUs8N0Vmnb35pvYBLHU1uNSvGjww+0SDOevzVp/aT/c/8eqgKfhKz2+DYLSKIiS6heXd/tbto/SuF8QS7vEupEsWxOeagDoPhu/2dby4XOJJ442+m1iaz/HzxL4jitO9naqnHvlv60AQ/DfTluNUv5HyALcxfUv0/lUfxJlaSPSUHCiOU/8AkUrQBmeAbbzPEodx8qRP+ox/Wm/ELCatDAGzjex/E0uoD9B09j4JWaMn97PMrhe6KN39KrXPh2O6t4LzRpvt1vc42Mq8ofRh25oA6O68PJZR+S7BIoI+o/uqMv8A+PV5/b6rqQ1ma5tN6Xt0QFEZ7Z5BqhHS6ok3iNrixuoPL1e1XzIvKHDL6VB4Gny82iTquJP3sfmDp/eX/PeiwzY1v7b4b/4melS/dwLq3YfL/suPY96k0PxNZ30kyY8i3ujm4t0HET/3k/wqHG6sOLszQt8PnaQ3+70q4oOBXzdfSpY9+lrAsKQCKk4zWJoh4QMMUiwhD8vympKJ44wcgqDVhY06GJcGgQxrOJ/70Z/2aQ2vVWLfWgVyCXT2bYEbfz89SDSmzzmmFxh0RzwG/OlGhIOfl3fSlcCX+ytknylV+gq5FbeUvy4z3IWp5hDZImqFbbcwzWZqiwtsitU3l47VAxuzLZFKI+cGgQ8L2UUP8vHQ01qQcR408QLbwtpVpMwl63ci/wB3+5Vr4e+Gbhbr+1LiBlnMf+hr0Kr/AHj9RX1+HhyUjwMRLmmYvjPUH8Q+KVt9OMjLZoFaQD5ePvP7VH8VI/tk0tikUhms70+Yc9mXIxXQjE1fB2m6Ro1zpPh+Xy7+71r99e4Py26ojOMe/FUvi35dzpmmxQR7f3zRQ47pTA9R0K2/4Rvwnb5RVb+zSHGOpWMVwmkvd2+n61rEartSEW6sezOf8KIgegfDlkj8LRy/wWBffn3Oa5DxNHFB4mvogvymXdSW4HV+BLm2l0e8g24nhlxGe3zqcf8AoNcr8RbJ4/EbJL/rPIQFv73FT1A5fwbCh1HVFmh+0fu08uDtKx4rB06SfQ9fkhkjVJra53Yz91gelbAfTOrWjap4ZmPmQqHh3wqPueua800iYL4jsZH3Dc/lHHq3y/1rKPUC78TNKa58F3S2/wC+itNkiyHh27Oa8v8AhvqH9neJJrWV9gmXKM78Rep92xVRAvy3sWm+Kb6C3DeQJiYvMGDj1q//AG+3qPyoA76xzZ6daxx5QWPl5/3cZNeJ3Ux/euct5jk5/GpQHpfga0DeDY44kPmXfmOD7g4rhvEVx9s8VahME6OFx/uqF/pQgOi8AWbHTbjUPup9sizj/Zz/AI1k/EO5W78SfZ0QBLaMKPx+b+tHUA8CwPbrfTgeYfNiiwOwJrN+JUZHjXZs2lYVo+0Bt6DZ/wDFtrLnGWunOP8AdrzjT9Q1PTXzpt3JaGbbvVOj+lC3A7mbxkdT0C50ie2xq3+rB29icvj/AHq5GeGUA+SfLlT5lboRVANsr69Gpx6pHMzXMfc9fxrd120SSO38SaaQA7ZmAP8Aq5aoD0bSb6w8WeG0mMkK3D5je0xnnup9j1FeW+LfClz4U1oC1VzaSDzLWX+8PT6ipA09B8Xec6wX0ax3A+Xcn8Y7V1x2bQUbdXjYzD/aPSw1W/ukwGV6U/FeQemO5J4OKsA+vNAh/Q+1WEOKkY7zAGzUqSY/GmMk/CpCc1IgC96Rh+VIBO9H4VJVhMUqrjNSA7FPCetIBdntSCP86YC9Kx/EN5/ZuiT3Ib5xwgPdq1oR5qiSMavuxOR8G6BHq17/AGhfB2t7aX6iWU/wt/s112u6vctaTxx4s45W8kM527kHX8/5V9ha1kfPPU4j+3LW7mTw34btnhtgR5l0fvSd2/WujjtItf8AF8l9qCpZ6THdfM7cee/Y/QUwOA8N3i2Xj6O8DFx9qePf/eDZX+RrX1C9PiXx3ZWluha1tLlIkA9Q3NUB7b44aOx8MT3u0mMebb/i5xXm8ln9m+G+m3KhkW9vna5X+9tHy0RA7T4cQSSaZcxsCY5bnbLn02GsnxpbCLxVdIjh1nUTD6Ht+lJfEBpfDeS18+6trgYaJ47lf+A5X/2es74nwT/2hYm4OB9lx+O8/wBKXUDlfC0sFh4oldUJuWtJBAq93/hrlPElh/Znja+tTN9oOQ8sp/idhk1qgPoHwHqBvfB2n4t/IH2QJNI3K7Yzs/UVwurR3Oka3PHCdz28+Y2UfiDWcdwO61yzXWNAaWd9n2mzeCAxdG/j3fmK+arqQ2WrQ36tgxSBkK0Q3A6Lx0PL1ay1SIttvoM/Mcs3HB9uK5z7dN/fP5VQj2nxFe7PDF3dQv5aSWhUf72f8K8ekwkJxnOzIFZxGe06fMsGkwQxxmJbCMb26cbM14221vNcs2Cxbd3NNAek+BoZX8KWdp5WxLiKSTPq27/61cL4mIk8UXrgfKGC5z6DFAHWeAbYSeHdUuOdodWG3/Zrl/iB+98a3Mh/55xg/lQviA7DR4fL+E1pLjH+h3En5nFeaaLp6ya9pqnB/e8j8KEBP4xtLzTriw1Oyla0k3kxzpw3PvXQeEtU0jxLrULzQw2urRqRLbZOxm/vUXAr/ECC20U2N1FCqPKT5wxhjH2Zh61Z8JNazzvokxja01BPMiLDjzP6VYFeNrr4YeJVuJF8/SrltrHOcj8O4r0i90638YeHntUbbFe/Pb3GMpDJ2P49KQHi99oxtr6W1uVMU0b7Sv8AED9a6Dw8txN5iq+68t48xxjpOv8AjU1I88RwlyO50FhcfbYdy7gwOJEPVTWjGMtivmq9P2bPepyuiQDFH8RHauU0RMi7V9afvoLJduFyVzUiqXb0FSBOEx3pw6VADxRt60DAoBS7akoaRT9ue1IBwTnpUmKRI5UzRsxTEQv8orgPHN8Zb+K0STeUGTHjvXoYGP705cS/3ZDN4n+yaVaeHfDUEssx+aecLnEp64/Sq974N8RajrFqmrXXmXd7HuHO4QR9PmHY19RI8M0NcsdL8CwOlizSS2qK807Y5f8AhB/wrLF7qUfw41LUbqb7RdXeGuLaYfc3cK0dJgcHE0iwqsGf3WMkevtXoXw90knxBp7yfea5DZHfmr6Aej/E2S8g0+20IIjf2pcb4c/wsGrJ8Tx39nrFpot1HGsNtZK+yP8AvH7xqUB0HgC+Nsl9bLGTHK6BfUFuKq+PNKWLWYZI5uDbgL/wEn/GlH4gKvw/uYrbW3iuUBSaI8nrwc/0rV+JFjNNplpfSncPOdh7BgMfyp/bA860WKKz8babN5oWRZfT16Vm/Eq0FnqWmuYfuoYJZT/y0lzkmtOoHp3we1JZvCslvJcDNvcf6g9wy9PzqDx5HPbeIFmeLyjcxjDoe9Zr4gOl8K3FzqXhZD+5kmiPlM3ZIx/XrXgXiXSjDqV5G0OweYwCD+EZ4oW4GtYW3/CQ/DRLAsi3NpkQqOvHdzXMf8IPrP8Az1t/zatAPTfiDm30M26jEct4vkr/ALIj5/WvO7WB7vVrOHbnzbhIvzNYrYD1fVrvd4SvrpiESWzaMjp83mKB+grx6Rn+Zf4R0poD2bREiTTdLt03Rrb+Tu+mzcf/AEKvGZGVnfcd/wAxOfWhAeifDfKaFawmLZDOs+7J+8fNwP0rh/FV6NT8T31zEAA836ULcD0BWaH4NQbR8q6Rz+M1ee+E0DeMtJVs/wCtY/8AkNqI7MDrPiRZp/wr+0yf3gZQK8YGoTWWuJqMORKkm/5fT0pIR3Otz3OuWMWs3N5Dey3/ACQG+aP/AGWHrWB5p0fUra380wrMizIx/hPamB7HaS23jXwnL9qjt4mb93cIB/qj2YfzrlvC+uXXgrW38PahMf7OkkxGz5AA/hP071W4zsvGnh4anEur2H+k3IVYrlAPnb+7J9K8+MN/4Z12G8kikgnjwypKuN1FxHdatoIvPL13w/Msjyp5oRsbGB6xt/tA8Zqvp16t1biQrIknR7d+GQjrXl4+neN0ehhaj2Zfxnmk2e2K8FHqjynORUiH5aYybmpBk+1QUSbTjrUmOKmwxUqdFwaQClc00KcVID9tKoNAx64LFf4h1FP8ukSSdB0qM1SJKN4wSPn5V7ivOrDRNQ8aeJrxbVtkG/5rrrheyj/ar2stj7zODGP3Tdm1zRvBcU2jeG7L+1NTRgXaEbgD7nvWYsmueHBNLqErx694g+ch+fs8fYD0r3dzyTltQUan4gg8N29yLuKFmur6UdJivJX3rqfGsEP/AAiNha6bbiOa7mEcUOe3pQwMm80RdFsNL0ziZrhPtnngevGP0rsvBGnSJ4is0QoZxbvJGP8AaFV0As61u8SfFaz0231BplUecrg/6mRfvCtDx9I0XjQuwZibNEHHeoAs+ANRjs7q881crtVvoc4/rU3j+wazlsbkzZUoRjPQ5p/aAxPCcyxeI4GufmSXehrrfHEMkvhrz1XfAZlMcfoMUS+IDyw7bbV7K4eAv5cisWHfmr/xRsUTwt9oMvm/ZL3ZF6gty1UAz4MarPDr95YW0K3HnW3mfP8A3gR/jXd/EKzttliytJ8rPE4PT3IqX8QEXgJ4FW8sWZmiOJgq9ZK434h6X5Xi+7K7kEqrKB6HFL7QGb4C/cXOp2+JFBKT/uxkuemK7Xz2/wCeNx/37rQDlfiRM7XWm2jtmSKKVyfTc+R+lYngpVl8Ww+ZH+5t1eYkf7IzWa2A6T4jHytEiUSbIZ7uLy4x/dCNn9cVwdpCL3Vba2/vyKh/E0ID1fxDe/ZNBv5rDYB9jmVCvUFcIP5V4tg7enShAe0abClr4c0qJcEwLA74H3dw3V4kwJO4nnccn15ojuB6trBWL4KbgcD+y4V/OWuI8AQNdeOdJYDO15P/AEW1EdgOn+Je46Dp0BVQPtD5x6bRXi95bETEomR3pIRpeF7Fd9xKBuMbxnb/AMCxTvF8atc2rY/gMX/fNAzW+HHi4aHrK2d4f9Fuf3crSdB6N+Feh+OtAh1i0tQsv2i9S4W1HkDrC/f8Ov4072QHsXh/QrbQNOt9MslLfZ4wm9+Wb3JqTxD4csvEujT2F9ApyPlkx8yt61wqT5gPIfBF2NMnufC9+qBop2MGTj953Qn3qXxRov8AZl0NXsYJUkAPnQn/AJaoOuf9oV2zhzRKjLlK+n6lHqKrJArGJ13K9X+vSvl68OSZ71GXNEkQZFTRp82TWDNUThOac3yrkdKktDkG9d1TqvegocOtPHzVIh+GoAOelQIfingdKCgWBVdnA+dupqZUy3zUEDH4ziqrv8g7fWtEI567mm1gvaadudeVnul+5F9P71YFlLrD27eG/CtkkHmt8+oM/wDB/Exb+GvpsJS5YniYmfMzobTVbDwY8mh6Bp1tc3qR4ubuQcCQ9l/nXDeKfENxY3VxNdSi91q9+7Iw/wCPc/SvQOQxPA8cUHiNrqd/kW2k81vQv8v9a67RL99e19Zzai60zT5mhR/7nGd36VAGv4ne0PilbK2wFsrcRL8vT+I/zrW+HURN5ql3Jte5slWSD3wDuFX0AzvhtINV+LX9oyQiFphNcbQOOQf8a6n4ixJB4k+0l93mw/d7cVP2gM7wJPapr9wLpA0LWhKr/u/N/StTx9bz/Y7e6mfMDSvtH1wV/lQ/jA5LQpF/4SnTS4+QzgD8eK9I8T25n8LXjWxOyJQir/unk057geS3BmESSNCmG6d8c11Xj63tp/AuqSwr++ktYpUT/bYhyf8AvnND6Aef/DiVLfxfYI8zQxzbrfen+2uB+teyeIoZ9Q8Kb4mja0iKFXfrt6N+ZonuByvhJxB4pgQMyGVWi8z+6Ov9Ku/FCxLNZ3KsrqfkeU/eJ9Kn7QHC+H7Rv+EyggW4a2a4je3WVP75HFegf8IDrn/Qz3NNgeZ+M8v43vjJ++8kJGMH0HNa/wAOzDaX95cvDn7tv9PMpfZAT4izXEsulwTYHk+Y238eKxfB9pHeeNLLzSFVP3x+i0dAOv8AHkg0/wAL3EFvGdr3Cru/2XO415osH2hoIkPLuFoQHsesSxaXpd7HD8ubENn/AHF214hLFmPk8U47Aeo+LYTB8G1gjQki1twSfrmuV+GkXn+P7N+V8uKR/wDx2ktgL/j/APe/2aCzbfPnOPwFcL/ZjXBu4xhf3ZcCn0Ak8Dbvtt3ENnEYdg3qDUvjXRJ7K301nX5ZfMOfqaQHP6LaC41u3gYcscCvW/BniScbNOu3LXWmMNqhfmniz0/ClID3X7XI6Jd6fslEo3f/AFqff6mljZG4nKhscLn7zegrklLW1ijwC8V59Su7r7OLdpJ3LRkfOhzXoXhzU4NbsPLebN7bqPN8znP92TP867/sknFarps3hDVnuiD/AGTey8onS2l/wNaqjKrt6dc14eY02vfPWwMr+6PU4NWFNeOeiS5Jj96mMYaP6VKKQ6JQvTvUhVuPSgslRflNOVaQE31pduagkNmKcBigCQAZzTmyTzwtUiCpIeDtrI1ZneMxySCz0/YfNuCfm/3E/wBqu3C0vaTMKs+SJR0+2iv7RrnVM6H4ZsY+YVG15z6D60svi+afQ47Pw5p8Oj2RXa0jYD7f6V9XFWVj597nP2NvbStHJAxmt5mZTMeqSD3715/4rlRfFd4M+Z5O2Lef9kYpXAy4rmSGxmRd3m3fXH/POvZPhxo5/wCEJnsbcZkv7WWfd/tA7RSYDdbgRfEF8VblGCMfooBrVh2aX8KbyC83W91dsZrCaPq2eMfpVdAIPhb/AKJ4oR5f+WcDszenFafjfe9/ZLPlIxFI25v9+k/jAy/A7Qv4tt0nV40mjkiHvuUr/Wuy+INtMfDqPHlrf7RHx/cAQj+dEvjA4CG+S3uFu8D/AEeVZAqL6V6xrEP9uaDcmz/cn7O2B2JbDUpbgeRPtK7eQex7V2+pabbX3g+K7gkH2g6a0KIf4n8vy/6U5AeF6TcvpepWtzCB58EyuN3TcDX0vrNtBLo9xDLAUEsbxwpH0OPn/WnU6AeVCb7Pd2k5LKqSox2dQvpXe/ESOa48OWk0tqkmZl8sR/8ALPd1LfhiokB5laXDWGs2t/AFZ7ecNtPtXbf8LQ1b/n2h/wC+acgPLboq19PIPmDSE8133w+ihTwzdkxh5rkyzxt/e8rjH50vsgcx8RJPO8YziR8pHGiqF/3aX4e6V9t1m7dMhY7XyxIf4Cx/+tR0A1PiNqEMmj6dZRD5nJkf/gPyj+tcj4dtWufE2lQL2ukJ/A0ID0PxHix8E3j3Tjzf9QT/ALxzXk8MH2h47dXx5sioPxNOOwHqfxAmMPw5ZVzwY4a434aY/wCEyeQ5AhspGB/FaS2A0/GiLt0fzZN+4Tvx/vkVzmiW6XniW1R87SG3fgKfQChYRJYePLmzl2w292hOfRRzWn46vvt2kWP7ogRXEiLk9QKQHKaDHnxTp/y/8tK6jxLbzaNBJfWe5Lmw2Sbx3yTxRIDXTxXrkvhs6v4P1S5tX+9dRYDjPfg1J4M8Zar4j194de1Izyv81uyjaNw7KB0qFADrfF+nSSwJrib0YOILlfK79pOPWsLS9Qn0HVI7wZbafmT/AJ7J3Wtugj07UNM0zxRoMycPb3UfLI2Cv91vqprzTTIbrSL2Xw/qCEzWvSXd98dq4cVDnpHTh5+zqGwB/wDq9KkCZ+b0r5lo99E68fSpYyDxSGTinjn60mWPzz1xUw5FSAqdOakGMcVIDwOeKFG33NUAhbyx61C83m8E4pkGdeahFZQvPOfLCVzTa/Zz41m6gkm0W2fbbK/H2ib/AAr3csp68x5WMn0MTxB4il1DdeeIbkKytujtgvH0HrXB6t4mvNUzbxvJbWS/8s8/e+te29DzD0HSc2eiRW87eVbR2ySx/wA2Nec2Onv4m8SXc4RzbCR539duc1lbUCaG2F9eykfcJ+TA7V9L+DbGDw94f0STZhzCU/P5qqYHlusXyz3eoXDsUMt1k/TOK7Lx3pqWfgvTNM83zDZXKmM99pX/AOvVAV/AenLdanMgOG+zbdufetz4hXMd2LUsFTy5nTkfeWp+2Bx3htoR4u02N3JQT/e969E8Y2t1/wAI5cw2snmQRImT/wAC5py+IDzELgebt4U5/CvZLSUavpVtJZYHlw+b7MdtTIDyaOHzUkhmMcO5v4vu7q9A8OwabeeFrFpm2vAGi6d8mqmB873tp9i1S6tlcv5MzJu9wa+jfCF3Nc6JZTiRbxrm3jXY3/LLAw1FQDzvU4JLW4uYrhcOhPyj68V3j+bqPw28xQ73M1t88vYeufwFTIDyVpGyZPKXev3B2pv9pXX/ADxirRgZ33YAeCe/vXqHhi0h0/wfpc8a/OTn/vt/mrOWwHmev3X9oa/f3LoA/nnp9cV1Pw4+WG6I/wCWl7Zow/7+0dAMjx/brHrdrGpbCQHr7yMai8EEQeKJLjbvNtbySc9+KaA6H4mOw0nGflmvo8jH/TCuE8PoJPEukcD5r6D/ANDFC2A7zxu5fwJJG3IF4g/9C/wrk/h43l+INQ4zixb/ANGR0lsBo+MyBb6O4X/lnP8A+j3qh4PiEniHJ/595v8A0Gn0AwvG37jx3ayjBCMibfZaveKJPtPhtZCAu25XAH+0uaQHMaSuzXbAg8+en/oVd/4qQT+ENWkb73Jz/utQwPOPCOrz6P4ghji+a31FhDLEen1FbWtW40HxP/oTsm1UuYiP+WbYzVRA920vy7jSbEXStONUtYJG3P8A6vzhu4/3T0rgQuyeUdcORz7U0B2ngTVZYoL22kVJYox5iAjpuOGH0p/xB0eE6dBfozLcWFz5CMecqeazkugeZgXDNF9naM7dwFW0PzfXmvl66tNn0VL4UTgU/p0rmOglSpUOGpMsl/jp6yHdSETocGptozmkIP0pjcYAoAY4qjPOYkdsfdqoq9RGNR2RhWulnxxrH9n3FybSyiuNkscK8z8Z5asDxMqebeELi100bbS0/wCWceK+ww8FGJ8/WleRx2oxm48LR6xcyNNc3coVPSBfRa5aWXyj5YUHLitDI7nx7eXGn6S0auGUuQnH3K0NI0OLSfhjPqttIRcCBJgcd5ODQBkaXCsNrvUDk4r6VMStYXhb5vsIJi/74pTA8c0Oyi1XxHa2dz80U8rbh9FLf0rtfiCmYdKJ/wCWiZb8KvqBheGriSy8QKYDs/cSH9K6/wCIUEJ8N27eX8yTJ83ruHNR9oDzfTAF1rT/AGu4h/4+K9T1nOl+F7mCJiyPbPK27ud9EtwPNfs6fYJphnK+9evaVEtt4Z0x4P3ZlWMNj0C05geUNGLqVmlORJITt9Dmu58G26z+GomP8N06/X5jQ9gPFPElnHaeJb2FeQ07kn8a9c+Hyj/hXlpNF+6lB8ncPQPTnsBh67keI79GYsyPu3Gut8EbpvA8cW7bFJNIrr69al7AeZXwVZZVRQF3kY9BVPyl/uitAP/Z";
            clsVida1.TpProvavida = "1";

            listaProvaVida.Add(clsVida);
            listaProvaVida.Add(clsVida1);

            var result2 = AutenticarFace("D0FB963FF976F9C37FC81FE03C21EA7B", listaProvaVida);

            //teste ok
            var result = await ValidarProvaVida("D0FB963FF976F9C37FC81FE03C21EA7B", listaProvaVida);

            //StatusCarteira result = await ChecarStatusCarteira("1ED021A05EF5089233379BE996F7BBDD");
            //return result;

            //teste ok na validacao do qrcode
            //var result = await ValidarQrCode(clsSolicitacao);

            //teste ok na validacao do barcode
            //var result = await ValidarBarCode(clsSolicitacao);

            //var result = await CarregarDadosCidadao("1ED021A05EF5089233379BE996F7BBDD");

            return result;
        }



    }
}
