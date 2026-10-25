using System.Collections.Generic;
using UnityEngine;

namespace FirstJobber
{
    /// <summary>List of atmosphere phases. Order in the list does not matter.</summary>
    [CreateAssetMenu(menuName = "First Jobber/Atmosphere Configuration", fileName = "AtmosphereConfiguration")]
    public class AtmosphereConfiguration : ScriptableObject
    {
        public List<AtmospherePhaseData> phases = new List<AtmospherePhaseData>();

        /// <summary>Enabled phase with the highest startDay &lt;= day. Null if none qualifies.</summary>
        public AtmospherePhaseData Resolve(int day)
        {
            AtmospherePhaseData best = null;
            for (int i = 0; i < phases.Count; i++)
            {
                var p = phases[i];
                if (p == null || !p.enabled || p.startDay > day) continue;
                if (best == null || p.startDay > best.startDay) best = p;
            }
            return best;
        }

        public List<string> Validate()
        {
            var problems = new List<string>();
            var ids = new HashSet<string>();
            var days = new Dictionary<int, string>();
            bool coversDayOne = false;

            for (int i = 0; i < phases.Count; i++)
            {
                var p = phases[i];
                if (p == null) { problems.Add("Atmosphere list has a missing reference at index " + i + "."); continue; }
                if (string.IsNullOrWhiteSpace(p.id)) problems.Add("Atmosphere phase '" + p.name + "' has an empty id.");
                else if (!ids.Add(p.id)) problems.Add("Atmosphere phase id '" + p.id + "' is duplicated.");

                if (!p.enabled) continue;
                if (p.startDay <= 1) coversDayOne = true;
                if (days.TryGetValue(p.startDay, out var other))
                    problems.Add("Phases '" + other + "' and '" + p.name + "' both start on day " + p.startDay + " (the first in the list wins).");
                else days.Add(p.startDay, p.name);

                var targets = new HashSet<string>();
                foreach (var o in p.spriteOverrides)
                    if (o != null && !string.IsNullOrEmpty(o.targetId) && !targets.Add(o.targetId))
                        problems.Add("Phase '" + p.name + "' has duplicate sprite override for target '" + o.targetId + "'.");
            }
            if (phases.Count > 0 && !coversDayOne) problems.Add("No enabled phase starts on day 1 - early days use the scene's original look.");
            return problems;
        }
    }
}
