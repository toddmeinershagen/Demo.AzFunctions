using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Demo.AzFunctions.PerfDriver
{
    class Program
    {
        private static Lazy<HttpClient> Client = new Lazy<HttpClient>(() =>
        {
            var client = new HttpClient();
            client.BaseAddress = new Uri("https://demo-azfunction.azurewebsites.net/");
            client.DefaultRequestHeaders.Accept.Clear();
            return client;
         });  

        static async Task Main(string[] args)
        {
            while (true)
            {
                var tasks = new List<Task>();
                Enumerable.Range(1, 20).ToList()
                    .ForEach(x => tasks.Add(Task.Run(CallFunction)));

                await Task.WhenAll(tasks);
            }
        }

        private static async Task CallFunction()
        {
            var watch = Stopwatch.StartNew();
            var response = await Client.Value.GetAsync("api/Function1?name=Todd");
            watch.Stop();

            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadAsStringAsync();
            Debug.Assert(result == "Hello, Todd");

            var duration = watch.ElapsedMilliseconds;
            await Console.Out.WriteLineAsync($"The duration was {duration} ms.");
        }
    }
}
