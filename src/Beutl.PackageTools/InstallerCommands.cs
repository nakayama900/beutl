﻿namespace Beutl.PackageTools;

public sealed partial class InstallerCommands
{
    private readonly BeutlApiApplication _application;
    private readonly CancellationToken _cancellationToken;
    private readonly PackageInstaller _installer;
    private readonly InstalledPackageRepository _installedPackageRepository;

    public InstallerCommands(BeutlApiApplication application, CancellationToken cancellationToken)
    {
        _application = application;
        _cancellationToken = cancellationToken;
        _installer = _application.GetResource<PackageInstaller>();
        _installedPackageRepository = _application.GetResource<InstalledPackageRepository>();
    }
}
