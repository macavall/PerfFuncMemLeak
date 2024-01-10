using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;
using PerfTestDLL;
using System;

namespace DurableMemory
{
    public static class durable
    {
        [Function(nameof(durable))]
        public static async Task<string> RunOrchestrator(
            [OrchestrationTrigger] TaskOrchestrationContext context)
        {
            ILogger logger = context.CreateReplaySafeLogger(nameof(durable));
            logger.LogInformation("Saying hello.");
            var outputs = new List<string>();

            // Replace name and input with values relevant for your Durable Functions Activity
            //outputs.Add(await context.CallActivityAsync<string>(nameof(SayHello), "Tokyo"));
            //outputs.Add(await context.CallActivityAsync<string>(nameof(SayHello), "Seattle"));
            //outputs.Add(await context.CallActivityAsync<string>(nameof(SayHello), "London"));
            //outputs.Add(await context.CallActivityAsync<string>(nameof(FinalActivity), "Complete"));

            try
            {
                var x = await context.CallActivityAsync<string>("SayHello", "first");
                var y = await context.CallActivityAsync<string>("SayHello", x);
                var z = await context.CallActivityAsync<string>("SayHello", y);
                return await context.CallActivityAsync<string>("FinalActivity", z);
            }
            catch (Exception)
            {
                // Error handling or compensation goes here.
                return "Error";
            }

            //return outputs;
        }

        [Function(nameof(SayHello))]
        public static string SayHello([ActivityTrigger] string name, FunctionContext executionContext)
        {
            ILogger logger = executionContext.GetLogger("SayHello");
            logger.LogInformation("Saying hello to {name}.", name);

            _ = Task.Factory.StartNew(async () =>
            {
                await MemoryClass.AddMemory(50, 10);
            });

            logger.LogInformation(MemoryClass.GetMemory()) ;

            return $"Hello {name}!";
        }

        [Function(nameof(FinalActivity))]
        public static string FinalActivity([ActivityTrigger] string input, FunctionContext executionContext)
        {
            ILogger logger = executionContext.GetLogger("FinalActivity: Clearing Memory Used by Orchestration");
            logger.LogInformation(input);

            MemoryClass.ClearMemory();

            return input;
        }

        [Function("durable_HttpStart")]
        public static async Task<HttpResponseData> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req,
            [DurableClient] DurableTaskClient client,
            FunctionContext executionContext)
        {
            ILogger logger = executionContext.GetLogger("durable_HttpStart");

            // Function input comes from the request content.
            string instanceId = await client.ScheduleNewOrchestrationInstanceAsync(
                nameof(durable));

            logger.LogInformation("Started orchestration with ID = '{instanceId}'.", instanceId);

            // Returns an HTTP 202 response with an instance management payload.
            // See https://learn.microsoft.com/azure/azure-functions/durable/durable-functions-http-api#start-orchestration
            return client.CreateCheckStatusResponse(req, instanceId);
        }
    }
}
