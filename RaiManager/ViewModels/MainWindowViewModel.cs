﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using RaiManager.Models.GameProviders;
using RaiManager.Models.Manifest;
using RaiManager.Models.Settings;
using ReactiveUI;

namespace RaiManager.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private string _windowTitle = $"Team Beef PCVR Installer";
    public string WindowTitle
    {
        get => _windowTitle;
        private set => this.RaiseAndSetIfChanged(ref _windowTitle, value);
    }
    
    private const string IconPath = "./Mod/icon.png";
    private Bitmap? _icon;
    public Bitmap? Icon
    {
        get => _icon;
        private set => this.RaiseAndSetIfChanged(ref _icon, value);
    }
        
    private string _statusText = "Loading...";
    public string StatusText
    {
        get => _statusText;
        set => this.RaiseAndSetIfChanged(ref _statusText, value);
    }
        
    private string _supportedProvidersText = "Loading...";
    public string SupportedProvidersText
    {
        get => _supportedProvidersText;
        set => this.RaiseAndSetIfChanged(ref _supportedProvidersText, value);
    }

    private List<GameProvider> _gameProviders = new();
    public List<GameProvider> GameProviders
    {
        get => _gameProviders;
        set => this.RaiseAndSetIfChanged(ref _gameProviders, value);
    }

    private AppManifest? _manifest;
    public AppManifest? Manifest
    {
        get => _manifest;
        private set => this.RaiseAndSetIfChanged(ref _manifest, value);
    }

    private ManualProvider? _manualProvider;
    private AppSettings? _appSettings;

    public MainWindowViewModel()
    {
        SetUp();
    }

    private async void SetUp()
    {
        SetUpWindowTitle();
        
        LoadIcon();
            
        _manualProvider = new ManualProvider("", false);

        try
        {
            Manifest = await AppManifest.LoadManifest();
        }
        catch (Exception exception)
        {
            StatusText = $"Failed to read mod manifest. This might mean the files are corrupted, try re-downloading and re-installing.\n\n{exception}";
            return;
        }

        _appSettings = await AppSettings.LoadSettings(Manifest, _manualProvider);

        var gameProviders = Manifest.Providers.Select(GameProvider.Create).ToList();
        SupportedProvidersText = string.Join(", ", gameProviders.Select(provider => provider.DisplayName));
        gameProviders.Insert(0, _manualProvider);
        GameProviders = gameProviders;

        UpdateStatusText();
    }

    private void UpdateStatusText()
    {
        var hasAvailableProvider = GameProviders.Any(provider => provider.IsAvailable);

        StatusText = hasAvailableProvider
            ? "If the game can't be found automatically, drag the game exe and drop it on this window."
            : $"Failed to find the game automatically. Drag the game exe and drop it on this window to install {Manifest?.ModTitle ?? "the mod"}";
    }

    public void DropFiles(IEnumerable<string> files)
    {
        if (_manualProvider == null || Manifest == null) return;
        
        var firstExePath = files.FirstOrDefault(file => Path.GetExtension(file) == ".exe");

        if (firstExePath == null)
        {
            throw new FileNotFoundException("None of the files dropped have the exe extension");
        }

        if (_manualProvider.SetGamePath(firstExePath))
        {
            AppSettings.WriteSettings(_appSettings, firstExePath, Manifest);
        }

        UpdateStatusText();
    }
        
    private async void LoadIcon()
    {
        if (File.Exists(IconPath))
        {
            Icon = await Task.Run(() => Bitmap.DecodeToWidth(File.OpenRead(IconPath), 400));
        }
    }
    
    public static void OnClickOpenModFolder()
    {
        var modFolderPath = Path.GetDirectoryName("./Mod/");

        if (!Directory.Exists(modFolderPath)) return;
        
        Process.Start(new ProcessStartInfo
        {
            FileName = Path.GetDirectoryName("./Mod/"),
            UseShellExecute = true,
            Verb = "open"
        });
    }
    
    public static void ClickShowDebugLogs()
    {
        var logsFilePath = Path.GetFullPath("./Mod/BepInEx/LogOutput.log");

        if (!File.Exists(logsFilePath)) return;
        
        Process.Start(new ProcessStartInfo
        {
            FileName = logsFilePath,
            UseShellExecute = true,
            Verb = "open"
        });
    }

    private void SetUpWindowTitle()
    {
        WindowTitle = $"Team Beef PCVR Installer {Assembly.GetExecutingAssembly().GetName().Version}";
    }
}