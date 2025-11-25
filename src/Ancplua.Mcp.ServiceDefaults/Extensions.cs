using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Ancplua.Mcp.ServiceDefaults;

public static class Extensions
{
    extension<TBuilder>(TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        public TBuilder AddServiceDefaults()
        {
            builder.ConfigureOpenTelemetry();
            builder.AddDefaultHealthChecks();
            builder.ConfigureNetworking();

            return builder;
        }

        private void ConfigureOpenTelemetry()
        {
            var optimizeForStdio = !builder.Configuration.GetValue<bool>("MCP:DisableStdioLoggingOptimization");
            if (optimizeForStdio)
            {
                builder.Logging.AddConsole(options =>
                {
                    options.LogToStandardErrorThreshold = LogLevel.Trace;
                });
            }

            builder.Logging.AddOpenTelemetry(logging =>
            {
                logging.IncludeFormattedMessage = true;
                logging.IncludeScopes = true;
            });

            var otel = builder.Services.AddOpenTelemetry();

            otel.WithMetrics(metrics =>
            {
                metrics.AddRuntimeInstrumentation()
                    .AddHttpClientInstrumentation();

                if (builder is WebApplicationBuilder)
                {
                    metrics.AddAspNetCoreInstrumentation();
                }
            });

            otel.WithTracing(tracing =>
            {
                tracing.AddSource(builder.Environment.ApplicationName)
                    .AddHttpClientInstrumentation();

                if (builder is WebApplicationBuilder)
                {
                    tracing.AddAspNetCoreInstrumentation(options =>
                    {
                        options.Filter = context =>
                            !context.Request.Path.StartsWithSegments("/health") &&
                            !context.Request.Path.StartsWithSegments("/alive");
                    });
                }
            });

            var otlpEndpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];
            if (!string.IsNullOrWhiteSpace(otlpEndpoint))
            {
                otel.UseOtlpExporter();
            }
        }

        private void AddDefaultHealthChecks()
        {
            builder.Services.AddHealthChecks()
                .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"]);
        }

        private void ConfigureNetworking()
        {
            builder.Services.AddServiceDiscovery();

            builder.Services.ConfigureHttpClientDefaults(http =>
            {
                http.AddStandardResilienceHandler();

                http.AddServiceDiscovery();
            });
        }
    }

    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.MapHealthChecks("/health");

            app.MapHealthChecks("/alive", new HealthCheckOptions { Predicate = check => check.Tags.Contains("live") });
        }

        return app;
    }
}
