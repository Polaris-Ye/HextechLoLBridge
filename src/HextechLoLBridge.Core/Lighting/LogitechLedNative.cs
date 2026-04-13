using System.Runtime.InteropServices;

namespace HextechLoLBridge.Core.Lighting;

internal static class LogitechLedNative
{
    private const string DllName = "LogitechLedEnginesWrapper.dll";

    private const int LogiDeviceTypeMonochrome = 0x1;
    private const int LogiDeviceTypeRgb = 0x2;
    private const int LogiDeviceTypePerKeyRgb = 0x4;
    private const int LogiDeviceTypeAll = LogiDeviceTypeMonochrome | LogiDeviceTypeRgb | LogiDeviceTypePerKeyRgb;

    [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool SetDllDirectory(string? lpPathName);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern bool LogiLedInit();

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void LogiLedShutdown();

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern bool LogiLedSaveCurrentLighting();

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern bool LogiLedRestoreLighting();

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern bool LogiLedSetLighting(int redPercentage, int greenPercentage, int bluePercentage);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern bool LogiLedSetTargetDevice(int targetDevice);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern bool LogiLedPulseLighting(int redPercentage, int greenPercentage, int bluePercentage, int durationMilliseconds, int intervalMilliseconds);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern bool LogiLedStopEffects();

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern bool LogiLedSetLightingForKeyWithScanCode(int scanCode, int redPercentage, int greenPercentage, int bluePercentage);


    public static bool TrySetRgbTargetDevice(out string message)
    {
        try
        {
            var result = LogiLedSetTargetDevice(LogiDeviceTypeRgb);
            message = result ? "已切换到 RGB 设备（如鼠标/耳机）。" : "LogiLedSetTargetDevice(RGB) 返回失败。";
            return result;
        }
        catch (Exception ex)
        {
            message = $"切换 RGB 设备失败：{ex.Message}";
            return false;
        }
    }

    public static bool TryRestoreLighting(out string message)
    {
        try
        {
            var result = LogiLedRestoreLighting();
            message = result ? "已把灯光控制权交还给 G HUB / 原灯效。" : "LogiLedRestoreLighting 返回失败。";
            return result;
        }
        catch (Exception ex)
        {
            message = $"恢复原灯效失败：{ex.Message}";
            return false;
        }
    }
    public static bool TrySetAllTargetDevices(out string message)
    {
        try
        {
            var result = LogiLedSetTargetDevice(LogiDeviceTypeAll);
            message = result ? "已切换到全部 Logitech 灯光设备。" : "LogiLedSetTargetDevice(ALL) 返回失败。";
            return result;
        }
        catch (Exception ex)
        {
            message = $"切换目标设备失败：{ex.Message}";
            return false;
        }
    }

    public static bool TrySetPerKeyTargetDevice(out string message)
    {
        try
        {
            var result = LogiLedSetTargetDevice(LogiDeviceTypePerKeyRgb);
            message = result ? "已切换到每键 RGB 设备。" : "LogiLedSetTargetDevice(PERKEY_RGB) 返回失败。";
            return result;
        }
        catch (Exception ex)
        {
            message = $"切换每键 RGB 设备失败：{ex.Message}";
            return false;
        }
    }

    public static bool TryInit(string searchDirectory, out string message)
    {
        try
        {
            SetDllDirectory(searchDirectory);
            var result = LogiLedInit();
            if (!result)
            {
                message = "LogiLedInit 已调用，但 SDK 返回失败。请确认 G HUB 已安装并正在运行。";
                return false;
            }

            LogiLedSaveCurrentLighting();
            message = "Logitech LED SDK 初始化成功。";
            return true;
        }
        catch (DllNotFoundException ex)
        {
            message = $"未找到 LogitechLedEnginesWrapper.dll：{ex.Message}";
            return false;
        }
        catch (EntryPointNotFoundException ex)
        {
            message = $"已找到 DLL，但导出函数不匹配：{ex.Message}";
            return false;
        }
        catch (Exception ex)
        {
            message = $"初始化 Logitech SDK 失败：{ex.Message}";
            return false;
        }
    }

    public static bool TryApplyStaticColor(int redPercentage, int greenPercentage, int bluePercentage, out string message)
    {
        try
        {
            var result = LogiLedSetLighting(redPercentage, greenPercentage, bluePercentage);
            message = result ? "已向 Logitech SDK 发出静态灯色。" : "LogiLedSetLighting 返回失败。";
            return result;
        }
        catch (Exception ex)
        {
            message = $"设置灯光失败：{ex.Message}";
            return false;
        }
    }

    public static bool TryPulseColor(int redPercentage, int greenPercentage, int bluePercentage, int durationMilliseconds, int intervalMilliseconds, out string message)
    {
        try
        {
            var result = LogiLedPulseLighting(redPercentage, greenPercentage, bluePercentage, durationMilliseconds, intervalMilliseconds);
            message = result ? "已向 Logitech SDK 发出脉冲灯效。" : "LogiLedPulseLighting 返回失败。";
            return result;
        }
        catch (Exception ex)
        {
            message = $"设置脉冲灯效失败：{ex.Message}";
            return false;
        }
    }

    public static bool TryApplyColorToScanCode(int scanCode, int redPercentage, int greenPercentage, int bluePercentage, out string message)
    {
        try
        {
            var result = LogiLedSetLightingForKeyWithScanCode(scanCode, redPercentage, greenPercentage, bluePercentage);
            message = result
                ? $"已向 Logitech SDK 发出键位灯色（ScanCode={scanCode}）。"
                : $"LogiLedSetLightingForKeyWithScanCode 返回失败（ScanCode={scanCode}）。";
            return result;
        }
        catch (Exception ex)
        {
            message = $"设置单键灯光失败（ScanCode={scanCode}）：{ex.Message}";
            return false;
        }
    }

    public static void TryStopEffects()
    {
        try
        {
            LogiLedStopEffects();
        }
        catch
        {
        }
    }

    public static void TryShutdown()
    {
        try
        {
            LogiLedRestoreLighting();
            LogiLedShutdown();
        }
        catch
        {
        }
    }
}
