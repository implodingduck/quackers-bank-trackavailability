using System.Net.Http; 

public async static Task RunAvailabilityTestAsync(ILogger log) 
{ 
    using (var httpClient = new HttpClient()) 
    { 
        // TODO: Replace with your business logic 
        await httpClient.GetStringAsync("https://frontendov5kihos.azurewebsites.net/health"); 
    } 
} 