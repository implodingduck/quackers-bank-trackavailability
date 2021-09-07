#load "runAvailabilityTest.csx" 

using System; 

using System.Diagnostics; 

using Microsoft.ApplicationInsights; 

using Microsoft.ApplicationInsights.Channel; 

using Microsoft.ApplicationInsights.DataContracts; 

using Microsoft.ApplicationInsights.Extensibility; 

private static TelemetryClient telemetryClient; 

// ============================================================= 

// ****************** DO NOT MODIFY THIS FILE ****************** 

// Business logic must be implemented in RunAvailabilityTestAsync function in runAvailabilityTest.csx 

// If this file does not exist, please add it first 

// ============================================================= 

public async static Task Run(TimerInfo myTimer, ILogger log, ExecutionContext executionContext) 

{ 
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