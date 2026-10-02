namespace TF.EX.Domain.Utils
{
    public static class Platform
    {
        public static string Current { get; } =
            OperatingSystem.IsWindows() ? "Windows"
            : OperatingSystem.IsLinux() ? "Linux"
            : OperatingSystem.IsMacOS() ? "macOS"
            : "Unknown";
    }
}
