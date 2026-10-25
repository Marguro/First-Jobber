using System.Collections.Generic;
using System.Globalization;

namespace FirstJobber
{
    /// <summary>
    /// Pure, deterministic confidence maths (no Unity UI, no randomness, no state).
    /// Overall confidence = arithmetic mean of the selected choices' confidence percentages.
    /// </summary>
    public static class ResumeConfidenceCalculator
    {
        public static float Clamp(float value)
        {
            if (float.IsNaN(value)) return 0f;
            return value < 0f ? 0f : value > 100f ? 100f : value;
        }

        /// <summary>Mean of the values (each clamped to 0-100). An empty list returns 0.</summary>
        public static float Average(IReadOnlyList<float> values)
        {
            if (values == null || values.Count == 0) return 0f;
            float sum = 0f;
            for (int i = 0; i < values.Count; i++) sum += Clamp(values[i]);
            return Clamp(sum / values.Count);
        }

        /// <summary>Mean confidence of the given choices. Null entries are skipped; nothing selected = 0.</summary>
        public static float Calculate(IReadOnlyList<ResumeChoiceData> selected, out int count)
        {
            count = 0;
            if (selected == null) return 0f;
            float sum = 0f;
            for (int i = 0; i < selected.Count; i++)
            {
                if (selected[i] == null) continue;
                sum += selected[i].Confidence;
                count++;
            }
            return count == 0 ? 0f : Clamp(sum / count);
        }

        /// <summary>"76.7%" - trailing zeros are dropped ("80%").</summary>
        public static string FormatPercent(float value, int decimals = 1)
        {
            string format = decimals > 0 ? "0." + new string('#', decimals) : "0";
            return Clamp(value).ToString(format, CultureInfo.InvariantCulture) + "%";
        }
    }
}
