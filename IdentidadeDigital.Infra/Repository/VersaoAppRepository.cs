using System;
using System.Linq;
using IdentidadeDigital.Infra.Domain.Enums;
using IdentidadeDigital.Infra.Model;
using IdentidadeDigital.Infra.Model.IdDigital;
using IdentidadeDigital.Infra.Repository.Base;
using VersaoApp = IdentidadeDigital.Infra.Model.IdDigital.VersaoApp;

namespace IdentidadeDigital.Infra.Repository
{
    public class VersaoAppRepository : RepositoryBase<VersaoApp, IdDigitalDbContext>
    {

    }
}
