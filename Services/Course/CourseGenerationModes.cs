using System;

namespace AipptAddIn.Services.Course
{
    internal static class CourseGenerationModes
    {
        public const string VisualReplica = "视觉复刻模式";
        public const string Premium = "精美模式";
        public const string Fast = "快速模式";

        public static bool IsVisualReplica(string generationMode)
        {
            return !string.IsNullOrWhiteSpace(generationMode) &&
                   generationMode.IndexOf(VisualReplica, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
