using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Data.SqlClient;
using Dapper;
using System.Data;
using Microsoft.Extensions.Configuration;

namespace Demo.AzFunctions
{
    public static class Function1
    {
        private static IConfiguration Configuration { set; get; }

        static Function1()
        {
            var builder = new ConfigurationBuilder();
            var config = builder
            .SetBasePath(Environment.CurrentDirectory)
            .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();

            Configuration = config;
        }

        [FunctionName("HttpTriggeredFunction")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string name = req.Query["name"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            name = name ?? data?.name;

            return name != null
                ? (ActionResult)new OkObjectResult($"Hello, {name}")
                : new BadRequestObjectResult("Please pass a name on the query string or in the request body");
        }

        [FunctionName("TimerTriggeredFunction1")]
        public static async Task Handle1(
            [TimerTrigger("*/15 7-18 * * *")] TimerInfo timer, ILogger logger)
        {
            if (Convert.ToBoolean(Configuration["keep-db-alive"]))
            {
                logger.LogTrace("Executing logic to keep the db alive.");

                using (var connection = new SqlConnection(Configuration["connection-string"]))
                {
                    connection.Open();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandType = CommandType.Text;
                        command.CommandText = "SELECT 1";
                        var result = (int)await command.ExecuteScalarAsync().ConfigureAwait(false);
                    }
                }
            }
        }

        [FunctionName("HttpTriggeredFunction2")]
        public static async Task<IActionResult> Handle2(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            using (var connection = new SqlConnection(Configuration["connection-string"]))
            {
                var rules = await connection.QueryAsync<WorkflowRule>("select * from workflowRules");
                return new OkObjectResult(rules);
            }
        }

        public class WorkflowRule
        {
            public int WorkFlowRuleId { get; set; }
            public string WorkFlowRules { get; set; }
        }
    }
}
