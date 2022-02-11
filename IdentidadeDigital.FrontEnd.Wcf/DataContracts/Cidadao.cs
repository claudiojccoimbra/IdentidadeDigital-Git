using System.Runtime.Serialization;

namespace IdentidadeDigital.FrontEnd.Wcf.DataContracts
{
    [DataContract]
    public class Cidadao
    {
        [DataMember]
        public string NuRic { get; set; }
        [DataMember]
        public string NuVia { get; set; }
        [DataMember]
        public string NuPid { get; set; }
        [DataMember]
        public string NoCidadao { get; set; }
        [DataMember]
        public string NoSocial { get; set; }
        [DataMember]
        public string NoPaiCidadao { get; set; }
        [DataMember]
        public string NoMaeCidadao { get; set; }
        [DataMember]
        public string DtNascimento { get; set; }
        [DataMember]
        public string DtNascimentoAproximada { get; set; }
        [DataMember]
        public string Naturalidade { get; set; }
        [DataMember]
        public string Observacao { get; set; }
        [DataMember]
        public string TipoSanguineo { get; set; }
        [DataMember]
        public string FatorRh { get; set; }
        [DataMember]
        public string Cpf { get; set; }
        [DataMember]
        public string Dni { get; set; }
        [DataMember]
        public string DtExpedicao { get; set; }
        [DataMember]
        public string NuMatriculaCertidao { get; set; }
        [DataMember]
        public string TpCertidao { get; set; }
        [DataMember]
        public string DescricaoCertidao { get; set; }
        [DataMember]
        public string NuCertidaoLivro { get; set; }
        [DataMember]
        public string NuCertidaoFolha { get; set; }
        [DataMember]
        public string NuCertidaoTermo { get; set; }
        [DataMember]
        public string NuCertidaoCircunscricao { get; set; }
        [DataMember]
        public string NuCertidaoDistrito { get; set; }
        [DataMember]
        public string NuCertidaoSubDistrito { get; set; }
        [DataMember]
        public string NuCertidaoZona { get; set; }
        [DataMember]
        public string NoMunicipioNascimento { get; set; }
        [DataMember]
        public string SgUfNascimento { get; set; }
        [DataMember]
        public string NuPortaria { get; set; }
        [DataMember]
        public string AnoPortaria { get; set; }
        [DataMember]
        public string Nacionalidade { get; set; }
        [DataMember]
        public string DtValidade { get; set; }
        [DataMember]
        public string TituloEleitor { get; set; }
        [DataMember]
        public string NuCtps { get; set; }
        [DataMember]
        public string SerieCtps { get; set; }
        [DataMember]
        public string Ufctps { get; set; }
        [DataMember]
        public string Nispispasep { get; set; }
        [DataMember]
        public string IdentProfissional1 { get; set; }
        [DataMember]
        public string IdentProfissional2 { get; set; }
        [DataMember]
        public string IdentProfissional3 { get; set; }
        [DataMember]
        public string CertificadoMilitar { get; set; }
        [DataMember]
        public string Cnh { get; set; }
        [DataMember]
        public string Cns { get; set; }
        [DataMember]
        public string Pcd { get; set; }
        [DataMember]
        public string Cid1 { get; set; }
        [DataMember]
        public string Cid2 { get; set; }
        [DataMember]
        public string Cid3 { get; set; }
        [DataMember]
        public string Cid4 { get; set; }
        [DataMember]
        public string NuPosto { get; set; }
        [DataMember]
        public string NuEspelho { get; set; }
        [DataMember]
        public string NuTipografico { get; set; }
        [DataMember]
        public string MultiParental1 { get; set; }
        [DataMember]
        public string MultiParental2 { get; set; }
        [DataMember]
        public string ImpossAssinar { get; set; }
        [DataMember]
        public string MsgRetorno { get; set; }
        [DataMember]
        public string FotoCivil { get; set; }
        [DataMember]
        public byte[] FotoCivil64 { get; set; }
        [DataMember]
        public string PolegarDireito { get; set; }
        [DataMember]
        public byte[] PolegarDireito64 { get; set; }
        [DataMember]
        public string Assinatura { get; set; }
        [DataMember]
        public byte[] Assinatura64 { get; set; }
        [DataMember]
        public string Chancela { get; set; }
        [DataMember]
        public byte[] Chancela64 { get; set; }
        [DataMember]
        public string Erro { get; set; }
    }
}
