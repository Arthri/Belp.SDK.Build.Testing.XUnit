using Microsoft.Build.Locator;
using System.Runtime.CompilerServices;

file static class Initializer
{
#pragma warning disable CA2255 // The 'ModuleInitializer' attribute should not be used in libraries
    [ModuleInitializer]
#pragma warning restore CA2255 // The 'ModuleInitializer' attribute should not be used in libraries
    public static void InitializeModule()
    {
        if (MSBuildLocator.IsRegistered)
        {
            return;
        }

        VisualStudioInstance? latestInstance = MSBuildLocator
            .QueryVisualStudioInstances()
            .Where(static i => i.DiscoveryType == DiscoveryType.DotNetSdk)
            .OrderByDescending(static i => i.Version)
            .FirstOrDefault()
            ?? throw new InvalidOperationException(".NET SDK not found")
            ;

        if (latestInstance.Version.Major < 5)
        {
            throw new InvalidOperationException(".NET SDK 5 or higher not found");
        }

        MSBuildLocator.RegisterInstance(latestInstance);
    }
}
