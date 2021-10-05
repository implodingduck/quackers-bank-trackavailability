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

using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

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
        }

        public async static Task RunAvailabilityTestAsync(ILogger log, ChromeDriver driver) 
        { 
            log.LogInformation($"Attempting to run Availability Test");
//             using (var httpClient = new HttpClient()) 
//             { 
//                 // TODO: Replace with your business logic 
//                 await httpClient.GetStringAsync(""); 
//             } 
            
            log.LogInformation("Pre get EnvironmentVariable");
            //Navigate to DotNet website
            var baseurl = Environment.GetEnvironmentVariable("BASE_URL");
            log.LogInformation($"Going to {baseurl}");
            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(20));

            //Navigate to base website
            driver.Navigate().GoToUrl(baseurl);
            IReadOnlyList<IWebElement> boxEles = driver.FindElements(By.CssSelector(".box"));
            log.LogInformation(boxEles.ElementAt(0).Text);

            //Click on the navbar toggle
            IWebElement navbarToggler = driver.FindElement(By.CssSelector(".navbar-toggler.collapsed"));
            navbarToggler.Click();

            //Click on the login link
            var loginEleXpath = By.XPath("//a[contains(text(),'Login')]");
            wait.Until(ExpectedConditions.ElementIsVisible(loginEleXpath));
            IWebElement loginEle = driver.FindElement(loginEleXpath);
            log.LogInformation("Login button");
            log.LogInformation($"Login Displayed: {loginEle.Displayed}");
            log.LogInformation($"Login Enabled: {loginEle.Enabled}");
            loginEle.Click();

            //Wait for the B2C login page
            wait.Until(ExpectedConditions.ElementIsVisible(By.CssSelector(".intro")));
            IReadOnlyList<IWebElement> introEles = driver.FindElements(By.CssSelector(".intro"));
            log.LogInformation(introEles.ElementAt(0).Text);
            
            //fill out the form
            var testemail = Environment.GetEnvironmentVariable("TEST_EMAIL");
            var testpassword = Environment.GetEnvironmentVariable("TEST_PASSWORD");
            driver.FindElement(By.Id("email")).SendKeys(testemail);
            IWebElement passwordEle = driver.FindElement(By.Id("password"));
            passwordEle.SendKeys(testpassword);
            passwordEle.SendKeys(Keys.Enter);
            
            //Wait for login to complete
            wait.Until(ExpectedConditions.ElementIsVisible(By.CssSelector(".navbar-toggler.collapsed")));
            log.LogInformation("Login has completed");
            navbarToggler = driver.FindElement(By.CssSelector(".navbar-toggler.collapsed"));
            navbarToggler.Click();

            //Go to accounts
            var accountEleXpath = By.XPath("//a[contains(text(),'Accounts')]");
            wait.Until(ExpectedConditions.ElementIsVisible(accountEleXpath));
            IWebElement accountEle = driver.FindElement(accountEleXpath);
            accountEle.Click();
            log.LogInformation("Clicking on Accounts");

            wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("//h3[contains(text(),'Checking')]")));
            log.LogInformation("Accounts are visible");


            
        } 
    }
}
