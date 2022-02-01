using IdentidadeDigital.Infra.Domain;
using IdentidadeDigital.Infra.Domain.Enums;
using IdentidadeDigital.Infra.Repository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;

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
                return Ok(statusCarteira);
            }
            catch (Exception ex)
            {
                statusCarteira.Erro = ex.Message;
                return Ok(statusCarteira);
            }
        }

        [HttpGet("ChecarVersao/{nuVersao}")]
        [ProducesResponseType(200, Type = typeof(VersaoApp))]
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
                return Ok(versaoApp);
            }
            catch (Exception ex)
            {
                versaoApp.Erro = ex.Message;
                return Ok(versaoApp);
            }
        }

        [HttpPost("InserirPedidoQrCode")]
        public IActionResult InserirPedidoQrCode([FromBody] Solicitacao solicitacao)
        {
            try
            {
                return Ok(new PedidosRepository().InserirPedidoQrCode(solicitacao));
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        [HttpPost("InserirPedidoBarCode")]
        public IActionResult InserirPedidoBarCode([FromBody] Solicitacao solicitacao)
        {
            try
            {
                return Ok(new PedidosRepository().InserirPedidoBarCode(solicitacao));
            }
            catch (Exception ex)
            {
                throw ex;
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
                throw ex;
            }
        }

        [HttpPost("InserirImagemCarteira")]
        public IActionResult InserirImagemCarteira([FromBody] Carteira carteira, string idTransacao)
        {
            try
            {
                return Ok(new IdentidadesRepository(_configuration).InserirIdentidade(carteira, idTransacao));
            }
            catch (Exception ex)
            {
                throw ex;
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
                throw ex;
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
                throw ex;
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
                throw ex;
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
                return Ok(carteira);
            }
            catch (Exception ex)
            {
                carteira.Erro = ex.Message;
                return Ok(carteira);
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
                throw ex;
            }
        }

        [HttpGet("ConsultarDadosRic/{idTransacao}")]
        [ProducesResponseType(200, Type = typeof(Cidadao))]
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
                return Ok(cidadao);
            }
            catch (Exception ex)
            {
                cidadao.Erro = ex.Message;
                return Ok(cidadao);
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
                throw ex;
            }
        }

        [HttpGet("VerificarPermissoesQrCodeAcessoCarteira/{dataInclusao}/{nuRg}")]
        [ProducesResponseType(200, Type = typeof(bool))]
        public IActionResult VerificarPermissoesQrCodeAcessoCarteira(DateTime dataInclusao, long nuRg)
        {
            try
            {
                return Ok(new PedidosRepository().VerificarPermissoesAcessoCarteira(dataInclusao, nuRg));
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
