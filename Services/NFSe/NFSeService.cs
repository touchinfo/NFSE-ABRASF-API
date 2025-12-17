using NFSE_ABRASF.DTOs.NFSe;
using NFSE_ABRASF.Exceptions;
using NFSE_ABRASF.Models;
using NFSE_ABRASF.Repositories.Interfaces;

namespace NFSE_ABRASF.Services.NFSe
{
    /// <summary>
    /// Interface do serviço principal de NFSe
    /// </summary>
    public interface INFSeService
    {
        Task<GerarNfseResponse> GerarNfseAsync(int empresaId, GerarNfseRequest request);
        Task<EnviarLoteRpsResponse> EnviarLoteRpsAsync(int empresaId, EnviarLoteRpsRequest request);
        Task<EnviarLoteRpsSincronoResponse> EnviarLoteRpsSincronoAsync(int empresaId, EnviarLoteRpsRequest request);
        Task<ConsultarSituacaoLoteRpsResponse> ConsultarSituacaoLoteRpsAsync(int empresaId, string protocolo);
        Task<ConsultarLoteRpsResponse> ConsultarLoteRpsAsync(int empresaId, string protocolo);
        Task<ConsultarNfsePorRpsResponse> ConsultarNfsePorRpsAsync(int empresaId, ConsultarNfsePorRpsRequest request);
        Task<ConsultarNfseResponse> ConsultarNfseAsync(int empresaId, ConsultarNfseRequest request);
        Task<CancelarNfseResponse> CancelarNfseAsync(int empresaId, CancelarNfseRequest request);
        Task<SubstituirNfseResponse> SubstituirNfseAsync(int empresaId, SubstituirNfseRequest request);
        IEnumerable<MunicipioInfo> ListarMunicipiosDisponiveis();
    }

    /// <summary>
    /// Serviço principal de NFSe que coordena as operações
    /// Resolve automaticamente o provedor correto baseado no município da empresa
    /// </summary>
    public class NFSeService : INFSeService
    {
        private readonly IEmpresaRepository _empresaRepository;
        private readonly INFSeProviderFactory _providerFactory;
        private readonly ILogger<NFSeService> _logger;

        public NFSeService(
            IEmpresaRepository empresaRepository,
            INFSeProviderFactory providerFactory,
            ILogger<NFSeService> logger)
        {
            _empresaRepository = empresaRepository;
            _providerFactory = providerFactory;
            _logger = logger;
        }

        public async Task<GerarNfseResponse> GerarNfseAsync(int empresaId, GerarNfseRequest request)
        {
            var (empresa, provider, homologacao) = await ObterContextoAsync(empresaId);

            _logger.LogInformation(
                "Gerando NFSe para empresa {EmpresaId} no município {Municipio} ({Ambiente})",
                empresaId, provider.NomeMunicipio, homologacao ? "Homologação" : "Produção");

            return await provider.GerarNfseAsync(request, empresa, homologacao);
        }

        public async Task<EnviarLoteRpsResponse> EnviarLoteRpsAsync(int empresaId, EnviarLoteRpsRequest request)
        {
            var (empresa, provider, homologacao) = await ObterContextoAsync(empresaId);

            _logger.LogInformation(
                "Enviando lote RPS ({QtdRps} RPS) para empresa {EmpresaId} no município {Municipio}",
                request.QuantidadeRps, empresaId, provider.NomeMunicipio);

            return await provider.EnviarLoteRpsAsync(request, empresa, homologacao);
        }

        public async Task<EnviarLoteRpsSincronoResponse> EnviarLoteRpsSincronoAsync(int empresaId, EnviarLoteRpsRequest request)
        {
            var (empresa, provider, homologacao) = await ObterContextoAsync(empresaId);

            _logger.LogInformation(
                "Enviando lote RPS síncrono ({QtdRps} RPS) para empresa {EmpresaId}",
                request.QuantidadeRps, empresaId);

            return await provider.EnviarLoteRpsSincronoAsync(request, empresa, homologacao);
        }

        public async Task<ConsultarSituacaoLoteRpsResponse> ConsultarSituacaoLoteRpsAsync(int empresaId, string protocolo)
        {
            var (empresa, provider, homologacao) = await ObterContextoAsync(empresaId);

            _logger.LogInformation(
                "Consultando situação do lote {Protocolo} para empresa {EmpresaId}",
                protocolo, empresaId);

            return await provider.ConsultarSituacaoLoteRpsAsync(protocolo, empresa, homologacao);
        }

        public async Task<ConsultarLoteRpsResponse> ConsultarLoteRpsAsync(int empresaId, string protocolo)
        {
            var (empresa, provider, homologacao) = await ObterContextoAsync(empresaId);

            _logger.LogInformation(
                "Consultando lote RPS {Protocolo} para empresa {EmpresaId}",
                protocolo, empresaId);

            return await provider.ConsultarLoteRpsAsync(protocolo, empresa, homologacao);
        }

        public async Task<ConsultarNfsePorRpsResponse> ConsultarNfsePorRpsAsync(int empresaId, ConsultarNfsePorRpsRequest request)
        {
            var (empresa, provider, homologacao) = await ObterContextoAsync(empresaId);

            _logger.LogInformation(
                "Consultando NFSe por RPS {NumeroRps} para empresa {EmpresaId}",
                request.IdentificacaoRps.Numero, empresaId);

            return await provider.ConsultarNfsePorRpsAsync(request, empresa, homologacao);
        }

        public async Task<ConsultarNfseResponse> ConsultarNfseAsync(int empresaId, ConsultarNfseRequest request)
        {
            var (empresa, provider, homologacao) = await ObterContextoAsync(empresaId);

            _logger.LogInformation(
                "Consultando NFSe para empresa {EmpresaId} - Período: {DataInicial} a {DataFinal}",
                empresaId, request.DataInicial, request.DataFinal);

            return await provider.ConsultarNfseAsync(request, empresa, homologacao);
        }

        public async Task<CancelarNfseResponse> CancelarNfseAsync(int empresaId, CancelarNfseRequest request)
        {
            var (empresa, provider, homologacao) = await ObterContextoAsync(empresaId);

            _logger.LogInformation(
                "Cancelando NFSe {NumeroNfse} para empresa {EmpresaId}",
                request.NumeroNfse, empresaId);

            return await provider.CancelarNfseAsync(request, empresa, homologacao);
        }

        public async Task<SubstituirNfseResponse> SubstituirNfseAsync(int empresaId, SubstituirNfseRequest request)
        {
            var (empresa, provider, homologacao) = await ObterContextoAsync(empresaId);

            _logger.LogInformation(
                "Substituindo NFSe {NumeroNfse} para empresa {EmpresaId}",
                request.NumeroNfseSubstituida, empresaId);

            return await provider.SubstituirNfseAsync(request, empresa, homologacao);
        }

        public IEnumerable<MunicipioInfo> ListarMunicipiosDisponiveis()
        {
            return _providerFactory.ListarMunicipiosDisponiveis();
        }

        /// <summary>
        /// Obtém o contexto necessário para as operações: empresa, provedor e ambiente
        /// </summary>
        private async Task<(Empresas empresa, Interfaces.INFSeProvider provider, bool homologacao)> ObterContextoAsync(int empresaId)
        {
            var empresa = await _empresaRepository.ObterPorIdAsync(empresaId)
                ?? throw new NotFoundException("Empresa", empresaId);

            if (!empresa.Ativa)
                throw new BusinessException("Esta empresa está inativa.");

            if (string.IsNullOrEmpty(empresa.Codigo_Municipio))
                throw new BusinessException("Código do município não configurado para esta empresa.");

            if (empresa.Certificado_Pfx == null || empresa.Certificado_Pfx.Length == 0)
                throw new BusinessException("Certificado digital não configurado para esta empresa.");

            if (empresa.Certificado_Validade.HasValue && empresa.Certificado_Validade.Value < DateTime.Now)
                throw new BusinessException($"Certificado digital vencido em {empresa.Certificado_Validade.Value:dd/MM/yyyy}.");

            var provider = _providerFactory.ObterPorCodigoMunicipio(empresa.Codigo_Municipio);

            // Tipo_Ambiente: "1" = Produção, "2" = Homologação
            var homologacao = empresa.Tipo_Ambiente != "1";

            return (empresa, provider, homologacao);
        }
    }
}