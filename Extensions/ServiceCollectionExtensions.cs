using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.EntityFrameworkCore;
using NFSE_ABRASF.Data.Context;
using NFSE_ABRASF.Repositories;
using NFSE_ABRASF.Repositories.Interfaces;
using NFSE_ABRASF.Services;
using NFSE_ABRASF.Services.Interfaces;
using NFSE_ABRASF.Validators;

namespace NFSE_ABRASF.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            // Registrar Services
            services.AddScoped<IEmpresaService, EmpresaService>();
            services.AddScoped<ICertificadoService, CertificadoService>();

            // Registrar Repositories
            services.AddScoped<IEmpresaRepository, EmpresaRepository>();

            return services;
        }

        public static IServiceCollection AddFluentValidationConfig(this IServiceCollection services)
        {
            services.AddFluentValidationAutoValidation()
                    .AddFluentValidationClientsideAdapters();

            services.AddValidatorsFromAssemblyContaining<CriarEmpresaDtoValidator>();

            return services;
        }

        public static IServiceCollection AddDatabaseConfiguration(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            services.AddDbContext<AppDbContext>(options =>
                options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

            return services;
        }

        public static IServiceCollection AddSecurityConfiguration(this IServiceCollection services)
        {
            // Adicionar Data Protection para criptografia
            services.AddDataProtection();

            // Configurar CORS se necessário
            services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", builder =>
                {
                    builder.AllowAnyOrigin()
                           .AllowAnyMethod()
                           .AllowAnyHeader();
                });
            });

            return services;
        }

        public static IServiceCollection AddSwaggerConfiguration(this IServiceCollection services)
        {
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
                {
                    Title = "NFSE ABRASF API",
                    Version = "v1",
                    Description = "API para gerenciamento de empresas e emissão de NFSe",
                    Contact = new Microsoft.OpenApi.Models.OpenApiContact
                    {
                        Name = "Suporte",
                        Email = "suporte@exemplo.com"
                    }
                });

                // Adicionar autenticação via API Key no Swagger
                c.AddSecurityDefinition("ApiKey", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Description = "API Key necessária para acessar os endpoints. Exemplo: 'X-Api-Key: sua-api-key'",
                    Name = "X-Api-Key",
                    In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                    Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
                    Scheme = "ApiKeyScheme"
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
                    }
                });
            });

            return services;
        }
    }
}