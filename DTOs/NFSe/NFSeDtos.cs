namespace NFSE_ABRASF.DTOs.NFSe
{
    #region Tributos Federais e IBS/CBS (Reforma Tributária)

    /// <summary>
    /// Informações de tributação (tributos federais + totais aproximados)
    /// </summary>
    public class InfoTributacao
    {
        /// <summary>
        /// Tributos federais (PIS/COFINS)
        /// </summary>
        public TributosFederais? TribFed { get; set; }

        /// <summary>
        /// Totais aproximados dos tributos
        /// </summary>
        public TotalTributos TotTrib { get; set; } = new();
    }

    /// <summary>
    /// Tributos federais - PIS/COFINS
    /// </summary>
    public class TributosFederais
    {
        public PisCofins? PisCofins { get; set; }
    }

    /// <summary>
    /// Informações de PIS/COFINS
    /// </summary>
    public class PisCofins
    {
        /// <summary>
        /// CST PIS/COFINS: 00-Nenhum, 01-Alíquota Básica, 02-Diferenciada, etc.
        /// </summary>
        public string CST { get; set; } = "00";
        public decimal? VBCPisCofins { get; set; }
        public decimal? PAliqPis { get; set; }
        public decimal? PAliqCofins { get; set; }
        public decimal? VPis { get; set; }
        public decimal? VCofins { get; set; }
        /// <summary>
        /// 1-Retido, 2-Não Retido
        /// </summary>
        public string? TpRetPisCofins { get; set; }
    }

    /// <summary>
    /// Total dos tributos aproximados
    /// </summary>
    public class TotalTributos
    {
        /// <summary>
        /// Se true, usa percentuais separados (Fed/Est/Mun). Se false, usa Simples Nacional
        /// </summary>
        public bool UsaPercentualSeparado { get; set; } = true;

        /// <summary>
        /// Percentuais separados (empresas não optantes do Simples)
        /// </summary>
        public TotalTributosPercent? PTotTrib { get; set; }

        /// <summary>
        /// Percentual Simples Nacional (%)
        /// </summary>
        public decimal? PTotTribSN { get; set; }
    }

    public class TotalTributosPercent
    {
        /// <summary>
        /// % total tributos federais
        /// </summary>
        public decimal PTotTribFed { get; set; }
        /// <summary>
        /// % total tributos estaduais
        /// </summary>
        public decimal PTotTribEst { get; set; }
        /// <summary>
        /// % total tributos municipais
        /// </summary>
        public decimal PTotTribMun { get; set; }
    }

    /// <summary>
    /// Informações IBS/CBS (Reforma Tributária - LC 214/2025)
    /// </summary>
    public class InfoIBSCBS
    {
        /// <summary>
        /// Finalidade da NFSe: 0 = Regular
        /// </summary>
        public string FinNFSe { get; set; } = "0";

        /// <summary>
        /// Operação de consumo pessoal: 0 = Não, 1 = Sim
        /// </summary>
        public string IndFinal { get; set; } = "0";

        /// <summary>
        /// Código indicador da operação (6 dígitos)
        /// </summary>
        public string CIndOp { get; set; } = "000000";

        /// <summary>
        /// Tipo operação com entes governamentais (1-5)
        /// </summary>
        public string? TpOper { get; set; }

        /// <summary>
        /// NFSes referenciadas
        /// </summary>
        public List<string>? RefNFSe { get; set; }

        /// <summary>
        /// Tipo ente governamental: 1-União, 2-Estado, 3-DF, 4-Município, 9-Outro
        /// </summary>
        public string? TpEnteGov { get; set; }

        /// <summary>
        /// Indicador destinatário: 0-4
        /// </summary>
        public string IndDest { get; set; } = "0";

        /// <summary>
        /// Valores IBS/CBS
        /// </summary>
        public ValoresIBSCBS Valores { get; set; } = new();
    }

    public class ValoresIBSCBS
    {
        /// <summary>
        /// Grupo reembolso/repasse/ressarcimento
        /// </summary>
        public InfoReeRepRes? GReeRepRes { get; set; }

        /// <summary>
        /// Tributos IBS/CBS
        /// </summary>
        public TributosIBSCBS Trib { get; set; } = new();

        /// <summary>
        /// Código IBGE município incidência (7 dígitos)
        /// </summary>
        public string CLocalidadeIncid { get; set; } = string.Empty;

        /// <summary>
        /// Percentual redutor compra governamental
        /// </summary>
        public decimal PRedutor { get; set; }

        /// <summary>
        /// Base de cálculo
        /// </summary>
        public decimal? VBC { get; set; }
    }

    public class TributosIBSCBS
    {
        public SituacaoClassificacaoIBSCBS GIBSCBS { get; set; } = new();
    }

    public class SituacaoClassificacaoIBSCBS
    {
        /// <summary>
        /// Código Situação Tributária (3 dígitos)
        /// </summary>
        public string CST { get; set; } = "000";

        /// <summary>
        /// Código Classificação Tributária (6 dígitos)
        /// </summary>
        public string CClassTrib { get; set; } = "000000";
    }

    public class InfoReeRepRes
    {
        public List<DocumentoReeRepRes> Documentos { get; set; } = new();
    }

    public class DocumentoReeRepRes
    {
        // Simplificado - pode expandir conforme necessidade
        public string TpReeRepRes { get; set; } = "99";
        public decimal VlrReeRepRes { get; set; }
    }

    #endregion

    #region Comércio Exterior

    public class ComercioExterior
    {
        /// <summary>
        /// Modo prestação: 0-Desconhecido, 1-Transfronteiriço, 2-Consumo BR, 3-Presença Comercial, 4-Mov. Temp. Pessoas
        /// </summary>
        public string MdPrestacao { get; set; } = "0";

        /// <summary>
        /// Vínculo: 0-Sem vínculo, 1-Controlada, 2-Controladora, 3-Coligada, 4-Matriz, 5-Filial, 6-Outro
        /// </summary>
        public string VincPrest { get; set; } = "0";

        /// <summary>
        /// Código moeda BACEN (3 dígitos)
        /// </summary>
        public string TpMoeda { get; set; } = "790"; // BRL

        /// <summary>
        /// Valor em moeda estrangeira
        /// </summary>
        public decimal VServMoeda { get; set; }

        /// <summary>
        /// Mecanismo apoio comércio exterior - Prestador (00-08)
        /// </summary>
        public string MecAFComexP { get; set; } = "01";

        /// <summary>
        /// Mecanismo apoio comércio exterior - Tomador (00-26)
        /// </summary>
        public string MecAFComexT { get; set; } = "01";

        /// <summary>
        /// Vínculo movimentação temporária bens: 0-Desconhecido, 1-Não, 2-Vinculada DI, 3-Vinculada DE
        /// </summary>
        public string MovTempBens { get; set; } = "1";

        /// <summary>
        /// Número Declaração Importação
        /// </summary>
        public string? NDI { get; set; }

        /// <summary>
        /// Número Registro Exportação
        /// </summary>
        public string? NRE { get; set; }

        /// <summary>
        /// Enviar para MDIC: 0-Não, 1-Sim
        /// </summary>
        public string Mdic { get; set; } = "0";
    }

    #endregion

    #region Identificação

    public class IdentificacaoRps
    {
        public long Numero { get; set; }
        public string Serie { get; set; } = string.Empty;
        public int Tipo { get; set; } = 1; // 1-RPS, 2-Nota Fiscal Conjugada, 3-Cupom
    }

    public class IdentificacaoNfse
    {
        public long Numero { get; set; }
        public string CodigoVerificacao { get; set; } = string.Empty;
    }

    public class IdentificacaoPrestador
    {
        public string Cnpj { get; set; } = string.Empty;
        public string InscricaoMunicipal { get; set; } = string.Empty;
    }

    public class IdentificacaoTomador
    {
        public string? CpfCnpj { get; set; }
        public string? InscricaoMunicipal { get; set; }
    }

    public class IdentificacaoIntermediario
    {
        public string? CpfCnpj { get; set; }
        public string? InscricaoMunicipal { get; set; }
        public string? RazaoSocial { get; set; }
    }

    #endregion

    #region Endereço e Contato

    public class Endereco
    {
        public string? Logradouro { get; set; }
        public string? Numero { get; set; }
        public string? Complemento { get; set; }
        public string? Bairro { get; set; }
        public string? CodigoMunicipio { get; set; }
        public string? Uf { get; set; }
        public string? Cep { get; set; }
    }

    public class Contato
    {
        public string? Telefone { get; set; }
        public string? Email { get; set; }
    }

    #endregion

    #region Prestador e Tomador

    public class DadosPrestador
    {
        public IdentificacaoPrestador Identificacao { get; set; } = new();
        public string RazaoSocial { get; set; } = string.Empty;
        public string? NomeFantasia { get; set; }
        public Endereco? Endereco { get; set; }
        public Contato? Contato { get; set; }
    }

    public class DadosTomador
    {
        public IdentificacaoTomador? Identificacao { get; set; }
        public string? RazaoSocial { get; set; }
        public Endereco? Endereco { get; set; }
        public Contato? Contato { get; set; }
    }

    #endregion

    #region Serviço e Valores

    public class DadosServico
    {
        public ValoresServico Valores { get; set; } = new();
        public string ItemListaServico { get; set; } = string.Empty;
        public string? CodigoCnae { get; set; }
        public string? CodigoTributacaoMunicipio { get; set; }
        /// <summary>
        /// Código NBS - Obrigatório no GISS
        /// </summary>
        public string CodigoNbs { get; set; } = string.Empty;
        public string Discriminacao { get; set; } = string.Empty;
        public string CodigoMunicipio { get; set; } = string.Empty;
        public string? CodigoPais { get; set; }
        public int? ExigibilidadeISS { get; set; } // 1-Exigível, 2-NãoIncidência, 3-Isenção, 4-Exportação, 5-Imunidade, 6-SuspensaDecisãoJudicial, 7-SuspensaProcessoAdm
        public string? IdentifNaoExigibilidade { get; set; }
        public string? MunicipioIncidencia { get; set; }
        public string? NumeroProcesso { get; set; }
        /// <summary>
        /// Comércio Exterior - quando serviço é exportação/importação
        /// </summary>
        public ComercioExterior? ComExt { get; set; }
    }

    public class ValoresServico
    {
        public decimal ValorServicos { get; set; }
        public decimal? ValorDeducoes { get; set; }
        public decimal? ValorPis { get; set; }
        public decimal? ValorCofins { get; set; }
        public decimal? ValorInss { get; set; }
        public decimal? ValorIr { get; set; }
        public decimal? ValorCsll { get; set; }
        public decimal? OutrasRetencoes { get; set; }
        public decimal? ValTotTributos { get; set; }
        public decimal? ValorIss { get; set; }
        public decimal? Aliquota { get; set; }
        public decimal? DescontoIncondicionado { get; set; }
        public decimal? DescontoCondicionado { get; set; }
        public int? IssRetido { get; set; } // 1-Sim, 2-Não
        public int? ResponsavelRetencao { get; set; } // 1-Tomador, 2-Intermediário
        public decimal? ValorLiquidoNfse { get; set; }
        public decimal? BaseCalculo { get; set; }

        /// <summary>
        /// Informações de tributação (PIS/COFINS + totais aproximados) - OBRIGATÓRIO GISS
        /// </summary>
        public InfoTributacao Trib { get; set; } = new();

        /// <summary>
        /// Informações IBS/CBS (Reforma Tributária) - OBRIGATÓRIO GISS
        /// </summary>
        public InfoIBSCBS IBSCBS { get; set; } = new();
    }

    #endregion

    #region Construção Civil

    public class DadosConstrucaoCivil
    {
        public string? CodigoObra { get; set; }
        public string? Art { get; set; }
    }

    #endregion

    #region RPS

    public class InfRps
    {
        public IdentificacaoRps Identificacao { get; set; } = new();
        public DateTime DataEmissao { get; set; } = DateTime.Now;
        public int NaturezaOperacao { get; set; } // 1-TribMunicipio, 2-TribForaMunicipio, 3-Isenção, 4-Imune, 5-ExigívelSuspDecJud, 6-ExigívelSuspProcAdm
        public int? RegimeEspecialTributacao { get; set; } // 1-ME, 2-EstimativaProfAutônomo, 3-SocProfissionais, 4-Cooperativa, 5-MEI, 6-ME/EPP-Simples
        public int OptanteSimplesNacional { get; set; } // 1-Sim, 2-Não
        public int IncentivadorCultural { get; set; } // 1-Sim, 2-Não
        public DateTime? Competencia { get; set; }
        public DadosServico Servico { get; set; } = new();
        public DadosTomador? Tomador { get; set; }
        public IdentificacaoIntermediario? Intermediario { get; set; }
        public DadosConstrucaoCivil? ConstrucaoCivil { get; set; }
    }

    public class Rps
    {
        public InfRps InfDeclaracaoPrestacaoServico { get; set; } = new();
    }

    #endregion

    #region Requests

    public class GerarNfseRequest
    {
        public Rps Rps { get; set; } = new();
    }

    public class EnviarLoteRpsRequest
    {
        public string NumeroLote { get; set; } = string.Empty;
        public int QuantidadeRps => ListaRps?.Count ?? 0;
        public List<Rps> ListaRps { get; set; } = new();
    }

    public class ConsultarNfsePorRpsRequest
    {
        public IdentificacaoRps IdentificacaoRps { get; set; } = new();
    }

    public class ConsultarNfseRequest
    {
        public DateTime? DataInicial { get; set; }
        public DateTime? DataFinal { get; set; }
        public IdentificacaoTomador? Tomador { get; set; }
        public IdentificacaoIntermediario? Intermediario { get; set; }
        public long? NumeroNfse { get; set; }
        public int Pagina { get; set; } = 1;
    }

    public class CancelarNfseRequest
    {
        public long NumeroNfse { get; set; }
        public string CodigoCancelamento { get; set; } = string.Empty;
    }

    public class SubstituirNfseRequest
    {
        public long NumeroNfseSubstituida { get; set; }
        public string CodigoCancelamento { get; set; } = string.Empty;
        public Rps RpsSubstituto { get; set; } = new();
    }

    #endregion

    #region Responses

    public class MensagemRetorno
    {
        public string Codigo { get; set; } = string.Empty;
        public string Mensagem { get; set; } = string.Empty;
        public string? Correcao { get; set; }
    }

    public class NfseGerada
    {
        public long Numero { get; set; }
        public string CodigoVerificacao { get; set; } = string.Empty;
        public DateTime DataEmissao { get; set; }
        public string? XmlNfse { get; set; }
        public string? LinkVisualizacao { get; set; }
    }

    public abstract class BaseResponse
    {
        public bool Sucesso { get; set; }
        public List<MensagemRetorno> Mensagens { get; set; } = new();
        public string? XmlEnviado { get; set; }
        public string? XmlRetorno { get; set; }
    }

    public class GerarNfseResponse : BaseResponse
    {
        public NfseGerada? Nfse { get; set; }
    }

    public class EnviarLoteRpsResponse : BaseResponse
    {
        public string? NumeroLote { get; set; }
        public string? Protocolo { get; set; }
        public DateTime? DataRecebimento { get; set; }
    }

    public class EnviarLoteRpsSincronoResponse : BaseResponse
    {
        public string? NumeroLote { get; set; }
        public List<NfseGerada> NfsesGeradas { get; set; } = new();
    }

    public class ConsultarSituacaoLoteRpsResponse : BaseResponse
    {
        public int? Situacao { get; set; } // 1-NãoRecebido, 2-NãoProcessado, 3-ProcessadoComErro, 4-ProcessadoComSucesso
        public string? DescricaoSituacao { get; set; }
    }

    public class ConsultarLoteRpsResponse : BaseResponse
    {
        public List<NfseGerada> NfsesGeradas { get; set; } = new();
    }

    public class ConsultarNfsePorRpsResponse : BaseResponse
    {
        public NfseGerada? Nfse { get; set; }
    }

    public class ConsultarNfseResponse : BaseResponse
    {
        public List<NfseGerada> Nfses { get; set; } = new();
        public int TotalPaginas { get; set; }
        public int PaginaAtual { get; set; }
    }

    public class CancelarNfseResponse : BaseResponse
    {
        public long? NumeroNfseCancelada { get; set; }
        public DateTime? DataCancelamento { get; set; }
    }

    public class SubstituirNfseResponse : BaseResponse
    {
        public long? NumeroNfseCancelada { get; set; }
        public NfseGerada? NfseSubstituta { get; set; }
    }

    #endregion
}