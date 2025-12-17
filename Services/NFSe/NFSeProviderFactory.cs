using NFSE_ABRASF.Exceptions;
using NFSE_ABRASF.Services.NFSe.Interfaces;

namespace NFSE_ABRASF.Services.NFSe
{
    /// <summary>
    /// Interface para factory de provedores NFSe
    /// </summary>
    public interface INFSeProviderFactory
    {
        /// <summary>
        /// Obtém o provedor NFSe pelo código do município (IBGE)
        /// </summary>
        INFSeProvider ObterPorCodigoMunicipio(string codigoMunicipio);

        /// <summary>
        /// Obtém o provedor NFSe pelo nome do município
        /// </summary>
        INFSeProvider ObterPorNomeMunicipio(string nomeMunicipio);

        /// <summary>
        /// Lista todos os municípios disponíveis
        /// </summary>
        IEnumerable<MunicipioInfo> ListarMunicipiosDisponiveis();

        /// <summary>
        /// Verifica se existe provedor para o município
        /// </summary>
        bool MunicipioDisponivel(string codigoMunicipio);
    }

    /// <summary>
    /// Informações do município
    /// </summary>
    public class MunicipioInfo
    {
        public string CodigoIbge { get; set; } = string.Empty;
        public string Nome { get; set; } = string.Empty;
        public string UF { get; set; } = string.Empty;
        public string Provedor { get; set; } = string.Empty;
        public string VersaoAbrasf { get; set; } = string.Empty;
    }

    /// <summary>
    /// Factory para resolver o provedor NFSe correto baseado no município
    /// </summary>
    public class NFSeProviderFactory : INFSeProviderFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<NFSeProviderFactory> _logger;

        /// <summary>
        /// Mapeamento de código IBGE para tipo do provedor
        /// </summary>
        private static readonly Dictionary<string, Type> _provedoresPorCodigo = new()
        {
            // Santos/SP - GISS
            { "3548500", typeof(Providers.SantosNFSeProvider) },

            // Adicione mais municípios aqui conforme necessário
            // { "3550308", typeof(Providers.SaoPauloNFSeProvider) }, // São Paulo
            // { "3304557", typeof(Providers.RioDeJaneiroNFSeProvider) }, // Rio de Janeiro
        };

        /// <summary>
        /// Mapeamento de nome do município para código IBGE
        /// </summary>
        private static readonly Dictionary<string, string> _codigosPorNome = new(StringComparer.OrdinalIgnoreCase)
        {
            { "Santos", "3548500" },
            { "Santos/SP", "3548500" },

            // Adicione mais mapeamentos conforme necessário
            // { "São Paulo", "3550308" },
            // { "Rio de Janeiro", "3304557" },
        };

        /// <summary>
        /// Informações detalhadas dos municípios disponíveis
        /// </summary>
        private static readonly List<MunicipioInfo> _municipiosDisponiveis = new()
        {
            new MunicipioInfo
            {
                CodigoIbge = "3548500",
                Nome = "Santos",
                UF = "SP",
                Provedor = "GISS",
                VersaoAbrasf = "2.04"
            },
            // Adicione mais municípios conforme implementados
        };

        public NFSeProviderFactory(
            IServiceProvider serviceProvider,
            ILogger<NFSeProviderFactory> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public INFSeProvider ObterPorCodigoMunicipio(string codigoMunicipio)
        {
            if (string.IsNullOrEmpty(codigoMunicipio))
                throw new BusinessException("Código do município não informado.");

            // Normalizar código (remover pontos, traços, etc.)
            codigoMunicipio = codigoMunicipio.Replace(".", "").Replace("-", "").Trim();

            if (!_provedoresPorCodigo.TryGetValue(codigoMunicipio, out var tipoProvedor))
            {
                _logger.LogWarning("Município não suportado: {CodigoMunicipio}", codigoMunicipio);
                throw new BusinessException(
                    $"Município com código {codigoMunicipio} não está disponível para emissão de NFSe. " +
                    $"Municípios disponíveis: {string.Join(", ", _municipiosDisponiveis.Select(m => $"{m.Nome}/{m.UF}"))}");
            }

            var provider = _serviceProvider.GetService(tipoProvedor) as INFSeProvider;

            if (provider == null)
            {
                _logger.LogError("Falha ao resolver provedor para município {CodigoMunicipio}", codigoMunicipio);
                throw new BusinessException($"Erro interno ao carregar provedor do município {codigoMunicipio}.");
            }

            _logger.LogDebug("Provedor {Provedor} resolvido para município {Municipio}",
                provider.NomeProvedor, provider.NomeMunicipio);

            return provider;
        }

        public INFSeProvider ObterPorNomeMunicipio(string nomeMunicipio)
        {
            if (string.IsNullOrEmpty(nomeMunicipio))
                throw new BusinessException("Nome do município não informado.");

            if (!_codigosPorNome.TryGetValue(nomeMunicipio, out var codigoMunicipio))
            {
                _logger.LogWarning("Município não encontrado pelo nome: {NomeMunicipio}", nomeMunicipio);
                throw new BusinessException(
                    $"Município '{nomeMunicipio}' não está disponível para emissão de NFSe. " +
                    $"Municípios disponíveis: {string.Join(", ", _municipiosDisponiveis.Select(m => $"{m.Nome}/{m.UF}"))}");
            }

            return ObterPorCodigoMunicipio(codigoMunicipio);
        }

        public IEnumerable<MunicipioInfo> ListarMunicipiosDisponiveis()
        {
            return _municipiosDisponiveis.AsReadOnly();
        }

        public bool MunicipioDisponivel(string codigoMunicipio)
        {
            if (string.IsNullOrEmpty(codigoMunicipio))
                return false;

            codigoMunicipio = codigoMunicipio.Replace(".", "").Replace("-", "").Trim();
            return _provedoresPorCodigo.ContainsKey(codigoMunicipio);
        }
    }
}