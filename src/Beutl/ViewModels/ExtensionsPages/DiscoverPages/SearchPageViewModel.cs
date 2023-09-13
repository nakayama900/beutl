﻿using Avalonia.Collections;

using Beutl.Api.Objects;
using Beutl.Api.Services;

using OpenTelemetry.Trace;

using Reactive.Bindings;

using Serilog;

namespace Beutl.ViewModels.ExtensionsPages.DiscoverPages;

public sealed class SearchPageViewModel : BasePageViewModel
{
    private readonly ILogger _logger = Log.ForContext<SearchPageViewModel>();
    private readonly CompositeDisposable _disposables = new();
    private readonly DiscoverService _discoverService;

    public SearchPageViewModel(DiscoverService discoverService, string keyword)
    {
        _discoverService = discoverService;
        Keyword = keyword;

        Refresh = new AsyncReactiveCommand(IsBusy.Not())
            .WithSubscribe(async () =>
            {
                using Activity? activity = Services.Telemetry.StartActivity("SearchPage.Refresh");

                try
                {
                    IsBusy.Value = true;
                    if (SearchType.Value == 0)
                        await RefreshPackages();
                    else
                        await RefreshUsers();
                }
                catch (Exception e)
                {
                    activity?.SetStatus(ActivityStatusCode.Error);
                    activity?.RecordException(e);
                    ErrorHandle(e);
                    _logger.Error(e, "An unexpected error has occurred.");
                }
                finally
                {
                    IsBusy.Value = false;
                }
            })
            .DisposeWith(_disposables);

        More = new AsyncReactiveCommand(IsBusy.Not())
            .WithSubscribe(async () =>
            {
                using Activity? activity = Services.Telemetry.StartActivity("SearchPage.More");

                try
                {
                    IsBusy.Value = true;
                    if (SearchType.Value == 0)
                        await MoreLoadPackages();
                    else
                        await MoreLoadUsers();
                }
                catch (Exception e)
                {
                    activity?.SetStatus(ActivityStatusCode.Error);
                    activity?.RecordException(e);
                    ErrorHandle(e);
                    _logger.Error(e, "An unexpected error has occurred.");
                }
                finally
                {
                    IsBusy.Value = false;
                }
            })
            .DisposeWith(_disposables);

        SearchType.Subscribe(async type =>
            {
                using Activity? activity = Services.Telemetry.StartActivity("SearchPage.SearchType");

                try
                {
                    IsBusy.Value = true;
                    if (type == 0 && Packages.Count == 0)
                    {
                        await RefreshPackages();
                    }
                    else if (Users.Count == 0)
                    {
                        await RefreshUsers();
                    }
                }
                catch (Exception e)
                {
                    activity?.SetStatus(ActivityStatusCode.Error);
                    activity?.RecordException(e);
                    ErrorHandle(e);
                    _logger.Error(e, "An unexpected error has occurred.");
                }
                finally
                {
                    IsBusy.Value = false;
                }
            })
            .DisposeWith(_disposables);
    }

    public string Keyword { get; }

    public AvaloniaList<Package?> Packages { get; } = new();

    public AvaloniaList<Profile?> Users { get; } = new();

    public AsyncReactiveCommand Refresh { get; }

    public AsyncReactiveCommand More { get; }

    public ReactivePropertySlim<bool> IsBusy { get; } = new();

    public ReactivePropertySlim<int> SearchType { get; } = new();

    public override void Dispose()
    {
        _disposables.Dispose();
    }

    private async Task<Package[]> SearchPackages(int start, int count)
    {
        return await _discoverService.SearchPackages(Keyword, start, count);
    }

    private async Task RefreshPackages()
    {
        using (await _discoverService.Lock.LockAsync())
        {
            Packages.Clear();
            Package[] array = await SearchPackages(0, 30);
            Packages.AddRange(array);

            if (array.Length == 30)
            {
                Packages.Add(null);
            }
        }
    }

    private async Task MoreLoadPackages()
    {
        using (await _discoverService.Lock.LockAsync())
        {
            Packages.RemoveAt(Packages.Count - 1);
            Package[] array = await SearchPackages(Packages.Count, 30);
            Packages.AddRange(array);

            if (array.Length == 30)
            {
                Packages.Add(null);
            }
        }
    }

    private async Task<Profile[]> SearchUsers(int start, int count)
    {
        return await _discoverService.SearchUsers(Keyword, start, count);
    }

    private async Task RefreshUsers()
    {
        using (await _discoverService.Lock.LockAsync())
        {
            Users.Clear();
            Profile[] array = await SearchUsers(0, 30);
            Users.AddRange(array);

            if (array.Length == 30)
            {
                Users.Add(null);
            }
        }
    }

    private async Task MoreLoadUsers()
    {
        using (await _discoverService.Lock.LockAsync())
        {
            Users.RemoveAt(Users.Count - 1);
            Profile[] array = await SearchUsers(Users.Count, 30);
            Users.AddRange(array);

            if (array.Length == 30)
            {
                Users.Add(null);
            }
        }
    }
}
