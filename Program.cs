using Microsoft.EntityFrameworkCore;
using NFSE_ABRASF.Repositories;
using NFSE_ABRASF.Repositories.Interfaces;
using NFSE_ABRASF.Services;
using NFSE_ABRASF.Services.Interfaces;
using FluentValidation;
using FluentValidation.AspNetCore;
using NFSE_ABRASF.Validators;
using NFSE_ABRASF.Middleware;
using NFSE_ABRASF.Extensions;
using System.Text.Json.Serialization;
using NFSE_ABRASF.Data.Context;
using NFSE_ABRASF.Services.NFSe;

var builder = WebApplication.CreateBuilder(args);

// Controllers
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

// Banco de Dados
var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING_2")
    ?? builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException(
        "Connection string não encontrada. Configure a variável de ambiente 'DB_CONNECTION_STRING_2' " +
        "ou defina 'ConnectionStrings:DefaultConnection' no appsettings.json");
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// Admin Password
var adminPassword = Environment.GetEnvironmentVariable("ADMIN_PASSWORD");
if (!string.IsNullOrEmpty(adminPassword))
{
    builder.Configuration["AdminPassword"] = adminPassword;
}

// Services
builder.Services.AddScoped<IEmpresaService, EmpresaService>();
builder.Services.AddScoped<ICertificadoService, CertificadoService>();
builder.Services.AddSingleton<IAdminAuthService, AdminAuthService>();

// Repositories
builder.Services.AddScoped<IEmpresaRepository, EmpresaRepository>();

// ===== SERVIÇOS NFSe =====
builder.Services.AddNFSeServices();

// FluentValidation
builder.Services.AddFluentValidationAutoValidation()
                .AddFluentValidationClientsideAdapters();
builder.Services.AddValidatorsFromAssemblyContaining<CriarEmpresaDtoValidator>();

// Data Protection (necessário para criptografia de senha do certificado)
builder.Services.AddDataProtection();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "NFSE ABRASF API",
        Version = "v1",
        Description = @"API para gerenciamento de empresas e emissão de NFSe - Padrão ABRASF

## Autenticação

Esta API utiliza dois métodos de autenticação:

### 1. Rotas de Empresas (`/v1/empresas/*`)
- Use o header `X-Admin-Password` com a senha de administrador
- Exemplo: `X-Admin-Password: sua-senha-admin`

### 2. Rotas de NFSe (`/v1/nfse/*`)
- Use o header `X-Api-Key` com a API Key da empresa
- A API Key é gerada automaticamente ao criar uma empresa
- Se a empresa estiver inativa, a API Key não funcionará
- Exemplo: `X-Api-Key: sua-api-key`

### Rota Pública
- `GET /v1/nfse/municipios` - Lista municípios disponíveis (não requer autenticação)"
    });

    // Autenticação via API Key (para rotas de NFSe)
    c.AddSecurityDefinition("ApiKey", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "API Key para rotas de NFSe. Obtida ao criar uma empresa.",
        Name = "X-Api-Key",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "ApiKeyScheme"
    });

    // Autenticação via Admin Password (para rotas de Empresas)
    c.AddSecurityDefinition("AdminPassword", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "Senha de administrador para rotas de gerenciamento de empresas.",
        Name = "X-Admin-Password",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "AdminPasswordScheme"
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "ApiKey"
                }
            },
            Array.Empty<string>()
        },
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "AdminPassword"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// ===== ORDEM DOS MIDDLEWARES É IMPORTANTE =====

// 1. Tratamento de erros (primeiro para capturar exceções de todos os middlewares)
app.UseErrorHandling();

// 2. Swagger (antes da autenticação para ser acessível)
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "NFSE ABRASF API v1");
    c.RoutePrefix = "swagger";
});

// 3. HTTPS e CORS
app.UseHttpsRedirection();
app.UseCors("AllowAll");

// 4. Autenticação por API Key (protege rotas de NFSe)
app.UseApiKeyAuth();

// 5. Authorization padrão do ASP.NET
app.UseAuthorization();

// 6. Mapear controllers
app.MapControllers();

// Health e Redirect
app.MapGet("/health", () => new { status = "Healthy", timestamp = DateTime.Now });
app.MapGet("/", () => Results.Redirect("/swagger"));

Console.WriteLine("==============================================");
Console.WriteLine("🚀 API NFSE ABRASF INICIADA");
Console.WriteLine("==============================================");
Console.WriteLine($"📄 Swagger: https://localhost:7064/swagger");
Console.WriteLine($"📄 Swagger: http://localhost:5000/swagger");
Console.WriteLine($"🏥 Health: https://localhost:7064/health");
Console.WriteLine("----------------------------------------------");
Console.WriteLine($"🔌 DB conectada: {!string.IsNullOrEmpty(connectionString)}");
Console.WriteLine($"🔑 Admin password configurada: {!string.IsNullOrEmpty(adminPassword)}");
Console.WriteLine("==============================================");
Console.WriteLine("🔐 AUTENTICAÇÃO:");
Console.WriteLine("   - Rotas /v1/empresas/* → AdminPassword (no body)");
Console.WriteLine("   - Rotas /v1/nfse/* → API Key (header X-Api-Key)");
Console.WriteLine("==============================================");
Console.WriteLine("📍 Municípios disponíveis: Santos/SP (GISS)");
Console.WriteLine("==============================================");

app.Run();