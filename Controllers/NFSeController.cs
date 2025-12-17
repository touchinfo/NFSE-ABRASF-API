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
        /// Lista todos os municípios disponíveis para emissão de NFSe
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
        /// </summary>
        /// <param name="empresaId">ID da empresa emissora</param>
        /// <param name="request">Dados do RPS</param>
        [HttpPost("{empresaId:int}/gerar")]
        [ProducesResponseType(typeof(GerarNfseResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<GerarNfseResponse>> GerarNfse(
            int empresaId,
            [FromBody] GerarNfseRequest request)
        {
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
        /// </summary>
        /// <param name="empresaId">ID da empresa emissora</param>
        /// <param name="request">Dados do lote de RPS</param>
        [HttpPost("{empresaId:int}/lote")]
        [ProducesResponseType(typeof(EnviarLoteRpsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<EnviarLoteRpsResponse>> EnviarLoteRps(
            int empresaId,
            [FromBody] EnviarLoteRpsRequest request)
        {
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
        /// </summary>
        /// <param name="empresaId">ID da empresa emissora</param>
        /// <param name="request">Dados do lote de RPS</param>
        [HttpPost("{empresaId:int}/lote/sincrono")]
        [ProducesResponseType(typeof(EnviarLoteRpsSincronoResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<EnviarLoteRpsSincronoResponse>> EnviarLoteRpsSincrono(
            int empresaId,
            [FromBody] EnviarLoteRpsRequest request)
        {
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
        /// </summary>
        /// <param name="empresaId">ID da empresa emissora</param>
        /// <param name="protocolo">Protocolo do lote</param>
        [HttpGet("{empresaId:int}/lote/{protocolo}/situacao")]
        [ProducesResponseType(typeof(ConsultarSituacaoLoteRpsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ConsultarSituacaoLoteRpsResponse>> ConsultarSituacaoLote(
            int empresaId,
            string protocolo)
        {
            _logger.LogInformation(
                "Requisição para consultar situação do lote - Empresa: {EmpresaId}, Protocolo: {Protocolo}",
                empresaId, protocolo);

            var response = await _nfseService.ConsultarSituacaoLoteRpsAsync(empresaId, protocolo);

            return Ok(response);
        }

        /// <summary>
        /// Consulta as NFSes geradas de um lote pelo protocolo
        /// </summary>
        /// <param name="empresaId">ID da empresa emissora</param>
        /// <param name="protocolo">Protocolo do lote</param>
        [HttpGet("{empresaId:int}/lote/{protocolo}")]
        [ProducesResponseType(typeof(ConsultarLoteRpsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ConsultarLoteRpsResponse>> ConsultarLoteRps(
            int empresaId,
            string protocolo)
        {
            _logger.LogInformation(
                "Requisição para consultar lote RPS - Empresa: {EmpresaId}, Protocolo: {Protocolo}",
                empresaId, protocolo);

            var response = await _nfseService.ConsultarLoteRpsAsync(empresaId, protocolo);

            return Ok(response);
        }

        /// <summary>
        /// Consulta uma NFSe pelo número do RPS
        /// </summary>
        /// <param name="empresaId">ID da empresa emissora</param>
        /// <param name="request">Identificação do RPS</param>
        [HttpPost("{empresaId:int}/consultar/rps")]
        [ProducesResponseType(typeof(ConsultarNfsePorRpsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ConsultarNfsePorRpsResponse>> ConsultarNfsePorRps(
            int empresaId,
            [FromBody] ConsultarNfsePorRpsRequest request)
        {
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
        /// </summary>
        /// <param name="empresaId">ID da empresa emissora</param>
        /// <param name="request">Filtros da consulta</param>
        [HttpPost("{empresaId:int}/consultar")]
        [ProducesResponseType(typeof(ConsultarNfseResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ConsultarNfseResponse>> ConsultarNfse(
            int empresaId,
            [FromBody] ConsultarNfseRequest request)
        {
            _logger.LogInformation(
                "Requisição para consultar NFSe - Empresa: {EmpresaId}",
                empresaId);

            var response = await _nfseService.ConsultarNfseAsync(empresaId, request);

            return Ok(response);
        }

        /// <summary>
        /// Cancela uma NFSe
        /// </summary>
        /// <param name="empresaId">ID da empresa emissora</param>
        /// <param name="request">Dados do cancelamento</param>
        [HttpPost("{empresaId:int}/cancelar")]
        [ProducesResponseType(typeof(CancelarNfseResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<CancelarNfseResponse>> CancelarNfse(
            int empresaId,
            [FromBody] CancelarNfseRequest request)
        {
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
        /// </summary>
        /// <param name="empresaId">ID da empresa emissora</param>
        /// <param name="request">Dados da substituição</param>
        [HttpPost("{empresaId:int}/substituir")]
        [ProducesResponseType(typeof(SubstituirNfseResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<SubstituirNfseResponse>> SubstituirNfse(
            int empresaId,
            [FromBody] SubstituirNfseRequest request)
        {
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