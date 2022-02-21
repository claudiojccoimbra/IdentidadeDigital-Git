using System.Collections.Generic;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Threading.Tasks;
using IdentidadeDigital.FrontEnd.Wcf.DataContracts;

namespace IdentidadeDigital.FrontEnd.Wcf.Interface
{
    [ServiceContract]
    public interface IServiceIdentidadeDigital
    {
        [OperationContract]
        [WebGet(BodyStyle = WebMessageBodyStyle.Bare, RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, UriTemplate = "ChecarStatusCarteira/{idTransacao}")]
        Task<StatusCarteira> ChecarStatusCarteira(string idTransacao);

        [OperationContract]
        [WebGet(BodyStyle = WebMessageBodyStyle.Bare, RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, UriTemplate = "ChecarVersao/{versao}")]
        Task<VersaoApp> ChecarVersao(string versao);

        [OperationContract]
        [WebGet(BodyStyle = WebMessageBodyStyle.Bare, RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, UriTemplate = "ValidarQrCode/{cls}")]
        Task<QrCode> ValidarQrCode(clsSolicitacao cls);

        [OperationContract]
        [WebGet(BodyStyle = WebMessageBodyStyle.Bare, RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, UriTemplate = "ValidarBarCode/{cls}")]
        Task<BarCode> ValidarBarCode(clsSolicitacao cls);

        [OperationContract]
        [WebGet(BodyStyle = WebMessageBodyStyle.Bare, RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, UriTemplate = "ValidarProvaVida/{idTransacao}/{lstVida}")]
        Task<QrCode> ValidarProvaVida(string idTransacao, List<clsVida> lstVida);

        [OperationContract]
        [WebGet(BodyStyle = WebMessageBodyStyle.Bare, RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, UriTemplate = "AutenticarFace/{idTransacao}/{lstVida}")]
        Task<QrCode> AutenticarFace(string idTransacao, List<clsVida> lstVida);

        [OperationContract]
        [WebGet(BodyStyle = WebMessageBodyStyle.Bare, RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, UriTemplate = "ConsultarIdentidade/{idTransacao}")]
        Task<Carteira> ConsultarIdentidade(string idTransacao);

        [OperationContract]
        Task<QrCode> Teste();
    }
}
