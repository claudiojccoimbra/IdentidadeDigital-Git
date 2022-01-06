using System;
using System.Collections.Generic;
using System.Text;

namespace IdentidadeDigital.Infra.Domain
{
    public class Cidadao
    {
        public string NuRic { get; set; }
        public string NuVia { get; set; }
        public string NuPid { get; set; }
        public string NoCidadao { get; set; }
        public string NoSocial { get; set; }
        public string NoPaiCidadao { get; set; }
        public string NoMaeCidadao { get; set; }
        public string DtNascimento { get; set; }
        public string DtNascimentoAproximada { get; set; }
        public string Naturalidade { get; set; }
        public string Observacao { get; set; }
        public string TipoSanguineo { get; set; }
        public string FatorRh { get; set; }
        public string Cpf { get; set; }
        public string Dni { get; set; }
        public string DtExpedicao { get; set; }
        public string NuMatriculaCertidao { get; set; }
        public string TpCertidao { get; set; }
        public string DescricaoCertidao { get; set; }
        public string NuCertidaoLivro { get; set; }
        public string NuCertidaoFolha { get; set; }
        public string NuCertidaoTermo { get; set; }
        public string NuCertidaoCircunscricao { get; set; }
        public string NuCertidaoDistrito { get; set; }
        public string NuCertidaoSubDistrito { get; set; }
        public string NuCertidaoZona { get; set; }
        public string NoMunicipioNascimento { get; set; }
        public string SgUfNascimento { get; set; }
        public string NuPortaria { get; set; }
        public string AnoPortaria { get; set; }
        public string Nacionalidade { get; set; }
        public string DtValidade { get; set; }
        public string TituloEleitor { get; set; }
        public string NuCtps { get; set; }
        public string SerieCtps { get; set; }
        public string Ufctps { get; set; }
        public string Nispispasep { get; set; }
        public string IdentProfissional1 { get; set; }
        public string IdentProfissional2 { get; set; }
        public string IdentProfissional3 { get; set; }
        public string CertificadoMilitar { get; set; }
        public string Cnh { get; set; }
        public string Cns { get; set; }
        public string Pcd { get; set; }
        public string Cid1 { get; set; }
        public string Cid2 { get; set; }
        public string Cid3 { get; set; }
        public string Cid4 { get; set; }
        public string NuPosto { get; set; }
        public string NuEspelho { get; set; }
        public string NuTipografico { get; set; }
        public string MultiParental1 { get; set; }
        public string MultiParental2 { get; set; }
        public string ImpossAssinar { get; set; }
        public string MsgRetorno { get; set; }

        public string FotoCivil { get; set; }
        public byte[] FotoCivil64 { get; set; }
        public string PolegarDireito { get; set; }
        public byte[] PolegarDireito64 { get; set; }
        public string Assinatura { get; set; }
        public byte[] Assinatura64 { get; set; }
        public string Chancela { get; set; }
        public byte[] Chancela64 { get; set; }
        public string Erro { get; set; }
    }
}
