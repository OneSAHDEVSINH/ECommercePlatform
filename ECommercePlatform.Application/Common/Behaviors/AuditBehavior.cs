using ECommercePlatform.Application.Interfaces;
using ECommercePlatform.Application.Interfaces.IUserAuth;
using ECommercePlatform.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ECommercePlatform.Application.Common.Behaviors;

public class AuditBehavior<TRequest, TResponse>(
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService,
    ILogger<AuditBehavior<TRequest, TResponse>> logger
) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ILogger<AuditBehavior<TRequest, TResponse>> _logger = logger;
    // Add null check for AuditFolder before using Path.Combine and Directory.CreateDirectory
    private static readonly string AuditFolder = GetAuditFolder();

    private static string GetAuditFolder()
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        if (string.IsNullOrWhiteSpace(baseDir))
            baseDir = AppContext.BaseDirectory;
        if (string.IsNullOrWhiteSpace(baseDir))
            baseDir = Directory.GetCurrentDirectory();
        if (string.IsNullOrWhiteSpace(baseDir))
            throw new InvalidOperationException("Base directory for audit logs is not set or is invalid.");

        var folder = Path.Combine(baseDir, "AuditLogs");
        if (string.IsNullOrWhiteSpace(folder))
            throw new InvalidOperationException("AuditFolder path is not set or is invalid.");
        if (folder.Any(c => Path.GetInvalidPathChars().Contains(c)))
            throw new InvalidOperationException("AuditFolder path contains invalid characters.");
        return folder;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var dbContext = _unitOfWork.DbContext;
        var now = DateTime.UtcNow;
        var userId = currentUserService.IsAuthenticated
            ? currentUserService.UserId ?? currentUserService.Email ?? "system"
            : "system";
        var requestTypeName = typeof(TRequest).Name;

        _logger.LogInformation("AuditBehavior running for {RequestType} by user {UserId} at {Time}", typeof(TRequest).Name, userId, now);
        _logger.LogDebug("Request details: {@Request}", request);

        TResponse response;
        try
        {
            response = await next(cancellationToken);
            _logger.LogInformation("Request {RequestType} handled successfully.", typeof(TRequest).Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while handling {RequestType} by user {UserId}", typeof(TRequest).Name, userId);
            throw;
        }

        // Skip auditing for queries - only audit commands that modify data
        if (requestTypeName.Contains("Query", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogDebug("Skipping audit for query request: {RequestType}", requestTypeName);
            return response;
        }

        if (dbContext != null)
        {
            var auditEntries = new List<object>();

            foreach (var entry in dbContext.ChangeTracker.Entries())
            {
                var entityType = entry.Entity.GetType().Name;
                var auditEntry = new
                {
                    EntityType = entityType,
                    Request = typeof(TRequest).Name,
                    State = entry.State.ToString(),
                    UserId = userId,
                    Time = now,
                    Changes = new List<object>()
                };

                if (entry.State == EntityState.Added)
                {
                    if (entry.Entity is BaseEntity baseEntity)
                    {
                        baseEntity.CreatedBy = userId;
                        baseEntity.CreatedOn = now;
                    }
                    else if (entry.Entity is Role role)
                    {
                        role.CreatedBy = userId;
                        role.CreatedOn = now;
                    }
                    else if (entry.Entity is User user)
                    {
                        user.CreatedBy = userId;
                        user.CreatedOn = now;
                    }
                    else if (entry.Entity is UserRole userRole)
                    {
                        userRole.CreatedBy = userId;
                        userRole.CreatedOn = now;
                    }
                }
                else if (entry.State == EntityState.Modified)
                {
                    if (entry.Entity is BaseEntity baseEntity)
                    {
                        baseEntity.ModifiedBy = userId;
                        baseEntity.ModifiedOn = now;
                    }
                    else if (entry.Entity is Role role)
                    {
                        role.ModifiedBy = userId;
                        role.ModifiedOn = now;
                    }
                    else if (entry.Entity is User user)
                    {
                        user.ModifiedBy = userId;
                        user.ModifiedOn = now;
                    }
                    else if (entry.Entity is UserRole userRole)
                    {
                        userRole.ModifiedBy = userId;
                        userRole.ModifiedOn = now;
                    }
                }

                foreach (var property in entry.CurrentValues.Properties)
                {
                    var originalValue = entry.State == EntityState.Modified ? entry.OriginalValues[property] : null;
                    var currentValue = entry.CurrentValues[property];
                    if (entry.State == EntityState.Added || !Equals(originalValue, currentValue))
                    {
                        ((List<object>)auditEntry.Changes).Add(new
                        {
                            Property = property.Name,
                            OldValue = originalValue,
                            NewValue = currentValue
                        });
                    }
                }

                //if (entry.CurrentValues.Properties.Count > 0)
                //{
                //    Directory.CreateDirectory(AuditFolder);
                //    var fileName = $"{typeof(TRequest).Name}_{now:yyyyMMdd_HHmmss_fff}_{Guid.NewGuid()}.json";
                //    var filePath = Path.Combine(AuditFolder, fileName);
                //    var options = new JsonSerializerOptions { WriteIndented = true, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };
                //    await File.WriteAllTextAsync(filePath, JsonSerializer.Serialize(entry.CurrentValues.ToObject(), options), cancellationToken);
                //    _logger.LogInformation("Audit log written to {FilePath}", filePath);
                //}

                auditEntries.Add(auditEntry);
            }

            if (auditEntries.Count > 0)
            {
                Directory.CreateDirectory(AuditFolder);
                var fileName = $"{typeof(TRequest).Name}_{now:yyyyMMdd_HHmmss_fff}_{Guid.NewGuid()}.json";
                var filePath = Path.Combine(AuditFolder, fileName);
                var options = new JsonSerializerOptions { WriteIndented = true, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };
                await File.WriteAllTextAsync(filePath, JsonSerializer.Serialize(auditEntries, options), cancellationToken);
                _logger.LogInformation("Audit log written to {FilePath}", filePath);
            }
        }
        else
        {
            _logger.LogWarning("DbContext is null in AuditBehavior for {RequestType}", typeof(TRequest).Name);
        }

        try
        {
            var changes = await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation("AuditBehavior saved {ChangeCount} changes to the database for {RequestType}", changes, typeof(TRequest).Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving changes in AuditBehavior for {RequestType} by user {UserId}", typeof(TRequest).Name, userId);
            throw;
        }

        return response;
    }
}