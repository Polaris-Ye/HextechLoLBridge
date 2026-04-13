using System.Globalization;
using HextechLoLBridge.Core.Catalog;
using HextechLoLBridge.Core.Lighting;
using HextechLoLBridge.Core.Models;

namespace HextechLoLBridge.Core.Services;

public sealed class LogitechLedSdkService : ILightingService
{
    private static readonly string[] HealthBarKeys = new[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "0", "MINUS", "EQUALS" };
    private static readonly string[] ManaBarOrder = new[] { "Z", "X", "C", "V", "B", "N", "M", "COMMA", "PERIOD", "SLASH" };
    private static readonly string[] CountdownKeys = new[] { "EQUALS", "MINUS", "0", "9", "8", "7", "6", "5", "4", "3", "2", "1" };
    private static readonly string[] PerimeterKeys = new[]
    {
        "ESC", "F1", "F2", "F3", "F4", "F5", "F6", "F7", "F8", "F9", "F10", "F11", "F12",
        "PRNTSCR", "SCROLLLOCK", "PAUSEBREAK",
        "GRAVE", "TAB", "CAPSLOCK", "BACKSPACE", "LSHIFT", "RSHIFT"
    };
    private static readonly string[] ManagedPerKeyKeys = PerimeterKeys
        .Concat(HealthBarKeys)
        .Concat(ManaBarOrder)
        .Concat(new[] { "Q", "W", "E", "R", "D", "F", "SPACE" })
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray();

    private readonly IAppLogger _logger;
    private readonly LightingProfileService _profileService;
    private bool _triedInitialize;
    private string? _lastAppliedSignature;

    public LogitechLedSdkService(IAppLogger logger, LightingProfileService profileService)
    {
        _logger = logger;
        _profileService = profileService;
        Status = LogitechSdkStatusSnapshot.NotStarted;
    }

    public LogitechSdkStatusSnapshot Status { get; private set; }

    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_triedInitialize && Status.IsInitialized)
        {
            return Task.CompletedTask;
        }

        _triedInitialize = true;

        var candidateDirectory = Path.Combine(AppContext.BaseDirectory, "native", "logitech");
        var candidateDll = Path.Combine(candidateDirectory, "LogitechLedEnginesWrapper.dll");

        if (!File.Exists(candidateDll))
        {
            Status = Status with
            {
                DllFound = false,
                IsInitialized = false,
                AdapterState = "dll-missing",
                Message = "当前输出目录里没有 LogitechLedEnginesWrapper.dll。",
                DllProbePath = candidateDll
            };

            _logger.Warn(Status.Message);
            return Task.CompletedTask;
        }

        var initSucceeded = LogitechLedNative.TryInit(candidateDirectory, out var message);
        Status = Status with
        {
            DllFound = true,
            IsInitialized = initSucceeded,
            AdapterState = initSucceeded ? "ready" : "init-failed",
            Message = message,
            DllProbePath = candidateDll
        };

        if (initSucceeded)
        {
            _lastAppliedSignature = null;
            _logger.Info(message);
        }
        else
        {
            _logger.Warn(message);
        }

        return Task.CompletedTask;
    }

    public Task ApplyPlaceholderFrameAsync(CancellationToken cancellationToken = default)
        => ReleaseLightingToGHubAsync("启动完成，当前默认把灯光控制权交还给 G HUB。", cancellationToken);

    public Task ApplyThemeHexAsync(string hex, string label, CancellationToken cancellationToken = default)
    {
        if (!Status.IsInitialized)
        {
            return Task.CompletedTask;
        }

        var signature = $"theme:{NormalizeHex(hex)}";
        if (signature == _lastAppliedSignature)
        {
            return Task.CompletedTask;
        }

        LogitechLedNative.TryStopEffects();
        LogitechLedNative.TrySetAllTargetDevices(out _);
        var color = ToBrightPercent(hex);
        if (LogitechLedNative.TryApplyStaticColor(color.R, color.G, color.B, out var message))
        {
            _lastAppliedSignature = signature;
            SetAppliedState("ready", label, $"{label} 已应用到整机静态灯色。", NormalizeHex(hex));
            _logger.Info(message);
        }
        else
        {
            Status = Status with { Message = message };
            _logger.Warn(message);
        }

        return Task.CompletedTask;
    }

    public Task ApplySnapshotAsync(LeagueSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        if (!Status.IsInitialized)
        {
            return Task.CompletedTask;
        }

        var inferredInGame = IsEffectivelyInGame(snapshot);

        if (inferredInGame)
        {
            if (snapshot.ActivePlayer.IsDead || snapshot.ActivePlayer.RespawnTimerSeconds > 0.01)
            {
                return ApplyDeathCountdownLightingAsync(snapshot.ActivePlayer, cancellationToken);
            }

            return ApplyInGameFunctionalLightingAsync(snapshot, cancellationToken);
        }

        return ReleaseLightingToGHubAsync(snapshot.ClientPhase.IsClientConnected
            ? $"当前为 {snapshot.ClientPhase.PhaseDisplayName} 阶段，灯光控制权已交还 G HUB。"
            : "当前不在对局中，灯光控制权已交还 G HUB。", cancellationToken);
    }

    public Task ShutdownAsync(CancellationToken cancellationToken = default)
    {
        if (Status.IsInitialized)
        {
            LogitechLedNative.TryShutdown();
            _logger.Info("已请求 Logitech SDK 恢复原灯效并关闭。");
        }

        return Task.CompletedTask;
    }

    private Task ApplyReadyCheckLightingAsync(ClientPhaseSnapshot clientPhase, CancellationToken cancellationToken)
    {
        var signature = $"ready-check:{clientPhase.ReadyCheckState}:{clientPhase.ReadyCheckPlayerResponse}:{clientPhase.ReadyCheckTimerSeconds}";
        if (_lastAppliedSignature == signature)
        {
            return Task.CompletedTask;
        }

        LogitechLedNative.TryStopEffects();
        LogitechLedNative.TrySetAllTargetDevices(out _);
        LogitechLedNative.TryPulseColor(25, 65, 100, 1200, 90, out _);
        var acceptApplied = ApplySingleKey("ready-check-accept", "#FFFFFF");
        _lastAppliedSignature = signature;
        SetAppliedState("ready", "准备确认蓝白闪烁", acceptApplied ? "Ready Check：空格白色闪烁，其它按键蓝色脉冲。" : "Ready Check：已退回整机蓝白脉冲，单键空格未命中。", "#FFFFFF");
        return Task.CompletedTask;
    }

    private Task ApplyChampSelectLightingAsync(ClientPhaseSnapshot clientPhase, CancellationToken cancellationToken)
    {
        var signature = $"champ-select:{clientPhase.ChampSelectTimerPhase}:{clientPhase.ChampSelectCountdownSeconds}:{string.Join(',', clientPhase.SummonerSpells.Select(x => x.SpellId + ':' + NormalizeHex(x.ThemeHex)))}";
        if (_lastAppliedSignature == signature)
        {
            return Task.CompletedTask;
        }

        LogitechLedNative.TryStopEffects();
        LogitechLedNative.TrySetAllTargetDevices(out _);
        LogitechLedNative.TryApplyStaticColor(0, 0, 0, out _);

        var lit = 0;
        var attempts = 0;
        var spells = clientPhase.SummonerSpells.ToArray();
        if (spells.Length > 0)
        {
            attempts++;
            lit += ApplySingleKey("summoner-1-ready", spells[0].ThemeHex) ? 1 : 0;
        }
        if (spells.Length > 1)
        {
            attempts++;
            lit += ApplySingleKey("summoner-2-ready", spells[1].ThemeHex) ? 1 : 0;
        }

        if (clientPhase.ChampSelectCountdownSeconds is int champSeconds)
        {
            var champUnits = Math.Clamp((int)Math.Ceiling(Math.Min(champSeconds, 10) / 10d * HealthBarKeys.Length), 0, HealthBarKeys.Length);
            attempts += champUnits;
            lit += ApplyCountdownBar(champUnits, "#EED766");
        }

        if (lit == 0 && attempts > 0)
        {
            var fallbackHex = spells.FirstOrDefault()?.ThemeHex ?? "#EED766";
            ApplyWholeKeyboardFallback(fallbackHex, "选人阶段回退整机预览");
        }

        _lastAppliedSignature = signature;
        SetAppliedState("ready", "选人阶段双招预览", lit > 0 ? $"选人阶段：已点亮 D / F 双招颜色，倒计时键位 {clientPhase.ChampSelectCountdownSeconds ?? 0}s。" : "选人阶段：单键未命中，已退回整机预览颜色。", spells.FirstOrDefault()?.ThemeHex ?? "#EED766");
        return Task.CompletedTask;
    }

    private Task ApplyInGameFunctionalLightingAsync(LeagueSnapshot snapshot, CancellationToken cancellationToken)
    {
        var player = snapshot.ActivePlayer;
        var signatureParts = new List<string>
        {
            "functional",
            player.IsDead.ToString(CultureInfo.InvariantCulture),
            player.RespawnTimerSeconds.ToString("0.0", CultureInfo.InvariantCulture),
            player.HealthPercent.ToString("0.000", CultureInfo.InvariantCulture),
            player.ResourcePercent.ToString("0.000", CultureInfo.InvariantCulture),
            player.ResourceType ?? string.Empty
        };

        foreach (var ability in player.Abilities.Where(x => x.Slot is "Q" or "W" or "E" or "R"))
        {
            signatureParts.Add($"{ability.Slot}:{ability.IsLearned}:{ability.IsReady}:{ability.CooldownSeconds}");
        }

        foreach (var spell in player.SummonerSpells)
        {
            signatureParts.Add($"{spell.SpellId}:{spell.IsReady}:{spell.CooldownSeconds}:{NormalizeHex(spell.ThemeHex)}");
        }

        var signature = string.Join('|', signatureParts);
        if (signature == _lastAppliedSignature)
        {
            return Task.CompletedTask;
        }

        var championThemeHex = _profileService.GetHeroHex(player.ChampionName);

        LogitechLedNative.TryStopEffects();
        ApplyRgbDeviceTheme(championThemeHex, $"{player.ChampionName} 主题鼠标灯");
        ClearPerKeyKeyboard();

        var litCount = 0;
        var attempts = 0;

        if (CanLightAbility(player.Abilities.FirstOrDefault(x => x.Slot == "Q"))) attempts++;
        litCount += TryApplyReadyAbility(player.Abilities.FirstOrDefault(x => x.Slot == "Q"), "ability-q-ready", championThemeHex);
        if (CanLightAbility(player.Abilities.FirstOrDefault(x => x.Slot == "W"))) attempts++;
        litCount += TryApplyReadyAbility(player.Abilities.FirstOrDefault(x => x.Slot == "W"), "ability-w-ready", championThemeHex);
        if (CanLightAbility(player.Abilities.FirstOrDefault(x => x.Slot == "E"))) attempts++;
        litCount += TryApplyReadyAbility(player.Abilities.FirstOrDefault(x => x.Slot == "E"), "ability-e-ready", championThemeHex);
        if (CanLightAbility(player.Abilities.FirstOrDefault(x => x.Slot == "R"))) attempts++;
        litCount += TryApplyReadyAbility(player.Abilities.FirstOrDefault(x => x.Slot == "R"), "ability-r-ready", championThemeHex);

        var spells = player.SummonerSpells.ToArray();
        if (CanLightSpell(spells.ElementAtOrDefault(0))) attempts++;
        litCount += TryApplyReadySpell(spells.ElementAtOrDefault(0), "summoner-1-ready");
        if (CanLightSpell(spells.ElementAtOrDefault(1))) attempts++;
        litCount += TryApplyReadySpell(spells.ElementAtOrDefault(1), "summoner-2-ready");

        attempts += PerimeterKeys.Length;
        litCount += ApplyPerimeterHeroColor(championThemeHex);

        var healthSegments = Math.Clamp((int)Math.Ceiling(player.HealthPercent * HealthBarKeys.Length), 0, HealthBarKeys.Length);
        attempts += healthSegments;
        litCount += ApplyHealthBar(player.HealthPercent, "#42E35D");

        var hasResourceBar = player.MaxResource > 0.01 && !string.Equals(player.ResourceType, "NONE", StringComparison.OrdinalIgnoreCase);
        var manaSegments = hasResourceBar ? Math.Clamp((int)Math.Ceiling(player.ResourcePercent * ManaBarOrder.Length), 0, ManaBarOrder.Length) : 0;
        attempts += manaSegments;
        if (hasResourceBar)
        {
            litCount += ApplyManaBar(player.ResourcePercent, ResolveManaHex(player.ResourceType));
        }

        _lastAppliedSignature = signature;

        if (litCount == 0)
        {
            if (attempts > 0)
            {
                var fallbackHex = ResolveInGameFallbackHex(player);
                ApplyWholeKeyboardFallback(fallbackHex, "局内单键未命中，已退回整机颜色");
                SetAppliedState("ready", "单键回退整机预览", "局内本应有亮键，但单键未命中，已退回整机颜色方便排查。", fallbackHex);
            }
            else
            {
                SetAppliedState("ready", "当前无可点亮功能", "当前没有 ready 且已映射的功能键，键盘保持熄灯。", "#000000");
            }

            return Task.CompletedTask;
        }

        SetAppliedState("ready", $"已点亮 {litCount} 个功能键", $"局内灯效：QWER/DF 功能键 + 顶部红/绿条 + 蓝条 + 英雄主题外圈，共 {litCount} 个亮点。", championThemeHex);
        return Task.CompletedTask;
    }

    private Task ApplyDeathCountdownLightingAsync(PlayerSnapshot player, CancellationToken cancellationToken)
    {
        var seconds = Math.Max(0, (int)Math.Ceiling(player.RespawnTimerSeconds));
        var signature = $"death:{seconds}";
        if (_lastAppliedSignature == signature)
        {
            return Task.CompletedTask;
        }

        LogitechLedNative.TryStopEffects();
        ApplyRgbDeviceTheme("#FF3434", "死亡阶段鼠标红色主题");
        ClearPerKeyKeyboard();

        var litCount = 0;
        var attempts = HealthBarKeys.Length + ManaBarOrder.Length;
        if (seconds > 0)
        {
            var countdownUnits = Math.Clamp((int)Math.Ceiling(Math.Min(seconds, 10) / 10d * CountdownKeys.Length), 0, CountdownKeys.Length);
            litCount += ApplyCountdownBar(countdownUnits, "#FF3434");
        }
        else
        {
            litCount += ApplyHealthBar(1, "#42E35D");
        }

        if (player.MaxResource > 0.01 && !string.Equals(player.ResourceType, "NONE", StringComparison.OrdinalIgnoreCase))
        {
            litCount += ApplyManaBar(player.ResourcePercent, ResolveManaHex(player.ResourceType));
        }

        if (litCount == 0 && attempts > 0)
        {
            ApplyWholeKeyboardFallback(seconds > 0 ? "#FF3434" : "#42E35D", "死亡/复活回退整机颜色");
        }

        _lastAppliedSignature = signature;
        SetAppliedState("ready", "死亡倒计时 / 顶部条", litCount > 0 ? (seconds > 0 ? $"死亡倒计时：{seconds}s，顶部 15 段宽条红色递减。" : "倒计时结束，顶部切到 15 段绿色血量条，并保留蓝条。") : (seconds > 0 ? "死亡倒计时：单键未命中，已退回整机红色。" : "复活后：单键未命中，已退回整机绿色。"), seconds > 0 ? "#FF3434" : "#42E35D");
        return Task.CompletedTask;
    }

    private int TryApplyReadyAbility(AbilitySnapshot? ability, string actionId, string hex)
    {
        if (!CanLightAbility(ability))
        {
            return 0;
        }

        return ApplySingleKey(actionId, hex) ? 1 : 0;
    }

    private int TryApplyReadySpell(SummonerSpellSnapshot? spell, string actionId)
    {
        if (!CanLightSpell(spell))
        {
            return 0;
        }

        return ApplySingleKey(actionId, spell.ThemeHex) ? 1 : 0;
    }

    private bool ApplySingleKey(string actionId, string hex)
    {
        var scanCode = _profileService.ResolveScanCodeForAction(actionId);
        if (scanCode == 0)
        {
            return false;
        }

        LogitechLedNative.TrySetPerKeyTargetDevice(out _);
        var color = ToBrightPercent(hex);
        if (!LogitechLedNative.TryApplyColorToScanCode(scanCode, color.R, color.G, color.B, out var message))
        {
            _logger.Warn(message);
            return false;
        }

        return true;
    }

    private int ApplyFixedKeys(IEnumerable<string> keyCodes, string hex)
    {
        var lit = 0;
        LogitechLedNative.TrySetPerKeyTargetDevice(out _);
        var color = ToBrightPercent(hex);
        foreach (var keyCode in keyCodes)
        {
            var scanCode = KeyboardLayoutCatalog.ResolveScanCode(keyCode);
            if (scanCode == 0)
            {
                continue;
            }

            if (LogitechLedNative.TryApplyColorToScanCode(scanCode, color.R, color.G, color.B, out var message))
            {
                lit++;
            }
            else
            {
                _logger.Warn(message);
            }
        }

        return lit;
    }

    private int ApplyCountdownBar(int remainingUnits, string hex)
    {
        if (remainingUnits <= 0)
        {
            return 0;
        }

        return ApplyFixedKeys(CountdownKeys.Take(remainingUnits), hex);
    }

    private int ApplyHealthBar(double percent, string hex)
    {
        var units = Math.Clamp((int)Math.Ceiling(percent * HealthBarKeys.Length), 0, HealthBarKeys.Length);
        if (units <= 0)
        {
            return 0;
        }

        return ApplyFixedKeys(HealthBarKeys.Take(units), hex);
    }

    private int ApplyManaBar(double manaPercent, string hex)
    {
        var segments = Math.Clamp((int)Math.Ceiling(manaPercent * ManaBarOrder.Length), 0, ManaBarOrder.Length);
        if (segments <= 0)
        {
            return 0;
        }

        return ApplyFixedKeys(ManaBarOrder.Take(segments), hex);
    }

    private void ClearPerKeyKeyboard()
    {
        LogitechLedNative.TrySetPerKeyTargetDevice(out _);
        foreach (var keyCode in ManagedPerKeyKeys)
        {
            var scanCode = KeyboardLayoutCatalog.ResolveScanCode(keyCode);
            if (scanCode == 0)
            {
                continue;
            }

            LogitechLedNative.TryApplyColorToScanCode(scanCode, 0, 0, 0, out _);
        }
    }

    private int ApplyPerimeterHeroColor(string hex)
        => ApplyFixedKeys(PerimeterKeys, hex);

    private void ApplyRgbDeviceTheme(string hex, string reason)
    {
        if (!LogitechLedNative.TrySetRgbTargetDevice(out var targetMessage))
        {
            _logger.Warn(targetMessage);
            return;
        }

        var color = ToBrightPercent(hex);
        if (!LogitechLedNative.TryApplyStaticColor(color.R, color.G, color.B, out var message))
        {
            _logger.Warn($"{reason}：{message}");
        }
    }

    private Task ReleaseLightingToGHubAsync(string label, CancellationToken cancellationToken)
    {
        if (!Status.IsInitialized)
        {
            return Task.CompletedTask;
        }

        const string signature = "released-to-ghub";
        if (_lastAppliedSignature == signature)
        {
            return Task.CompletedTask;
        }

        LogitechLedNative.TryStopEffects();
        if (LogitechLedNative.TryRestoreLighting(out var message))
        {
            _lastAppliedSignature = signature;
            SetAppliedState("ready", "控制权已交还 G HUB", label, "#000000");
            _logger.Info(message);
        }
        else
        {
            Status = Status with { Message = message };
            _logger.Warn(message);
        }

        return Task.CompletedTask;
    }

    private Task ApplyKeyboardOffAsync(string label, CancellationToken cancellationToken)
    {
        if (!Status.IsInitialized)
        {
            return Task.CompletedTask;
        }

        const string signature = "keyboard-off";
        if (_lastAppliedSignature == signature)
        {
            return Task.CompletedTask;
        }

        LogitechLedNative.TryStopEffects();
        LogitechLedNative.TrySetAllTargetDevices(out _);
        if (LogitechLedNative.TryApplyStaticColor(0, 0, 0, out var message))
        {
            _lastAppliedSignature = signature;
            SetAppliedState("ready", label, label, "#000000");
            _logger.Info(message);
        }
        else
        {
            Status = Status with { Message = message };
            _logger.Warn(message);
        }

        return Task.CompletedTask;
    }


    private static bool IsEffectivelyInGame(LeagueSnapshot snapshot)
    {
        if (snapshot.InGame)
        {
            return true;
        }

        if (snapshot.ActivePlayer.IsDead || snapshot.ActivePlayer.RespawnTimerSeconds > 0.01)
        {
            return true;
        }

        if (snapshot.ActivePlayer.MaxHealth > 0 || snapshot.ActivePlayer.MaxResource > 0)
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(snapshot.Game.GameMode) && snapshot.Game.GameMode != "-")
        {
            return true;
        }

        return snapshot.RecentEvents.Count > 0;
    }

    private static bool CanLightAbility(AbilitySnapshot? ability)
        => ability is not null
           && ability.IsLearned
           && (ability.IsReady == true || (ability.IsReady is null && (!ability.CooldownSeconds.HasValue || ability.CooldownSeconds.Value <= 0.01)))
           && (!ability.CooldownSeconds.HasValue || ability.CooldownSeconds.Value <= 0.01);

    private static bool CanLightSpell(SummonerSpellSnapshot? spell)
        => spell is not null
           && (spell.IsReady == true
               || (spell.IsReady is null && (!spell.CooldownSeconds.HasValue || spell.CooldownSeconds.Value <= 0.01))
               || (spell.CooldownSeconds.HasValue && spell.CooldownSeconds.Value <= 0.01));

    private string ResolveInGameFallbackHex(PlayerSnapshot player)
    {
        var championTheme = _profileService.GetHeroHex(player.ChampionName);
        if (CanLightAbility(player.Abilities.FirstOrDefault(x => x.Slot == "R"))) return championTheme;
        if (CanLightAbility(player.Abilities.FirstOrDefault(x => x.Slot == "Q"))) return championTheme;
        if (CanLightAbility(player.Abilities.FirstOrDefault(x => x.Slot == "W"))) return championTheme;
        if (CanLightAbility(player.Abilities.FirstOrDefault(x => x.Slot == "E"))) return championTheme;
        if (CanLightSpell(player.SummonerSpells.ElementAtOrDefault(0))) return player.SummonerSpells.ElementAtOrDefault(0)?.ThemeHex ?? "#FFFFFF";
        if (CanLightSpell(player.SummonerSpells.ElementAtOrDefault(1))) return player.SummonerSpells.ElementAtOrDefault(1)?.ThemeHex ?? "#FFFFFF";
        if (player.HealthPercent > 0) return "#42E35D";
        return "#00E5FF";
    }

    private static string ResolveManaHex(string? resourceType)
    {
        if (string.IsNullOrWhiteSpace(resourceType))
        {
            return "#3F8CFF";
        }

        return resourceType.ToUpperInvariant() switch
        {
            "MANA" => "#3F8CFF",
            "ENERGY" => "#FFD84D",
            "FURY" => "#FF6A3D",
            "RAGE" => "#FF6A3D",
            "HEAT" => "#FF7A45",
            "FEROCITY" => "#FF6A5C",
            "SHIELD" => "#9FE8FF",
            _ => "#3F8CFF"
        };
    }

    private void ApplyWholeKeyboardFallback(string hex, string reason)
    {
        LogitechLedNative.TrySetAllTargetDevices(out _);
        var color = ToBrightPercent(hex);
        if (LogitechLedNative.TryApplyStaticColor(color.R, color.G, color.B, out var message))
        {
            _logger.Warn($"{reason}：{message}");
        }
        else
        {
            _logger.Warn($"{reason}：{message}");
        }
    }

    private void SetAppliedState(string adapterState, string effectName, string message, string hex)
    {
        Status = Status with
        {
            AdapterState = adapterState,
            Message = message,
            ActiveEffect = effectName,
            ActiveHex = NormalizeHex(hex),
            LastAppliedAt = DateTimeOffset.Now
        };
    }

    private static BrightPercentColor ToBrightPercent(string? hex)
    {
        var normalized = NormalizeHex(hex);
        if (normalized.Length != 7)
        {
            return new BrightPercentColor(0, 0, 0);
        }

        var r = int.Parse(normalized[1..3], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        var g = int.Parse(normalized[3..5], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        var b = int.Parse(normalized[5..7], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        var max = Math.Max(1, Math.Max(r, Math.Max(g, b)));

        return new BrightPercentColor(
            Math.Clamp((int)Math.Round(r * 100d / max), 0, 100),
            Math.Clamp((int)Math.Round(g * 100d / max), 0, 100),
            Math.Clamp((int)Math.Round(b * 100d / max), 0, 100));
    }

    private static string NormalizeHex(string? hex)
    {
        if (string.IsNullOrWhiteSpace(hex))
        {
            return "#000000";
        }

        var text = hex.Trim();
        if (!text.StartsWith('#'))
        {
            text = $"#{text}";
        }

        return text.Length == 7 ? text.ToUpperInvariant() : "#000000";
    }

    private readonly record struct BrightPercentColor(int R, int G, int B);
}
