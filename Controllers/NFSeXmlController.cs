using Microsoft.AspNetCore.Mvc;
using NFSE_ABRASF.DTOs.NFSe;
using NFSE_ABRASF.Repositories.Interfaces;
using NFSE_ABRASF.Services.NFSe;

namespace NFSE_ABRASF.Controllers
{
    /// <summary>
    /// Controller para processar XML já montado pelo cliente
    /// O cliente envia o XML completo e a API apenas assina e envia para a prefeitura
    /// </summary>
    [Route("v1/nfse/xml")]
    [ApiController]
    public class NFSeXmlController : ControllerBase
    {
        private readonly INFSeXmlDirectService _xmlDirectService;
        private readonly IEmpresaRepository _empresaRepository;
        private readonly ILogger<NFSeXmlController> _logger;

        public NFSeXmlController(
            INFSeXmlDirectService xmlDirectService,
            IEmpresaRepository empresaRepository,
            ILogger<NFSeXmlController> logger)
        {
            _xmlDirectService = xmlDirectService;
            _empresaRepository = empresaRepository;
            _logger = logger;
        }

        /// <summary>
        /// Obtém o ID da empresa autenticada via API Key
        /// </summary>
        private int GetEmpresaIdFromContext()
        {
            if (HttpContext.Items.TryGetValue("EmpresaId", out var empresaIdObj) && empresaIdObj is int empresaId)
            {
                return empresaId;
            }

            throw new UnauthorizedAccessException("Empresa não identificada. Verifique sua API Key.");
        }

        /// <summary>
        /// Busca a empresa e valida se está ativa
        /// </summary>
        private async Task<Models.Empresas> GetEmpresaAsync(int empresaId)
        {
            var empresa = await _empresaRepository.ObterPorIdAsync(empresaId);

            if (empresa == null)
            {
                throw new UnauthorizedAccessException("Empresa não encontrada.");
            }

            if (!empresa.Ativa)
            {
                throw new UnauthorizedAccessException("Empresa inativa.");
            }

            return empresa;
        }

        /// <summary>
        /// Processa qualquer XML genérico (método universal)
        /// Requer autenticação via API Key (header X-Api-Key)
        /// </summary>
        /// <remarks>
        /// Este é o endpoint genérico que aceita qualquer tipo de XML.
        /// 
        /// **Exemplo de uso:**
        /// 
        /// ```json
        /// {
        ///   "xmlContent": "&lt;GerarNfseEnvio xmlns='http://www.abrasf.org.br/nfse.xsd'&gt;...&lt;/GerarNfseEnvio&gt;",
        ///   "metodoSoap": "GerarNfse",
        ///   "validarXsd": false
        /// }
        /// ```
        /// 
        /// **Métodos SOAP disponíveis:**
        /// - GerarNfse
        /// - RecepcionarLoteRps
        /// - RecepcionarLoteRpsSincrono
        /// - ConsultarSituacaoLoteRps
        /// - ConsultarLoteRps
        /// - ConsultarNfsePorRps
        /// - ConsultarNfseServicoPrestado
        /// - CancelarNfse
        /// - SubstituirNfse
        /// </remarks>
        [HttpPost("processar")]
        [ProducesResponseType(typeof(ProcessarXmlResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<ProcessarXmlResponse>> ProcessarXml([FromBody] XmlDirectRequest request)
        {
            var empresaId = GetEmpresaIdFromContext();
            var empresa = await GetEmpresaAsync(empresaId);

            _logger.LogInformation(
                "Requisição de processamento XML direto - Empresa: {EmpresaId}, Método: {Metodo}",
                empresaId, request.MetodoSoap);

            var processorRequest = new ProcessarXmlRequest
            {
                XmlContent = request.XmlContent,
                MetodoSoap = request.MetodoSoap,
            };

            var response = await _xmlDirectService.ProcessarXmlAsync(processorRequest, empresa);

            if (!response.Sucesso && response.HttpStatusCode != 200)
            {
                return StatusCode(response.HttpStatusCode, response);
            }

            return Ok(response);
        }

        /// <summary>
        /// Gera NFSe com XML pronto
        /// Requer autenticação via API Key (header X-Api-Key)
        /// </summary>
        /// <remarks>
        /// Endpoint específico para GerarNfse.
        /// 
        /// **Exemplo do XML que deve ser enviado:**
        /// 
        /// ```xml
        /// &lt;GerarNfseEnvio xmlns="http://www.abrasf.org.br/nfse.xsd"&gt;
        ///   &lt;Rps&gt;
        ///     &lt;InfDeclaracaoPrestacaoServico Id="rps1"&gt;
        ///       &lt;!-- ... conteúdo do RPS ... --&gt;
        ///     &lt;/InfDeclaracaoPrestacaoServico&gt;
        ///   &lt;/Rps&gt;
        /// &lt;/GerarNfseEnvio&gt;
        /// ```
        /// 
        /// **IMPORTANTE:** 
        /// - O XML deve ter elementos com atributo `Id` para serem assinados
        /// - Não inclua a assinatura digital (a API faz isso automaticamente)
        /// - O XML deve estar em formato texto (não base64)
        /// </remarks>
        [HttpPost("gerar-nfse")]
        [ProducesResponseType(typeof(ProcessarXmlResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<ProcessarXmlResponse>> GerarNfse([FromBody] GerarNfseXmlRequest request)
        {
            var empresaId = GetEmpresaIdFromContext();
            var empresa = await GetEmpresaAsync(empresaId);

            _logger.LogInformation("Requisição GerarNfse com XML - Empresa: {EmpresaId}", empresaId);

            var processorRequest = new ProcessarXmlRequest
            {
                XmlContent = request.XmlContent,
                MetodoSoap = "GerarNfse",
            };

            var response = await _xmlDirectService.ProcessarXmlAsync(processorRequest, empresa);

            if (!response.Sucesso && response.HttpStatusCode != 200)
            {
                return StatusCode(response.HttpStatusCode, response);
            }

            return Ok(response);
        }

        /// <summary>
        /// Envia lote de RPS com XML pronto (assíncrono)
        /// Requer autenticação via API Key (header X-Api-Key)
        /// </summary>
        /// <remarks>
        /// Endpoint específico para RecepcionarLoteRps.
        /// 
        /// **Exemplo do XML que deve ser enviado:**
        /// 
        /// ```xml
        /// &lt;EnviarLoteRpsEnvio xmlns="http://www.abrasf.org.br/nfse.xsd"&gt;
        ///   &lt;LoteRps Id="lote1" versao="2.04"&gt;
        ///     &lt;NumeroLote&gt;1&lt;/NumeroLote&gt;
        ///     &lt;CpfCnpj&gt;&lt;Cnpj&gt;12345678000100&lt;/Cnpj&gt;&lt;/CpfCnpj&gt;
        ///     &lt;InscricaoMunicipal&gt;123456&lt;/InscricaoMunicipal&gt;
        ///     &lt;QuantidadeRps&gt;1&lt;/QuantidadeRps&gt;
        ///     &lt;ListaRps&gt;
        ///       &lt;!-- ... RPS ... --&gt;
        ///     &lt;/ListaRps&gt;
        ///   &lt;/LoteRps&gt;
        /// &lt;/EnviarLoteRpsEnvio&gt;
        /// ```
        /// </remarks>
        [HttpPost("enviar-lote")]
        [ProducesResponseType(typeof(ProcessarXmlResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<ProcessarXmlResponse>> EnviarLoteRps([FromBody] EnviarLoteRpsXmlRequest request)
        {
            var empresaId = GetEmpresaIdFromContext();
            var empresa = await GetEmpresaAsync(empresaId);

            _logger.LogInformation("Requisição EnviarLoteRps com XML - Empresa: {EmpresaId}", empresaId);

            var processorRequest = new ProcessarXmlRequest
            {
                XmlContent = request.XmlContent,
                MetodoSoap = "RecepcionarLoteRps",
       
            };

            var response = await _xmlDirectService.ProcessarXmlAsync(processorRequest, empresa);

            if (!response.Sucesso && response.HttpStatusCode != 200)
            {
                return StatusCode(response.HttpStatusCode, response);
            }

            return Ok(response);
        }

        /// <summary>
        /// Envia lote de RPS com XML pronto (síncrono)
        /// Requer autenticação via API Key (header X-Api-Key)
        /// </summary>
        [HttpPost("enviar-lote-sincrono")]
        [ProducesResponseType(typeof(ProcessarXmlResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<ProcessarXmlResponse>> EnviarLoteRpsSincrono([FromBody] EnviarLoteRpsSincronoXmlRequest request)
        {
            var empresaId = GetEmpresaIdFromContext();
            var empresa = await GetEmpresaAsync(empresaId);

            _logger.LogInformation("Requisição EnviarLoteRpsSincrono com XML - Empresa: {EmpresaId}", empresaId);

            var processorRequest = new ProcessarXmlRequest
            {
                XmlContent = request.XmlContent,
                MetodoSoap = "RecepcionarLoteRpsSincrono",
         
            };

            var response = await _xmlDirectService.ProcessarXmlAsync(processorRequest, empresa);

            if (!response.Sucesso && response.HttpStatusCode != 200)
            {
                return StatusCode(response.HttpStatusCode, response);
            }

            return Ok(response);
        }

        /// <summary>
        /// Consulta situação do lote com XML pronto
        /// Requer autenticação via API Key (header X-Api-Key)
        /// </summary>
        [HttpPost("consultar-situacao-lote")]
        [ProducesResponseType(typeof(ProcessarXmlResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<ProcessarXmlResponse>> ConsultarSituacaoLote([FromBody] ConsultarSituacaoLoteXmlRequest request)
        {
            var empresaId = GetEmpresaIdFromContext();
            var empresa = await GetEmpresaAsync(empresaId);

            _logger.LogInformation("Requisição ConsultarSituacaoLote com XML - Empresa: {EmpresaId}", empresaId);

            var processorRequest = new ProcessarXmlRequest
            {
                XmlContent = request.XmlContent,
                MetodoSoap = "ConsultarSituacaoLoteRps",
            };

            var response = await _xmlDirectService.ProcessarXmlAsync(processorRequest, empresa);

            if (!response.Sucesso && response.HttpStatusCode != 200)
            {
                return StatusCode(response.HttpStatusCode, response);
            }

            return Ok(response);
        }

        /// <summary>
        /// Consulta lote de RPS com XML pronto
        /// Requer autenticação via API Key (header X-Api-Key)
        /// </summary>
        [HttpPost("consultar-lote")]
        [ProducesResponseType(typeof(ProcessarXmlResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<ProcessarXmlResponse>> ConsultarLoteRps([FromBody] ConsultarLoteRpsXmlRequest request)
        {
            var empresaId = GetEmpresaIdFromContext();
            var empresa = await GetEmpresaAsync(empresaId);

            _logger.LogInformation("Requisição ConsultarLoteRps com XML - Empresa: {EmpresaId}", empresaId);

            var processorRequest = new ProcessarXmlRequest
            {
                XmlContent = request.XmlContent,
                MetodoSoap = "ConsultarLoteRps",
         
            };

            var response = await _xmlDirectService.ProcessarXmlAsync(processorRequest, empresa);

            if (!response.Sucesso && response.HttpStatusCode != 200)
            {
                return StatusCode(response.HttpStatusCode, response);
            }

            return Ok(response);
        }

        /// <summary>
        /// Consulta NFSe por RPS com XML pronto
        /// Requer autenticação via API Key (header X-Api-Key)
        /// </summary>
        [HttpPost("consultar-nfse-rps")]
        [ProducesResponseType(typeof(ProcessarXmlResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<ProcessarXmlResponse>> ConsultarNfsePorRps([FromBody] ConsultarNfsePorRpsXmlRequest request)
        {
            var empresaId = GetEmpresaIdFromContext();
            var empresa = await GetEmpresaAsync(empresaId);

            _logger.LogInformation("Requisição ConsultarNfsePorRps com XML - Empresa: {EmpresaId}", empresaId);

            var processorRequest = new ProcessarXmlRequest
            {
                XmlContent = request.XmlContent,
                MetodoSoap = "ConsultarNfsePorRps",
            };

            var response = await _xmlDirectService.ProcessarXmlAsync(processorRequest, empresa);

            if (!response.Sucesso && response.HttpStatusCode != 200)
            {
                return StatusCode(response.HttpStatusCode, response);
            }

            return Ok(response);
        }

        /// <summary>
        /// Consulta NFSe com XML pronto
        /// Requer autenticação via API Key (header X-Api-Key)
        /// </summary>
        [HttpPost("consultar-nfse")]
        [ProducesResponseType(typeof(ProcessarXmlResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<ProcessarXmlResponse>> ConsultarNfse([FromBody] ConsultarNfseXmlRequest request)
        {
            var empresaId = GetEmpresaIdFromContext();
            var empresa = await GetEmpresaAsync(empresaId);

            _logger.LogInformation("Requisição ConsultarNfse com XML - Empresa: {EmpresaId}", empresaId);

            var processorRequest = new ProcessarXmlRequest
            {
                XmlContent = request.XmlContent,
                MetodoSoap = "ConsultarNfseServicoPrestado",
             
            };

            var response = await _xmlDirectService.ProcessarXmlAsync(processorRequest, empresa);

            if (!response.Sucesso && response.HttpStatusCode != 200)
            {
                return StatusCode(response.HttpStatusCode, response);
            }

            return Ok(response);
        }

        /// <summary>
        /// Cancela NFSe com XML pronto
        /// Requer autenticação via API Key (header X-Api-Key)
        /// </summary>
        [HttpPost("cancelar-nfse")]
        [ProducesResponseType(typeof(ProcessarXmlResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<ProcessarXmlResponse>> CancelarNfse([FromBody] CancelarNfseXmlRequest request)
        {
            var empresaId = GetEmpresaIdFromContext();
            var empresa = await GetEmpresaAsync(empresaId);

            _logger.LogInformation("Requisição CancelarNfse com XML - Empresa: {EmpresaId}", empresaId);

            var processorRequest = new ProcessarXmlRequest
            {
                XmlContent = request.XmlContent,
                MetodoSoap = "CancelarNfse",
           
            };

            var response = await _xmlDirectService.ProcessarXmlAsync(processorRequest, empresa);

            if (!response.Sucesso && response.HttpStatusCode != 200)
            {
                return StatusCode(response.HttpStatusCode, response);
            }

            return Ok(response);
        }

        /// <summary>
        /// Substitui NFSe com XML pronto
        /// Requer autenticação via API Key (header X-Api-Key)
        /// </summary>
        [HttpPost("substituir-nfse")]
        [ProducesResponseType(typeof(ProcessarXmlResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<ProcessarXmlResponse>> SubstituirNfse([FromBody] SubstituirNfseXmlRequest request)
        {
            var empresaId = GetEmpresaIdFromContext();
            var empresa = await GetEmpresaAsync(empresaId);

            _logger.LogInformation("Requisição SubstituirNfse com XML - Empresa: {EmpresaId}", empresaId);

            var processorRequest = new ProcessarXmlRequest
            {
                XmlContent = request.XmlContent,
                MetodoSoap = "SubstituirNfse",
  
            };

            var response = await _xmlDirectService.ProcessarXmlAsync(processorRequest, empresa);

            if (!response.Sucesso && response.HttpStatusCode != 200)
            {
                return StatusCode(response.HttpStatusCode, response);
            }

            return Ok(response);
        }
    }
}