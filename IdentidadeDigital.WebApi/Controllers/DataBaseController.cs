using IdentidadeDigital.Infra.Domain;
using IdentidadeDigital.Infra.Domain.Enums;
using IdentidadeDigital.Infra.Repository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using Microsoft.AspNetCore.Http;

namespace IdentidadeDigital.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DataBaseController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public DataBaseController(IConfiguration configuration) => _configuration = configuration;

        [HttpGet("get")]
        public ActionResult<string> get()
        {
            //PedidosRepository pedidosRepository = new PedidosRepository();
            //return ActionResult<string>.op_Implicit((ActionResult)this.Ok());
            return Ok("");
        }

        [HttpGet("ChecarStatusCarteira/{idTransacao}")]
        [ProducesResponseType(200, Type = typeof(StatusCarteira))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult ChecarStatusCarteira(string idTransacao)
        {
            StatusCarteira statusCarteira = new StatusCarteira();

            try
            {
                statusCarteira = new IdentidadesRepository(_configuration).ChecarStatusCarteira(idTransacao);
                return Ok(statusCarteira);
            }
            catch (CommunicationException ex)
            {
                statusCarteira.Erro = EnumHelper.GetDescriptionFromEnumValue(TipoErroEnum.EndPointError);
                return NotFound(statusCarteira);
            }
            catch (Exception ex)
            {
                statusCarteira.Erro = ex.Message;
                return NotFound(statusCarteira);
            }
        }

        [HttpGet("ChecarVersao/{nuVersao}")]
        [ProducesResponseType(200, Type = typeof(VersaoApp))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult ChecarVersao(string nuVersao)
        {
            VersaoApp versaoApp = new VersaoApp();

            try
            {
                var obj = new VersaoAppRepository().FindAll().FirstOrDefault(f => f.DsVersao == nuVersao);

                if (obj == null)
                {
                    versaoApp.Erro = "Versão Inexistente.";
                    return Ok(versaoApp);
                }
                return Ok(obj);
            }
            catch (CommunicationException ex)
            {
                versaoApp.Erro = EnumHelper.GetDescriptionFromEnumValue(TipoErroEnum.EndPointError);
                return NotFound(versaoApp);
            }
            catch (Exception ex)
            {
                versaoApp.Erro = ex.Message;
                return NotFound(versaoApp);
            }
        }

        [HttpPost("InserirPedidoQrCode")]
        [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(bool))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult InserirPedidoQrCode([FromBody] Solicitacao solicitacao)
        {
            try
            {
                return Ok(new PedidosRepository().InserirPedidoQrCode(solicitacao));
            }
            catch (Exception ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpPost("InserirPedidoBarCode")]
        [ProducesResponseType(200, Type = typeof(string))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult InserirPedidoBarCode([FromBody] Solicitacao solicitacao)
        {
            try
            {
                return Ok(new PedidosRepository().InserirPedidoBarCode(solicitacao));
            }
            catch (Exception ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpPost("AtualizarEscore/{idTransacao}/{score}")]
        public IActionResult AtualizarEscore(string idTransacao, int score)
        {
            try
            {
                return Ok(new PedidosRepository().AtualizarEscore(idTransacao, score));
            }
            catch (Exception ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpPost("InserirImagemCarteira")]
        [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(bool))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult InserirImagemCarteira([FromBody] Carteira carteira)
        {
            try
            {
                return Ok(new IdentidadesRepository(_configuration).InserirIdentidade(carteira));
            }
            catch (Exception ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpGet("VerificarIdTransacaoProvaVida/{idTransacao}")]
        [ProducesResponseType(200, Type = typeof(bool))]
        public IActionResult VerificarIdTransacaoProvaVida(string idTransacao)
        {
            try
            {
                return Ok(new PedidosRepository().VerificarIdTransacaoProvaVida(idTransacao));
            }
            catch (Exception ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpPost]
        [ProducesResponseType(200, Type = typeof(bool))]
        public IActionResult InserirProvaVida([FromBody] List<ImagemProvaVida> listaImagemProvaVida, string idTransacao)
        {
            try
            {
                return Ok(new ProvaVidaRepository().InserirProvaVida(listaImagemProvaVida, idTransacao));
            }
            catch (Exception ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpGet("ConsultarFotoCarteira/{idTransacao}")]
        [ProducesResponseType(200, Type = typeof(string))]
        public IActionResult ConsultarFotoCarteira(string idTransacao)
        {
            try
            {
                return Ok(Convert.ToBase64String(new IdentidadesRepository(_configuration).ConsultarImagem(idTransacao)));
            }
            catch (Exception ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpGet("ConsultarIdentidade/{idTransacao}")]
        [ProducesResponseType(200, Type = typeof(Carteira))]
        public IActionResult ConsultarIdentidade(string idTransacao)
        {
            Carteira carteira = new Carteira();
            try
            {
                carteira = new IdentidadesRepository(_configuration).ConsultarIdentidade(idTransacao);
                return Ok(carteira);
            }
            catch (CommunicationException ex)
            {
                carteira.Erro = EnumHelper.GetDescriptionFromEnumValue(TipoErroEnum.EndPointError);
                return NotFound(carteira);
            }
            catch (Exception ex)
            {
                carteira.Erro = ex.Message;
                return NotFound(carteira);
            }
        }

        [HttpGet("GetSession")]
        [ProducesResponseType(200, Type = typeof(string))]
        public IActionResult GetSession()
        {
            try
            {
                return Ok(new PedidosRepository().GetSession());
            }
            catch (Exception ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpGet("ConsultarDadosRic/{idTransacao}")]
        [ProducesResponseType(200, Type = typeof(Cidadao))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult ConsultarDadosRic(string idTransacao)
        {
            Cidadao cidadao = new Cidadao();
            try
            {
                cidadao = new IdentidadesRepository(_configuration).ConsultarDadosRic(idTransacao);
                return Ok(cidadao);
            }
            catch (CommunicationException ex)
            {
                cidadao.Erro = EnumHelper.GetDescriptionFromEnumValue(TipoErroEnum.EndPointError);
                return NotFound(cidadao);
            }
            catch (Exception ex)
            {
                cidadao.Erro = ex.Message;
                return NotFound(cidadao);
            }
        }

        [HttpPost("AtualizarStatusIdentidade/{idTransacao}/{statusId}")]
        [ProducesResponseType(200, Type = typeof(bool))]
        public IActionResult AtualizarStatusIdentidade(string idTransacao, int statusId)
        {
            try
            {
                return Ok(new IdentidadesRepository(_configuration).AtualizarStatusIdentidade(idTransacao, statusId));
            }
            catch (Exception ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpGet("VerificarPermissoesQrCodeAcessoCarteira/{dataInclusao}/{nuRg}")]
        [ProducesResponseType(200, Type = typeof(bool))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult VerificarPermissoesQrCodeAcessoCarteira(DateTime dataInclusao, long nuRg)
        {
            try
            {
                return Ok(new PedidosRepository().VerificarPermissoesAcessoCarteira(dataInclusao, nuRg));
            }
            catch (Exception ex)
            {
                return NotFound(ex.Message);
            }
        }









        [HttpGet("Teste")]
        public IActionResult Teste()
        {
            try
            {

                var qrcodeGeorge =
              "AUHvP6Gwt1oPvY4HvJQ+TBq11wsw/hHTTiyMpedcPMOdnPIHQqjpEFXpeLjmT3q0aheBy/0BaisrlrThLW36scQOkY0wUwRvADUE9ATL1IGF3toVNsnQlXSMFJhV00/QekJJ1/Gz9jJgXbcQw+6drIbMABRDX7ZF6IE5hgGSEALbHjcAA4CxQbx0nENnIwC5MkKERA5TgoeQLSr8yG189oJGr4HnHsgR/ImEnFlXlWcxoV37I4eWko23nIiYdrBYIxVKr1dI/OfAbTkVBg0jj0UoZBbKdNyRwUd1x/7+8gpDLvue2uW0tAEab7eRquHZ8x0mSwdaego5q97AzyRLMC973WF4dfWN0jKZIeBi3OTWglA5qDLNv1faGDXTh4OcRTjGVMvFOJEBUC8+L4ydTLcfxJWgusR0lY6y/bq/QCXJ+Hyc6+/3rME8/AV12fwy1gJTNCF4g/lHnlVICqJyoYWq/ohUPLBBLZRR2TxCk+G4KvxlD44m4dyRXW+IiCNKnJC8RHneZh4C1+6hBBbvFd2kz30S9S+dQx7fhs27Q7bot4cl+IdbT9+cH/Ax1/+ET4HFGtpV8UOFwzS/+6m9yfpg70S4O4+JeNJHMOCWdPgABcJmt6LlbDmE1U3ASQMr9WftbxsgRlGb/0RSIf/IDC0Okc45kCFCkEHuiz6Toe2NE2LXjZGo/nf5mGuYBgK64ptfj34YTyWxrLRb0EGFLfJsZWtYoyfTZ5ZK/JEf/BE0aBxb+y7YWhXCnqScobo7I/keHC9CPQdxy1bAkgr9hLW/bkfEmCY5I5+/mi/A1khtj0G3ZZCTJBGtqU/C9shW2lK/uvjqkJHYrNEHFH4tSpVYQXpo43wrqBt4cT178b/IV1pbDIlDwA8mvhTD0ewvs8h8qkwe9F+ceDe2Y8QEldSbqA+dq3AdATweFKKhEYbIB6RrEmLWE/F1vWHri/4mHGvPCr/uoXlJGKyMlyadihOo2ztGtkhxkacWrRhv9yJ0UTru3W+inTFNd5AZ54B45dKxOsPYMXMT6riHoVz7sw8lwWwOCvA4BAqij7DnImLSdT60gx7WYVNKJDv+E80HmQuKxEA5aeUE7XF+6jNgCTS4b0CBLmZizzNUtjwUsyDZOEXxuL3M02CPT6urSbMia2fhq0pZMUc7Zh2X38+syByr+w3AUfQxyINM1JBlRXDiBy6wVU1x4sWyXnkLZoTBiryuVxKON/RLqOUAqXctVQCpcjWR0pH7EE5yyntnsvQBIy2wJd+SvR4+XATgIT7sK/2ZvnQDqw9Sx/b+7DRIzh+K55FhaD4/8d1yovKlgCj35F9+XE5Mt0d+pgw4FWgg2X22XYHdsQYrlzIwAWjCZ4ykzC5HmsK5WnlnSf0BfcMMVXvFk8zkT3+6frxMDUBowehUT3z4CoA6oLyh61rusyKUqFeSSvVhE6uDV1js76XmExLDEZP0hj2Xcdm5gC3Jrxc7IuSd7254LhM8UfNWeoiFGWmcMlqB/1dBSg7cR3d2ogOT63GkxB4XvRl1vEWoKtFvctKBkgAAs5yXNgC/ml6sEFmELYYOmjpg8oqyefwnQYzQS6TbnYLsimMTUUDttkixK6cWsn64i9KbXAVsc6PuthLxAIm6HHcv6RH705sklVf2vl1cVTJVtVJp2yul79y7/biG5AhKZmuYcfnPFwhg7teM4HquTaQVdsBrW5jOwoijH38z+3JJEx8ufOyBdmR23HvDo4pvycziVfI0hqus22Wufr9PrFVLY9f99AFd06M0gsNL9iV9UUUrQ+jX6ZXVzD5yeKdXIsKrZu0DPjWwdT24gPU/RvExLr3yAqio/oT2A51T5W/y3llEqEUWFTkAAg==";

                var clsSolicitacao = new Solicitacao();

                clsSolicitacao.Pid = "992100026991"; // george "080200280746";
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
                clsSolicitacao.DeTpGrafico = "AC00194824"; 
                //clsSolicitacao.DeQrCode = qrcodeGeorge;


                //return Ok(new PedidosRepository().InserirPedidoBarCode(clsSolicitacao));
                var k = new IdentidadesRepository(_configuration).ConsultarDadosRic("1ED021A05EF5089233379BE996F7BBDD");
                return Ok();
            }
            catch (Exception ex)
            {
                return NotFound(ex.Message);
            }
        }
    }
}
