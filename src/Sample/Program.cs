using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

using Dx29;

namespace Sample
{
    class Program
    {
        const string ENDPOINT = "https://localhost:5108/api/v1/";

        static async Task Main(string[] args)
        {
            var http = new HttpClient
            {
                BaseAddress = new Uri(ENDPOINT)
            };

            //await SyncTestsAsync(http);

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            // Begin timer
            await ConcurrentTestsAsync(http);
            // End timer
            stopwatch.Stop(); Console.WriteLine(stopwatch.Elapsed.TotalSeconds);
        }

        private static async Task SyncTestsAsync(HttpClient http)
        {
            // Delete Literals
            await http.DELETEAsync("Localization/literal?key=Hello world!&lang=fr");
            await http.DELETEAsync("Localization/literal?key=Hello world!!!&lang=fr");

            // WARNING: Remove all literals
            //await http.PUTAsync("Localization/literals?lang=fr", new Dictionary<string, string>());

            // Get Literals
            var dic = await http.GETAsync<IDictionary<string, string>>("Localization/literals?lang=fr");
            Console.WriteLine(dic.Serialize());

            // Register Literal
            var keyValue = new KeyValuePair<string, string>("Hello world!", null);
            var resp = await http.PUTAsync("Localization/register?lang=fr", keyValue);
            Console.WriteLine(resp);

            // Set Literal
            keyValue = new KeyValuePair<string, string>("Hello world!!!", "Salut tout le monde!!!");
            resp = await http.PUTAsync("Localization/literal?lang=fr", keyValue);
            Console.WriteLine(resp);

            // Get Literals
            dic = await http.GETAsync<IDictionary<string, string>>("Localization/literals?lang=fr");
            Console.WriteLine(dic.Serialize());
        }

        private static async Task ConcurrentTestsAsync(HttpClient http)
        {
            // WARNING: Remove all literals
            //await http.PUTAsync("Localization/literals?lang=fr", new Dictionary<string, string>());

            var tasks = new List<Task>();
            for (int n = 0; n < 10; n++)
            {
                var httpLocal = new HttpClient
                {
                    BaseAddress = new Uri(ENDPOINT)
                };
                var keyValue = new KeyValuePair<string, string>($"Hello {n}", $"Salut {n}!!!");
                var task = httpLocal.PUTAsync("Localization/literal?lang=fr", keyValue);
                tasks.Add(task);
            }
            Task.WaitAll(tasks.ToArray(), 60_000);

            // Get Literals
            var dic = await http.GETAsync<IDictionary<string, string>>("Localization/literals?lang=fr");
            Console.WriteLine(dic.Serialize());
        }
    }
}
