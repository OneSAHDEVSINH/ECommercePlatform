using ECommercePlatform.Application.Interfaces;
using ECommercePlatform.Application.Interfaces.IUserAuth;
using ECommercePlatform.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ECommercePlatform.Application.Common.Behaviors;

public class AuditBehavior<TRequest, TResponse>(
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService,
    ILogger<AuditBehavior<TRequest, TResponse>> logger
) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var dbContext = unitOfWork.DbContext;
        var now = DateTime.UtcNow;
        var userId = currentUserService.IsAuthenticated
            ? currentUserService.UserId ?? currentUserService.Email ?? "system"
            : "system";

        logger.LogInformation("AuditBehavior running for {RequestType}", typeof(TRequest).Name);
        // Handler runs first (entities are added/modified here)
        var response = await next();

        // Set audit fields on tracked entities
        if (dbContext != null)
        {
            
            foreach (var entry in dbContext.ChangeTracker.Entries())
            {
                logger.LogInformation("Setting audit fields for {EntityType}", entry.Entity.GetType().Name);
                if (entry.State == EntityState.Added)
                {
                    if (entry.Entity is BaseEntity baseEntity)
                    {
                        baseEntity.CreatedBy ??= userId;
                        baseEntity.CreatedOn = baseEntity.CreatedOn == default ? now : baseEntity.CreatedOn;
                    }
                    else if (entry.Entity is Role role)
                    {
                        role.CreatedBy ??= userId;
                        role.CreatedOn = role.CreatedOn == default ? now : role.CreatedOn;
                    }
                    else if (entry.Entity is User user)
                    {
                        user.CreatedBy ??= userId;
                        user.CreatedOn = user.CreatedOn == default ? now : user.CreatedOn;
                    }
                    else if (entry.Entity is UserRole userRole)
                    {
                        userRole.CreatedBy ??= userId;
                        userRole.CreatedOn = userRole.CreatedOn == default ? now : userRole.CreatedOn;
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
            }
        }

        return response;
    }
}