﻿using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;

using Avalonia.Threading;

using BEditor.Audio;
using BEditor.Command;
using BEditor.Data;
using BEditor.Drawing;
using BEditor.Extensions;
using BEditor.Models.Authentication;
using BEditor.Models.ManagePlugins;
using BEditor.Packaging;
using BEditor.ViewModels.Dialogs;
using BEditor.Views;
using BEditor.Views.Dialogs;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Serilog;
using Serilog.Formatting.Json;

#nullable disable

namespace BEditor.Models
{
    public sealed class AppModel : BasePropertyChanged, IApplication
    {
        private static readonly PropertyChangedEventArgs _projectArgs = new(nameof(Project));
        private static readonly PropertyChangedEventArgs _statusArgs = new(nameof(AppStatus));
        private static readonly PropertyChangedEventArgs _isPlayingArgs = new(nameof(IsNotPlaying));
        private static readonly PropertyChangedEventArgs _userArgs = new(nameof(User));
        private Project _project;
        private Status _status;
        private bool _isplaying = true;
        private IServiceProvider _serviceProvider;
        private AuthenticationLink _user;
        private readonly Navigatable[] _navigatables =
        {
            // 設定
            new Navigatable("settings", async _ => await new SettingsWindow().ShowDialog(App.GetMainWindow())),
            new Navigatable("settings/appearance", async _ => await new SettingsWindow().Navigate(typeof(Views.Settings.Appearance)).ShowDialog(App.GetMainWindow())),
            new Navigatable("settings/fonts", async _ => await new SettingsWindow().Navigate(typeof(Views.Settings.Fonts)).ShowDialog(App.GetMainWindow())),
            new Navigatable("settings/project", async _ => await new SettingsWindow().Navigate(typeof(Views.Settings.Project)).ShowDialog(App.GetMainWindow())),
            new Navigatable("settings/package-source", async _ => await new SettingsWindow().Navigate(typeof(Views.Settings.PackageSource)).ShowDialog(App.GetMainWindow())),
            new Navigatable("settings/key-bindings", async _ => await new SettingsWindow().Navigate(typeof(Views.Settings.KeyBindings)).ShowDialog(App.GetMainWindow())),
            new Navigatable("settings/license", async _ => await new SettingsWindow().Navigate(typeof(Views.Settings.License)).ShowDialog(App.GetMainWindow())),

            // プラグインを管理
            new Navigatable("manage-plugin", async _ => await new Views.ManagePlugins.ManagePluginsWindow().ShowDialog(App.GetMainWindow())),
            new Navigatable("manage-plugin/installed", async _ => await new Views.ManagePlugins.ManagePluginsWindow()
                .Navigate(typeof(Views.ManagePlugins.LoadedPlugins), null)
                .ShowDialog(App.GetMainWindow())),
            new Navigatable("manage-plugin/search", async pre => await new Views.ManagePlugins.ManagePluginsWindow()
                .Navigate(typeof(Views.ManagePlugins.Search), pre)
                .ShowDialog(App.GetMainWindow())),
            new Navigatable("manage-plugin/changes", async _ => await new Views.ManagePlugins.ManagePluginsWindow()
                .Navigate(typeof(Views.ManagePlugins.ScheduleChanges), null)
                .ShowDialog(App.GetMainWindow())),
            new Navigatable("manage-plugin/update", async _ => await new Views.ManagePlugins.ManagePluginsWindow()
                .Navigate(typeof(Views.ManagePlugins.Update), null)
                .ShowDialog(App.GetMainWindow())),
            new Navigatable("manage-plugin/create-package", async _ => await new Views.ManagePlugins.ManagePluginsWindow()
                .Navigate(typeof(Views.ManagePlugins.CreatePluginPackage), null)
                .ShowDialog(App.GetMainWindow())),

            // プロジェクトを作成
            new Navigatable("new-project", async _ =>
            {
                var viewmodel = new CreateProjectViewModel();
                var dialog = new CreateProject { DataContext = viewmodel };

                await dialog.ShowDialog(App.GetMainWindow());
            }),
        };

        private AppModel()
        {
            CommandManager.Default.Executed += async (_, _) =>
            {
                if (Project is not null)
                {
                    await Project.PreviewUpdateAsync(ApplyType.Edit);
                }
                AppStatus = Status.Edit;
            };

            Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .WriteTo.File(new JsonFormatter(null, true), Path.Combine(ServicesLocator.GetUserFolder(), "log.json"))
                .CreateLogger();

            LoggingFactory = LoggerFactory.Create(builder => builder.AddSerilog(Log.Logger, true));

            // DIの設定
            Services = new ServiceCollection()
                .AddSingleton<IAuthenticationProvider, MockAuthenticationProvider>()
                .AddSingleton<IRemotePackageProvider, MockPackageUploader>()
                .AddSingleton<ITopLevel>(_ => this)
                .AddSingleton<IApplication>(_ => this)
                .AddSingleton(_ => FileDialog)
                .AddSingleton(_ => Message)
                .AddSingleton(_ => LoggingFactory)
                .AddSingleton<Microsoft.Extensions.Logging.ILogger>(_ => LoggingFactory.CreateLogger<IApplication>())
                .AddSingleton<HttpClient>()
                .AddSingleton<PluginUpdateService>();

            if (Settings.Default.PrioritizeGPU)
            {
                DrawingContext = DrawingContext.Create(0);
                Services = Services.AddSingleton(_ => DrawingContext);
            }

            // 設定が変更されたときにUIに変更を適用
            Settings.Default.PropertyChanged += Settings_PropertyChanged;
        }

        private void Settings_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (e.PropertyName == nameof(Settings.LayerBorder) && Project != null)
                {
                    foreach (var item in Project.SceneList)
                    {
                        var timeline = item.GetCreateTimeline();
                        timeline.UpdateLayerBorderColor();
                    }
                }
            }, DispatcherPriority.MinValue);
        }

        public static AppModel Current { get; } = new();

        public Project Project
        {
            get => _project;
            set => SetAndRaise(value, ref _project, _projectArgs);
        }

        public Status AppStatus
        {
            get => _status;
            set => SetAndRaise(value, ref _status, _statusArgs);
        }

        public bool IsNotPlaying
        {
            get => _isplaying;
            set => SetAndRaise(value, ref _isplaying, _isPlayingArgs);
        }

        public IServiceCollection Services { get; }

        public IServiceProvider ServiceProvider
        {
            get => _serviceProvider ??= Services.BuildServiceProvider();
            set => _serviceProvider = value;
        }

        public IMessage Message { get; } = new MessageService();

        public IFileDialogService FileDialog { get; } = new FileDialogService();

        public ILoggerFactory LoggingFactory { get; }

        public AuthenticationLink User
        {
            get => _user;
            set => SetAndRaise(value, ref _user, _userArgs);
        }

        public SynchronizationContext UIThread { get; set; }

        Project IParentSingle<Project>.Child => Project;

        public object AudioContext { get; set; }

        public DrawingContext DrawingContext { get; }

        public event EventHandler<ProjectOpenedEventArgs> ProjectOpened;
        public event EventHandler Exit;

        public void RaiseExit()
        {
            Exit?.Invoke(this, EventArgs.Empty);
        }

        public void RaiseProjectOpened(Project project)
        {
            ProjectOpened?.Invoke(this, new(project));
        }

        public void SaveAppConfig(Project project, string directory)
        {
            static void IfNotExistCreateDir(string dir)
            {
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
            }

            var cache = Path.Combine(directory, "cache");

            IfNotExistCreateDir(cache);

            {
                var projConfig = new ProjectConfig
                {
                    BackgroundType = ProjectConfig.GetBackgroundType(project),
                    Speed = ProjectConfig.GetSpeed(project),
                };

                using var writer = new StreamWriter(Path.Combine(directory, ".config"));
                var json = JsonSerializer.Serialize(projConfig, PackageFile._serializerOptions);
                writer.Write(json);
            }

            {
                var sceneCacheDir = Path.Combine(cache, "scene");
                IfNotExistCreateDir(sceneCacheDir);

                foreach (var scene in project.SceneList)
                {
                    var sceneCache = Path.Combine(sceneCacheDir, scene.Name + ".cache");
                    var cacheObj = new SceneCache
                    {
                        Select = scene.SelectItem?.Name,
                        PreviewFrame = scene.PreviewFrame,
                        TimelineScale = scene.TimeLineScale,
                        TimelineHorizonOffset = scene.TimeLineHorizonOffset,
                        TimelineVerticalOffset = scene.TimeLineVerticalOffset
                    };

                    using var writer = new StreamWriter(sceneCache);
                    var json = JsonSerializer.Serialize(cacheObj, PackageFile._serializerOptions);
                    writer.Write(json);
                }
            }
        }

        public unsafe void RestoreAppConfig(Project project, string directory)
        {
            static void IfNotExistCreateDir(string dir)
            {
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
            }

            var cache = Path.Combine(directory, "cache");

            IfNotExistCreateDir(cache);

            {
                var file = Path.Combine(directory, ".config");
                if (!File.Exists(file))
                {
                    ProjectConfig.SetBackgroundType(project, ViewModels.ConfigurationViewModel.BackgroundType.Transparent);
                    ProjectConfig.SetSpeed(project, 1);
                }
                else
                {
                    using var reader = new StreamReader(file);
                    var projConfig = JsonSerializer.Deserialize<ProjectConfig>(reader.ReadToEnd(), PackageFile._serializerOptions);

                    ProjectConfig.SetBackgroundType(project, projConfig.BackgroundType);
                    ProjectConfig.SetSpeed(project, projConfig.Speed);
                }
            }

            {
                var sceneCacheDir = Path.Combine(cache, "scene");
                IfNotExistCreateDir(sceneCacheDir);

                foreach (var scene in project.SceneList)
                {
                    var sceneCache = Path.Combine(sceneCacheDir, scene.Name + ".cache");

                    if (!File.Exists(sceneCache)) continue;
                    Stream stream = null;
                    UnmanagedArray<byte> buffer = default;

                    try
                    {
                        stream = File.OpenRead(sceneCache);
                        buffer = new UnmanagedArray<byte>((int)stream.Length);
                        var span = buffer.AsSpan();
                        stream.Read(span);

                        var cacheObj = JsonSerializer.Deserialize<SceneCache>(span, PackageFile._serializerOptions);

                        if (cacheObj is not null)
                        {
                            scene.SelectItem = scene[cacheObj.Select];
                            scene.PreviewFrame = cacheObj.PreviewFrame;
                            scene.TimeLineScale = cacheObj.TimelineScale;
                            scene.TimeLineHorizonOffset = cacheObj.TimelineHorizonOffset;
                            scene.TimeLineVerticalOffset = cacheObj.TimelineVerticalOffset;
                        }
                    }
                    finally
                    {
                        stream?.Dispose();
                        buffer.Dispose();
                    }
                }
            }
        }

        public void Navigate(Uri uri, object parameter = null)
        {
            if (uri.Scheme == "beditor")
            {
                var abs = uri.AbsoluteUri.Remove(0, 10).TrimEnd('/');
                var item = Array.Find(_navigatables, i => i.Uri == abs);
                if (item != null)
                {
                    item.Execute.Invoke(parameter);
                }
            }
        }
    }

    public record Navigatable(string Uri, Action<object> Execute);
}