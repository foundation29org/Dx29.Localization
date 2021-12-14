using System;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Dx29.Services;

namespace Dx29.Localization
{
    static public class ServiceConfiguration
    {
        static public void AddAppServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddTranslation(configuration);
            services.AddLocalization(configuration);
        }

        static public void AddTranslation(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<TranslationService>();
            services.AddHttpClient<TranslationService>(http =>
            {
                http.BaseAddress = new Uri(configuration["CognitiveServices:Endpoint"]);
                http.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", configuration["CognitiveServices:Authorization"]);
                http.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Region", configuration["CognitiveServices:Region"]);
            });
        }

        static public void AddLocalization(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton(new BlobStorage(configuration.GetConnectionString("BlobStorage")));
            services.AddSingleton<LocalizationService>();
        }
    }
}
