using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
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
using Newtonsoft.Json;

namespace IdentidadeDigital.FrontEnd.Wcf
{
    public class ServiceIdentidadeDigital : IServiceIdentidadeDigital
    {
        private static readonly HttpClient HttpClient = new HttpClient();
        private readonly string _dataBaseWebApi = ConfigurationManager.AppSettings["IdentidadeDigitalDataBase"];

        public async Task<StatusCarteira> ChecarStatusCarteira(string idTransacao)
        {
            try
            {
                var statusCarteira = new StatusCarteira();

                HttpResponseMessage response = await HttpClient.GetAsync(_dataBaseWebApi + "ChecarStatusCarteira/" + idTransacao);

                if (response.IsSuccessStatusCode)
                {
                    statusCarteira = await response.Content.ReadAsAsync<StatusCarteira>();
                }
                return statusCarteira;
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        public async Task<VersaoApp> ChecarVersao(string versao)
        {
            try
            {
                var versaoApp = new VersaoApp();
                
                HttpResponseMessage response = await HttpClient.GetAsync(_dataBaseWebApi + "checarversao/" + versao);

                if (response.IsSuccessStatusCode)
                {
                    versaoApp = await response.Content.ReadAsAsync<VersaoApp>();
                }
                return versaoApp;
            }
            catch (Exception e)
            {
                throw e;
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
                    var sessionId = await response.Content.ReadAsAsync<string>();
                    var id = sessionId.Split(';');

                    if (id.Length > 0)
                    {
                        sessionSqTransacao = id[0];
                        sessionIdTransacao = id[1];
                    }
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

                if (await VerificarPermissoesQrCodeAcessoCarteira(sessionIdTransacao))
                {
                    // atribuição para que o método de inserir tenha somente uma classe
                    cls.SqTransacao = sessionSqTransacao;
                    cls.IdTransacao = sessionIdTransacao;

                    await InserirPedidoQrCode(cls);
                }

                qrCode.Codigo = sessionIdTransacao;

                return qrCode;
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        #region Métodos Privados

        private async Task<bool> VerificarPermissoesQrCodeAcessoCarteira(string sessionIdTransacao)
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
                HttpResponseMessage response = await HttpClient.GetAsync(_dataBaseWebApi + "VerificarPermissoesQrCodeAcessoCarteira/" + DateTime.Now + "/" + qrcd.Rg);

                return await response.Content.ReadAsAsync<bool>();
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
        private async Task InserirPedidoQrCode(clsSolicitacao cls)
        {
            try
            {
                clsQrCodeRG qrcd = new clsQrCodeRG();

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

                using (StreamReader r = new StreamReader(ConfigurationManager.AppSettings["CertificaCarteiraPath"] + cls.IdTransacao + ".json"))
                {
                    string json = r.ReadToEnd();
                    qrcd = JsonConvert.DeserializeObject<clsQrCodeRG>(json);
                }
                //qrcd.Rg = qrcd.Rg.Replace(".", "");
                //qrcd.Rg = qrcd.Rg.Replace("-", "");
                cls.Pid = qrcd.Pid;

                //testar 
                HttpResponseMessage response = await HttpClient.PostAsJsonAsync(ConfigurationManager.AppSettings["IdentidadeDigitalDataBase"] + "InserirPedidoQrCode", cls);
                response.EnsureSuccessStatusCode();

            }
            catch (Exception e)
            {
                throw e;
            }
        }

        #endregion
    }
}
