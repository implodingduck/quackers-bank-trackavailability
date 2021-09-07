using System;
using System.Net.Http; 

using System.Diagnostics; 
using Microsoft.ApplicationInsights; 
using Microsoft.ApplicationInsights.Channel; 
using Microsoft.ApplicationInsights.DataContracts; 
using Microsoft.ApplicationInsights.Extensibility; 
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;

using Microsoft.Extensions.Logging;
using System.Threading.Tasks;



namespace qbtrackavailability
{
    public static class CheckAvailability
    {
        private static TelemetryClient telemetryClient; 
        [FunctionName("CheckAvailability")]
        public static async Task Run([TimerTrigger("0 */5 * * * *")]TimerInfo myTimer, ILogger log,  ExecutionContext executionContext)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
            if (telemetryClient == null) 
            { 
                // Initializing a telemetry configuration for Application Insights based on connection string 

                var telemetryConfiguration = new TelemetryConfiguration(); 
                telemetryConfiguration.ConnectionString = Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING"); 
                telemetryConfiguration.TelemetryChannel = new InMemoryChannel(); 
                telemetryClient = new TelemetryClient(telemetryConfiguration); 
            } 

            string testName = executionContext.FunctionName; 
            string location = Environment.GetEnvironmentVariable("REGION_NAME"); 
            var availability = new AvailabilityTelemetry 
            { 
                Name = testName, 

                RunLocation = location, 

                Success = false, 
            }; 

            availability.Context.Operation.ParentId = Activity.Current.SpanId.ToString(); 
            availability.Context.Operation.Id = Activity.Current.RootId; 
            var stopwatch = new Stopwatch(); 
            stopwatch.Start(); 

            try 
            { 
                using (var activity = new Activity("AvailabilityContext")) 
                { 
                    activity.Start(); 
                    availability.Id = Activity.Current.SpanId.ToString(); 
                    // Run business logic 
                    await RunAvailabilityTestAsync(log); 
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
                availability.Duration = stopwatch.Elapsed; 
                availability.Timestamp = DateTimeOffset.UtcNow; 
                telemetryClient.TrackAvailability(availability); 
                telemetryClient.Flush(); 
            } 
        }

        public async static Task RunAvailabilityTestAsync(ILogger log) 
        { 
            using (var httpClient = new HttpClient()) 
            { 
                // TODO: Replace with your business logic 
                await httpClient.GetStringAsync("https://frontendov5kihos.azurewebsites.net/health"); 
            } 
        } 
    }
}
