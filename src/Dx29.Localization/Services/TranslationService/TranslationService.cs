using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

using Dx29.Models;

namespace Dx29.Services
{
    public class TranslationService
    {
        const string TRANSLATE_QUERYSTRING = "translate?api-version=3.0&from=en&to={0}";

        public TranslationService(HttpClient httpClient)
        {
            HttpClient = httpClient;
        }

        public HttpClient HttpClient { get; }

        public async Task<string> TranslateAsync(string text, string language)
        {
            var lan = NormalizeLanguage(language);
            if (lan != "en")
            {
                var obj = new[] { new { Text = text } };
                var path = String.Format(TRANSLATE_QUERYSTRING, lan);
                var resp = await HttpClient.POSTAsync<TranslationModel[]>(path, obj);
                return resp.First().translations.First().text;
            }
            return text;
        }

        private string NormalizeLanguage(string language)
        {
            switch (language.ToLowerInvariant())
            {
                case "es":
                case "es-es":
                    return "es";
                case "fr":
                case "fr-fr":
                    return "fr";
                default:
                    return "en";
            }
        }
    }
}
