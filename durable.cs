using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;
using PerfTestDLL;
using System;
using System.Threading;
using System.Linq;

namespace DurableMemory
{
    public static class durable
    {
        [Function(nameof(durable))]
        public static async Task Run(
    [OrchestrationTrigger] TaskOrchestrationContext context)
        {
            var parallelTasks = new List<Task<int>>();

            // Get a list of N work items to process in parallel.
            //object[] workBatch = await context.CallActivityAsync("SayHello1", null); //.CallActivityAsync("SayHello1", null);
            for (int i = 0; i < 10; i++)
            {
                Task<int> task = context.CallActivityAsync<int>("SayHello", null);
                parallelTasks.Add(task);
            }

            await Task.WhenAll(parallelTasks);

            // Aggregate all N outputs and send the result to F3.
            int sum = parallelTasks.Sum(t => t.Result);
            await context.CallActivityAsync("SayHello", sum);
        }

        //public static async Task RunOrchestrator(
        //    [OrchestrationTrigger] TaskOrchestrationContext context)
        //{
        //    ILogger logger = context.CreateReplaySafeLogger(nameof(durable));
        //    logger.LogInformation("Saying hello.");

        //    await context.CallActivityAsync<string>(nameof(SayHello), "Tokyo");
        //    await context.CallActivityAsync<string>(nameof(SayHello), "Seattle");
        //    await context.CallActivityAsync<string>(nameof(SayHello), "London");
        //    await context.CallActivityAsync<string>(nameof(FinalActivity), "Complete");

        //    //var outputs = new List<string>();

        //    //// Replace name and input with values relevant for your Durable Functions Activity
        //    //outputs.Add(await context.CallActivityAsync<string>(nameof(SayHello), "Tokyo"));
        //    //outputs.Add(await context.CallActivityAsync<string>(nameof(SayHello), "Seattle"));
        //    //outputs.Add(await context.CallActivityAsync<string>(nameof(SayHello), "London"));
        //    //outputs.Add(await context.CallActivityAsync<string>(nameof(FinalActivity), "Complete"));

        //    //return outputs;

        //    //try
        //    //{
        //    //    var x = await context.CallActivityAsync<string>("SayHello", "first");
        //    //    var y = await context.CallActivityAsync<string>("SayHello", x);
        //    //    var z = await context.CallActivityAsync<string>("SayHello", y);
        //    //    return await context.CallActivityAsync<string>("FinalActivity", z);
        //    //}
        //    //catch (Exception)
        //    //{
        //    //    // Error handling or compensation goes here.
        //    //    return "Error";
        //    //}
        //}

        [Function(nameof(SayHello1))]
        public static async Task<int> SayHello1([ActivityTrigger] FunctionContext executionContext)
        {
            ILogger logger = executionContext.GetLogger("SayHello1");
            //logger.LogInformation("Saying hello to {name}.", name);

            // Simulate adding 1 MB of data to the list

            //var tempMemClass = new MemClass();

            //Thread.Sleep(5000);

            //Console.WriteLine($"Total memory allocated: {GC.GetTotalMemory(false) / (1024 * 1024)} MB");

            //tempMemClass.Dispose();

            //_ = Task.Factory.StartNew(() =>
            //{
            //    Thread.Sleep(1000);

            //    GC.Collect(2, GCCollectionMode.Forced, true, true);
            //});

            Console.WriteLine($"Total memory allocated: {GC.GetTotalMemory(false) / (1024 * 1024)} MB");

            return 3;

            //return $"Hello {name}!";
        }

        [Function(nameof(SayHello))]
        public static async Task<int> SayHello([ActivityTrigger] FunctionContext executionContext)
        {
            ILogger logger = executionContext.GetLogger("SayHello");
            //logger.LogInformation("Saying hello to {name}.", name);

            // Simulate adding 1 MB of data to the list

            var tempMemClass = new MemClass();

            Thread.Sleep(3000);

            Console.WriteLine($"Total memory allocated: {GC.GetTotalMemory(false) / (1024 * 1024)} MB");

            tempMemClass.Dispose();

            _ = Task.Factory.StartNew(() =>
            {
                Thread.Sleep(1000);

                GC.Collect(2, GCCollectionMode.Forced, true, true);
            });

            Console.WriteLine($"Total memory allocated: {GC.GetTotalMemory(false) / (1024 * 1024)} MB");

            return 3;

            //return $"Hello {name}!";
        }

        [Function(nameof(FinalActivity))]
        public static async Task<string> FinalActivity([ActivityTrigger] string input, FunctionContext executionContext)
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

    public class MemClass : IDisposable
    {
        private byte[] data;
        public MemClass()
        {
            data = new byte[1024 * 1024 * 50];
        }
        public void Dispose()
        {
            // Release the allocated memory by setting data to null
            if (data != null)
            {
                // Optionally, you can clear the array first to remove sensitive data
                Array.Clear(data, 0, data.Length);

                // Release the memory
                data = null;
            }
        }
    }
}
