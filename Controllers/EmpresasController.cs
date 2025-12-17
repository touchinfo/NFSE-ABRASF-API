using Microsoft.AspNetCore.Mvc;
using NFSE_ABRASF.DTOs.Requests;
using NFSE_ABRASF.DTOs.Responses;
using NFSE_ABRASF.Services.Interfaces;

namespace NFSE_ABRASF.Controllers
{
    [Route("v1/empresas")]
    [ApiController]
    public class EmpresasController : ControllerBase
    {
        private readonly IEmpresaService _empresaService;
        private readonly IAdminAuthService _adminAuthService;
        private readonly ILogger<EmpresasController> _logger;

        public EmpresasController(
            IEmpresaService empresaService,
            IAdminAuthService adminAuthService,
            ILogger<EmpresasController> logger)
        {
            _empresaService = empresaService;
            _adminAuthService = adminAuthService;
            _logger = logger;
        }

        /// <summary>
        /// Valida a senha de administrador e retorna Unauthorized se inválida
        /// </summary>
        private ActionResult? ValidarAdminPassword(string? adminPassword, string operacao)
        {
            if (string.IsNullOrEmpty(adminPassword))
            {
                _logger.LogWarning("Tentativa de {Operacao} sem senha admin", operacao);
                return Unauthorized(new { message = "Senha de administrador é obrigatória. Use o header 'X-Admin-Password'." });
            }

            if (!_adminAuthService.ValidarSenhaAdmin(adminPassword))
            {
                _logger.LogWarning("Tentativa de {Operacao} com senha admin inválida", operacao);
                return Unauthorized(new { message = "Senha de administrador inválida." });
            }

            return null;
        }

        /// <summary>
        /// Lista todas as empresas com paginação
        /// Requer header X-Admin-Password
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<EmpresaResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<IEnumerable<EmpresaResponseDto>>> ObterTodas(
            [FromHeader(Name = "X-Admin-Password")] string? adminPassword,
            [FromQuery] int pagina = 1,
            [FromQuery] int itensPorPagina = 10)
        {
            var authResult = ValidarAdminPassword(adminPassword, "listar empresas");
            if (authResult != null) return authResult;

            if (pagina < 1) pagina = 1;
            if (itensPorPagina < 1 || itensPorPagina > 100) itensPorPagina = 10;

            var empresas = await _empresaService.ObterTodasAsync(pagina, itensPorPagina);

            if (!empresas.Any())
                return NotFound(new { message = "Nenhuma empresa encontrada." });

            return Ok(empresas);
        }

        /// <summary>
        /// Obtém uma empresa por ID
        /// Requer header X-Admin-Password
        /// </summary>
        [HttpGet("{id:int}", Name = "ObterEmpresa")]
        [ProducesResponseType(typeof(EmpresaResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<EmpresaResponseDto>> ObterPorId(
            int id,
            [FromHeader(Name = "X-Admin-Password")] string? adminPassword)
        {
            var authResult = ValidarAdminPassword(adminPassword, "consultar empresa");
            if (authResult != null) return authResult;

            var empresa = await _empresaService.ObterPorIdAsync(id);
            return Ok(empresa);
        }

        /// <summary>
        /// Cria uma nova empresa
        /// Requer header X-Admin-Password
        /// </summary>
        [HttpPost("criar")]
        [ProducesResponseType(typeof(CriarEmpresaResponseDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Criar(
            [FromHeader(Name = "X-Admin-Password")] string? adminPassword,
            [FromForm] CriarEmpresaDto dto)
        {
            var authResult = ValidarAdminPassword(adminPassword, "criar empresa");
            if (authResult != null) return authResult;

            var empresa = await _empresaService.CriarAsync(dto);

            _logger.LogInformation("Empresa {EmpresaId} criada com sucesso", empresa.EmpresaId);

            return CreatedAtRoute("ObterEmpresa", new { id = empresa.EmpresaId }, empresa);
        }

        /// <summary>
        /// Atualiza uma empresa existente
        /// Requer header X-Admin-Password
        /// </summary>
        [HttpPut("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Atualizar(
            int id,
            [FromHeader(Name = "X-Admin-Password")] string? adminPassword,
            [FromForm] CriarEmpresaDto dto)
        {
            var authResult = ValidarAdminPassword(adminPassword, "atualizar empresa");
            if (authResult != null) return authResult;

            var sucesso = await _empresaService.AtualizarAsync(id, dto);

            if (!sucesso)
                return NotFound(new { message = $"Empresa com ID {id} não encontrada." });

            _logger.LogInformation("Empresa {EmpresaId} atualizada com sucesso", id);

            return NoContent();
        }

        /// <summary>
        /// Remove uma empresa
        /// Requer header X-Admin-Password
        /// </summary>
        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Deletar(
            int id,
            [FromHeader(Name = "X-Admin-Password")] string? adminPassword)
        {
            var authResult = ValidarAdminPassword(adminPassword, "deletar empresa");
            if (authResult != null) return authResult;

            var sucesso = await _empresaService.DeletarAsync(id);

            if (!sucesso)
                return NotFound(new { message = $"Empresa com ID {id} não encontrada." });

            _logger.LogInformation("Empresa {EmpresaId} deletada com sucesso", id);

            return NoContent();
        }

        /// <summary>
        /// Ativa ou desativa uma empresa
        /// Quando desativada, a empresa não consegue mais usar a API Key para emitir NFSe
        /// Requer header X-Admin-Password
        /// </summary>
        [HttpPatch("{id:int}/status")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> AlterarStatus(
            int id,
            [FromHeader(Name = "X-Admin-Password")] string? adminPassword,
            [FromQuery] bool ativa)
        {
            var authResult = ValidarAdminPassword(adminPassword, "alterar status empresa");
            if (authResult != null) return authResult;

            var sucesso = await _empresaService.AlterarStatusAsync(id, ativa);

            if (!sucesso)
                return NotFound(new { message = $"Empresa com ID {id} não encontrada." });

            _logger.LogInformation("Status da empresa {EmpresaId} alterado para {Status}", id, ativa ? "Ativa" : "Inativa");

            return Ok(new
            {
                message = $"Empresa {(ativa ? "ativada" : "desativada")} com sucesso.",
                empresaId = id,
                ativa = ativa
            });
        }
    }
}