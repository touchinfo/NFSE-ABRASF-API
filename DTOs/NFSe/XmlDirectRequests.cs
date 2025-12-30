namespace NFSE_ABRASF.DTOs.NFSe
{
    /// <summary>
    /// Request genérico para envio de XML já montado
    /// O cliente monta o XML completo e a API apenas assina e envia
    /// </summary>
    public class XmlDirectRequest
    {
        /// <summary>
        /// XML completo já montado pelo cliente (sem assinatura)
        /// </summary>
        public string XmlContent { get; set; } = string.Empty;

        /// <summary>
        /// Nome do método/operação SOAP (ex: "GerarNfse", "RecepcionarLoteRps")
        /// </summary>
        public string MetodoSoap { get; set; } = string.Empty;

        /// <summary>
        /// Validar o XML contra XSD antes de enviar? (opcional, default: false)
        /// </summary>
        public bool ValidarXsd { get; set; } = false;

        /// <summary>
        /// Nome do arquivo XSD para validação (ex: "servico_enviar_lote_rps_envio.xsd")
        /// Obrigatório se ValidarXsd = true
        /// </summary>
        public string? NomeArquivoXsd { get; set; }
    }

    /// <summary>
    /// Resposta genérica para requisições com XML direto
    /// </summary>
    public class XmlDirectResponse
    {
        /// <summary>
        /// Indica se a operação foi bem-sucedida
        /// </summary>
        public bool Sucesso { get; set; }

        /// <summary>
        /// XML original enviado pelo cliente (sem assinatura)
        /// </summary>
        public string? XmlOriginal { get; set; }

        /// <summary>
        /// XML após assinatura digital
        /// </summary>
        public string? XmlAssinado { get; set; }

        /// <summary>
        /// XML retornado pela prefeitura
        /// </summary>
        public string? XmlResposta { get; set; }

        /// <summary>
        /// Mensagens de erro ou aviso (se houver)
        /// </summary>
        public List<MensagemRetorno> Mensagens { get; set; } = new();

        /// <summary>
        /// Código HTTP retornado pela prefeitura
        /// </summary>
        public int? HttpStatusCode { get; set; }

        /// <summary>
        /// Tempo de processamento em milissegundos
        /// </summary>
        public long TempoProcessamentoMs { get; set; }
    }

    /// <summary>
    /// Request específico para GerarNfse com XML
    /// </summary>
    public class GerarNfseXmlRequest : XmlDirectRequest
    {
        public GerarNfseXmlRequest()
        {
            MetodoSoap = "GerarNfse";
            NomeArquivoXsd = "servico_enviar_lote_rps_envio.xsd";
        }
    }

    /// <summary>
    /// Request específico para EnviarLoteRps com XML
    /// </summary>
    public class EnviarLoteRpsXmlRequest : XmlDirectRequest
    {
        public EnviarLoteRpsXmlRequest()
        {
            MetodoSoap = "RecepcionarLoteRps";
            NomeArquivoXsd = "servico_enviar_lote_rps_envio.xsd";
        }
    }

    /// <summary>
    /// Request específico para RecepcionarLoteRpsSincrono com XML
    /// </summary>
    public class EnviarLoteRpsSincronoXmlRequest : XmlDirectRequest
    {
        public EnviarLoteRpsSincronoXmlRequest()
        {
            MetodoSoap = "RecepcionarLoteRpsSincrono";
            NomeArquivoXsd = "servico_enviar_lote_rps_envio.xsd";
        }
    }

    /// <summary>
    /// Request específico para ConsultarSituacaoLoteRps com XML
    /// </summary>
    public class ConsultarSituacaoLoteXmlRequest : XmlDirectRequest
    {
        public ConsultarSituacaoLoteXmlRequest()
        {
            MetodoSoap = "ConsultarSituacaoLoteRps";
            NomeArquivoXsd = "servico_consultar_situacao_lote_rps_envio.xsd";
        }
    }

    /// <summary>
    /// Request específico para ConsultarLoteRps com XML
    /// </summary>
    public class ConsultarLoteRpsXmlRequest : XmlDirectRequest
    {
        public ConsultarLoteRpsXmlRequest()
        {
            MetodoSoap = "ConsultarLoteRps";
            NomeArquivoXsd = "servico_consultar_lote_rps_envio.xsd";
        }
    }

    /// <summary>
    /// Request específico para ConsultarNfsePorRps com XML
    /// </summary>
    public class ConsultarNfsePorRpsXmlRequest : XmlDirectRequest
    {
        public ConsultarNfsePorRpsXmlRequest()
        {
            MetodoSoap = "ConsultarNfsePorRps";
            NomeArquivoXsd = "servico_consultar_nfse_rps_envio.xsd";
        }
    }

    /// <summary>
    /// Request específico para ConsultarNfse com XML
    /// </summary>
    public class ConsultarNfseXmlRequest : XmlDirectRequest
    {
        public ConsultarNfseXmlRequest()
        {
            MetodoSoap = "ConsultarNfseServicoPrestado";
            NomeArquivoXsd = "servico_consultar_nfse_envio.xsd";
        }
    }

    /// <summary>
    /// Request específico para CancelarNfse com XML
    /// </summary>
    public class CancelarNfseXmlRequest : XmlDirectRequest
    {
        public CancelarNfseXmlRequest()
        {
            MetodoSoap = "CancelarNfse";
            NomeArquivoXsd = "servico_cancelar_nfse_envio.xsd";
        }
    }

    /// <summary>
    /// Request específico para SubstituirNfse com XML
    /// </summary>
    public class SubstituirNfseXmlRequest : XmlDirectRequest
    {
        public SubstituirNfseXmlRequest()
        {
            MetodoSoap = "SubstituirNfse";
            NomeArquivoXsd = "servico_substituir_nfse_envio.xsd";
        }
    }
}