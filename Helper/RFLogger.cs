using ExileCore2;

namespace ReforgeHelper.Helper
{
    public static class RFLogger
    {
        private static ReforgeHelperSettings _settings;

        public static void Initialize(ReforgeHelperSettings settings)
        {
            _settings = settings;
        }

        public static void Debug(string message)
        {
            if (_settings?.EnableDebug == true)
            {
                DebugWindow.LogMsg($"[ReforgeHelper] {message}");
            }
        }

        public static void Info(string message)
        {
            DebugWindow.LogMsg($"[ReforgeHelper] {message}");
        }

        public static void Error(string message)
        {
            DebugWindow.LogError($"[ReforgeHelper] {message}");
        }
    }
}