using System.Reflection;
using System.Runtime.InteropServices;
using HextechLoLBridge.Core.Models;

namespace HextechLoLBridge.Core.Services;

public static class AppVersionService
{
    public static AppVersionSnapshot CreateSnapshot(Assembly assembly)
    {
        var informationalVersion = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion ?? assembly.GetName().Version?.ToString() ?? "0.0.0";

        var product = assembly
            .GetCustomAttribute<AssemblyProductAttribute>()?
            .Product ?? assembly.GetName().Name ?? "Hextech LoL Bridge";

        return new AppVersionSnapshot(
            Product: product,
            Version: informationalVersion,
            Framework: RuntimeInformation.FrameworkDescription,
            RuntimeDescription: RuntimeInformation.OSDescription);
    }
}
