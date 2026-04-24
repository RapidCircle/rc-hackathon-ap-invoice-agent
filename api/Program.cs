using System.Reflection;
using System.Text.Json;
using Api.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

// Configure JSON serialization to use camelCase for Azure Functions Worker
// JsonSerializerDefaults.Web enables camelCase naming and case-insensitive deserialization
builder.Services.Configure<WorkerOptions>(options =>
{
    options.Serializer = new Azure.Core.Serialization.JsonObjectSerializer(
        new JsonSerializerOptions(JsonSerializerDefaults.Web));
});

// Auto-register mock auth if MockAuthRegistration exists
// When mock auth files are deleted, this becomes a no-op (no code changes needed)
var mockAuthType = Type.GetType("Api.Auth.MockAuthRegistration, api");
if (mockAuthType != null)
{
    var addMockAuth = mockAuthType.GetMethod("AddMockAuth", BindingFlags.Public | BindingFlags.Static);
    addMockAuth?.Invoke(null, new object[] { builder.Services });
}

// Register storage context and services
builder.Services.AddSingleton<TableStorageContext>();
builder.Services.AddSingleton<InvoiceService>();
builder.Services.AddSingleton<VendorService>();
builder.Services.AddSingleton<ValidationService>();
builder.Services.AddSingleton<ExtractionService>();
builder.Services.AddSingleton<BatchService>();
builder.Services.AddSingleton<AppDataSeeder>();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

builder.Build().Run();
