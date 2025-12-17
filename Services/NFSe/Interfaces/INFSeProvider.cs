using NFSE_ABRASF.DTOs.NFSe;
using NFSE_ABRASF.Models;

namespace NFSE_ABRASF.Services.NFSe.Interfaces
{
    /// <summary>
    /// Interface base para provedores de NFSe
    /// Cada município/provedor implementa esta interface
    /// </summary>
    public interface INFSeProvider
    {
        /// <summary>
        /// Código IBGE do município
        /// </summary>
        string CodigoMunicipio { get; }

        /// <summary>
        /// Nome do município
        /// </summary>
        string NomeMunicipio { get; }

        /// <summary>
        /// Versão do ABRASF implementada (ex: "2.04")
        /// </summary>
        string VersaoAbrasf { get; }

        /// <summary>
        /// Nome do provedor/sistema (ex: "GISS", "BETHA", "IPM")
        /// </summary>
        string NomeProvedor { get; }

        /// <summary>
        /// URL do WebService de Homologação
        /// </summary>
        string UrlHomologacao { get; }

        /// <summary>
        /// URL do WebService de Produção
        /// </summary>
        string UrlProducao { get; }

        /// <summary>
        /// Gera o RPS (Recibo Provisório de Serviço)
        /// </summary>
        Task<GerarNfseResponse> GerarNfseAsync(GerarNfseRequest request, Empresas empresa, bool homologacao);

        /// <summary>
        /// Envia lote de RPS para conversão em NFSe
        /// </summary>
        Task<EnviarLoteRpsResponse> EnviarLoteRpsAsync(EnviarLoteRpsRequest request, Empresas empresa, bool homologacao);

        /// <summary>
        /// Envia lote de RPS de forma síncrona
        /// </summary>
        Task<EnviarLoteRpsSincronoResponse> EnviarLoteRpsSincronoAsync(EnviarLoteRpsRequest request, Empresas empresa, bool homologacao);

        /// <summary>
        /// Consulta situação do lote de RPS
        /// </summary>
        Task<ConsultarSituacaoLoteRpsResponse> ConsultarSituacaoLoteRpsAsync(string protocolo, Empresas empresa, bool homologacao);

        /// <summary>
        /// Consulta lote de RPS
        /// </summary>
        Task<ConsultarLoteRpsResponse> ConsultarLoteRpsAsync(string protocolo, Empresas empresa, bool homologacao);

        /// <summary>
        /// Consulta NFSe por RPS
        /// </summary>
        Task<ConsultarNfsePorRpsResponse> ConsultarNfsePorRpsAsync(ConsultarNfsePorRpsRequest request, Empresas empresa, bool homologacao);

        /// <summary>
        /// Consulta NFSe por período/tomador/intermediário
        /// </summary>
        Task<ConsultarNfseResponse> ConsultarNfseAsync(ConsultarNfseRequest request, Empresas empresa, bool homologacao);

        /// <summary>
        /// Cancela uma NFSe
        /// </summary>
        Task<CancelarNfseResponse> CancelarNfseAsync(CancelarNfseRequest request, Empresas empresa, bool homologacao);

        /// <summary>
        /// Substitui uma NFSe (cancela e gera nova)
        /// </summary>
        Task<SubstituirNfseResponse> SubstituirNfseAsync(SubstituirNfseRequest request, Empresas empresa, bool homologacao);
    }
}