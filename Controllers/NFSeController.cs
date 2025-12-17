using Microsoft.AspNetCore.Mvc;
using NFSE_ABRASF.DTOs.NFSe;
using NFSE_ABRASF.Services.NFSe;

namespace NFSE_ABRASF.Controllers
{
    [Route("v1/nfse")]
    [ApiController]
    public class NFSeController : ControllerBase
    {
        private readonly INFSeService _nfseService;
        private readonly ILogger<NFSeController> _logger;

        public NFSeController(
            INFSeService nfseService,
            ILogger<NFSeController> logger)
        {
            _nfseService = nfseService;
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
        /// Lista todos os municípios disponíveis para emissão de NFSe
        /// Rota pública - não requer autenticação
        /// </summary>
        [HttpGet("municipios")]
        [ProducesResponseType(typeof(IEnumerable<MunicipioInfo>), StatusCodes.Status200OK)]
        public ActionResult<IEnumerable<MunicipioInfo>> ListarMunicipios()
        {
            var municipios = _nfseService.ListarMunicipiosDisponiveis();
            return Ok(municipios);
        }

        /// <summary>
        /// Gera uma NFSe a partir de um único RPS
        /// Requer autenticação via API Key (header X-Api-Key)
        /// </summary>
        /// <param name="request">Dados do RPS</param>
        [HttpPost("gerar")]
        [ProducesResponseType(typeof(GerarNfseResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<GerarNfseResponse>> GerarNfse([FromBody] GerarNfseRequest request)
        {
            var empresaId = GetEmpresaIdFromContext();
            _logger.LogInformation("Requisição para gerar NFSe - Empresa: {EmpresaId}", empresaId);

            var response = await _nfseService.GerarNfseAsync(empresaId, request);

            if (!response.Sucesso)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        /// <summary>
        /// Envia um lote de RPS para processamento assíncrono
        /// Requer autenticação via API Key (header X-Api-Key)
        /// </summary>
        /// <param name="request">Dados do lote de RPS</param>
        [HttpPost("lote")]
        [ProducesResponseType(typeof(EnviarLoteRpsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<EnviarLoteRpsResponse>> EnviarLoteRps([FromBody] EnviarLoteRpsRequest request)
        {
            var empresaId = GetEmpresaIdFromContext();
            _logger.LogInformation(
                "Requisição para enviar lote RPS - Empresa: {EmpresaId}, Qtd RPS: {Quantidade}",
                empresaId, request.QuantidadeRps);

            var response = await _nfseService.EnviarLoteRpsAsync(empresaId, request);

            if (!response.Sucesso)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        /// <summary>
        /// Envia um lote de RPS para processamento síncrono (aguarda retorno)
        /// Requer autenticação via API Key (header X-Api-Key)
        /// </summary>
        /// <param name="request">Dados do lote de RPS</param>
        [HttpPost("lote/sincrono")]
        [ProducesResponseType(typeof(EnviarLoteRpsSincronoResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<EnviarLoteRpsSincronoResponse>> EnviarLoteRpsSincrono([FromBody] EnviarLoteRpsRequest request)
        {
            var empresaId = GetEmpresaIdFromContext();
            _logger.LogInformation(
                "Requisição para enviar lote RPS síncrono - Empresa: {EmpresaId}, Qtd RPS: {Quantidade}",
                empresaId, request.QuantidadeRps);

            var response = await _nfseService.EnviarLoteRpsSincronoAsync(empresaId, request);

            if (!response.Sucesso)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        /// <summary>
        /// Consulta a situação de um lote de RPS pelo protocolo
        /// Requer autenticação via API Key (header X-Api-Key)
        /// </summary>
        /// <param name="protocolo">Protocolo do lote</param>
        [HttpGet("lote/{protocolo}/situacao")]
        [ProducesResponseType(typeof(ConsultarSituacaoLoteRpsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<ConsultarSituacaoLoteRpsResponse>> ConsultarSituacaoLote(string protocolo)
        {
            var empresaId = GetEmpresaIdFromContext();
            _logger.LogInformation(
                "Requisição para consultar situação do lote - Empresa: {EmpresaId}, Protocolo: {Protocolo}",
                empresaId, protocolo);

            var response = await _nfseService.ConsultarSituacaoLoteRpsAsync(empresaId, protocolo);

            return Ok(response);
        }

        /// <summary>
        /// Consulta as NFSes geradas de um lote pelo protocolo
        /// Requer autenticação via API Key (header X-Api-Key)
        /// </summary>
        /// <param name="protocolo">Protocolo do lote</param>
        [HttpGet("lote/{protocolo}")]
        [ProducesResponseType(typeof(ConsultarLoteRpsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<ConsultarLoteRpsResponse>> ConsultarLoteRps(string protocolo)
        {
            var empresaId = GetEmpresaIdFromContext();
            _logger.LogInformation(
                "Requisição para consultar lote RPS - Empresa: {EmpresaId}, Protocolo: {Protocolo}",
                empresaId, protocolo);

            var response = await _nfseService.ConsultarLoteRpsAsync(empresaId, protocolo);

            return Ok(response);
        }

        /// <summary>
        /// Consulta uma NFSe pelo número do RPS
        /// Requer autenticação via API Key (header X-Api-Key)
        /// </summary>
        /// <param name="request">Identificação do RPS</param>
        [HttpPost("consultar/rps")]
        [ProducesResponseType(typeof(ConsultarNfsePorRpsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<ConsultarNfsePorRpsResponse>> ConsultarNfsePorRps([FromBody] ConsultarNfsePorRpsRequest request)
        {
            var empresaId = GetEmpresaIdFromContext();
            _logger.LogInformation(
                "Requisição para consultar NFSe por RPS - Empresa: {EmpresaId}, RPS: {NumeroRps}",
                empresaId, request.IdentificacaoRps.Numero);

            var response = await _nfseService.ConsultarNfsePorRpsAsync(empresaId, request);

            if (!response.Sucesso)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        /// <summary>
        /// Consulta NFSes por período, tomador ou número
        /// Requer autenticação via API Key (header X-Api-Key)
        /// </summary>
        /// <param name="request">Filtros da consulta</param>
        [HttpPost("consultar")]
        [ProducesResponseType(typeof(ConsultarNfseResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<ConsultarNfseResponse>> ConsultarNfse([FromBody] ConsultarNfseRequest request)
        {
            var empresaId = GetEmpresaIdFromContext();
            _logger.LogInformation(
                "Requisição para consultar NFSe - Empresa: {EmpresaId}",
                empresaId);

            var response = await _nfseService.ConsultarNfseAsync(empresaId, request);

            return Ok(response);
        }

        /// <summary>
        /// Cancela uma NFSe
        /// Requer autenticação via API Key (header X-Api-Key)
        /// </summary>
        /// <param name="request">Dados do cancelamento</param>
        [HttpPost("cancelar")]
        [ProducesResponseType(typeof(CancelarNfseResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<CancelarNfseResponse>> CancelarNfse([FromBody] CancelarNfseRequest request)
        {
            var empresaId = GetEmpresaIdFromContext();
            _logger.LogInformation(
                "Requisição para cancelar NFSe - Empresa: {EmpresaId}, NFSe: {NumeroNfse}",
                empresaId, request.NumeroNfse);

            var response = await _nfseService.CancelarNfseAsync(empresaId, request);

            if (!response.Sucesso)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        /// <summary>
        /// Substitui uma NFSe (cancela a anterior e gera uma nova)
        /// Requer autenticação via API Key (header X-Api-Key)
        /// </summary>
        /// <param name="request">Dados da substituição</param>
        [HttpPost("substituir")]
        [ProducesResponseType(typeof(SubstituirNfseResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<SubstituirNfseResponse>> SubstituirNfse([FromBody] SubstituirNfseRequest request)
        {
            var empresaId = GetEmpresaIdFromContext();
            _logger.LogInformation(
                "Requisição para substituir NFSe - Empresa: {EmpresaId}, NFSe Original: {NumeroNfse}",
                empresaId, request.NumeroNfseSubstituida);

            var response = await _nfseService.SubstituirNfseAsync(empresaId, request);

            if (!response.Sucesso)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }
    }
}