using System.Net.Http;
using System.Threading.Tasks;
using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Collections;
using System.Collections.Generic;
using HttpHealthCheck.Authorization;

namespace HttpHealthCheck
{
    /// <summary>
    /// Provides extensions.
    /// </summary
    public static class HealthCheckExtensions
    {
        /// <summary>
        /// Add HTTP health service to the provided <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="healthService"></param>
        public static IServiceCollection AddHttpHealthService (this IServiceCollection services, IHttpHealthService healthService)
        {
            return services.AddSingleton (healthService);
        }
        /// <summary>
        /// Add default HTTP health service to the provided <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="healthService"></param>
        public static IServiceCollection AddHttpHealthService (this IServiceCollection services)
        {
            return services.AddSingleton<IHttpHealthService>(HttpHealthService.Default);
        }

        /// <summary>
        /// Add default HTTP health service to the provided <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="healthService"></param>
        public static IServiceCollection AddHttpHealthService(this IServiceCollection services, IAuthorizationFilter filter)
        {
            services.AddSingleton<IHttpHealthService>(HttpHealthService.Default);
            return services.AddSingleton(filter);
        }

        /// <summary>
        /// Add HTTP base health check middleware to the <see cref="IApplicationBuilder"/> request execution pipe.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="path">The path to use as healhcheck route.</param>
        public static void UseHttpHealthCheck (this IApplicationBuilder builder, string path = "/status")
        {
            builder.Map (path, (app) =>
            {
                app.Run (async ctx =>
                {
                    IHttpHealthService service = ctx.RequestServices.GetRequiredService<IHttpHealthService> ();
                    
                    if (HttpMethod.Get.Method.Equals (ctx.Request.Method))
                    {
                        IHttpHealthService checker = ctx.RequestServices.GetRequiredService<IHttpHealthService> ();
                        ctx.Response.ContentType = "text/plain";
                        await ctx.Response.WriteAsync (checker.Health);
                        if (!checker.IsHealthy)
                        {
                            ctx.Response.StatusCode = 503;
                        }
                    }
                    else if (HttpMethod.Put.Method.Equals (ctx.Request.Method))
                    {
                        if (service != null)
                        {
                            IAuthorizationFilter filter = app.ApplicationServices.GetService<IAuthorizationFilter>();

                            if(filter != null)
                            {
                                if(await filter.FilterAsync(ctx))
                                {
                                    await service.HttpPutRequestReceived (ctx);
                                }
                            }
                        }
                    }
                });
            });
        }
    }
}