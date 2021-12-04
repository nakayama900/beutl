﻿using System.Reflection;

using BEditorNext.Configuration;
using BEditorNext.Media;

namespace BEditorNext.Graphics.UnitTests;

public static class TypefaceProvider
{
    static TypefaceProvider()
    {
        GlobalConfiguration.Instance.FontConfig.FontDirectories.Clear();
        Assembly asm = typeof(TypefaceProvider).Assembly;
        string[] array = new string[]
        {
            "NotoSansJP-Black.otf",
            "NotoSansJP-Bold.otf",
            "NotoSansJP-Light.otf",
            "NotoSansJP-Medium.otf",
            "NotoSansJP-Regular.otf",
            "NotoSansJP-Thin.otf",
            "Roboto-Medium.ttf",
            "Roboto-Regular.ttf",
        };

        foreach (string item in array)
        {
            Stream? stream = asm.GetManifestResourceStream("BEditorNext.Graphics.UnitTests.Assets.Font." + item);

            if (stream != null)
            {
                FontManager.Instance.AddFont(stream);
                stream.Dispose();
            }
        }
    }

    public static Typeface Typeface()
    {
        return new Typeface(new FontFamily("Noto Sans JP"));
    }
}
