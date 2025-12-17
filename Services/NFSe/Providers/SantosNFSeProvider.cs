using Microsoft.AspNetCore.DataProtection;
using NFSE_ABRASF.Services.NFSe.Base;

namespace NFSE_ABRASF.Services.NFSe.Providers
{
    /// <summary>
    /// Provedor NFSe para Santos/SP - Sistema GISS
    /// Versão ABRASF: 2.04
    /// </summary>
    public class SantosNFSeProvider : NFSeProviderBase
    {
        public override string CodigoMunicipio => "3548500";
        public override string NomeMunicipio => "Santos";
        public override string VersaoAbrasf => "2.04";
        public override string NomeProvedor => "GISS";
        public override string UrlHomologacao => "https://ws-homologacao-rtc.giss.com.br/service-ws/nf/nfse-ws";
        public override string UrlProducao => "https://ws.giss.com.br/service-ws/nf/nfse-ws";

        /// <summary>
        /// Namespace específico do GISS para Santos
        /// </summary>
        protected override string NamespaceNfse => "http://nfse.abrasf.org.br";

        public SantosNFSeProvider(
            ILogger<SantosNFSeProvider> logger,
            IDataProtectionProvider dataProtectionProvider,
            IWebHostEnvironment environment)
            : base(logger, dataProtectionProvider, environment)
        {
        }

        /// <summary>
        /// Monta o envelope SOAP específico do GISS
        /// </summary>
        protected override string MontarEnvelopeSoap(string xmlConteudo, string metodo)
        {
            // O GISS usa um envelope SOAP 1.1 padrão
            return $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"" 
               xmlns:nfse=""http://nfse.abrasf.org.br"">
    <soap:Body>
        <nfse:{metodo}Request>
            <nfseCabecMsg>
                <![CDATA[<?xml version=""1.0"" encoding=""UTF-8""?>
                <cabecalho xmlns=""http://www.abrasf.org.br/nfse.xsd"" versao=""{VersaoAbrasf}"">
                    <versaoDados>{VersaoAbrasf}</versaoDados>
                </cabecalho>]]>
            </nfseCabecMsg>
            <nfseDadosMsg>
                <![CDATA[{xmlConteudo}]]>
            </nfseDadosMsg>
        </nfse:{metodo}Request>
    </soap:Body>
</soap:Envelope>";
        }

        /// <summary>
        /// Extrai a resposta do envelope SOAP do GISS
        /// </summary>
        protected override string ExtrairRespostaSoap(string xmlResposta, string metodo)
        {
            try
            {
                var doc = System.Xml.Linq.XDocument.Parse(xmlResposta);

                // O GISS retorna o XML dentro de um CDATA na tag outputXML ou return
                var outputElement = doc.Descendants()
                    .FirstOrDefault(e => e.Name.LocalName == "outputXML" ||
                                         e.Name.LocalName == "return" ||
                                         e.Name.LocalName == $"{metodo}Response");

                if (outputElement != null)
                {
                    var conteudo = outputElement.Value;

                    // Se o conteúdo estiver em CDATA, extrair
                    if (conteudo.Contains("<?xml"))
                    {
                        return conteudo;
                    }

                    // Tentar extrair do primeiro descendente que contenha XML
                    foreach (var node in outputElement.Nodes())
                    {
                        if (node is System.Xml.Linq.XCData cdata)
                        {
                            return cdata.Value;
                        }
                    }

                    return outputElement.ToString();
                }

                // Se não encontrar, retornar o XML completo sem o envelope SOAP
                var body = doc.Descendants()
                    .FirstOrDefault(e => e.Name.LocalName == "Body");

                return body?.FirstNode?.ToString() ?? xmlResposta;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao extrair resposta SOAP");
                return xmlResposta;
            }
        }

        /// <summary>
        /// Retorna o SOAPAction específico do GISS
        /// </summary>
        protected override string ObterSoapAction(string metodo)
        {
            // O GISS utiliza o padrão de SOAPAction com o namespace + método
            return metodo switch
            {
                "GerarNfse" => "http://nfse.abrasf.org.br/GerarNfse",
                "RecepcionarLoteRps" => "http://nfse.abrasf.org.br/RecepcionarLoteRps",
                "RecepcionarLoteRpsSincrono" => "http://nfse.abrasf.org.br/RecepcionarLoteRpsSincrono",
                "ConsultarSituacaoLoteRps" => "http://nfse.abrasf.org.br/ConsultarSituacaoLoteRps",
                "ConsultarLoteRps" => "http://nfse.abrasf.org.br/ConsultarLoteRps",
                "ConsultarNfsePorRps" => "http://nfse.abrasf.org.br/ConsultarNfsePorRps",
                "ConsultarNfseServicoPrestado" => "http://nfse.abrasf.org.br/ConsultarNfseServicoPrestado",
                "ConsultarNfseServicoTomado" => "http://nfse.abrasf.org.br/ConsultarNfseServicoTomado",
                "CancelarNfse" => "http://nfse.abrasf.org.br/CancelarNfse",
                "SubstituirNfse" => "http://nfse.abrasf.org.br/SubstituirNfse",
                _ => $"http://nfse.abrasf.org.br/{metodo}"
            };
        }
    }
}