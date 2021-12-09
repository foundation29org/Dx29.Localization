using System;
using System.Net.Http;
using System.Threading.Tasks;

using Dx29.Services;

namespace Sample.Fixtures
{
    class TranslationTests
    {
        const string ENDPOINT = "https://api.cognitive.microsofttranslator.com";
        const string AUTH_KEY = "89261090473f49c0be59bc4f71f3e59d";

        static public async Task RunAsync()
        {
            var http = new HttpClient
            {
                BaseAddress = new Uri(ENDPOINT)
            };
            http.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", AUTH_KEY);

            var svc = new TranslationService(http);
            var res = await svc.TranslateAsync("Total {0} items", "es-es");
            Console.WriteLine(res);
        }
    }
}
