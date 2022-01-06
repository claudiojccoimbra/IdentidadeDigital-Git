using System.ComponentModel;

namespace IdentidadeDigital.Infra.Domain.Enums
{
    public enum TipoStatusIdEnum
    {
        [Description("Identidade digital solicitada, aguarde.")]
        Solicitado = 1,
        [Description("Atualizando identidade digital. Aguarde o download.")]
        Match = 2,
        [Description("Aguarde a geração da Identidade digital.")]
        Finalizado = 3,
        [Description("Identidade digital válida.")]
        Valido = 5,
        [Description("Identidade digital expirada. Realize nova solicitação.")]
        Cancelado = 6,
        [Description("Problemas durante o processo, tente novamente.")]
        Interrompido = 7
    }
}
