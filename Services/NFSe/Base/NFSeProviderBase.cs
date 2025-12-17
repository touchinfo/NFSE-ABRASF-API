using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Xml.Serialization;
using Microsoft.AspNetCore.DataProtection;
using NFSE_ABRASF.DTOs.NFSe;
using NFSE_ABRASF.Exceptions;
using NFSE_ABRASF.Models;
using NFSE_ABRASF.Services.NFSe.Interfaces;

namespace NFSE_ABRASF.Services.NFSe.Base
{
    /// <summary>
    /// Classe base abstrata que implementa a lógica comum do ABRASF
    /// Provedores específicos herdam e sobrescrevem conforme necessário
    /// </summary>
    public abstract class NFSeProviderBase : INFSeProvider
    {
        protected readonly ILogger _logger;
        protected readonly IDataProtectionProvider _dataProtectionProvider;
        protected readonly IWebHostEnvironment _environment;

        public abstract string CodigoMunicipio { get; }
        public abstract string NomeMunicipio { get; }
        public abstract string VersaoAbrasf { get; }
        public abstract string NomeProvedor { get; }
        public abstract string UrlHomologacao { get; }
        public abstract string UrlProducao { get; }

        /// <summary>
        /// Namespace principal do XML (varia por versão ABRASF)
        /// </summary>
        protected virtual string NamespaceNfse => "http://www.abrasf.org.br/nfse.xsd";

        /// <summary>
        /// Namespace dos tipos do XML
        /// </summary>
        protected virtual string NamespaceTipos => "http://www.abrasf.org.br/nfse.xsd";

        /// <summary>
        /// Caminho base para os schemas XSD
        /// </summary>
        protected virtual string CaminhoSchemas => Path.Combine(_environment.ContentRootPath, "Schemas", VersaoAbrasf);

        protected NFSeProviderBase(
            ILogger logger,
            IDataProtectionProvider dataProtectionProvider,
            IWebHostEnvironment environment)
        {
            _logger = logger;
            _dataProtectionProvider = dataProtectionProvider;
            _environment = environment;
        }

        #region Métodos Abstratos - Implementados pelos provedores específicos

        /// <summary>
        /// Monta o envelope SOAP específico do provedor
        /// </summary>
        protected abstract string MontarEnvelopeSoap(string xmlConteudo, string metodo);

        /// <summary>
        /// Extrai a resposta do envelope SOAP
        /// </summary>
        protected abstract string ExtrairRespostaSoap(string xmlResposta, string metodo);

        /// <summary>
        /// Retorna o SOAPAction para o método
        /// </summary>
        protected abstract string ObterSoapAction(string metodo);

        #endregion

        #region Implementação INFSeProvider

        public virtual async Task<GerarNfseResponse> GerarNfseAsync(GerarNfseRequest request, Empresas empresa, bool homologacao)
        {
            var response = new GerarNfseResponse();

            try
            {
                // Montar XML
                var xml = MontarXmlGerarNfse(request, empresa);

                // Validar contra XSD
                ValidarXml(xml, "servico_enviar_lote_rps_envio.xsd");

                // Assinar XML
                var xmlAssinado = AssinarXml(xml, empresa);
                response.XmlEnviado = xmlAssinado;

                // Enviar para o WebService
                var url = homologacao ? UrlHomologacao : UrlProducao;
                var xmlRetorno = await EnviarRequisicaoAsync(xmlAssinado, url, "GerarNfse");
                response.XmlRetorno = xmlRetorno;

                // Processar resposta
                ProcessarRespostaGerarNfse(xmlRetorno, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao gerar NFSe para empresa {EmpresaId}", empresa.EmpresaId);
                response.Sucesso = false;
                response.Mensagens.Add(new MensagemRetorno
                {
                    Codigo = "ERRO_INTERNO",
                    Mensagem = ex.Message
                });
            }

            return response;
        }

        public virtual async Task<EnviarLoteRpsResponse> EnviarLoteRpsAsync(EnviarLoteRpsRequest request, Empresas empresa, bool homologacao)
        {
            var response = new EnviarLoteRpsResponse();

            try
            {
                var xml = MontarXmlEnviarLoteRps(request, empresa);
                ValidarXml(xml, "servico_enviar_lote_rps_envio.xsd");

                var xmlAssinado = AssinarXml(xml, empresa);
                response.XmlEnviado = xmlAssinado;

                var url = homologacao ? UrlHomologacao : UrlProducao;
                var xmlRetorno = await EnviarRequisicaoAsync(xmlAssinado, url, "RecepcionarLoteRps");
                response.XmlRetorno = xmlRetorno;

                ProcessarRespostaEnviarLote(xmlRetorno, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao enviar lote RPS para empresa {EmpresaId}", empresa.EmpresaId);
                response.Sucesso = false;
                response.Mensagens.Add(new MensagemRetorno
                {
                    Codigo = "ERRO_INTERNO",
                    Mensagem = ex.Message
                });
            }

            return response;
        }

        public virtual async Task<EnviarLoteRpsSincronoResponse> EnviarLoteRpsSincronoAsync(EnviarLoteRpsRequest request, Empresas empresa, bool homologacao)
        {
            var response = new EnviarLoteRpsSincronoResponse();

            try
            {
                var xml = MontarXmlEnviarLoteRps(request, empresa, sincrono: true);
                ValidarXml(xml, "servico_enviar_lote_rps_envio.xsd");

                var xmlAssinado = AssinarXml(xml, empresa);
                response.XmlEnviado = xmlAssinado;

                var url = homologacao ? UrlHomologacao : UrlProducao;
                var xmlRetorno = await EnviarRequisicaoAsync(xmlAssinado, url, "RecepcionarLoteRpsSincrono");
                response.XmlRetorno = xmlRetorno;

                ProcessarRespostaEnviarLoteSincrono(xmlRetorno, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao enviar lote RPS síncrono para empresa {EmpresaId}", empresa.EmpresaId);
                response.Sucesso = false;
                response.Mensagens.Add(new MensagemRetorno
                {
                    Codigo = "ERRO_INTERNO",
                    Mensagem = ex.Message
                });
            }

            return response;
        }

        public virtual async Task<ConsultarSituacaoLoteRpsResponse> ConsultarSituacaoLoteRpsAsync(string protocolo, Empresas empresa, bool homologacao)
        {
            var response = new ConsultarSituacaoLoteRpsResponse();

            try
            {
                var xml = MontarXmlConsultarSituacaoLote(protocolo, empresa);
                var xmlAssinado = AssinarXml(xml, empresa);
                response.XmlEnviado = xmlAssinado;

                var url = homologacao ? UrlHomologacao : UrlProducao;
                var xmlRetorno = await EnviarRequisicaoAsync(xmlAssinado, url, "ConsultarSituacaoLoteRps");
                response.XmlRetorno = xmlRetorno;

                ProcessarRespostaConsultarSituacao(xmlRetorno, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao consultar situação do lote para empresa {EmpresaId}", empresa.EmpresaId);
                response.Sucesso = false;
                response.Mensagens.Add(new MensagemRetorno
                {
                    Codigo = "ERRO_INTERNO",
                    Mensagem = ex.Message
                });
            }

            return response;
        }

        public virtual async Task<ConsultarLoteRpsResponse> ConsultarLoteRpsAsync(string protocolo, Empresas empresa, bool homologacao)
        {
            var response = new ConsultarLoteRpsResponse();

            try
            {
                var xml = MontarXmlConsultarLoteRps(protocolo, empresa);
                var xmlAssinado = AssinarXml(xml, empresa);
                response.XmlEnviado = xmlAssinado;

                var url = homologacao ? UrlHomologacao : UrlProducao;
                var xmlRetorno = await EnviarRequisicaoAsync(xmlAssinado, url, "ConsultarLoteRps");
                response.XmlRetorno = xmlRetorno;

                ProcessarRespostaConsultarLote(xmlRetorno, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao consultar lote RPS para empresa {EmpresaId}", empresa.EmpresaId);
                response.Sucesso = false;
                response.Mensagens.Add(new MensagemRetorno
                {
                    Codigo = "ERRO_INTERNO",
                    Mensagem = ex.Message
                });
            }

            return response;
        }

        public virtual async Task<ConsultarNfsePorRpsResponse> ConsultarNfsePorRpsAsync(ConsultarNfsePorRpsRequest request, Empresas empresa, bool homologacao)
        {
            var response = new ConsultarNfsePorRpsResponse();

            try
            {
                var xml = MontarXmlConsultarNfsePorRps(request, empresa);
                var xmlAssinado = AssinarXml(xml, empresa);
                response.XmlEnviado = xmlAssinado;

                var url = homologacao ? UrlHomologacao : UrlProducao;
                var xmlRetorno = await EnviarRequisicaoAsync(xmlAssinado, url, "ConsultarNfsePorRps");
                response.XmlRetorno = xmlRetorno;

                ProcessarRespostaConsultarNfsePorRps(xmlRetorno, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao consultar NFSe por RPS para empresa {EmpresaId}", empresa.EmpresaId);
                response.Sucesso = false;
                response.Mensagens.Add(new MensagemRetorno
                {
                    Codigo = "ERRO_INTERNO",
                    Mensagem = ex.Message
                });
            }

            return response;
        }

        public virtual async Task<ConsultarNfseResponse> ConsultarNfseAsync(ConsultarNfseRequest request, Empresas empresa, bool homologacao)
        {
            var response = new ConsultarNfseResponse();

            try
            {
                var xml = MontarXmlConsultarNfse(request, empresa);
                var xmlAssinado = AssinarXml(xml, empresa);
                response.XmlEnviado = xmlAssinado;

                var url = homologacao ? UrlHomologacao : UrlProducao;
                var xmlRetorno = await EnviarRequisicaoAsync(xmlAssinado, url, "ConsultarNfseServicoPrestado");
                response.XmlRetorno = xmlRetorno;

                ProcessarRespostaConsultarNfse(xmlRetorno, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao consultar NFSe para empresa {EmpresaId}", empresa.EmpresaId);
                response.Sucesso = false;
                response.Mensagens.Add(new MensagemRetorno
                {
                    Codigo = "ERRO_INTERNO",
                    Mensagem = ex.Message
                });
            }

            return response;
        }

        public virtual async Task<CancelarNfseResponse> CancelarNfseAsync(CancelarNfseRequest request, Empresas empresa, bool homologacao)
        {
            var response = new CancelarNfseResponse();

            try
            {
                var xml = MontarXmlCancelarNfse(request, empresa);
                var xmlAssinado = AssinarXml(xml, empresa);
                response.XmlEnviado = xmlAssinado;

                var url = homologacao ? UrlHomologacao : UrlProducao;
                var xmlRetorno = await EnviarRequisicaoAsync(xmlAssinado, url, "CancelarNfse");
                response.XmlRetorno = xmlRetorno;

                ProcessarRespostaCancelarNfse(xmlRetorno, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao cancelar NFSe para empresa {EmpresaId}", empresa.EmpresaId);
                response.Sucesso = false;
                response.Mensagens.Add(new MensagemRetorno
                {
                    Codigo = "ERRO_INTERNO",
                    Mensagem = ex.Message
                });
            }

            return response;
        }

        public virtual async Task<SubstituirNfseResponse> SubstituirNfseAsync(SubstituirNfseRequest request, Empresas empresa, bool homologacao)
        {
            var response = new SubstituirNfseResponse();

            try
            {
                var xml = MontarXmlSubstituirNfse(request, empresa);
                var xmlAssinado = AssinarXml(xml, empresa);
                response.XmlEnviado = xmlAssinado;

                var url = homologacao ? UrlHomologacao : UrlProducao;
                var xmlRetorno = await EnviarRequisicaoAsync(xmlAssinado, url, "SubstituirNfse");
                response.XmlRetorno = xmlRetorno;

                ProcessarRespostaSubstituirNfse(xmlRetorno, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao substituir NFSe para empresa {EmpresaId}", empresa.EmpresaId);
                response.Sucesso = false;
                response.Mensagens.Add(new MensagemRetorno
                {
                    Codigo = "ERRO_INTERNO",
                    Mensagem = ex.Message
                });
            }

            return response;
        }

        #endregion

        #region Métodos de Montagem XML - Virtuais para customização

        protected virtual string MontarXmlGerarNfse(GerarNfseRequest request, Empresas empresa)
        {
            var ns = XNamespace.Get(NamespaceNfse);

            var xml = new XDocument(
                new XDeclaration("1.0", "UTF-8", null),
                new XElement(ns + "GerarNfseEnvio",
                    MontarXmlRps(request.Rps, empresa, ns)
                )
            );

            return xml.ToString(SaveOptions.DisableFormatting);
        }

        protected virtual string MontarXmlEnviarLoteRps(EnviarLoteRpsRequest request, Empresas empresa, bool sincrono = false)
        {
            var ns = XNamespace.Get(NamespaceNfse);
            var tagEnvio = sincrono ? "EnviarLoteRpsSincronoEnvio" : "EnviarLoteRpsEnvio";

            var listaRps = new XElement(ns + "ListaRps");
            foreach (var rps in request.ListaRps)
            {
                listaRps.Add(MontarXmlRps(rps, empresa, ns));
            }

            var xml = new XDocument(
                new XDeclaration("1.0", "UTF-8", null),
                new XElement(ns + tagEnvio,
                    new XElement(ns + "LoteRps",
                        new XAttribute("Id", $"lote{request.NumeroLote}"),
                        new XAttribute("versao", VersaoAbrasf),
                        new XElement(ns + "NumeroLote", request.NumeroLote),
                        new XElement(ns + "CpfCnpj",
                            new XElement(ns + "Cnpj", empresa.Cnpj?.Replace(".", "").Replace("/", "").Replace("-", ""))
                        ),
                        new XElement(ns + "InscricaoMunicipal", empresa.Inscricao_Municipal),
                        new XElement(ns + "QuantidadeRps", request.QuantidadeRps),
                        listaRps
                    )
                )
            );

            return xml.ToString(SaveOptions.DisableFormatting);
        }

        protected virtual XElement MontarXmlRps(Rps rps, Empresas empresa, XNamespace ns)
        {
            var infRps = rps.InfDeclaracaoPrestacaoServico;
            var idRps = $"rps{infRps.Identificacao.Serie}{infRps.Identificacao.Numero}";

            var xmlRps = new XElement(ns + "Rps",
                new XElement(ns + "InfDeclaracaoPrestacaoServico",
                    new XAttribute("Id", idRps),
                    new XElement(ns + "Rps",
                        new XElement(ns + "IdentificacaoRps",
                            new XElement(ns + "Numero", infRps.Identificacao.Numero),
                            new XElement(ns + "Serie", infRps.Identificacao.Serie),
                            new XElement(ns + "Tipo", infRps.Identificacao.Tipo)
                        ),
                        new XElement(ns + "DataEmissao", infRps.DataEmissao.ToString("yyyy-MM-dd")),
                        new XElement(ns + "Status", 1) // 1-Normal, 2-Cancelado
                    ),
                    new XElement(ns + "Competencia", (infRps.Competencia ?? infRps.DataEmissao).ToString("yyyy-MM-dd")),
                    MontarXmlServico(infRps.Servico, ns),
                    MontarXmlPrestador(empresa, ns),
                    MontarXmlTomador(infRps.Tomador, ns),
                    new XElement(ns + "OptanteSimplesNacional", infRps.OptanteSimplesNacional),
                    new XElement(ns + "IncentivoFiscal", infRps.IncentivadorCultural)
                )
            );

            // Adicionar intermediário se existir
            if (infRps.Intermediario != null)
            {
                xmlRps.Element(ns + "InfDeclaracaoPrestacaoServico")?.Add(
                    MontarXmlIntermediario(infRps.Intermediario, ns)
                );
            }

            // Adicionar construção civil se existir
            if (infRps.ConstrucaoCivil != null)
            {
                xmlRps.Element(ns + "InfDeclaracaoPrestacaoServico")?.Add(
                    MontarXmlConstrucaoCivil(infRps.ConstrucaoCivil, ns)
                );
            }

            return xmlRps;
        }

        protected virtual XElement MontarXmlServico(DadosServico servico, XNamespace ns)
        {
            var valores = servico.Valores;

            // Montar XML de valores com campos obrigatórios do GISS
            var xmlValores = new XElement(ns + "Valores",
                new XElement(ns + "ValorServicos", valores.ValorServicos.ToString("F2").Replace(",", ".")),
                valores.ValorDeducoes.HasValue ? new XElement(ns + "ValorDeducoes", valores.ValorDeducoes.Value.ToString("F2").Replace(",", ".")) : null,
                valores.ValorPis.HasValue ? new XElement(ns + "ValorPis", valores.ValorPis.Value.ToString("F2").Replace(",", ".")) : null,
                valores.ValorCofins.HasValue ? new XElement(ns + "ValorCofins", valores.ValorCofins.Value.ToString("F2").Replace(",", ".")) : null,
                valores.ValorInss.HasValue ? new XElement(ns + "ValorInss", valores.ValorInss.Value.ToString("F2").Replace(",", ".")) : null,
                valores.ValorIr.HasValue ? new XElement(ns + "ValorIr", valores.ValorIr.Value.ToString("F2").Replace(",", ".")) : null,
                valores.ValorCsll.HasValue ? new XElement(ns + "ValorCsll", valores.ValorCsll.Value.ToString("F2").Replace(",", ".")) : null,
                valores.OutrasRetencoes.HasValue ? new XElement(ns + "OutrasRetencoes", valores.OutrasRetencoes.Value.ToString("F2").Replace(",", ".")) : null,
                valores.ValTotTributos.HasValue ? new XElement(ns + "ValTotTributos", valores.ValTotTributos.Value.ToString("F2").Replace(",", ".")) : null,
                valores.ValorIss.HasValue ? new XElement(ns + "ValorIss", valores.ValorIss.Value.ToString("F2").Replace(",", ".")) : null,
                valores.Aliquota.HasValue ? new XElement(ns + "Aliquota", valores.Aliquota.Value.ToString("F4").Replace(",", ".")) : null,
                valores.DescontoIncondicionado.HasValue ? new XElement(ns + "DescontoIncondicionado", valores.DescontoIncondicionado.Value.ToString("F2").Replace(",", ".")) : null,
                valores.DescontoCondicionado.HasValue ? new XElement(ns + "DescontoCondicionado", valores.DescontoCondicionado.Value.ToString("F2").Replace(",", ".")) : null,
                // Tributos federais e totais aproximados - OBRIGATÓRIO GISS
                MontarXmlTrib(valores.Trib, ns),
                // IBS/CBS - OBRIGATÓRIO GISS
                MontarXmlIBSCBS(valores.IBSCBS, servico.CodigoMunicipio, ns)
            );

            var xmlServico = new XElement(ns + "Servico",
                xmlValores,
                valores.IssRetido.HasValue ? new XElement(ns + "IssRetido", valores.IssRetido.Value) : null,
                valores.ResponsavelRetencao.HasValue ? new XElement(ns + "ResponsavelRetencao", valores.ResponsavelRetencao.Value) : null,
                new XElement(ns + "ItemListaServico", servico.ItemListaServico),
                !string.IsNullOrEmpty(servico.CodigoCnae) ? new XElement(ns + "CodigoCnae", servico.CodigoCnae) : null,
                !string.IsNullOrEmpty(servico.CodigoTributacaoMunicipio) ? new XElement(ns + "CodigoTributacaoMunicipio", servico.CodigoTributacaoMunicipio) : null,
                // CodigoNbs - OBRIGATÓRIO GISS
                new XElement(ns + "CodigoNbs", servico.CodigoNbs),
                new XElement(ns + "Discriminacao", servico.Discriminacao),
                new XElement(ns + "CodigoMunicipio", servico.CodigoMunicipio),
                !string.IsNullOrEmpty(servico.CodigoPais) ? new XElement(ns + "CodigoPais", servico.CodigoPais) : null,
                servico.ExigibilidadeISS.HasValue ? new XElement(ns + "ExigibilidadeISS", servico.ExigibilidadeISS.Value) : null,
                !string.IsNullOrEmpty(servico.IdentifNaoExigibilidade) ? new XElement(ns + "IdentifNaoExigibilidade", servico.IdentifNaoExigibilidade) : null,
                !string.IsNullOrEmpty(servico.MunicipioIncidencia) ? new XElement(ns + "MunicipioIncidencia", servico.MunicipioIncidencia) : null,
                !string.IsNullOrEmpty(servico.NumeroProcesso) ? new XElement(ns + "NumeroProcesso", servico.NumeroProcesso) : null
            );

            // Comércio exterior se existir
            if (servico.ComExt != null)
            {
                xmlServico.Add(MontarXmlComercioExterior(servico.ComExt, ns));
            }

            return xmlServico;
        }

        /// <summary>
        /// Monta XML dos tributos federais e totais aproximados
        /// </summary>
        protected virtual XElement MontarXmlTrib(InfoTributacao trib, XNamespace ns)
        {
            var xmlTrib = new XElement(ns + "trib");

            // Tributos federais (PIS/COFINS) - opcional
            if (trib.TribFed?.PisCofins != null)
            {
                var piscofins = trib.TribFed.PisCofins;
                xmlTrib.Add(new XElement(ns + "tribFed",
                    new XElement(ns + "piscofins",
                        new XElement(ns + "CST", piscofins.CST),
                        piscofins.VBCPisCofins.HasValue ? new XElement(ns + "vBCPisCofins", piscofins.VBCPisCofins.Value.ToString("F2").Replace(",", ".")) : null,
                        piscofins.PAliqPis.HasValue ? new XElement(ns + "pAliqPis", piscofins.PAliqPis.Value.ToString("F2").Replace(",", ".")) : null,
                        piscofins.PAliqCofins.HasValue ? new XElement(ns + "pAliqCofins", piscofins.PAliqCofins.Value.ToString("F2").Replace(",", ".")) : null,
                        piscofins.VPis.HasValue ? new XElement(ns + "vPis", piscofins.VPis.Value.ToString("F2").Replace(",", ".")) : null,
                        piscofins.VCofins.HasValue ? new XElement(ns + "vCofins", piscofins.VCofins.Value.ToString("F2").Replace(",", ".")) : null,
                        !string.IsNullOrEmpty(piscofins.TpRetPisCofins) ? new XElement(ns + "tpRetPisCofins", piscofins.TpRetPisCofins) : null
                    )
                ));
            }

            // Total tributos - obrigatório
            if (trib.TotTrib.UsaPercentualSeparado && trib.TotTrib.PTotTrib != null)
            {
                xmlTrib.Add(new XElement(ns + "totTrib",
                    new XElement(ns + "pTotTrib",
                        new XElement(ns + "pTotTribFed", trib.TotTrib.PTotTrib.PTotTribFed.ToString("F2").Replace(",", ".")),
                        new XElement(ns + "pTotTribEst", trib.TotTrib.PTotTrib.PTotTribEst.ToString("F2").Replace(",", ".")),
                        new XElement(ns + "pTotTribMun", trib.TotTrib.PTotTrib.PTotTribMun.ToString("F2").Replace(",", "."))
                    )
                ));
            }
            else
            {
                xmlTrib.Add(new XElement(ns + "totTrib",
                    new XElement(ns + "pTotTribSN", (trib.TotTrib.PTotTribSN ?? 0).ToString("F2").Replace(",", "."))
                ));
            }

            return xmlTrib;
        }

        /// <summary>
        /// Monta XML das informações IBS/CBS (Reforma Tributária)
        /// </summary>
        protected virtual XElement MontarXmlIBSCBS(InfoIBSCBS ibscbs, string codigoMunicipio, XNamespace ns)
        {
            var xmlIBSCBS = new XElement(ns + "IBSCBS",
                new XElement(ns + "finNFSe", ibscbs.FinNFSe),
                new XElement(ns + "indFinal", ibscbs.IndFinal),
                new XElement(ns + "cIndOp", ibscbs.CIndOp)
            );

            if (!string.IsNullOrEmpty(ibscbs.TpOper))
                xmlIBSCBS.Add(new XElement(ns + "tpOper", ibscbs.TpOper));

            // NFSes referenciadas
            if (ibscbs.RefNFSe != null && ibscbs.RefNFSe.Count > 0)
            {
                var gRefNFSe = new XElement(ns + "gRefNFSe");
                foreach (var refNfse in ibscbs.RefNFSe)
                {
                    gRefNFSe.Add(new XElement(ns + "refNFSe", refNfse));
                }
                xmlIBSCBS.Add(gRefNFSe);
            }

            if (!string.IsNullOrEmpty(ibscbs.TpEnteGov))
                xmlIBSCBS.Add(new XElement(ns + "tpEnteGov", ibscbs.TpEnteGov));

            xmlIBSCBS.Add(new XElement(ns + "indDest", ibscbs.IndDest));

            // Valores IBS/CBS
            var xmlValores = new XElement(ns + "valores",
                new XElement(ns + "trib",
                    new XElement(ns + "gIBSCBS",
                        new XElement(ns + "CST", ibscbs.Valores.Trib.GIBSCBS.CST),
                        new XElement(ns + "cClassTrib", ibscbs.Valores.Trib.GIBSCBS.CClassTrib)
                    )
                ),
                new XElement(ns + "cLocalidadeIncid", !string.IsNullOrEmpty(ibscbs.Valores.CLocalidadeIncid) ? ibscbs.Valores.CLocalidadeIncid : codigoMunicipio),
                new XElement(ns + "pRedutor", ibscbs.Valores.PRedutor.ToString("F2").Replace(",", "."))
            );

            if (ibscbs.Valores.VBC.HasValue)
                xmlValores.Add(new XElement(ns + "vBC", ibscbs.Valores.VBC.Value.ToString("F2").Replace(",", ".")));

            xmlIBSCBS.Add(xmlValores);

            return xmlIBSCBS;
        }

        /// <summary>
        /// Monta XML de comércio exterior
        /// </summary>
        protected virtual XElement MontarXmlComercioExterior(ComercioExterior comExt, XNamespace ns)
        {
            var xml = new XElement(ns + "comExt",
                new XElement(ns + "mdPrestacao", comExt.MdPrestacao),
                new XElement(ns + "vincPrest", comExt.VincPrest),
                new XElement(ns + "tpMoeda", comExt.TpMoeda),
                new XElement(ns + "vServMoeda", comExt.VServMoeda.ToString("F2").Replace(",", ".")),
                new XElement(ns + "mecAFComexP", comExt.MecAFComexP),
                new XElement(ns + "mecAFComexT", comExt.MecAFComexT),
                new XElement(ns + "movTempBens", comExt.MovTempBens)
            );

            if (!string.IsNullOrEmpty(comExt.NDI))
                xml.Add(new XElement(ns + "nDI", comExt.NDI));

            if (!string.IsNullOrEmpty(comExt.NRE))
                xml.Add(new XElement(ns + "nRE", comExt.NRE));

            xml.Add(new XElement(ns + "mdic", comExt.Mdic));

            return xml;
        }

        protected virtual XElement MontarXmlPrestador(Empresas empresa, XNamespace ns)
        {
            return new XElement(ns + "Prestador",
                new XElement(ns + "CpfCnpj",
                    new XElement(ns + "Cnpj", empresa.Cnpj?.Replace(".", "").Replace("/", "").Replace("-", ""))
                ),
                new XElement(ns + "InscricaoMunicipal", empresa.Inscricao_Municipal)
            );
        }

        protected virtual XElement? MontarXmlTomador(DadosTomador? tomador, XNamespace ns)
        {
            if (tomador == null) return null;

            var xmlTomador = new XElement(ns + "Tomador");

            if (tomador.Identificacao != null)
            {
                var cpfCnpj = new XElement(ns + "CpfCnpj");
                var doc = tomador.Identificacao.CpfCnpj?.Replace(".", "").Replace("/", "").Replace("-", "");
                if (!string.IsNullOrEmpty(doc))
                {
                    if (doc.Length == 11)
                        cpfCnpj.Add(new XElement(ns + "Cpf", doc));
                    else
                        cpfCnpj.Add(new XElement(ns + "Cnpj", doc));
                }

                xmlTomador.Add(new XElement(ns + "IdentificacaoTomador",
                    cpfCnpj,
                    !string.IsNullOrEmpty(tomador.Identificacao.InscricaoMunicipal)
                        ? new XElement(ns + "InscricaoMunicipal", tomador.Identificacao.InscricaoMunicipal)
                        : null
                ));
            }

            if (!string.IsNullOrEmpty(tomador.RazaoSocial))
                xmlTomador.Add(new XElement(ns + "RazaoSocial", tomador.RazaoSocial));

            if (tomador.Endereco != null)
                xmlTomador.Add(MontarXmlEndereco(tomador.Endereco, ns));

            if (tomador.Contato != null)
                xmlTomador.Add(MontarXmlContato(tomador.Contato, ns));

            return xmlTomador;
        }

        protected virtual XElement? MontarXmlIntermediario(IdentificacaoIntermediario? intermediario, XNamespace ns)
        {
            if (intermediario == null) return null;

            var cpfCnpj = new XElement(ns + "CpfCnpj");
            var doc = intermediario.CpfCnpj?.Replace(".", "").Replace("/", "").Replace("-", "");
            if (!string.IsNullOrEmpty(doc))
            {
                if (doc.Length == 11)
                    cpfCnpj.Add(new XElement(ns + "Cpf", doc));
                else
                    cpfCnpj.Add(new XElement(ns + "Cnpj", doc));
            }

            return new XElement(ns + "Intermediario",
                new XElement(ns + "IdentificacaoIntermediario",
                    cpfCnpj,
                    !string.IsNullOrEmpty(intermediario.InscricaoMunicipal)
                        ? new XElement(ns + "InscricaoMunicipal", intermediario.InscricaoMunicipal)
                        : null
                ),
                !string.IsNullOrEmpty(intermediario.RazaoSocial)
                    ? new XElement(ns + "RazaoSocial", intermediario.RazaoSocial)
                    : null
            );
        }

        protected virtual XElement? MontarXmlConstrucaoCivil(DadosConstrucaoCivil? construcao, XNamespace ns)
        {
            if (construcao == null) return null;

            return new XElement(ns + "ConstrucaoCivil",
                !string.IsNullOrEmpty(construcao.CodigoObra) ? new XElement(ns + "CodigoObra", construcao.CodigoObra) : null,
                !string.IsNullOrEmpty(construcao.Art) ? new XElement(ns + "Art", construcao.Art) : null
            );
        }

        protected virtual XElement? MontarXmlEndereco(Endereco? endereco, XNamespace ns)
        {
            if (endereco == null) return null;

            return new XElement(ns + "Endereco",
                !string.IsNullOrEmpty(endereco.Logradouro) ? new XElement(ns + "Endereco", endereco.Logradouro) : null,
                !string.IsNullOrEmpty(endereco.Numero) ? new XElement(ns + "Numero", endereco.Numero) : null,
                !string.IsNullOrEmpty(endereco.Complemento) ? new XElement(ns + "Complemento", endereco.Complemento) : null,
                !string.IsNullOrEmpty(endereco.Bairro) ? new XElement(ns + "Bairro", endereco.Bairro) : null,
                !string.IsNullOrEmpty(endereco.CodigoMunicipio) ? new XElement(ns + "CodigoMunicipio", endereco.CodigoMunicipio) : null,
                !string.IsNullOrEmpty(endereco.Uf) ? new XElement(ns + "Uf", endereco.Uf) : null,
                !string.IsNullOrEmpty(endereco.Cep) ? new XElement(ns + "Cep", endereco.Cep?.Replace("-", "")) : null
            );
        }

        protected virtual XElement? MontarXmlContato(Contato? contato, XNamespace ns)
        {
            if (contato == null) return null;

            return new XElement(ns + "Contato",
                !string.IsNullOrEmpty(contato.Telefone) ? new XElement(ns + "Telefone", contato.Telefone) : null,
                !string.IsNullOrEmpty(contato.Email) ? new XElement(ns + "Email", contato.Email) : null
            );
        }

        protected virtual string MontarXmlConsultarSituacaoLote(string protocolo, Empresas empresa)
        {
            var ns = XNamespace.Get(NamespaceNfse);

            var xml = new XDocument(
                new XDeclaration("1.0", "UTF-8", null),
                new XElement(ns + "ConsultarSituacaoLoteRpsEnvio",
                    new XElement(ns + "Prestador",
                        new XElement(ns + "CpfCnpj",
                            new XElement(ns + "Cnpj", empresa.Cnpj?.Replace(".", "").Replace("/", "").Replace("-", ""))
                        ),
                        new XElement(ns + "InscricaoMunicipal", empresa.Inscricao_Municipal)
                    ),
                    new XElement(ns + "Protocolo", protocolo)
                )
            );

            return xml.ToString(SaveOptions.DisableFormatting);
        }

        protected virtual string MontarXmlConsultarLoteRps(string protocolo, Empresas empresa)
        {
            var ns = XNamespace.Get(NamespaceNfse);

            var xml = new XDocument(
                new XDeclaration("1.0", "UTF-8", null),
                new XElement(ns + "ConsultarLoteRpsEnvio",
                    new XElement(ns + "Prestador",
                        new XElement(ns + "CpfCnpj",
                            new XElement(ns + "Cnpj", empresa.Cnpj?.Replace(".", "").Replace("/", "").Replace("-", ""))
                        ),
                        new XElement(ns + "InscricaoMunicipal", empresa.Inscricao_Municipal)
                    ),
                    new XElement(ns + "Protocolo", protocolo)
                )
            );

            return xml.ToString(SaveOptions.DisableFormatting);
        }

        protected virtual string MontarXmlConsultarNfsePorRps(ConsultarNfsePorRpsRequest request, Empresas empresa)
        {
            var ns = XNamespace.Get(NamespaceNfse);

            var xml = new XDocument(
                new XDeclaration("1.0", "UTF-8", null),
                new XElement(ns + "ConsultarNfsePorRpsEnvio",
                    new XElement(ns + "IdentificacaoRps",
                        new XElement(ns + "Numero", request.IdentificacaoRps.Numero),
                        new XElement(ns + "Serie", request.IdentificacaoRps.Serie),
                        new XElement(ns + "Tipo", request.IdentificacaoRps.Tipo)
                    ),
                    new XElement(ns + "Prestador",
                        new XElement(ns + "CpfCnpj",
                            new XElement(ns + "Cnpj", empresa.Cnpj?.Replace(".", "").Replace("/", "").Replace("-", ""))
                        ),
                        new XElement(ns + "InscricaoMunicipal", empresa.Inscricao_Municipal)
                    )
                )
            );

            return xml.ToString(SaveOptions.DisableFormatting);
        }

        protected virtual string MontarXmlConsultarNfse(ConsultarNfseRequest request, Empresas empresa)
        {
            var ns = XNamespace.Get(NamespaceNfse);

            var xml = new XDocument(
                new XDeclaration("1.0", "UTF-8", null),
                new XElement(ns + "ConsultarNfseServicoPrestadoEnvio",
                    new XElement(ns + "Prestador",
                        new XElement(ns + "CpfCnpj",
                            new XElement(ns + "Cnpj", empresa.Cnpj?.Replace(".", "").Replace("/", "").Replace("-", ""))
                        ),
                        new XElement(ns + "InscricaoMunicipal", empresa.Inscricao_Municipal)
                    ),
                    request.NumeroNfse.HasValue ? new XElement(ns + "NumeroNfse", request.NumeroNfse.Value) : null,
                    request.DataInicial.HasValue || request.DataFinal.HasValue
                        ? new XElement(ns + "PeriodoEmissao",
                            request.DataInicial.HasValue ? new XElement(ns + "DataInicial", request.DataInicial.Value.ToString("yyyy-MM-dd")) : null,
                            request.DataFinal.HasValue ? new XElement(ns + "DataFinal", request.DataFinal.Value.ToString("yyyy-MM-dd")) : null
                          )
                        : null,
                    new XElement(ns + "Pagina", request.Pagina)
                )
            );

            return xml.ToString(SaveOptions.DisableFormatting);
        }

        protected virtual string MontarXmlCancelarNfse(CancelarNfseRequest request, Empresas empresa)
        {
            var ns = XNamespace.Get(NamespaceNfse);

            var xml = new XDocument(
                new XDeclaration("1.0", "UTF-8", null),
                new XElement(ns + "CancelarNfseEnvio",
                    new XElement(ns + "Pedido",
                        new XElement(ns + "InfPedidoCancelamento",
                            new XAttribute("Id", $"cancel{request.NumeroNfse}"),
                            new XElement(ns + "IdentificacaoNfse",
                                new XElement(ns + "Numero", request.NumeroNfse),
                                new XElement(ns + "CpfCnpj",
                                    new XElement(ns + "Cnpj", empresa.Cnpj?.Replace(".", "").Replace("/", "").Replace("-", ""))
                                ),
                                new XElement(ns + "InscricaoMunicipal", empresa.Inscricao_Municipal),
                                new XElement(ns + "CodigoMunicipio", empresa.Codigo_Municipio)
                            ),
                            new XElement(ns + "CodigoCancelamento", request.CodigoCancelamento)
                        )
                    )
                )
            );

            return xml.ToString(SaveOptions.DisableFormatting);
        }

        protected virtual string MontarXmlSubstituirNfse(SubstituirNfseRequest request, Empresas empresa)
        {
            var ns = XNamespace.Get(NamespaceNfse);

            var xml = new XDocument(
                new XDeclaration("1.0", "UTF-8", null),
                new XElement(ns + "SubstituirNfseEnvio",
                    new XElement(ns + "SubstituicaoNfse",
                        new XElement(ns + "Pedido",
                            new XElement(ns + "InfPedidoCancelamento",
                                new XAttribute("Id", $"cancel{request.NumeroNfseSubstituida}"),
                                new XElement(ns + "IdentificacaoNfse",
                                    new XElement(ns + "Numero", request.NumeroNfseSubstituida),
                                    new XElement(ns + "CpfCnpj",
                                        new XElement(ns + "Cnpj", empresa.Cnpj?.Replace(".", "").Replace("/", "").Replace("-", ""))
                                    ),
                                    new XElement(ns + "InscricaoMunicipal", empresa.Inscricao_Municipal),
                                    new XElement(ns + "CodigoMunicipio", empresa.Codigo_Municipio)
                                ),
                                new XElement(ns + "CodigoCancelamento", request.CodigoCancelamento)
                            )
                        ),
                        MontarXmlRps(request.RpsSubstituto, empresa, ns)
                    )
                )
            );

            return xml.ToString(SaveOptions.DisableFormatting);
        }

        #endregion

        #region Métodos de Processamento de Resposta - Virtuais

        protected virtual void ProcessarRespostaGerarNfse(string xmlRetorno, GerarNfseResponse response)
        {
            var doc = XDocument.Parse(xmlRetorno);
            var ns = XNamespace.Get(NamespaceNfse);

            // Verificar erros
            var listaMensagens = doc.Descendants(ns + "MensagemRetorno").ToList();
            if (listaMensagens.Any())
            {
                foreach (var msg in listaMensagens)
                {
                    response.Mensagens.Add(new MensagemRetorno
                    {
                        Codigo = msg.Element(ns + "Codigo")?.Value ?? "",
                        Mensagem = msg.Element(ns + "Mensagem")?.Value ?? "",
                        Correcao = msg.Element(ns + "Correcao")?.Value
                    });
                }
                response.Sucesso = false;
                return;
            }

            // Processar NFSe gerada
            var compNfse = doc.Descendants(ns + "CompNfse").FirstOrDefault();
            if (compNfse != null)
            {
                var nfse = compNfse.Element(ns + "Nfse")?.Element(ns + "InfNfse");
                if (nfse != null)
                {
                    response.Nfse = new NfseGerada
                    {
                        Numero = long.Parse(nfse.Element(ns + "Numero")?.Value ?? "0"),
                        CodigoVerificacao = nfse.Element(ns + "CodigoVerificacao")?.Value ?? "",
                        DataEmissao = DateTime.Parse(nfse.Element(ns + "DataEmissao")?.Value ?? DateTime.Now.ToString()),
                        XmlNfse = compNfse.ToString()
                    };
                    response.Sucesso = true;
                }
            }
        }

        protected virtual void ProcessarRespostaEnviarLote(string xmlRetorno, EnviarLoteRpsResponse response)
        {
            var doc = XDocument.Parse(xmlRetorno);
            var ns = XNamespace.Get(NamespaceNfse);

            // Verificar erros
            var listaMensagens = doc.Descendants(ns + "MensagemRetorno").ToList();
            if (listaMensagens.Any())
            {
                foreach (var msg in listaMensagens)
                {
                    response.Mensagens.Add(new MensagemRetorno
                    {
                        Codigo = msg.Element(ns + "Codigo")?.Value ?? "",
                        Mensagem = msg.Element(ns + "Mensagem")?.Value ?? "",
                        Correcao = msg.Element(ns + "Correcao")?.Value
                    });
                }
                response.Sucesso = false;
                return;
            }

            // Processar protocolo
            response.NumeroLote = doc.Descendants(ns + "NumeroLote").FirstOrDefault()?.Value;
            response.Protocolo = doc.Descendants(ns + "Protocolo").FirstOrDefault()?.Value;
            var dataRecebimento = doc.Descendants(ns + "DataRecebimento").FirstOrDefault()?.Value;
            if (!string.IsNullOrEmpty(dataRecebimento))
                response.DataRecebimento = DateTime.Parse(dataRecebimento);

            response.Sucesso = !string.IsNullOrEmpty(response.Protocolo);
        }

        protected virtual void ProcessarRespostaEnviarLoteSincrono(string xmlRetorno, EnviarLoteRpsSincronoResponse response)
        {
            var doc = XDocument.Parse(xmlRetorno);
            var ns = XNamespace.Get(NamespaceNfse);

            // Verificar erros
            var listaMensagens = doc.Descendants(ns + "MensagemRetorno").ToList();
            if (listaMensagens.Any())
            {
                foreach (var msg in listaMensagens)
                {
                    response.Mensagens.Add(new MensagemRetorno
                    {
                        Codigo = msg.Element(ns + "Codigo")?.Value ?? "",
                        Mensagem = msg.Element(ns + "Mensagem")?.Value ?? "",
                        Correcao = msg.Element(ns + "Correcao")?.Value
                    });
                }
                response.Sucesso = false;
                return;
            }

            // Processar NFSes geradas
            response.NumeroLote = doc.Descendants(ns + "NumeroLote").FirstOrDefault()?.Value;

            foreach (var compNfse in doc.Descendants(ns + "CompNfse"))
            {
                var nfse = compNfse.Element(ns + "Nfse")?.Element(ns + "InfNfse");
                if (nfse != null)
                {
                    response.NfsesGeradas.Add(new NfseGerada
                    {
                        Numero = long.Parse(nfse.Element(ns + "Numero")?.Value ?? "0"),
                        CodigoVerificacao = nfse.Element(ns + "CodigoVerificacao")?.Value ?? "",
                        DataEmissao = DateTime.Parse(nfse.Element(ns + "DataEmissao")?.Value ?? DateTime.Now.ToString()),
                        XmlNfse = compNfse.ToString()
                    });
                }
            }

            response.Sucesso = response.NfsesGeradas.Any();
        }

        protected virtual void ProcessarRespostaConsultarSituacao(string xmlRetorno, ConsultarSituacaoLoteRpsResponse response)
        {
            var doc = XDocument.Parse(xmlRetorno);
            var ns = XNamespace.Get(NamespaceNfse);

            // Verificar erros
            var listaMensagens = doc.Descendants(ns + "MensagemRetorno").ToList();
            if (listaMensagens.Any())
            {
                foreach (var msg in listaMensagens)
                {
                    response.Mensagens.Add(new MensagemRetorno
                    {
                        Codigo = msg.Element(ns + "Codigo")?.Value ?? "",
                        Mensagem = msg.Element(ns + "Mensagem")?.Value ?? "",
                        Correcao = msg.Element(ns + "Correcao")?.Value
                    });
                }
            }

            var situacao = doc.Descendants(ns + "Situacao").FirstOrDefault()?.Value;
            if (!string.IsNullOrEmpty(situacao))
            {
                response.Situacao = int.Parse(situacao);
                response.DescricaoSituacao = response.Situacao switch
                {
                    1 => "Não Recebido",
                    2 => "Não Processado",
                    3 => "Processado com Erro",
                    4 => "Processado com Sucesso",
                    _ => "Desconhecido"
                };
                response.Sucesso = true;
            }
        }

        protected virtual void ProcessarRespostaConsultarLote(string xmlRetorno, ConsultarLoteRpsResponse response)
        {
            var doc = XDocument.Parse(xmlRetorno);
            var ns = XNamespace.Get(NamespaceNfse);

            // Verificar erros
            var listaMensagens = doc.Descendants(ns + "MensagemRetorno").ToList();
            if (listaMensagens.Any())
            {
                foreach (var msg in listaMensagens)
                {
                    response.Mensagens.Add(new MensagemRetorno
                    {
                        Codigo = msg.Element(ns + "Codigo")?.Value ?? "",
                        Mensagem = msg.Element(ns + "Mensagem")?.Value ?? "",
                        Correcao = msg.Element(ns + "Correcao")?.Value
                    });
                }
            }

            // Processar NFSes
            foreach (var compNfse in doc.Descendants(ns + "CompNfse"))
            {
                var nfse = compNfse.Element(ns + "Nfse")?.Element(ns + "InfNfse");
                if (nfse != null)
                {
                    response.NfsesGeradas.Add(new NfseGerada
                    {
                        Numero = long.Parse(nfse.Element(ns + "Numero")?.Value ?? "0"),
                        CodigoVerificacao = nfse.Element(ns + "CodigoVerificacao")?.Value ?? "",
                        DataEmissao = DateTime.Parse(nfse.Element(ns + "DataEmissao")?.Value ?? DateTime.Now.ToString()),
                        XmlNfse = compNfse.ToString()
                    });
                }
            }

            response.Sucesso = !response.Mensagens.Any() || response.NfsesGeradas.Any();
        }

        protected virtual void ProcessarRespostaConsultarNfsePorRps(string xmlRetorno, ConsultarNfsePorRpsResponse response)
        {
            var doc = XDocument.Parse(xmlRetorno);
            var ns = XNamespace.Get(NamespaceNfse);

            // Verificar erros
            var listaMensagens = doc.Descendants(ns + "MensagemRetorno").ToList();
            if (listaMensagens.Any())
            {
                foreach (var msg in listaMensagens)
                {
                    response.Mensagens.Add(new MensagemRetorno
                    {
                        Codigo = msg.Element(ns + "Codigo")?.Value ?? "",
                        Mensagem = msg.Element(ns + "Mensagem")?.Value ?? "",
                        Correcao = msg.Element(ns + "Correcao")?.Value
                    });
                }
                response.Sucesso = false;
                return;
            }

            var compNfse = doc.Descendants(ns + "CompNfse").FirstOrDefault();
            if (compNfse != null)
            {
                var nfse = compNfse.Element(ns + "Nfse")?.Element(ns + "InfNfse");
                if (nfse != null)
                {
                    response.Nfse = new NfseGerada
                    {
                        Numero = long.Parse(nfse.Element(ns + "Numero")?.Value ?? "0"),
                        CodigoVerificacao = nfse.Element(ns + "CodigoVerificacao")?.Value ?? "",
                        DataEmissao = DateTime.Parse(nfse.Element(ns + "DataEmissao")?.Value ?? DateTime.Now.ToString()),
                        XmlNfse = compNfse.ToString()
                    };
                    response.Sucesso = true;
                }
            }
        }

        protected virtual void ProcessarRespostaConsultarNfse(string xmlRetorno, ConsultarNfseResponse response)
        {
            var doc = XDocument.Parse(xmlRetorno);
            var ns = XNamespace.Get(NamespaceNfse);

            // Verificar erros
            var listaMensagens = doc.Descendants(ns + "MensagemRetorno").ToList();
            if (listaMensagens.Any())
            {
                foreach (var msg in listaMensagens)
                {
                    response.Mensagens.Add(new MensagemRetorno
                    {
                        Codigo = msg.Element(ns + "Codigo")?.Value ?? "",
                        Mensagem = msg.Element(ns + "Mensagem")?.Value ?? "",
                        Correcao = msg.Element(ns + "Correcao")?.Value
                    });
                }
            }

            // Processar NFSes
            foreach (var compNfse in doc.Descendants(ns + "CompNfse"))
            {
                var nfse = compNfse.Element(ns + "Nfse")?.Element(ns + "InfNfse");
                if (nfse != null)
                {
                    response.Nfses.Add(new NfseGerada
                    {
                        Numero = long.Parse(nfse.Element(ns + "Numero")?.Value ?? "0"),
                        CodigoVerificacao = nfse.Element(ns + "CodigoVerificacao")?.Value ?? "",
                        DataEmissao = DateTime.Parse(nfse.Element(ns + "DataEmissao")?.Value ?? DateTime.Now.ToString()),
                        XmlNfse = compNfse.ToString()
                    });
                }
            }

            // Paginação
            var proxPagina = doc.Descendants(ns + "ProximaPagina").FirstOrDefault()?.Value;
            response.PaginaAtual = int.Parse(doc.Descendants(ns + "Pagina").FirstOrDefault()?.Value ?? "1");
            response.TotalPaginas = string.IsNullOrEmpty(proxPagina) ? response.PaginaAtual : response.PaginaAtual + 1;

            response.Sucesso = true;
        }

        protected virtual void ProcessarRespostaCancelarNfse(string xmlRetorno, CancelarNfseResponse response)
        {
            var doc = XDocument.Parse(xmlRetorno);
            var ns = XNamespace.Get(NamespaceNfse);

            // Verificar erros
            var listaMensagens = doc.Descendants(ns + "MensagemRetorno").ToList();
            if (listaMensagens.Any())
            {
                foreach (var msg in listaMensagens)
                {
                    response.Mensagens.Add(new MensagemRetorno
                    {
                        Codigo = msg.Element(ns + "Codigo")?.Value ?? "",
                        Mensagem = msg.Element(ns + "Mensagem")?.Value ?? "",
                        Correcao = msg.Element(ns + "Correcao")?.Value
                    });
                }
                response.Sucesso = false;
                return;
            }

            var cancelamento = doc.Descendants(ns + "Cancelamento").FirstOrDefault();
            if (cancelamento != null)
            {
                var infConfirmacao = cancelamento.Descendants(ns + "InfConfirmacaoCancelamento").FirstOrDefault();
                if (infConfirmacao != null)
                {
                    response.NumeroNfseCancelada = long.Parse(
                        infConfirmacao.Descendants(ns + "Numero").FirstOrDefault()?.Value ?? "0");
                    var dataCancelamento = infConfirmacao.Element(ns + "DataHora")?.Value;
                    if (!string.IsNullOrEmpty(dataCancelamento))
                        response.DataCancelamento = DateTime.Parse(dataCancelamento);
                    response.Sucesso = true;
                }
            }
        }

        protected virtual void ProcessarRespostaSubstituirNfse(string xmlRetorno, SubstituirNfseResponse response)
        {
            var doc = XDocument.Parse(xmlRetorno);
            var ns = XNamespace.Get(NamespaceNfse);

            // Verificar erros
            var listaMensagens = doc.Descendants(ns + "MensagemRetorno").ToList();
            if (listaMensagens.Any())
            {
                foreach (var msg in listaMensagens)
                {
                    response.Mensagens.Add(new MensagemRetorno
                    {
                        Codigo = msg.Element(ns + "Codigo")?.Value ?? "",
                        Mensagem = msg.Element(ns + "Mensagem")?.Value ?? "",
                        Correcao = msg.Element(ns + "Correcao")?.Value
                    });
                }
                response.Sucesso = false;
                return;
            }

            // Processar NFSe substituída (cancelada)
            var subNfse = doc.Descendants(ns + "SubstituicaoNfse").FirstOrDefault();
            if (subNfse != null)
            {
                response.NumeroNfseCancelada = long.Parse(
                    subNfse.Descendants(ns + "NfseSubstituida").FirstOrDefault()?.Value ?? "0");

                var compNfse = subNfse.Descendants(ns + "CompNfse").FirstOrDefault();
                if (compNfse != null)
                {
                    var nfse = compNfse.Element(ns + "Nfse")?.Element(ns + "InfNfse");
                    if (nfse != null)
                    {
                        response.NfseSubstituta = new NfseGerada
                        {
                            Numero = long.Parse(nfse.Element(ns + "Numero")?.Value ?? "0"),
                            CodigoVerificacao = nfse.Element(ns + "CodigoVerificacao")?.Value ?? "",
                            DataEmissao = DateTime.Parse(nfse.Element(ns + "DataEmissao")?.Value ?? DateTime.Now.ToString()),
                            XmlNfse = compNfse.ToString()
                        };
                    }
                }

                response.Sucesso = true;
            }
        }

        #endregion

        #region Métodos Auxiliares

        /// <summary>
        /// Obtém o certificado da empresa descriptografando a senha
        /// </summary>
        protected X509Certificate2 ObterCertificado(Empresas empresa)
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

        /// <summary>
        /// Assina o XML com o certificado digital
        /// </summary>
        protected virtual string AssinarXml(string xml, Empresas empresa)
        {
            using var cert = ObterCertificado(empresa);

            var doc = new XmlDocument { PreserveWhitespace = true };
            doc.LoadXml(xml);

            // Criar referência de assinatura
            var signedXml = new System.Security.Cryptography.Xml.SignedXml(doc)
            {
                SigningKey = cert.GetRSAPrivateKey()
            };

            // Encontrar elementos a serem assinados (com atributo Id)
            var elementsToSign = doc.SelectNodes("//*[@Id]");
            if (elementsToSign != null)
            {
                foreach (XmlElement element in elementsToSign)
                {
                    var reference = new System.Security.Cryptography.Xml.Reference($"#{element.GetAttribute("Id")}");
                    reference.AddTransform(new System.Security.Cryptography.Xml.XmlDsigEnvelopedSignatureTransform());
                    reference.AddTransform(new System.Security.Cryptography.Xml.XmlDsigC14NTransform());
                    signedXml.AddReference(reference);
                }
            }

            // Adicionar informações do certificado
            var keyInfo = new System.Security.Cryptography.Xml.KeyInfo();
            keyInfo.AddClause(new System.Security.Cryptography.Xml.KeyInfoX509Data(cert));
            signedXml.KeyInfo = keyInfo;

            signedXml.ComputeSignature();

            // Inserir assinatura no documento
            var signature = signedXml.GetXml();
            doc.DocumentElement?.AppendChild(doc.ImportNode(signature, true));

            return doc.OuterXml;
        }

        /// <summary>
        /// Valida o XML contra o schema XSD
        /// </summary>
        protected virtual void ValidarXml(string xml, string nomeSchema)
        {
            var caminhoSchema = Path.Combine(CaminhoSchemas, nomeSchema);

            if (!File.Exists(caminhoSchema))
            {
                _logger.LogWarning("Schema XSD não encontrado: {CaminhoSchema}", caminhoSchema);
                return; // Não falhar se o schema não existir
            }

            var settings = new XmlReaderSettings();
            settings.Schemas.Add(NamespaceNfse, caminhoSchema);
            settings.ValidationType = ValidationType.Schema;

            var erros = new List<string>();
            settings.ValidationEventHandler += (sender, args) =>
            {
                if (args.Severity == XmlSeverityType.Error)
                    erros.Add(args.Message);
            };

            using var reader = XmlReader.Create(new StringReader(xml), settings);
            while (reader.Read()) { }

            if (erros.Any())
            {
                throw new BusinessException($"XML inválido: {string.Join("; ", erros)}");
            }
        }

        /// <summary>
        /// Envia requisição SOAP para o WebService
        /// </summary>
        protected virtual async Task<string> EnviarRequisicaoAsync(string xmlConteudo, string url, string metodo)
        {
            var envelope = MontarEnvelopeSoap(xmlConteudo, metodo);
            var soapAction = ObterSoapAction(metodo);

            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(60);

            var content = new StringContent(envelope, Encoding.UTF8, "text/xml");
            content.Headers.Add("SOAPAction", soapAction);

            _logger.LogDebug("Enviando requisição para {Url}, método {Metodo}", url, metodo);

            var response = await httpClient.PostAsync(url, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Erro na requisição: {StatusCode} - {Content}",
                    response.StatusCode, responseContent);
                throw new BusinessException($"Erro na comunicação com o WebService: {response.StatusCode}");
            }

            return ExtrairRespostaSoap(responseContent, metodo);
        }

        /// <summary>
        /// Retorna a URL correta baseado no ambiente
        /// </summary>
        protected string ObterUrl(bool homologacao) => homologacao ? UrlHomologacao : UrlProducao;

        #endregion
    }
}