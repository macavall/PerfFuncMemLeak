using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using PerfTestDLL;

namespace DurableMemory
{
    public class clearmemory
    {
        private readonly ILogger _logger;

        public clearmemory(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<clearmemory>();
        }

        [Function("clearmemory")]
        public HttpResponseData Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "text/plain; charset=utf-8");

            response.WriteString(MemoryClass.ClearMemory().ToString());

            return response;
        }
    }
}
