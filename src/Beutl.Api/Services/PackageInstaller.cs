﻿using System.Reactive.Linq;
using System.Reactive.Subjects;

using Beutl.Api.Objects;

using BeUtl.Reactive;

using NuGet.Common;
using NuGet.Configuration;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Packaging.Signing;
using NuGet.Protocol.Core.Types;
using NuGet.Resolver;
using NuGet.Versioning;

namespace Beutl.Api.Services;

public partial class PackageInstaller : IBeutlApiResource
{
    private readonly HttpClient _httpClient;
    private readonly InstalledPackageRepository _installedPackageRepository;

    private readonly ISettings _settings;
    private readonly PackageSourceProvider _packageSourceProvider;
    private readonly SourceRepositoryProvider _sourceRepositoryProvider;
    private readonly SourceCacheContext _cacheContext;
    private readonly PackageResolver _resolver;

    private readonly Dictionary<PackageIdentity, PackageInstallContext> _installingContexts = new();

    private readonly Subject<(PackageIdentity Package, EventType Type)> _subject = new();

    private static readonly NuGetVersion s_beutlVersion = new("0.3.0");
    private static readonly PackageIdentity[] s_preferredVersions =
    {
        new PackageIdentity("BeUtl.Sdk", s_beutlVersion),
        new PackageIdentity("BeUtl.Configuration", s_beutlVersion),
        new PackageIdentity("BeUtl.Controls", s_beutlVersion),
        new PackageIdentity("BeUtl.Core", s_beutlVersion),
        new PackageIdentity("BeUtl.Framework", s_beutlVersion),
        new PackageIdentity("BeUtl.Graphics", s_beutlVersion),
        new PackageIdentity("BeUtl.Language", s_beutlVersion),
        new PackageIdentity("BeUtl.Operators", s_beutlVersion),
        new PackageIdentity("BeUtl.ProjectSystem", s_beutlVersion),
        new PackageIdentity("BeUtl.Threading", s_beutlVersion),
        new PackageIdentity("BeUtl.Utilities", s_beutlVersion),
    };

    private const string DefaultNuGetConfigContentTemplate = @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <packageSources>
    <add key=""Beutl Local Packages"" value=""{0}"" />
    <add key=""nuget.org"" value=""https://api.nuget.org/v3/index.json"" protocolVersion=""3"" />
  </packageSources>
</configuration>
";

    public enum EventType
    {
        Idle,
        Installing,
        Uninstalling,
    }

    public PackageInstaller(HttpClient httpClient, InstalledPackageRepository installedPackageRepository)
    {
        _httpClient = httpClient;
        _installedPackageRepository = installedPackageRepository;

        string configPath = Path.Combine(Helper.AppRoot, "nuget.config");
        if (!File.Exists(configPath))
        {
            using (StreamWriter writer =File.CreateText(configPath))
            {
                writer.Write(string.Format(DefaultNuGetConfigContentTemplate, Helper.LocalSourcePath));
            }
        }

        _settings = Settings.LoadDefaultSettings(Helper.AppRoot);
        _settings = new Settings(Helper.AppRoot);
        _packageSourceProvider = new PackageSourceProvider(_settings);

        _sourceRepositoryProvider = new SourceRepositoryProvider(_packageSourceProvider, Repository.Provider.GetCoreV3());
        _cacheContext = new SourceCacheContext()
        {
            DirectDownload = true
        };

        _resolver = new PackageResolver();
    }

    private static void CreateLocalSourceDirectory()
    {
        if (!Directory.Exists(Helper.LocalSourcePath))
        {
            Directory.CreateDirectory(Helper.LocalSourcePath);
        }
    }

    public IObservable<EventType> GetObservable(string name, string? version = null)
    {
        return new _Observable(this, name, version);
    }

    public async Task<PackageInstallContext> PrepareForInstall(
        Release release,
        bool force = false,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        string name = release.Package.Name;
        string version = release.Version.Value;
        var packageId = new PackageIdentity(name, new NuGetVersion(version));

        if (!force && _installedPackageRepository.ExistsPackage(name, version))
        {
            throw new Exception("This package is already installed.");
        }

        if (_installingContexts.TryGetValue(packageId, out PackageInstallContext? context))
        {
            return context;
        }
        else
        {
            Asset asset = await release.GetAssetAsync().ConfigureAwait(false);

            context = new PackageInstallContext(name, version, asset.DownloadUrl);
            _installingContexts.Add(packageId, context);
            return context;
        }
    }

    public PackageInstallContext PrepareForInstall(
        string name,
        string version,
        bool force = false,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var packageId = new PackageIdentity(name, new NuGetVersion(version));

        if (!force && _installedPackageRepository.ExistsPackage(name, version))
        {
            throw new Exception("This package is already installed.");
        }

        if (_installingContexts.TryGetValue(packageId, out PackageInstallContext? context))
        {
            return context;
        }
        else
        {
            context = new PackageInstallContext(name, version, string.Empty)
            {
                Phase = PackageInstallPhase.Downloaded
            };
            _installingContexts.Add(packageId, context);
            return context;
        }
    }

    public async Task DownloadPackageFile(
        PackageInstallContext context,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if ((int)context.Phase <= (int)PackageInstallPhase.Downloading)
        {
            context.Phase = PackageInstallPhase.Downloading;
            CreateLocalSourceDirectory();

            string name = context.PackageName;
            string version = context.Version;
            string downloadUrl = context.DownloadUrl;
            context.NuGetPackageFile = Helper.GetNupkgFilePath(name, version);
            using (FileStream destination = File.Create(context.NuGetPackageFile))
            {
                await Download(downloadUrl, destination, progress, cancellationToken).ConfigureAwait(false);
            }

            context.Phase = PackageInstallPhase.Downloaded;
        }
    }

    public async Task ResolveDependencies(
        PackageInstallContext context,
        ILogger? logger,
        CancellationToken cancellationToken = default)
    {
        PackageIdentity? package = null;
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            if ((int)context.Phase <= (int)PackageInstallPhase.ResolvingDependencies)
            {
                context.Phase = PackageInstallPhase.ResolvingDependencies;

                string packageId = context.PackageName;
                string version = context.Version;
                NuGetFramework nuGetFramework = Helper.GetFrameworkName();
                package = new PackageIdentity(packageId, NuGetVersion.Parse(version));

#if DEBUG
                logger ??= new ConsoleLogger();
#else
                logger ??= NullLogger.Instance;
#endif

                IEnumerable<SourceRepository> repositories = _sourceRepositoryProvider.GetRepositories();
                var availablePackages = new HashSet<SourcePackageDependencyInfo>(PackageIdentityComparer.Default);
                await Helper.GetPackageDependencies(
                    package,
                    nuGetFramework,
                    _cacheContext,
                    logger,
                    repositories,
                    availablePackages,
                    cancellationToken)
                    .ConfigureAwait(false);

                var resolverContext = new PackageResolverContext(
                    DependencyBehavior.Lowest,
                    new[] { packageId },
                    Enumerable.Empty<string>(),
                    Enumerable.Empty<PackageReference>(),
                    s_preferredVersions,
                    availablePackages,
                    repositories.Select(s => s.PackageSource),
                    logger);

                SourcePackageDependencyInfo[] packagesToInstall
                    = _resolver.Resolve(resolverContext, cancellationToken)
                        .Select(p => availablePackages.Single(x => PackageIdentityComparer.Default.Equals(x, p)))
                        .ToArray();

                var packageExtractionContext = new PackageExtractionContext(
                    PackageSaveMode.Defaultv3,
                    XmlDocFileSaveMode.None,
                    ClientPolicyContext.GetClientPolicy(_settings, logger),
                    logger);

                var installedPaths = new List<string>(packagesToInstall.Length);
                foreach (SourcePackageDependencyInfo packageToInstall in packagesToInstall)
                {
                    // BeUtl.Sdkに含まれるライブラリの場合、飛ばす。
                    if (Helper.IsCoreLibraries(packageToInstall.Id))
                    {
                        continue;
                    }

                    string installedPath = Helper.PackagePathResolver.GetInstalledPath(packageToInstall);
                    if (installedPath != null)
                    {
                        installedPaths.Add(installedPath);
                    }
                    else
                    {
                        DownloadResource downloadResource = await packageToInstall.Source.GetResourceAsync<DownloadResource>(cancellationToken).ConfigureAwait(false);
                        using DownloadResourceResult downloadResult = await downloadResource.GetDownloadResourceResultAsync(
                            packageToInstall,
                            new PackageDownloadContext(_cacheContext),
                            SettingsUtility.GetGlobalPackagesFolder(_settings),
                            logger, cancellationToken)
                            .ConfigureAwait(false);

                        await PackageExtractor.ExtractPackageAsync(
                            downloadResult.PackageSource,
                            downloadResult.PackageStream,
                            Helper.PackagePathResolver,
                            packageExtractionContext,
                            cancellationToken)
                            .ConfigureAwait(false);

                        installedPath = Helper.PackagePathResolver.GetInstalledPath(packageToInstall);
                        if (installedPath != null)
                        {
                            installedPaths.Add(installedPath);
                        }
                    }
                }

                context.Phase = PackageInstallPhase.ResolvedDependencies;
                context.InstalledPaths = installedPaths;
                _installedPackageRepository.AddPackage(package);
            }
        }
        finally
        {
            if (package is { })
            {
                _installingContexts.Remove(package);
            }
        }
    }

    private async Task Download(
        string url,
        Stream destination,
        IProgress<double>? progress,
        CancellationToken cancellationToken)
    {
        using (HttpResponseMessage response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false))
        {
            long? contentLength = response.Content.Headers.ContentLength;

            using (Stream download = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false))
            {
                if (!contentLength.HasValue)
                {
                    progress?.Report(double.PositiveInfinity);
                    await download.CopyToAsync(destination, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    int bufferSize = 81920;
                    byte[] buffer = new byte[bufferSize];
                    long totalBytesRead = 0;
                    int bytesRead;
                    while ((bytesRead = await download.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) != 0)
                    {
                        await destination.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken).ConfigureAwait(false);
                        totalBytesRead += bytesRead;
                        progress?.Report(totalBytesRead / (double)contentLength.Value);
                    }
                }
            }
        }
    }

    private sealed class _Observable : LightweightObservableBase<EventType>
    {
        private readonly PackageInstaller _installer;
        private readonly string _name;
        private readonly PackageIdentity? _packageIdentity;
        private IDisposable? _disposable;

        public _Observable(PackageInstaller installer, string name, string? version)
        {
            _installer = installer;
            _name = name;

            if (version is { })
            {
                _packageIdentity = new PackageIdentity(name, new NuGetVersion(version));
            }
        }

        protected override void Subscribed(IObserver<EventType> observer, bool first)
        {
            if (_packageIdentity is { })
            {
                if (_installer._installingContexts.ContainsKey(_packageIdentity))
                {
                    observer.OnNext(EventType.Installing);
                }
                //else if (_installer._uninstallingContexts.ContainsKey(_packageIdentity))
                //{
                //    observer.OnNext(EventType.Uninstalling);
                //}
                else
                {
                    observer.OnNext(EventType.Idle);
                }
            }
            else
            {
                if (_installer._installingContexts.Any(x => StringComparer.OrdinalIgnoreCase.Equals(x.Key.Id, _name)))
                {
                    observer.OnNext(EventType.Installing);
                }
                else
                {
                    observer.OnNext(EventType.Idle);
                }
            }
        }

        protected override void Deinitialize()
        {
            _disposable?.Dispose();
            _disposable = null;
        }

        protected override void Initialize()
        {
            _disposable = _installer._subject
                .Subscribe(OnReceived);
        }

        private void OnReceived((PackageIdentity Package, EventType Type) obj)
        {
            if ((_packageIdentity != null && _packageIdentity == obj.Package)
                || StringComparer.OrdinalIgnoreCase.Equals(obj.Package.Id, _name))
            {
                PublishNext(obj.Type);
            }
        }
    }
}
