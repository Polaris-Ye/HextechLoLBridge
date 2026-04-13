using System.Reflection;
using System.Text.Json;
using HextechLoLBridge.Core.Models;
using HextechLoLBridge.Core.Riot;
using HextechLoLBridge.Core.Services;
using Microsoft.UI.Xaml;
using Microsoft.Web.WebView2.Core;
using Windows.Graphics;

namespace HextechLoLBridge.App;

public sealed partial class MainWindow : Window
{
    private readonly InMemoryLogService _logger;
    private readonly RiotLiveClientService _riotLiveClientService;
    private readonly LeagueClientApiService _leagueClientApiService;
    private readonly GamePollingService _pollingService;
    private readonly LightingProfileService _profileService;
    private readonly ILightingService _lightingService;
    private readonly AppVersionSnapshot _versionSnapshot;
    private readonly JsonSerializerOptions _jsonSerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    private bool _webReady;

    public MainWindow()
    {
        InitializeComponent();

        _logger = new InMemoryLogService();
        _profileService = new LightingProfileService(_logger);
        _riotLiveClientService = new RiotLiveClientService(new RiotLiveClientSettings(), _logger);
        _leagueClientApiService = new LeagueClientApiService(_logger);
        _pollingService = new GamePollingService(_riotLiveClientService, _leagueClientApiService, _profileService, _logger, TimeSpan.FromMilliseconds(600));
        _lightingService = new LogitechLedSdkService(_logger, _profileService);
        _versionSnapshot = AppVersionService.CreateSnapshot(Assembly.GetExecutingAssembly());

        RootGrid.Loaded += OnLoaded;
        Closed += OnClosed;
        _pollingService.SnapshotUpdated += OnSnapshotUpdated;
        _pollingService.StatusChanged += OnStatusChanged;

        Title = $"Hextech LoL Bridge {_versionSnapshot.Version}";
        TryResizeWindow(1460, 940);
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        RootGrid.Loaded -= OnLoaded;
        await InitializeWebViewAsync();
        await _lightingService.InitializeAsync();
        PushLightingStatus(_lightingService.Status);
        await _lightingService.ApplyPlaceholderFrameAsync();
        PushLightingStatus(_lightingService.Status);
        await _pollingService.StartAsync();
    }

    private async void OnClosed(object sender, WindowEventArgs args)
    {
        await _pollingService.StopAsync();
        await _lightingService.ShutdownAsync();
    }

    private async Task InitializeWebViewAsync()
    {
        await MainWebView.EnsureCoreWebView2Async();
        MainWebView.NavigationCompleted += OnNavigationCompleted;

        if (MainWebView.CoreWebView2 is null)
        {
            _logger.Error("WebView2 初始化失败：CoreWebView2 为空。");
            return;
        }

        MainWebView.CoreWebView2.Settings.AreDevToolsEnabled = true;
        MainWebView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
        MainWebView.CoreWebView2.Settings.AreBrowserAcceleratorKeysEnabled = false;
        MainWebView.CoreWebView2.Settings.IsStatusBarEnabled = false;
        MainWebView.CoreWebView2.Settings.IsZoomControlEnabled = false;
        MainWebView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;

        var webRoot = Path.Combine(AppContext.BaseDirectory, "Assets", "Web");
        MainWebView.CoreWebView2.SetVirtualHostNameToFolderMapping(
            "appassets",
            webRoot,
            CoreWebView2HostResourceAccessKind.Allow);

        MainWebView.Source = new Uri("https://appassets/index.html");
    }

    private void OnNavigationCompleted(Microsoft.UI.Xaml.Controls.WebView2 sender, CoreWebView2NavigationCompletedEventArgs args)
    {
        if (!args.IsSuccess)
        {
            _logger.Error($"WebView2 页面加载失败：{args.WebErrorStatus}");
        }
    }

    private async void OnWebMessageReceived(CoreWebView2 sender, CoreWebView2WebMessageReceivedEventArgs args)
    {
        try
        {
            using var document = JsonDocument.Parse(args.WebMessageAsJson);
            var root = document.RootElement;
            var type = root.TryGetProperty("type", out var typeElement) ? typeElement.GetString() : null;
            var payload = root.TryGetProperty("payload", out var payloadElement) ? payloadElement : default;

            switch (type)
            {
                case "app:ready":
                    _webReady = true;
                    PushBootstrapPayload();
                    PushStatus(_pollingService.Status);
                    PushSnapshot(_pollingService.LastSnapshot);
                    PushLogs();
                    PushLightingStatus(_lightingService.Status);
                    break;

                case "sdk:initialize":
                    await _lightingService.InitializeAsync();
                    await _lightingService.ApplyPlaceholderFrameAsync();
                    PushLightingStatus(_lightingService.Status);
                    PushLogs();
                    break;

                case "lighting:test-theme":
                    {
                        var hex = TryGetString(payload, "hex") ?? "#00E5FF";
                        var label = TryGetString(payload, "label") ?? "测试色板";
                        await _lightingService.ApplyThemeHexAsync(hex, label);
                        PushLightingStatus(_lightingService.Status);
                        PushLogs();
                    }
                    break;

                case "lighting:sync-current":
                    await _lightingService.ApplySnapshotAsync(DecorateSnapshot(_pollingService.LastSnapshot));
                    PushLightingStatus(_lightingService.Status);
                    PushLogs();
                    break;

                case "queue:accept-ready":
                    {
                        var result = await _leagueClientApiService.AcceptReadyCheckAsync();
                        if (result.Success)
                        {
                            _logger.Info(result.Message);
                        }
                        else
                        {
                            _logger.Warn(result.Message);
                        }

                        await _pollingService.RefreshOnceAsync();
                        PushLogs();
                    }
                    break;

                case "settings:set-spell-color":
                    {
                        var spellId = TryGetString(payload, "spellId");
                        var hex = TryGetString(payload, "hex");
                        if (!string.IsNullOrWhiteSpace(spellId) && !string.IsNullOrWhiteSpace(hex))
                        {
                            _profileService.SetSpellColor(spellId!, hex!);
                            PushProfile();
                            PushSnapshot(_pollingService.LastSnapshot);
                            await _lightingService.ApplySnapshotAsync(DecorateSnapshot(_pollingService.LastSnapshot));
                            PushLightingStatus(_lightingService.Status);
                        }
                    }
                    break;

                case "settings:reset-spell-color":
                    {
                        var spellId = TryGetString(payload, "spellId");
                        if (!string.IsNullOrWhiteSpace(spellId))
                        {
                            _profileService.ResetSpellColor(spellId!);
                            PushProfile();
                            PushSnapshot(_pollingService.LastSnapshot);
                            await _lightingService.ApplySnapshotAsync(DecorateSnapshot(_pollingService.LastSnapshot));
                            PushLightingStatus(_lightingService.Status);
                        }
                    }
                    break;

                case "settings:set-hero-color":
                    {
                        var championKey = TryGetString(payload, "championKey");
                        var hex = TryGetString(payload, "hex");
                        if (!string.IsNullOrWhiteSpace(championKey) && !string.IsNullOrWhiteSpace(hex))
                        {
                            _profileService.SetHeroColor(championKey!, hex!);
                            PushProfile(_pollingService.LastSnapshot.ActivePlayer.ChampionName);
                            PushSnapshot(_pollingService.LastSnapshot);
                            await _lightingService.ApplySnapshotAsync(DecorateSnapshot(_pollingService.LastSnapshot));
                            PushLightingStatus(_lightingService.Status);
                        }
                    }
                    break;

                case "settings:reset-hero-color":
                    {
                        var championKey = TryGetString(payload, "championKey");
                        if (!string.IsNullOrWhiteSpace(championKey))
                        {
                            _profileService.ResetHeroColor(championKey!);
                            PushProfile(_pollingService.LastSnapshot.ActivePlayer.ChampionName);
                            PushSnapshot(_pollingService.LastSnapshot);
                            await _lightingService.ApplySnapshotAsync(DecorateSnapshot(_pollingService.LastSnapshot));
                            PushLightingStatus(_lightingService.Status);
                        }
                    }
                    break;

                case "settings:set-key-mapping":
                    {
                        var actionId = TryGetString(payload, "actionId");
                        var keyCode = TryGetString(payload, "keyCode");
                        if (!string.IsNullOrWhiteSpace(actionId) && !string.IsNullOrWhiteSpace(keyCode))
                        {
                            _profileService.SetKeyMapping(actionId!, keyCode!);
                            PushProfile();
                            await _lightingService.ApplySnapshotAsync(DecorateSnapshot(_pollingService.LastSnapshot));
                            PushLightingStatus(_lightingService.Status);
                        }
                    }
                    break;

                case "settings:reset-key-mapping":
                    {
                        var actionId = TryGetString(payload, "actionId");
                        if (!string.IsNullOrWhiteSpace(actionId))
                        {
                            _profileService.ResetKeyMapping(actionId!);
                            PushProfile();
                            await _lightingService.ApplySnapshotAsync(DecorateSnapshot(_pollingService.LastSnapshot));
                            PushLightingStatus(_lightingService.Status);
                        }
                    }
                    break;

                case "polling:start":
                    await _pollingService.StartAsync();
                    break;

                case "polling:stop":
                    await _pollingService.StopAsync();
                    break;

                case "snapshot:refresh":
                    await _pollingService.RefreshOnceAsync();
                    break;

                case "logs:clear":
                    _logger.Clear();
                    PushLogs();
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"处理 WebView2 消息失败：{ex.Message}");
            PushLogs();
        }
    }

    private async void OnSnapshotUpdated(object? sender, LeagueSnapshot snapshot)
    {
        var decorated = DecorateSnapshot(snapshot);
        PushProfile(decorated.ActivePlayer.ChampionName);
        PushSnapshot(decorated);
        await _lightingService.ApplySnapshotAsync(decorated);
        PushLightingStatus(_lightingService.Status);
        PushLogs();
    }

    private void OnStatusChanged(object? sender, PollingRuntimeStatus status)
    {
        PushStatus(status);
    }

    private void PushBootstrapPayload()
    {
        PostMessage(new
        {
            type = "bootstrap",
            payload = new
            {
                app = _versionSnapshot,
                theme = new
                {
                    background = "#0A1428",
                    surface = "#112240",
                    border = "#1E2A38",
                    gold = "#C8AA6E",
                    cyan = "#00E5FF",
                    danger = "#FF3434"
                },
                profile = _profileService.GetSnapshot(_pollingService.LastSnapshot.ActivePlayer.ChampionName),
                sdkStatus = _lightingService.Status
            }
        });
    }

    private void PushProfile(string? activeChampionName = null)
    {
        PostMessage(new
        {
            type = "profile",
            payload = _profileService.GetSnapshot(activeChampionName ?? _pollingService.LastSnapshot.ActivePlayer.ChampionName)
        });
    }

    private void PushSnapshot(LeagueSnapshot snapshot)
    {
        PostMessage(new
        {
            type = "snapshot",
            payload = DecorateSnapshot(snapshot)
        });
    }

    private void PushStatus(PollingRuntimeStatus status)
    {
        PostMessage(new
        {
            type = "polling-status",
            payload = status
        });
    }

    private void PushLogs()
    {
        PostMessage(new
        {
            type = "logs",
            payload = _logger.GetEntries()
        });
    }

    private void PushLightingStatus(LogitechSdkStatusSnapshot status)
    {
        PostMessage(new
        {
            type = "sdk-status",
            payload = status
        });
    }

    private LeagueSnapshot DecorateSnapshot(LeagueSnapshot snapshot)
        => _profileService.ApplyOverrides(snapshot);

    private void PostMessage(object message)
    {
        if (!_webReady)
        {
            return;
        }

        var json = JsonSerializer.Serialize(message, _jsonSerializerOptions);
        DispatcherQueue.TryEnqueue(() => MainWebView.CoreWebView2?.PostWebMessageAsJson(json));
    }

    private static string? TryGetString(JsonElement payload, string propertyName)
        => payload.ValueKind == JsonValueKind.Object && payload.TryGetProperty(propertyName, out var element)
            ? element.GetString()
            : null;

    private void TryResizeWindow(int width, int height)
    {
        try
        {
            AppWindow.Resize(new SizeInt32(width, height));
        }
        catch
        {
        }
    }
}
