using NFSE_ABRASF.Services.NFSe;
using NFSE_ABRASF.Services.NFSe.Interfaces;
using NFSE_ABRASF.Services.NFSe.Providers;

namespace NFSE_ABRASF.Extensions
{
    public static class NFSeServiceCollectionExtensions
    {
        /// <summary>
        /// Adiciona todos os serviços necessários para NFSe
        /// </summary>
        public static IServiceCollection AddNFSeServices(this IServiceCollection services)
        {
            // Factory principal
            services.AddScoped<INFSeProviderFactory, NFSeProviderFactory>();

            // Serviço de orquestração
            services.AddScoped<INFSeService, NFSeService>();

            // Registrar todos os provedores de municípios
            services.AddNFSeProviders();

            return services;
        }

        /// <summary>
        /// Registra todos os provedores de NFSe disponíveis
        /// Adicione novos provedores aqui conforme implementados
        /// </summary>
        private static IServiceCollection AddNFSeProviders(this IServiceCollection services)
        {
            // Santos/SP - GISS
            services.AddScoped<SantosNFSeProvider>();
            services.AddScoped<INFSeProvider, SantosNFSeProvider>();

            // Adicione mais provedores conforme implementados:
            // services.AddScoped<SaoPauloNFSeProvider>();
            // services.AddScoped<INFSeProvider, SaoPauloNFSeProvider>();

            return services;
        }
    }
}