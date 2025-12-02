using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using website;
using website.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Load configuration from appsettings.json
var configuration = builder.Configuration;

// Configure HttpClient with base address from configuration
// For API calls, we'll use the API Gateway URL from configuration
// For other HTTP calls, we use the host environment base address
builder.Services.AddScoped(sp => 
{
    var apiGatewayUrl = configuration["AWS:ApiGatewayUrl"];
    var baseAddress = !string.IsNullOrEmpty(apiGatewayUrl) && apiGatewayUrl != "PLACEHOLDER_API_GATEWAY_URL"
        ? new Uri(apiGatewayUrl)
        : new Uri(builder.HostEnvironment.BaseAddress);
    
    return new HttpClient { BaseAddress = baseAddress };
});

// Register AuthService
builder.Services.AddScoped<IAuthService, AuthService>();

// Register LeadService
builder.Services.AddScoped<ILeadService, LeadService>();

await builder.Build().RunAsync();
