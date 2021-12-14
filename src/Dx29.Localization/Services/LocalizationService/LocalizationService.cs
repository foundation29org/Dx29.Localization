using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Azure.Storage.Blobs.Models;

namespace Dx29.Services
{
    public class LocalizationService
    {
        const string CONTAINER = "literals";

        public LocalizationService(TranslationService translationService, BlobStorage blobStorage)
        {
            TranslationService = translationService;
            BlobStorage = blobStorage;
        }

        public TranslationService TranslationService { get; }
        public BlobStorage BlobStorage { get; }

        public async Task<IDictionary<string, string>> GetLiteralsAsync(string language)
        {
            language = NormalizeLanguage(language);
            string path = GetLiteralsPath(language);
            var dic = await BlobStorage.DownloadObjectAsync<IDictionary<string, string>>(CONTAINER, path);
            dic ??= new Dictionary<string, string>();
            return dic;
        }

        public async Task<string> GetLiteralAsync(string language, string key)
        {
            var dic = await GetLiteralsAsync(language);
            return dic.TryGetValue(key);
        }

        public async Task SetLiteralsAsync(string language, IDictionary<string, string> literals, BlobRequestConditions conditions = null)
        {
            language = NormalizeLanguage(language);
            string path = GetLiteralsPath(language);
            await BlobStorage.UploadObjectAsync(CONTAINER, path, literals, conditions);
        }

        public async Task SetLiteralAsync(string language, KeyValuePair<string, string> literal)
        {
            await SetLiteralAsync(language, literal.Key, literal.Value);
        }
        public async Task SetLiteralAsync(string language, string key, string value)
        {
            language = NormalizeLanguage(language);
            string path = GetLiteralsPath(language);

            var leaseClient = BlobStorage.GetLeaseClient(CONTAINER, path);
            var lease = await BlobStorage.AcquireLeaseAsync(leaseClient);
            var conditions = new BlobRequestConditions { LeaseId = lease.LeaseId };
            var dic = await GetLiteralsAsync(language);
            dic[key] = value;
            await SetLiteralsAsync(language, dic, conditions);
            await leaseClient.ReleaseAsync();
        }

        public async Task<string> RegisterLiteralAsync(string language, string key)
        {
            var dic = await GetLiteralsAsync(language);
            if (!dic.ContainsKey(key))
            {
                string value = await TranslationService.TranslateAsync(key, language);
                await SetLiteralAsync(language, key, value);
                return value;
            }
            return dic[key];
        }

        public async Task DeleteLiteralAsync(string language, string key)
        {
            var dic = await GetLiteralsAsync(language);
            if (dic.ContainsKey(key))
            {
                dic.Remove(key);
                await SetLiteralsAsync(language, dic);
            }
        }

        private string GetLiteralsPath(string language) => $"{language}.json";

        private string NormalizeLanguage(string language)
        {
            switch (language.ToLowerInvariant())
            {
                case "es":
                case "es-es":
                    return "es-es";
                case "fr":
                case "fr-fr":
                    return "fr-fr";
                default:
                    return "en-us";
            }
        }
    }
}
