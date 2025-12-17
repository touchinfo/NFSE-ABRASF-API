using Microsoft.EntityFrameworkCore;
using NFSE_ABRASF.Data.Context;

namespace NFSE_ABRASF.Middleware
{
    /// <summary>
    /// Middleware de autenticação por API Key para rotas de NFSe
    /// Rotas de empresas usam AdminPassword, rotas de NFSe usam API Key
    /// </summary>
    public class ApiKeyAuthMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ApiKeyAuthMiddleware> _logger;
        private const string API_KEY_HEADER = "X-Api-Key";

        public ApiKeyAuthMiddleware(RequestDelegate next, ILogger<ApiKeyAuthMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, AppDbContext dbContext)
        {
            var path = context.Request.Path;

            // Se não for rota de NFSe, deixa passar (outras rotas têm sua própria autenticação)
            if (!IsNFSeEndpoint(path))
            {
                await _next(context);
                return;
            }

            // Rota de listagem de municípios é pública
            if (path.StartsWithSegments("/v1/nfse/municipios", StringComparison.OrdinalIgnoreCase))
            {
                await _next(context);
                return;
            }

            // Verificar se o header X-Api-Key foi fornecido
            if (!context.Request.Headers.TryGetValue(API_KEY_HEADER, out var extractedApiKey))
            {
                _logger.LogWarning("Tentativa de acesso à rota NFSe sem API Key: {Path}", path);
                context.Response.StatusCode = 401;
                await context.Response.WriteAsJsonAsync(new
                {
                    statusCode = 401,
                    message = "API Key não fornecida. Adicione o header 'X-Api-Key'.",
                    traceId = context.TraceIdentifier
                });
                return;
            }

            var apiKey = extractedApiKey.ToString();

            // Buscar empresa pela API Key
            var empresa = await dbContext.Empresas!
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Api_Key_Ativa == apiKey);

            // Verificar se a API Key existe
            if (empresa == null)
            {
                _logger.LogWarning("Tentativa de acesso com API Key inválida: {ApiKey}",
                    apiKey.Length > 8 ? apiKey[..8] + "..." : apiKey);
                context.Response.StatusCode = 401;
                await context.Response.WriteAsJsonAsync(new
                {
                    statusCode = 401,
                    message = "API Key inválida.",
                    traceId = context.TraceIdentifier
                });
                return;
            }

            // Verificar se a empresa está ativa
            if (!empresa.Ativa)
            {
                _logger.LogWarning("Tentativa de acesso com empresa inativa. EmpresaId: {EmpresaId}, CNPJ: {Cnpj}",
                    empresa.EmpresaId, empresa.Cnpj);
                context.Response.StatusCode = 403;
                await context.Response.WriteAsJsonAsync(new
                {
                    statusCode = 403,
                    message = "Empresa inativa. Entre em contato com o administrador para reativar sua conta.",
                    traceId = context.TraceIdentifier
                });
                return;
            }

            // Verificar se o certificado está válido (aviso, não bloqueia)
            if (empresa.Certificado_Validade.HasValue && empresa.Certificado_Validade.Value < DateTime.Now)
            {
                _logger.LogWarning("Empresa com certificado vencido tentando acessar NFSe. EmpresaId: {EmpresaId}",
                    empresa.EmpresaId);
            }

            // Adicionar informações da empresa no contexto para uso posterior
            context.Items["EmpresaId"] = empresa.EmpresaId;
            context.Items["Cnpj"] = empresa.Cnpj;
            context.Items["CodigoMunicipio"] = empresa.Codigo_Municipio;

            _logger.LogDebug("Acesso autorizado via API Key. EmpresaId: {EmpresaId}", empresa.EmpresaId);

            await _next(context);
        }

        /// <summary>
        /// Verifica se a rota é de NFSe (requer autenticação por API Key)
        /// </summary>
        private static bool IsNFSeEndpoint(PathString path)
        {
            return path.StartsWithSegments("/v1/nfse", StringComparison.OrdinalIgnoreCase);
        }
    }

    public static class ApiKeyAuthMiddlewareExtensions
    {
        public static IApplicationBuilder UseApiKeyAuth(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ApiKeyAuthMiddleware>();
        }
    }
}