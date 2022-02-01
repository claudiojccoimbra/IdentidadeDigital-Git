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
                }

                return qrCode;
            }
            catch (Exception e)
            {
                throw e;
            }
        }


        public void teste()
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


            var result = ValidarQrCode(clsSolicitacao);

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
