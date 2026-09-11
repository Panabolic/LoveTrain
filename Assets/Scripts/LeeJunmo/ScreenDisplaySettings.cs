using System.Collections.Generic;
using UnityEngine;

internal enum ScreenDisplayModeOption
{
    Windowed = 0,
    Fullscreen = 1,
    Borderless = 2
}

internal struct ScreenResolutionOption
{
    public int Width;
    public int Height;

    public ScreenResolutionOption(int width, int height)
    {
        Width = width;
        Height = height;
    }

    public string Label => Width + " x " + Height;
}

internal static class ScreenDisplaySettings
{
    private const string ModeKey = "ScreenDisplayMode";
    private const string WindowWidthKey = "WindowResolutionWidth";
    private const string WindowHeightKey = "WindowResolutionHeight";

    private static readonly ScreenResolutionOption[] fallbackWindowResolutions =
    {
        new ScreenResolutionOption(800, 600),
        new ScreenResolutionOption(1024, 768),
        new ScreenResolutionOption(1280, 720),
        new ScreenResolutionOption(1600, 900),
        new ScreenResolutionOption(1920, 1080),
        new ScreenResolutionOption(2560, 1440),
        new ScreenResolutionOption(3840, 2160)
    };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ApplySavedPreferencesOnStartup()
    {
        ApplySavedPreferences();
    }

    public static void ApplySavedPreferences()
    {
        ApplyDisplayMode(GetSavedDisplayMode(), save: false);
    }

    public static ScreenDisplayModeOption GetSavedDisplayMode()
    {
        if (PlayerPrefs.HasKey(ModeKey))
        {
            int value = PlayerPrefs.GetInt(ModeKey, (int)GetCurrentDisplayMode());
            return ClampDisplayMode(value);
        }

        return GetCurrentDisplayMode();
    }

    public static ScreenDisplayModeOption GetCurrentDisplayMode()
    {
        switch (Screen.fullScreenMode)
        {
            case FullScreenMode.Windowed:
                return ScreenDisplayModeOption.Windowed;
            case FullScreenMode.FullScreenWindow:
                return ScreenDisplayModeOption.Borderless;
            default:
                return ScreenDisplayModeOption.Borderless;
        }
    }

    public static List<ScreenResolutionOption> GetWindowResolutionOptions()
    {
        List<ScreenResolutionOption> options = new List<ScreenResolutionOption>();

        Resolution[] supportedResolutions = Screen.resolutions;
        for (int i = 0; i < supportedResolutions.Length; i++)
        {
            AddUnique(options, supportedResolutions[i].width, supportedResolutions[i].height);
        }

        for (int i = 0; i < fallbackWindowResolutions.Length; i++)
        {
            AddUnique(options, fallbackWindowResolutions[i].Width, fallbackWindowResolutions[i].Height);
        }

        AddUnique(options, Screen.width, Screen.height);
        SortByWidth(options);
        return options;
    }

    public static ScreenResolutionOption GetSavedWindowResolution()
    {
        int width = PlayerPrefs.GetInt(WindowWidthKey, 1280);
        int height = PlayerPrefs.GetInt(WindowHeightKey, 720);

        if (width <= 0 || height <= 0)
        {
            return new ScreenResolutionOption(1280, 720);
        }

        return new ScreenResolutionOption(width, height);
    }

    public static ScreenResolutionOption GetResolutionForDisplayMode(ScreenDisplayModeOption mode)
    {
        if (mode == ScreenDisplayModeOption.Windowed)
        {
            return GetSavedWindowResolution();
        }

        return GetNativeResolution();
    }

    public static void ApplyDisplayMode(ScreenDisplayModeOption mode, bool save = true)
    {
        if (save)
        {
            PlayerPrefs.SetInt(ModeKey, (int)mode);
        }

        if (mode == ScreenDisplayModeOption.Windowed)
        {
            ScreenResolutionOption resolution = GetSavedWindowResolution();
            Screen.SetResolution(resolution.Width, resolution.Height, FullScreenMode.Windowed);
            FixedAspectRatioController.RequestRefresh();
        }
        else
        {
            ScreenResolutionOption nativeResolution = GetNativeResolution();
            Screen.SetResolution(nativeResolution.Width, nativeResolution.Height, FullScreenMode.FullScreenWindow);
            FixedAspectRatioController.RequestRefresh();
        }

        if (save)
        {
            PlayerPrefs.Save();
        }
    }

    public static void ApplyWindowResolution(ScreenResolutionOption resolution)
    {
        SaveWindowResolution(resolution, saveImmediately: false);
        Screen.SetResolution(resolution.Width, resolution.Height, FullScreenMode.Windowed);
        FixedAspectRatioController.RequestRefresh();
        PlayerPrefs.SetInt(ModeKey, (int)ScreenDisplayModeOption.Windowed);
        PlayerPrefs.Save();
    }

    public static void SaveWindowResolution(ScreenResolutionOption resolution, bool saveImmediately = true)
    {
        PlayerPrefs.SetInt(WindowWidthKey, resolution.Width);
        PlayerPrefs.SetInt(WindowHeightKey, resolution.Height);

        if (saveImmediately)
        {
            PlayerPrefs.Save();
        }
    }

    public static int FindClosestResolutionIndex(List<ScreenResolutionOption> options, ScreenResolutionOption resolution)
    {
        if (options == null || options.Count == 0)
        {
            return 0;
        }

        int bestIndex = 0;
        int bestDistance = int.MaxValue;

        for (int i = 0; i < options.Count; i++)
        {
            int widthDistance = Mathf.Abs(options[i].Width - resolution.Width);
            int heightDistance = Mathf.Abs(options[i].Height - resolution.Height);
            int distance = widthDistance + heightDistance;

            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestIndex = i;
            }
        }

        return bestIndex;
    }

    private static ScreenDisplayModeOption ClampDisplayMode(int value)
    {
        if (value < (int)ScreenDisplayModeOption.Windowed || value > (int)ScreenDisplayModeOption.Borderless)
        {
            return ScreenDisplayModeOption.Borderless;
        }

        return (ScreenDisplayModeOption)value;
    }

    private static ScreenResolutionOption GetNativeResolution()
    {
        Resolution currentResolution = Screen.currentResolution;
        if (currentResolution.width > 0 && currentResolution.height > 0)
        {
            return new ScreenResolutionOption(currentResolution.width, currentResolution.height);
        }

        if (Display.main != null && Display.main.systemWidth > 0 && Display.main.systemHeight > 0)
        {
            return new ScreenResolutionOption(Display.main.systemWidth, Display.main.systemHeight);
        }

        return new ScreenResolutionOption(Mathf.Max(Screen.width, 1280), Mathf.Max(Screen.height, 720));
    }

    private static void AddUnique(List<ScreenResolutionOption> options, int width, int height)
    {
        if (width <= 0 || height <= 0)
        {
            return;
        }

        for (int i = 0; i < options.Count; i++)
        {
            if (options[i].Width == width && options[i].Height == height)
            {
                return;
            }
        }

        options.Add(new ScreenResolutionOption(width, height));
    }

    private static void SortByWidth(List<ScreenResolutionOption> options)
    {
        options.Sort((left, right) =>
        {
            int widthCompare = left.Width.CompareTo(right.Width);
            if (widthCompare != 0)
            {
                return widthCompare;
            }

            return left.Height.CompareTo(right.Height);
        });
    }
}
