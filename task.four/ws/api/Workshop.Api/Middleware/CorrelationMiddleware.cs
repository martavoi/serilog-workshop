using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Workshop.Api.Middleware
{
    public static class CorrelationMiddleware
    {
        public static void UseCorrelationHeader(this IApplicationBuilder builder, ILoggerFactory loggerFactory)
        {
            builder.Use(async (context, next) =>
            {
                const string xCorrelationId = "X-Correlation-Id";
                string correlationId;
                if (context.Request.Headers.ContainsKey(xCorrelationId))
                {
                    correlationId = context.Request.Headers[xCorrelationId];
                }
                else
                {
                    correlationId = Guid.NewGuid().ToString();
                    context.Request.Headers.Append(xCorrelationId, correlationId);
                }
                
                context.Response.Headers.Append(xCorrelationId, correlationId);
                
                var logger = loggerFactory.CreateLogger("CorrelationMiddleware");
                using (logger.BeginScope("{CorrelationId}", correlationId))
                {
                    await next();
                }
            });
        }
    }
}