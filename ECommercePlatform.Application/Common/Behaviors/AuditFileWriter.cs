using System.Text.Json;

namespace ECommercePlatform.Application.AuditLogs;

public static class AuditFileWriter
{
    public static async Task WriteEntityChangeAsync(object entity, string entityType, CancellationToken cancellationToken)
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(entity, options);
        string uniqueFileName = $"AuditLogs/EntityChangeTracker_{entityType}_{Guid.NewGuid()}.json";

        Directory.CreateDirectory("AuditLogs");
        await File.WriteAllTextAsync(uniqueFileName, json, cancellationToken);
    }
}
// ... (other code remains unchanged)

// ... (inside the foreach loop in Handle method, replace file writing section with:)
//await AuditFileWriter.WriteEntityChangeAsync(prop, entry.Entity.GetType().Name, cancellationToken);

// ... (remove the old file writing code)
