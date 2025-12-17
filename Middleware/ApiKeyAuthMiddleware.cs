using Microsoft.EntityFrameworkCore;
using NFSE_ABRASF.Data.Context;

namespace NFSE_ABRASF.Middleware
{
    public class ApiKeyAuthMiddleware
    {
        private readonly RequestDelegate _next;
        private const string API_KEY_HEADER = "X-Api-Key";

        public ApiKeyAuthMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, AppDbContext dbContext)
        {
            // Ignorar verificação para endpoints públicos
            if (IsPublicEndpoint(context.Request.Path))
            {
                await _next(context);
                return;
            }

            if (!context.Request.Headers.TryGetValue(API_KEY_HEADER, out var extractedApiKey))
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsJsonAsync(new
                {
                    message = "API Key não fornecida. Adicione o header 'X-Api-Key'."
                });
                return;
            }

            var apiKey = extractedApiKey.ToString();

            var empresa = await dbContext.Empresas!
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Api_Key_Ativa == apiKey && e.Ativa);

            if (empresa == null)
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsJsonAsync(new
                {
                    message = "API Key inválida ou empresa inativa."
                });
                return;
            }

            // Adicionar informações da empresa no contexto para uso posterior
            context.Items["EmpresaId"] = empresa.EmpresaId;
            context.Items["Cnpj"] = empresa.Cnpj;

            await _next(context);
        }

        private bool IsPublicEndpoint(PathString path)
        {
            // Lista de endpoints que não precisam de autenticação
            var publicPaths = new[]
            {
                "/swagger",
                "/health",
                "/v1/empresas/criar", // Criação de empresa é pública
            };

            return publicPaths.Any(p => path.StartsWithSegments(p, StringComparison.OrdinalIgnoreCase));
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