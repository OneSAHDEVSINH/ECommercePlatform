using ECommercePlatform.Application.Common.Behaviors;
using ECommercePlatform.Application.Interfaces.Pagination;
using ECommercePlatform.Application.Services;
using FluentValidation;
using MediatR;
//using MediatR.Extensions.FluentValidation.AspNetCore;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace ECommercePlatform.Application.Common
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            // Add pagination service
            services.AddScoped<IPaginationService, PaginationService>();
            //Register MediatR
            services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
                cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
                cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(AuditBehavior<,>));
                cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>));
            });

            //Register FluentValidation
            services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

            return services;
        }
    }
}
