using Api.Auth;
using Api.Models;
using Microsoft.Extensions.Logging;

namespace Api.Services;

/// <summary>
/// Seeds demo data including user personas for testing.
/// Reads user data from data/demo-users.csv for easy customization.
/// </summary>
public class SampleDataSeeder
{
    private readonly MockAuthProvider _mockAuthProvider;
    private readonly ILogger<SampleDataSeeder> _logger;
    private readonly string _csvPath;

    public SampleDataSeeder(MockAuthProvider mockAuthProvider, ILogger<SampleDataSeeder> logger)
    {
        _mockAuthProvider = mockAuthProvider;
        _logger = logger;
        
        // Find the CSV file - check multiple possible locations
        // Azure Functions can run from various directories
        var basePath = AppContext.BaseDirectory;
        var currentDir = Directory.GetCurrentDirectory();
        
        var possiblePaths = new[]
        {
            // Direct paths from workspace root
            "/workspaces/ASD-Template/data/demo-users.csv",
            Path.Combine(currentDir, "data", "demo-users.csv"),
            Path.Combine(currentDir, "..", "data", "demo-users.csv"),
            // Relative from bin/Debug/net9.0
            Path.Combine(basePath, "..", "..", "..", "data", "demo-users.csv"),
            Path.Combine(basePath, "..", "..", "..", "..", "data", "demo-users.csv"),
            Path.Combine(basePath, "data", "demo-users.csv"),
        };
        
        _csvPath = possiblePaths.FirstOrDefault(File.Exists) ?? possiblePaths[0];
        _logger.LogInformation("CSV path resolved to: {Path}, exists: {Exists}", _csvPath, File.Exists(_csvPath));
    }

    /// <summary>
    /// Seeds demo personas if they don't exist.
    /// </summary>
    public async Task SeedAsync()
    {
        await _mockAuthProvider.InitializeAsync();

        var existingUsers = await _mockAuthProvider.GetAllUsersAsync();
        if (existingUsers.Count > 0)
        {
            _logger.LogInformation("Demo data already seeded ({Count} users)", existingUsers.Count);
            return;
        }

        var personas = LoadPersonasFromCsv();
        foreach (var persona in personas)
        {
            await _mockAuthProvider.SaveUserAsync(persona);
            _logger.LogInformation("Seeded user: {Email}", persona.Email);
        }

        _logger.LogInformation("Seeded {Count} demo personas", personas.Count);
    }

    /// <summary>
    /// Resets all data and re-seeds.
    /// </summary>
    public async Task ResetAsync()
    {
        await _mockAuthProvider.ClearAllDataAsync();
        
        var personas = LoadPersonasFromCsv();
        foreach (var persona in personas)
        {
            await _mockAuthProvider.SaveUserAsync(persona);
        }

        _logger.LogInformation("Reset and re-seeded {Count} demo personas", personas.Count);
    }

    /// <summary>
    /// Loads user personas from the CSV file.
    /// Falls back to hardcoded defaults if CSV is not found.
    /// </summary>
    private List<User> LoadPersonasFromCsv()
    {
        if (!File.Exists(_csvPath))
        {
            _logger.LogWarning("CSV file not found at {Path}, using default personas", _csvPath);
            return GetDefaultPersonas();
        }

        try
        {
            var users = new List<User>();
            var lines = File.ReadAllLines(_csvPath);
            
            // Skip header row
            foreach (var line in lines.Skip(1))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                
                var user = ParseCsvLine(line);
                if (user != null)
                {
                    users.Add(user);
                }
            }

            _logger.LogInformation("Loaded {Count} users from CSV: {Path}", users.Count, _csvPath);
            return users;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load CSV from {Path}, using defaults", _csvPath);
            return GetDefaultPersonas();
        }
    }

    /// <summary>
    /// Parses a CSV line into a User object.
    /// Expected format: UserId,Email,FullName,Status,IsSystemAdmin,Roles,PersonaDescription
    /// </summary>
    private User? ParseCsvLine(string line)
    {
        // Simple CSV parsing - handles quoted fields with commas
        var fields = ParseCsvFields(line);
        
        if (fields.Count < 7)
        {
            _logger.LogWarning("Invalid CSV line (expected 7 fields): {Line}", line);
            return null;
        }

        return new User
        {
            UserId = fields[0].Trim(),
            Email = fields[1].Trim(),
            FullName = fields[2].Trim(),
            Status = fields[3].Trim(),
            IsSystemAdmin = bool.TryParse(fields[4].Trim(), out var isAdmin) && isAdmin,
            Roles = fields[5].Trim().Trim('"'),
            PersonaDescription = fields[6].Trim()
        };
    }

    /// <summary>
    /// Parses CSV fields handling quoted values that may contain commas.
    /// </summary>
    private static List<string> ParseCsvFields(string line)
    {
        var fields = new List<string>();
        var current = new System.Text.StringBuilder();
        var inQuotes = false;

        foreach (var c in line)
        {
            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                fields.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }
        fields.Add(current.ToString());

        return fields;
    }

    /// <summary>
    /// Default personas used when CSV file is not available.
    /// </summary>
    private static List<User> GetDefaultPersonas()
    {
        return
        [
            new User
            {
                UserId = "user-admin",
                Email = "admin@demo.example.com",
                FullName = "System Admin",
                Status = "Active",
                IsSystemAdmin = true,
                Roles = "anonymous,authenticated,admin",
                PersonaDescription = "System administrator"
            },
            new User
            {
                UserId = "user-001",
                Email = "sarah.mitchell@example.com",
                FullName = "Sarah Mitchell",
                Status = "Active",
                IsSystemAdmin = false,
                Roles = "anonymous,authenticated",
                PersonaDescription = "Standard user in good standing"
            }
        ];
    }
}
