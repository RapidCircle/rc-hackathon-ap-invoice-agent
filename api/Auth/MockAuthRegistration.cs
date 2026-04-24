using System.Runtime.CompilerServices;
using Api.Services;
using Azure.Data.Tables;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Api.Auth;

/// <summary>
/// Extension methods for registering mock authentication services.
/// 
/// DELETE THIS FILE to remove mock authentication from the application.
/// The app will automatically fall back to Azure Static Web Apps (SWA) authentication.
/// </summary>
public static class MockAuthRegistration
{
    /// <summary>
    /// Adds mock authentication services to the service collection.
    /// </summary>
    public static IServiceCollection AddMockAuth(this IServiceCollection services)
    {
        var storageConnectionString = Environment.GetEnvironmentVariable("STORAGE") 
            ?? "UseDevelopmentStorage=true";

        // Register Table Storage client for mock auth session management
        services.AddSingleton(new TableServiceClient(storageConnectionString));

        // Register mock auth provider
        services.AddSingleton<MockAuthProvider>();
        services.AddSingleton<IAuthProvider>(sp => sp.GetRequiredService<MockAuthProvider>());

        // Register demo data seeder (only available in mock mode)
        services.AddSingleton<SampleDataSeeder>();

        return services;
    }
}
