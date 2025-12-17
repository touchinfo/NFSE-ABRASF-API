using NFSE_ABRASF.Services.Interfaces;

namespace NFSE_ABRASF.Services
{
    public class AdminAuthService : IAdminAuthService
    {
        private readonly string _adminPassword;
        private readonly ILogger<AdminAuthService> _logger;

        public AdminAuthService(IConfiguration configuration, ILogger<AdminAuthService> logger)
        {
            _adminPassword = configuration["ADMIN_PASSWORD"]
                ?? Environment.GetEnvironmentVariable("ADMIN_PASSWORD")
                ?? throw new InvalidOperationException("ADMIN_PASSWORD não configurada. Defina a variável de ambiente ou adicione no appsettings.json");

            _logger = logger;
        }

        public bool ValidarSenhaAdmin(string senha)
        {
            if (string.IsNullOrEmpty(senha))
            {
                _logger.LogWarning("Tentativa de acesso admin sem senha");
                return false;
            }

            var valido = senha == _adminPassword;

            if (!valido)
            {
                _logger.LogWarning("Tentativa de acesso admin com senha inválida");
            }

            return valido;
        }
    }
}