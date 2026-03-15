using Jellyfin.Plugin.JellyTube.Services;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Jellyfin.Plugin.JellyTube;

/// <summary>
/// Registers plugin services with Jellyfin's dependency injection container.
/// </summary>
public class ServiceRegistrator : IPluginServiceRegistrator
{
    /// <inheritdoc />
    public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
    {
        serviceCollection.AddSingleton<DownloadQueueService>();
        serviceCollection.AddSingleton<DownloadArchiveService>();
        serviceCollection.AddTransient<YtDlpService>();
        serviceCollection.AddTransient<NfoWriterService>();
        serviceCollection.AddTransient<ThumbnailService>();
        serviceCollection.AddTransient<LibraryOrganizationService>();
        serviceCollection.AddHttpClient("thumbnail");
        serviceCollection.AddHostedService<DownloadWorkerService>();
        serviceCollection.AddHostedService<WatchedVideoCleanupService>();
    }
}
