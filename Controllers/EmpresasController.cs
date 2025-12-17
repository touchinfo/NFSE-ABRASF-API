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
        /// Lista todas as empresas com paginação
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<EmpresaResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<IEnumerable<EmpresaResponseDto>>> ObterTodas(
            [FromQuery] int pagina = 1,
            [FromQuery] int itensPorPagina = 10)
        {
            if (pagina < 1) pagina = 1;
            if (itensPorPagina < 1 || itensPorPagina > 100) itensPorPagina = 10;

            var empresas = await _empresaService.ObterTodasAsync(pagina, itensPorPagina);

            if (!empresas.Any())
                return NotFound(new { message = "Nenhuma empresa encontrada." });

            return Ok(empresas);
        }

        /// <summary>
        /// Obtém uma empresa por ID
        /// </summary>
        [HttpGet("{id:int}", Name = "ObterEmpresa")]
        [ProducesResponseType(typeof(EmpresaResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<EmpresaResponseDto>> ObterPorId(int id)
        {
            var empresa = await _empresaService.ObterPorIdAsync(id);
            return Ok(empresa);
        }

        /// <summary>
        /// Cria uma nova empresa (requer senha de administrador)
        /// </summary>
        [HttpPost("criar")]
        [ProducesResponseType(typeof(EmpresaResponseDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Criar([FromForm] CriarEmpresaDto dto)
        {
            // Validar senha admin
            if (!_adminAuthService.ValidarSenhaAdmin(dto.AdminPassword!))
            {
                _logger.LogWarning("Tentativa de criar empresa com senha admin inválida");
                return Unauthorized(new { message = "Senha de administrador inválida." });
            }

            var empresa = await _empresaService.CriarAsync(dto);

            _logger.LogInformation("Empresa {EmpresaId} criada com sucesso", empresa.EmpresaId);

            return CreatedAtRoute("ObterEmpresa", new { id = empresa.EmpresaId }, empresa);
        }

        /// <summary>
        /// Atualiza uma empresa existente (requer senha de administrador)
        /// </summary>
        [HttpPut("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Atualizar(int id, [FromForm] CriarEmpresaDto dto)
        {
            // Validar senha admin
            if (!_adminAuthService.ValidarSenhaAdmin(dto.AdminPassword!))
            {
                _logger.LogWarning("Tentativa de atualizar empresa com senha admin inválida");
                return Unauthorized(new { message = "Senha de administrador inválida." });
            }

            var sucesso = await _empresaService.AtualizarAsync(id, dto);

            if (!sucesso)
                return NotFound(new { message = $"Empresa com ID {id} não encontrada." });

            _logger.LogInformation("Empresa {EmpresaId} atualizada com sucesso", id);

            return NoContent();
        }

        /// <summary>
        /// Remove uma empresa (requer senha de administrador)
        /// </summary>
        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Deletar(int id, [FromQuery] string adminPassword)
        {
            // Validar senha admin
            if (!_adminAuthService.ValidarSenhaAdmin(adminPassword))
            {
                _logger.LogWarning("Tentativa de deletar empresa com senha admin inválida");
                return Unauthorized(new { message = "Senha de administrador inválida." });
            }

            var sucesso = await _empresaService.DeletarAsync(id);

            if (!sucesso)
                return NotFound(new { message = $"Empresa com ID {id} não encontrada." });

            _logger.LogInformation("Empresa {EmpresaId} deletada com sucesso", id);

            return NoContent();
        }
    }
}