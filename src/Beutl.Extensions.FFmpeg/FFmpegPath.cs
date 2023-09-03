﻿using System.Diagnostics;
using System.Reflection;

using Beutl.Services;

using FFmpeg.AutoGen;

using Microsoft.Extensions.Logging;

#if FFMPEG_BUILD_IN
namespace Beutl.Embedding.FFmpeg;
#else
namespace Beutl.Extensions.FFmpeg;
#endif

public static class FFmpegLoader
{
    private static readonly ILogger s_logger = BeutlApplication.Current.LoggerFactory.CreateLogger(typeof(FFmpegLoader));
    private static bool s_isInitialized;
    private static readonly string s_defaultFFmpegExePath;
    private static readonly string s_defaultFFmpegPath;

    static FFmpegLoader()
    {
        s_defaultFFmpegPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".beutl", "ffmpeg");
        s_defaultFFmpegExePath = Path.Combine(s_defaultFFmpegPath, OperatingSystem.IsWindows() ? "ffmpeg.exe" : "ffmpeg");
    }

    public static void Initialize()
    {
        if (s_isInitialized)
            return;

        try
        {
            ffmpeg.RootPath = GetRootPath();

            foreach (KeyValuePair<string, int> item in ffmpeg.LibraryVersionMap)
            {
                s_logger.LogInformation("{LibraryName} {Version}", item.Key, item.Value);
            }

            s_logger.LogInformation("avcodec_license() {License}", ffmpeg.avcodec_license());
            s_logger.LogInformation("avdevice_license() {License}", ffmpeg.avdevice_license());
            s_logger.LogInformation("avfilter_license() {License}", ffmpeg.avfilter_license());
            s_logger.LogInformation("avformat_license() {License}", ffmpeg.avformat_license());
            s_logger.LogInformation("avutil_license() {License}", ffmpeg.avutil_license());
            s_logger.LogInformation("postproc_license() {License}", ffmpeg.postproc_license());
            s_logger.LogInformation("swresample_license() {License}", ffmpeg.swresample_license());
            s_logger.LogInformation("swscale_license() {License}", ffmpeg.swscale_license());

            s_isInitialized = true;
        }
        catch
        {
            NotificationService.ShowError(
                "FFmpeg error",
                "FFmpegがインストールされているかを確認してください。",
                onActionButtonClick: OpenDocumentUrl,
                actionButtonText: "ドキュメントを開く");

            throw;
        }
    }

    private static void OpenDocumentUrl()
    {
        Process.Start(new ProcessStartInfo("https://github.com/b-editor/beutl-docs/blob/main/ja/ffmpeg-install.md")
        {
            UseShellExecute = true,
            Verb = "open"
        });
    }

    public static string GetExecutable()
    {
        var paths = new List<string>
        {
            s_defaultFFmpegExePath,
            Path.Combine(AppContext.BaseDirectory, OperatingSystem.IsWindows() ? "ffmpeg.exe" : "ffmpeg")
        };

        if (OperatingSystem.IsLinux())
        {
            paths.Add("/usr/bin/ffmpeg");
        }

        foreach (string item in paths)
        {
            if (File.Exists(item))
            {
                return item;
            }
        }

        throw new InvalidOperationException();
    }

    public static string GetRootPath()
    {
        var paths = new List<string>
        {
            s_defaultFFmpegPath,
            Directory.GetParent(Assembly.GetExecutingAssembly().Location)!.FullName,
            AppContext.BaseDirectory
        };

        if (OperatingSystem.IsWindows())
        {
            paths.Add(Path.Combine(Directory.GetParent(Assembly.GetExecutingAssembly().Location)!.FullName,
                "runtimes",
                Environment.Is64BitProcess ? "win-x64" : "win-x86",
                "native"));

            paths.Add(Path.Combine(AppContext.BaseDirectory,
                "runtimes",
                Environment.Is64BitProcess ? "win-x64" : "win-x86",
                "native"));
        }
        else if (OperatingSystem.IsLinux())
        {
            paths.Add($"/usr/lib/{(Environment.Is64BitProcess ? "x86_64" : "x86")}-linux-gnu");
        }

        foreach (string item in paths)
        {
            if (LibrariesExists(item))
            {
                return item;
            }
        }

        throw new InvalidOperationException();
    }

    private static bool LibrariesExists(string basePath)
    {
        if (!Directory.Exists(basePath)) return false;

        string[] files = Directory.GetFiles(basePath);
        foreach (KeyValuePair<string, int> item in ffmpeg.LibraryVersionMap)
        {
            if (!files.Any(x => x.Contains(item.Key)))
            {
                return false;
            }
        }

        return true;
    }
}
