using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace IdentidadeDigital.Infra.Domain.Enums
{
    public enum TipoProvaVidaEnum
    {
        [Description("Frente")]
        Frente = 1,
        [Description("Sorriso")]
        Sorriso = 2,
        [Description("Lado")]
        Lado = 3
    }
}
