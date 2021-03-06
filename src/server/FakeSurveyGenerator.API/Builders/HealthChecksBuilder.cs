﻿using System;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

namespace FakeSurveyGenerator.API.Builders
{
    public static class HealthChecksBuilder
    {
        public static IServiceCollection AddHealthChecksConfiguration(this IServiceCollection services,
            IConfiguration configuration, IWebHostEnvironment environment)
        {
            var healthChecksBuilder = services.AddHealthChecks();

            healthChecksBuilder
                .AddSqlServer(
                    configuration["ConnectionStrings:SurveyContext"],
                    name: "FakeSurveyGeneratorDB-check",
                    tags: new[] { "fake-survey-generator-db", "ready" },
                    failureStatus: HealthStatus.Unhealthy);

            healthChecksBuilder
                .AddRedis(
                    $"{Environment.GetEnvironmentVariable("REDIS_URL")},ssl={Environment.GetEnvironmentVariable("REDIS_SSL")},password={Environment.GetEnvironmentVariable("REDIS_PASSWORD")},defaultDatabase={Environment.GetEnvironmentVariable("REDIS_DEFAULT_DATABASE")}",
                    "RedisCache-check",
                    tags: new[] { "redis-cache", "ready" },
                    failureStatus: HealthStatus.Degraded);

            healthChecksBuilder.AddUrlGroup(
                new Uri($"{configuration.GetValue<string>("IDENTITY_PROVIDER_BACKCHANNEL_URL")}/.well-known/openid-configuration"), 
                "IdentityServer-check",
                tags: new[] { "fake-survey-generator-db", "ready" },
                failureStatus: HealthStatus.Unhealthy);

            if (!environment.IsDevelopment())
                services.AddHealthChecksUI();

            return services;
        }

        public static IEndpointRouteBuilder UseHealthChecksConfiguration(this IEndpointRouteBuilder endpoints,
            IWebHostEnvironment environment)
        {
            endpoints.MapHealthChecks("/health/ready", new HealthCheckOptions
            {
                Predicate = check => check.Tags.Contains("ready"),
                ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
            });

            endpoints.MapHealthChecks("/health/live", new HealthCheckOptions
            {
                Predicate = _ => false
            });

            if (!environment.IsDevelopment())
                endpoints.MapHealthChecksUI();

            return endpoints;
        }
    }
}