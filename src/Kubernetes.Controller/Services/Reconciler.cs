// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Yarp.Kubernetes.Controller.Caching;
using Yarp.Kubernetes.Controller.Client;
using Yarp.Kubernetes.Controller.Configuration;
using Yarp.Kubernetes.Controller.Converters;

namespace Yarp.Kubernetes.Controller.Services;

/// <summary>
/// IReconciler is a service interface called by the <see cref="IngressController"/> to process
/// the work items as they are dequeued.
/// </summary>
public partial class Reconciler : IReconciler
{
    private readonly ICache _cache;
    private readonly IUpdateConfig _updateConfig;
    private readonly IIngressResourceStatusUpdater _ingressResourceStatusUpdater;
    private readonly ILogger<Reconciler> _logger;

    public Reconciler(ICache cache, IUpdateConfig updateConfig, IIngressResourceStatusUpdater ingressResourceStatusUpdater, ILogger<Reconciler> logger)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _updateConfig = updateConfig ?? throw new ArgumentNullException(nameof(updateConfig));
        _ingressResourceStatusUpdater = ingressResourceStatusUpdater ?? throw new ArgumentNullException(nameof(ingressResourceStatusUpdater));
        _logger = logger;
    }

    public async Task ProcessAsync(CancellationToken cancellationToken)
    {
        try
        {
            var ingresses = _cache.GetIngresses().ToArray();

            var configContext = new YarpConfigContext();

            foreach (var ingress in ingresses)
            {
                try
                {
                    if (!_cache.IsYarpIngress(ingress))
                    {
                        continue;
                    }

                    if (_cache.TryGetReconcileData(new NamespacedName(ingress.Metadata.NamespaceProperty, ingress.Metadata.Name), out var data))
                    {
                        var ingressContext = new YarpIngressContext(ingress, data.ServiceList, data.EndpointsList);
                        YarpParser.ConvertFromKubernetesIngress(ingressContext, configContext);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Uncaught exception occured while reconciling ingress {IngressNamespace}/{IngressName}", ingress.Metadata.NamespaceProperty, ingress.Metadata.Name);
                }
            }

            var clusters = configContext.BuildClusterConfig();

            _logger.LogInformation(JsonSerializer.Serialize(configContext.Routes));
            _logger.LogInformation(JsonSerializer.Serialize(clusters));

            await _updateConfig.UpdateAsync(configContext.Routes, clusters, cancellationToken).ConfigureAwait(false);
            await _ingressResourceStatusUpdater.UpdateStatusAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Uncaught exception occured while reconciling");
            throw;
        }
    }
}
