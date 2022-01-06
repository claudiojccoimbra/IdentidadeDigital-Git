using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace IdentidadeDigital.Infra.Domain.Enums
{
    public enum TipoErroEnum
    {
        [Description("[RF001]-Erro ao gravar a imagem no banco de dados.")]
        InserirImagem = 1,
        [Description("[RF002]-Erro um PID especificado não existe.")]
        SemDadosPid = 2,
        [Description("[RF003]-Erro ao inserir identificação. Tente novamente.")]
        InserirIdentificacao = 3,
        [Description("[RF004]-Erro na geração da imagem na carteira. Tente novamente")]
        InserirImagemCarteira = 4,
        [Description("[RF005]-Erro na geração no HTML da carteira. Tente novamente")]
        GerarHtmlCarteira = 5,
        [Description("[RF006]-Erro na carga de dados do cidadão. Tente novamente")]
        CarregamentoDadosCidadao = 6,
        [Description("[RF007]-Erro em buscar dados do PID. Tente novamente")]
        BuscarDadosPid = 7,
        [Description("[RF008]-Erro em carregar foto usuário")]
        CarregarImagemFoto = 8,
        [Description("[RF009]-Erro na validação do QR CODE. Tente novamente")]
        ValidacaoQrCode = 9,
        [Description("[RF010]-Erro na validação da face. Tente novamente")]
        ValidacaoFace = 10,
        [Description("[RF011]-Erro ao carregar dados da chancela")]
        CarregarDadosChancela = 11,
        [Description("[RF012]-Erro ao verificar o status da carteira")]
        VerificarStatusCarteira = 12,
        [Description("[RF013]-Erro ao checar a versão do aplicativo")]
        ChecarVersao = 13,
        [Description("[RF014]-Erro na validação da face. Tente novamente")]
        AtualizarStatusIdentidade = 14,
        [Description("[RF015]-Versão Inexistente")]
        ChecarVersaoApp = 15,
        [Description("[RF016]-Erro na consulta da foto na carteira")]
        ConsultarFotoCarteira = 16,
        [Description("[RF017]-Serviço temporariamente indisponível. Tente novamente")]
        EndPointError = 17,
        [Description("[RF018]-Erro ao inserir prova de vida. Tente novamente")]
        InserirProvaVida = 18,
        [Description("[RF019]-Carteira digital indisponível para identidades emitidas antes de 05 abril de 2019")]
        CarteiraDigitalIndiponivel = 19,
        [Description("[RF020]-Erro ao verificar a permissão da carteira digital. Tente novamente")]
        VerificarPermissaoCarteira = 20,
        [Description("[RF021]-Erro ao verificar ID Transação")]
        VerificarIdTransacaoProvaVida = 21,
        [Description("[RF022]-Erro ao carregar dados da assinatura.")]
        CarregarAssinatura = 22,
        [Description("[RF023]-Erro ao adquirir Sequence em Pedidos")]
        GetSession = 23
    }
}
