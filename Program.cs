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

// ===== SERVIÇOS NFSe - ADICIONE ESTA LINHA =====
builder.Services.AddNFSeServices();
// ===============================================

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
        Description = "API para gerenciamento de empresas e emissão de NFSe - Padrão ABRASF"
    });
});

var app = builder.Build();

// Middlewares
app.UseErrorHandling();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "NFSE ABRASF API v1");
    c.RoutePrefix = "swagger";
});

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthorization();
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
Console.WriteLine("📍 Municípios disponíveis: Santos/SP (GISS)");
Console.WriteLine("==============================================");

app.Run();
