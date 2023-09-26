using System;
using System.Net.Http; 
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics; 
using Microsoft.ApplicationInsights; 
using Microsoft.ApplicationInsights.Channel; 
using Microsoft.ApplicationInsights.DataContracts; 
using Microsoft.ApplicationInsights.Extensibility; 
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace qbtrackavailability
{
    public static class ApiCheck
    {
        private static TelemetryClient telemetryClient; 
        [FunctionName("ApiCheck")]
        //public static async Task Run([TimerTrigger("0 */5 * * * *")]TimerInfo myTimer, ILogger log,  ExecutionContext executionContext)
        public static async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]
            HttpRequest req, ILogger log,  ExecutionContext executionContext)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
            if (telemetryClient == null) 
         { 
                // Initializing a telemetry configuration for Application Insights based on connection string 

        var telemetryConfiguration = new TelemetryConfiguration(); 
         telemetryConfiguration.ConnectionString = Environment.GetEnvironmentVariable("AVAILABILITY_APPINSIGHTS_CONNECTION_STRING"); 
         telemetryConfiguration.TelemetryChannel = new InMemoryChannel(); 
         telemetryClient = new TelemetryClient(telemetryConfiguration); 
        } 
            log.LogInformation($"Telemetry Client has been initialized");
        string testName = executionContext.FunctionName; 
            log.LogInformation($"testName={testName}");
         string location = Environment.GetEnvironmentVariable("REGION_NAME");
            log.LogInformation($"location={location}");
        var availability = new AvailabilityTelemetry 
         { 
        Name = testName, 

                RunLocation = location, 

                Success = false, 
            }; 
            log.LogInformation($"AvailabilityTelemetry has been set");
            
            availability.Context.Operation.ParentId = Activity.Current.SpanId.ToString();
            log.LogInformation($"ParentId={availability.Context.Operation.ParentId}");
            availability.Context.Operation.Id = Activity.Current.RootId; 
            log.LogInformation($"OperationId={availability.Context.Operation.Id}");
            var stopwatch = new Stopwatch(); 
            stopwatch.Start(); 
            log.LogInformation($"StopWatch has started");
            try 
            { 
                var activity = new Activity("AvailabilityContext");
                if (activity != null)
                { 
                    activity.Start(); 
                    availability.Id = Activity.Current.SpanId.ToString(); 
                    var chromeOptions = new ChromeOptions();
                    chromeOptions.AddArgument("--headless");
                    chromeOptions.AddArgument("--disable-gpu");
                    chromeOptions.AddArgument("--no-sandbox");
                    
                    using (var driver = new ChromeDriver(chromeOptions))
                    {
                        // Run business logic 
                        await RunAvailabilityTestAsync(log, driver); 
                    }
                } 
                availability.Success = true; 
            } 

            catch (Exception ex) 
            { 
                availability.Message = ex.Message; 
                throw; 
            } 

            finally 
            { 
                stopwatch.Stop(); 
                log.LogInformation($"StopWatch has stopped");
                availability.Duration = stopwatch.Elapsed; 
                availability.Timestamp = DateTimeOffset.UtcNow; 
                telemetryClient.TrackAvailability(availability); 
                telemetryClient.Flush(); 
                
            }
            return (ActionResult)new OkObjectResult($"Work Complete!");
        }

        public async static Task RunAvailabilityTestAsync(ILogger log, ChromeDriver driver) 
        { 
            log.LogInformation($"Attempting to run Availability Test");          
            log.LogInformation("Pre get EnvironmentVariable");
     
            var baseurl = Environment.GetEnvironmentVariable("BASE_URL");
            log.LogInformation($"Going to {baseurl}");
            
            var httpClient = new HttpClient(){
                BaseAddress = new Uri(baseurl)
            };
            using HttpResponseMessage response = await httpClient.GetAsync("health");

            var jsonResponse = await response.Content.ReadAsStringAsync();
            log.LogInformation($"Response: {jsonResponse}");
            JsonNode responseNode = JsonNode.Parse(jsonResponse)!;
            log.LogInformation($"ResponseNode Id: {(int)responseNode["id"]}");
            if ( (int)responseNode["id"] != 200){
                throw new Exception("Bad ID received...")
            }
            
            
        } 
    }
}
