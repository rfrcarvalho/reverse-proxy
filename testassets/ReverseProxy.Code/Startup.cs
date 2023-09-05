// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Model;
using Yarp.ReverseProxy.Transforms;
using Yarp.Telemetry.Consumption;

namespace Yarp.ReverseProxy.Sample;

/// <summary>
/// ASP .NET Core pipeline initialization.
/// </summary>
public class Startup
{
    /// <summary>
    /// This method gets called by the runtime. Use this method to add services to the container.
    /// </summary>
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers();
        var routes = new[]
        {
            new RouteConfig()
            {
                RouteId = "route1",
                ClusterId = "cluster1",
                Match = new RouteMatch
                {
                    Path = "{**catch-all}"
                }
            }
        };
        var clusters = new[]
        {
            new ClusterConfig()
            {
                ClusterId = "cluster1",
                SessionAffinity = new SessionAffinityConfig { Enabled = true, Policy = "Cookie", AffinityKeyName = ".Yarp.ReverseProxy.Affinity" },
                Destinations = new Dictionary<string, DestinationConfig>(StringComparer.OrdinalIgnoreCase)
                {
                    { "destination1", new DestinationConfig() { Address = "https://localhost:10000" } }
                }
            },
            new ClusterConfig()
            {
                ClusterId = "cluster2",
                Destinations = new Dictionary<string, DestinationConfig>(StringComparer.OrdinalIgnoreCase)
                {
                    { "destination2", new DestinationConfig() { Address = "https://localhost:10001" } }
                }
            }
        };

        services.AddReverseProxy()
            .LoadFromMemory(routes, clusters)
            .ConfigureHttpClient((context, handler) =>
            {
                handler.Expect100ContinueTimeout = TimeSpan.FromMilliseconds(300);
            })
            .AddTransformFactory<MyTransformFactory>()
            .AddTransforms<MyTransformProvider>()
            .AddTransforms(transformBuilderContext =>
            {
                // For each route+cluster pair decide if we want to add transforms, and if so, which?
                // This logic is re-run each time a route is rebuilt.

                transformBuilderContext.AddPathPrefix("/prefix");

                // Only do this for routes that require auth.
                if (string.Equals("token", transformBuilderContext.Route.AuthorizationPolicy))
                {
                    transformBuilderContext.AddRequestTransform(async transformContext =>
                    {
                        // AuthN and AuthZ will have already been completed after request routing.
                        var ticket = await transformContext.HttpContext.AuthenticateAsync("token");
                        var tokenService = transformContext.HttpContext.RequestServices.GetRequiredService<TokenService>();
                        var token = await tokenService.GetAuthTokenAsync(ticket.Principal);
                        transformContext.ProxyRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    });
                }

                transformBuilderContext.AddResponseTransform(context =>
                {
                    // Suppress the response body from errors.
                    // The status code was already copied.
                    if (context.ProxyResponse?.IsSuccessStatusCode == false)
                    {
                        context.SuppressResponseBody = true;
                    }

                    return default;
                });
            });

        services.AddHttpContextAccessor();
        services.AddSingleton<IMetricsConsumer<ForwarderMetrics>, ForwarderMetricsConsumer>();
        services.AddTelemetryConsumer<ForwarderTelemetryConsumer>();
        services.AddTelemetryListeners();
    }

    /// <summary>
    /// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    /// </summary>
    public void Configure(IApplicationBuilder app, IProxyStateLookup lookup)
    {
        app.UseRouting();
        app.UseAuthorization();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
            endpoints.MapReverseProxy(proxyPipeline =>
            {
                // Custom endpoint selection
                proxyPipeline.Use((context, next) =>
                {
                    if (lookup.TryGetCluster("cluster2", out var cluster))
                    {
                        context.ReassignProxyRequest(cluster);
                    }

                    var someCriteria = false; // MeetsCriteria(context);
                    if (someCriteria)
                    {
                        var availableDestinationsFeature = context.Features.Get<IReverseProxyFeature>();
                        var destination = availableDestinationsFeature.AvailableDestinations[0]; // PickDestination(availableDestinationsFeature.Destinations);
                        // Load balancing will no-op if we've already reduced the list of available destinations to 1.
                        availableDestinationsFeature.AvailableDestinations = destination;
                    }

                    return next();
                });
                proxyPipeline.UseSessionAffinity();
                proxyPipeline.UseLoadBalancing();
            });
        });
    }
}
