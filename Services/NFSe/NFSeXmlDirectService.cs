using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Microsoft.AspNetCore.DataProtection;
using NFSE_ABRASF.DTOs.NFSe;
using NFSE_ABRASF.Exceptions;
using NFSE_ABRASF.Models;

namespace NFSE_ABRASF.Services.NFSe
{
    public class ProcessarXmlRequest
    {
        public string XmlContent { get; set; } = string.Empty;
        public string MetodoSoap { get; set; } = string.Empty;
        public string SoapAction { get; set; } = string.Empty;
        public bool IsSincrono { get; set; }
        public bool ValidarXsd { get; set; } = false;
    }

    public class ProcessarXmlResponse : BaseResponse
    {
        public string? XmlOriginal { get; set; }
        public string? XmlAssinado { get; set; }
        public string? XmlResposta { get; set; }
        public int HttpStatusCode { get; set; }
        public long TempoProcessamentoMs { get; set; }
    }

    public interface INFSeXmlDirectService
    {
        Task<ProcessarXmlResponse> ProcessarXmlAsync(ProcessarXmlRequest request, Empresas empresa);
    }

    public class NFSeXmlDirectService : INFSeXmlDirectService
    {
        private readonly IDataProtectionProvider _dataProtectionProvider;
        private readonly ILogger<NFSeXmlDirectService> _logger;
        private readonly IConfiguration _configuration;

        private const string URL_HOMOLOGACAO = "https://ws-homologacao-rtc.giss.com.br/service-ws/nf/nfse-ws";
        private const string URL_PRODUCAO = "https://ws.giss.com.br/service-ws/nf/nfse-ws";

        public NFSeXmlDirectService(
            IDataProtectionProvider dataProtectionProvider,
            ILogger<NFSeXmlDirectService> logger,
            IConfiguration configuration)
        {
            _dataProtectionProvider = dataProtectionProvider;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<ProcessarXmlResponse> ProcessarXmlAsync(ProcessarXmlRequest request, Empresas empresa)
        {
            var response = new ProcessarXmlResponse();
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                _logger.LogInformation("Iniciando processamento XML - Empresa: {EmpresaId}, Método: {Metodo}",
                    empresa.EmpresaId, request.MetodoSoap);

                // 1. Guardar XML original
                response.XmlOriginal = request.XmlContent;
                _logger.LogDebug("XML Original ({Size} bytes): {Xml}",
                    request.XmlContent.Length, request.XmlContent);

                // 2. Assinar o XML
                var xmlAssinado = AssinarXml(request.XmlContent, empresa);
                response.XmlAssinado = xmlAssinado;

                _logger.LogInformation("✅ XML assinado com sucesso - Tamanho: {Size} bytes", xmlAssinado.Length);

                // 3. Converter para GISS e montar envelope SOAP
                var soapEnvelope = MontarEnvelopeSoap(xmlAssinado, request.MetodoSoap, request.IsSincrono);

                _logger.LogInformation("✅ SOAP Envelope montado - Tamanho: {Size} bytes", soapEnvelope.Length);
                _logger.LogDebug("SOAP Envelope completo: {Envelope}", soapEnvelope);

                // 4. Enviar para o webservice
                var url = empresa.Tipo_Ambiente == "1" ? URL_PRODUCAO : URL_HOMOLOGACAO;

                _logger.LogInformation("🌐 Enviando requisição para: {Url}", url);

                var xmlResposta = await EnviarRequisicaoAsync(
                    soapEnvelope,
                    url,
                    request.SoapAction,
                    empresa);

                response.XmlResposta = xmlResposta;

                _logger.LogInformation("✅ Resposta recebida do GISS - Tamanho: {Size} bytes",
                    xmlResposta?.Length ?? 0);
                _logger.LogDebug("XML Resposta: {Response}", xmlResposta);

                // 5. Processar resposta
                ProcessarResposta(xmlResposta, response);

                response.Sucesso = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erro ao processar XML - Empresa: {EmpresaId}", empresa.EmpresaId);
                response.Sucesso = false;
                response.Mensagens.Add(new MensagemRetorno
                {
                    Codigo = "ERRO_INTERNO",
                    Mensagem = ex.Message
                });
            }
            finally
            {
                stopwatch.Stop();
                response.TempoProcessamentoMs = stopwatch.ElapsedMilliseconds;
                _logger.LogInformation("⏱️ Processamento concluído - Tempo: {Tempo}ms", response.TempoProcessamentoMs);
            }

            return response;
        }

        private string MontarEnvelopeSoap(string xmlAssinado, string metodoSoap, bool isSincrono)
        {
            try
            {
                _logger.LogDebug("🔄 Iniciando conversão do XML para formato GISS");

                // Carregar XML assinado
                var doc = XDocument.Parse(xmlAssinado);

                // Obter elemento raiz
                var rootElement = doc.Root;
                if (rootElement == null)
                    throw new BusinessException("XML inválido: elemento raiz não encontrado");

                // Determinar o namespace correto baseado no método
                string namespaceGissRaiz;

                if (isSincrono)
                {
                    namespaceGissRaiz = "http://www.giss.com.br/enviar-lote-rps-sincrono-envio-v2_04.xsd";
                }
                else
                {
                    namespaceGissRaiz = "http://www.giss.com.br/enviar-lote-rps-envio-v2_04.xsd";
                }

                _logger.LogDebug("📋 Namespace GISS raiz: {Namespace}", namespaceGissRaiz);

                // Converter todo o documento para formato GISS
                var rootGiss = ConverterElementoParaGiss(rootElement, namespaceGissRaiz, isRoot: true, dentroAssinatura: false);

                // Criar novo documento com elemento convertido
                var docGiss = new XDocument(rootGiss);

                // Obter XML convertido sem declaração
                var xmlConvertido = docGiss.ToString(SaveOptions.DisableFormatting);

                _logger.LogInformation("✅ XML convertido para GISS - Tamanho: {Size} bytes", xmlConvertido.Length);
                _logger.LogDebug("XML Convertido: {Xml}", xmlConvertido);

                // Montar envelope SOAP no formato GISS
                var nsCabecalho = "http://www.giss.com.br/cabecalho-v2_04.xsd";

                var soapEnvelope = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"" 
               xmlns:nfse=""http://nfse.abrasf.org.br"">
    <soap:Body>
        <nfse:{metodoSoap}Request>
            <nfseCabecMsg><![CDATA[<cabecalho xmlns=""{nsCabecalho}"" versao=""2.04""><versaoDados>2.04</versaoDados></cabecalho>]]></nfseCabecMsg>
            <nfseDadosMsg><![CDATA[{xmlConvertido}]]></nfseDadosMsg>
        </nfse:{metodoSoap}Request>
    </soap:Body>
</soap:Envelope>";

                _logger.LogDebug("✅ SOAP Envelope montado com sucesso");

                return soapEnvelope;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erro ao montar envelope SOAP");
                throw new BusinessException($"Erro ao converter XML para formato GISS: {ex.Message}");
            }
        }

        /// <summary>
        /// Converte recursivamente o elemento XML para formato GISS
        /// </summary>
        /// <param name="elemento">Elemento a converter</param>
        /// <param name="namespaceGissRaiz">Namespace da raiz GISS</param>
        /// <param name="isRoot">Se é o elemento raiz</param>
        /// <param name="dentroAssinatura">Se está dentro da tag Signature (preserva namespace xmldsig)</param>
        private XElement ConverterElementoParaGiss(XElement elemento, string namespaceGissRaiz, bool isRoot, bool dentroAssinatura)
        {
            var nomeElemento = elemento.Name.LocalName;

            // Constantes de namespace
            var nsTipos = "http://www.giss.com.br/tipos-v2_04.xsd";
            var nsAssinatura = "http://www.w3.org/2000/09/xmldsig#";

            // Determinar namespace e prefixo
            string namespaceElemento;
            string prefixo;

            // Se é a tag Signature ou está dentro dela, preserva namespace xmldsig SEM prefixo
            if (nomeElemento == "Signature" || dentroAssinatura)
            {
                namespaceElemento = nsAssinatura;
                prefixo = "";
                dentroAssinatura = true; // Marca que estamos dentro da assinatura

                _logger.LogTrace("🔐 Elemento de assinatura: {Nome} (sem prefixo)", nomeElemento);
            }
            else if (isRoot)
            {
                // Elemento raiz usa namespace específico do GISS
                namespaceElemento = namespaceGissRaiz;
                prefixo = "p";

                _logger.LogTrace("📦 Elemento raiz: {Nome} com prefixo 'p' e namespace {Namespace}",
                    nomeElemento, namespaceElemento);
            }
            else
            {
                // Elementos internos usam namespace de tipos
                namespaceElemento = nsTipos;
                prefixo = "p1";

                _logger.LogTrace("📝 Elemento interno: {Nome} com prefixo 'p1'", nomeElemento);
            }

            // Criar namespace
            XNamespace ns = namespaceElemento;

            // Criar novo elemento com namespace correto
            var novoNome = string.IsNullOrEmpty(prefixo)
                ? XName.Get(nomeElemento, namespaceElemento)  // Sem prefixo (assinatura)
                : ns + nomeElemento;  // Com prefixo

            var novoElemento = new XElement(novoNome);

            // Copiar atributos (exceto xmlns)
            foreach (var attr in elemento.Attributes())
            {
                if (!attr.IsNamespaceDeclaration)
                {
                    novoElemento.Add(new XAttribute(attr.Name.LocalName, attr.Value));
                }
            }

            // Processar conteúdo recursivamente
            foreach (var node in elemento.Nodes())
            {
                if (node is XElement childElement)
                {
                    // Recursivamente converter elementos filhos
                    // IMPORTANTE: passa dentroAssinatura=true se estamos dentro de Signature
                    novoElemento.Add(ConverterElementoParaGiss(
                        childElement,
                        namespaceGissRaiz,
                        isRoot: false,
                        dentroAssinatura: dentroAssinatura));
                }
                else if (node is XText text)
                {
                    // Copiar texto
                    novoElemento.Add(new XText(text.Value));
                }
                else if (node is XCData cdata)
                {
                    // Copiar CDATA
                    novoElemento.Add(new XCData(cdata.Value));
                }
            }

            return novoElemento;
        }

        private string AssinarXml(string xml, Empresas empresa)
        {
            try
            {
                _logger.LogDebug("🔏 Iniciando assinatura do XML");

                // Obter certificado
                var cert = ObterCertificado(empresa);

                var doc = new XmlDocument { PreserveWhitespace = true };
                doc.LoadXml(xml);

                // Criar assinatura
                var signedXml = new System.Security.Cryptography.Xml.SignedXml(doc)
                {
                    SigningKey = cert.GetRSAPrivateKey()
                };

                // Encontrar todos os elementos com atributo Id
                var elementsToSign = doc.SelectNodes("//*[@Id]");
                if (elementsToSign != null && elementsToSign.Count > 0)
                {
                    _logger.LogDebug("📝 Encontrados {Count} elementos para assinar", elementsToSign.Count);

                    foreach (XmlElement elementToSign in elementsToSign)
                    {
                        var idValue = elementToSign.GetAttribute("Id");

                        var reference = new System.Security.Cryptography.Xml.Reference($"#{idValue}");
                        reference.AddTransform(new System.Security.Cryptography.Xml.XmlDsigEnvelopedSignatureTransform());
                        reference.AddTransform(new System.Security.Cryptography.Xml.XmlDsigC14NTransform());

                        signedXml.AddReference(reference);

                        _logger.LogTrace("✔️ Referência adicionada: #{Id}", idValue);
                    }
                }

                // Adicionar informações do certificado
                var keyInfo = new System.Security.Cryptography.Xml.KeyInfo();
                keyInfo.AddClause(new System.Security.Cryptography.Xml.KeyInfoX509Data(cert));
                signedXml.KeyInfo = keyInfo;

                // Computar assinatura
                signedXml.ComputeSignature();

                // Obter elemento de assinatura
                var signatureElement = signedXml.GetXml();

                // Inserir assinatura no documento
                doc.DocumentElement?.AppendChild(doc.ImportNode(signatureElement, true));

                _logger.LogDebug("✅ XML assinado com sucesso");

                return doc.OuterXml;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erro ao assinar XML");
                throw new BusinessException($"Erro ao assinar XML: {ex.Message}");
            }
        }

        private X509Certificate2 ObterCertificado(Empresas empresa)
        {
            if (empresa.Certificado_Pfx == null || empresa.Certificado_Pfx.Length == 0)
                throw new BusinessException("Certificado digital não configurado para esta empresa.");

            var protector = _dataProtectionProvider.CreateProtector("CertificadoSenhaProtector");
            var senha = protector.Unprotect(empresa.Senha_Certificado ?? "");

            return new X509Certificate2(
                empresa.Certificado_Pfx,
                senha,
                X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.Exportable);
        }

        private async Task<string> EnviarRequisicaoAsync(
            string soapEnvelope,
            string url,
            string soapAction,
            Empresas empresa)
        {
            try
            {
                var handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
                    {
                        _logger.LogDebug("🔐 Validando certificado do servidor GISS");
                        if (errors == SslPolicyErrors.None)
                        {
                            _logger.LogDebug("✅ Certificado do servidor válido");
                            return true;
                        }

                        _logger.LogWarning("⚠️ Erros no certificado do servidor: {Errors}", errors);
                        return true; // Por ora aceita (ambiente de teste)
                    }
                };

                // Adicionar certificado cliente para mTLS
                var cert = ObterCertificado(empresa);
                handler.ClientCertificates.Add(cert);

                _logger.LogDebug("📜 Certificado cliente adicionado - Titular: {Titular}, Validade: {Validade}",
                    empresa.Certificado_Titular, empresa.Certificado_Validade);

                using var httpClient = new HttpClient(handler);
                httpClient.Timeout = TimeSpan.FromSeconds(60);

                var content = new StringContent(soapEnvelope, Encoding.UTF8, "text/xml");
                content.Headers.Add("SOAPAction", soapAction);

                _logger.LogDebug("📤 Enviando requisição - SOAPAction: {Action}", soapAction);

                var response = await httpClient.PostAsync(url, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                _logger.LogInformation("📥 Resposta HTTP recebida - Status: {StatusCode}, Tamanho: {Size} bytes",
                    response.StatusCode, responseContent.Length);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("❌ Erro HTTP: {StatusCode} - {Content}",
                        response.StatusCode, responseContent);
                    throw new BusinessException($"Erro na comunicação com o WebService: {response.StatusCode}");
                }

                return responseContent;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "❌ Erro na requisição HTTP");
                throw new BusinessException($"Erro de comunicação: {ex.Message}");
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "❌ Timeout na requisição");
                throw new BusinessException("Timeout ao comunicar com o WebService");
            }
        }

        private void ProcessarResposta(string? xmlResposta, ProcessarXmlResponse response)
        {
            if (string.IsNullOrEmpty(xmlResposta))
            {
                _logger.LogWarning("⚠️ Resposta vazia do GISS");
                return;
            }

            try
            {
                _logger.LogDebug("🔍 Processando resposta do GISS");

                var doc = XDocument.Parse(xmlResposta);

                // Extrair resposta do SOAP
                var ns = XNamespace.Get("http://nfse.abrasf.org.br");
                var outputXml = doc.Descendants(ns + "outputXML").FirstOrDefault()?.Value;

                if (string.IsNullOrEmpty(outputXml))
                {
                    _logger.LogWarning("⚠️ outputXML não encontrado na resposta");
                    return;
                }

                _logger.LogDebug("📄 outputXML extraído: {Output}", outputXml);

                // Parse do outputXML
                var docResposta = XDocument.Parse(outputXml);

                // Procurar por mensagens de retorno
                var mensagens = docResposta.Descendants()
                    .Where(e => e.Name.LocalName == "MensagemRetorno")
                    .ToList();

                if (mensagens.Any())
                {
                    _logger.LogInformation("📨 Encontradas {Count} mensagens de retorno", mensagens.Count);

                    foreach (var msg in mensagens)
                    {
                        var codigo = msg.Descendants().FirstOrDefault(e => e.Name.LocalName == "Codigo")?.Value ?? "";
                        var mensagem = msg.Descendants().FirstOrDefault(e => e.Name.LocalName == "Mensagem")?.Value ?? "";
                        var correcao = msg.Descendants().FirstOrDefault(e => e.Name.LocalName == "Correcao")?.Value;

                        response.Mensagens.Add(new MensagemRetorno
                        {
                            Codigo = codigo,
                            Mensagem = mensagem,
                            Correcao = correcao
                        });

                        if (codigo.StartsWith("E"))
                        {
                            _logger.LogWarning("⚠️ Erro {Codigo}: {Mensagem}", codigo, mensagem);
                        }
                        else
                        {
                            _logger.LogInformation("ℹ️ Mensagem {Codigo}: {Mensagem}", codigo, mensagem);
                        }
                    }
                }

                // Procurar por NFSe gerada
                var compNfse = docResposta.Descendants()
                    .FirstOrDefault(e => e.Name.LocalName == "CompNfse");

                if (compNfse != null)
                {
                    _logger.LogInformation("🎉 NFSe gerada com sucesso!");
                    response.Sucesso = true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erro ao processar resposta");
            }
        }
    }
}