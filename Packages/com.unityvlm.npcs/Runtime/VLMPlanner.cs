using System.Collections.Generic;
using UnityEngine;

namespace UnityVLM.NPCs
{
    public class VLMPlanner
    {
        public List<string> BuildReactivePlan(VLMNpc npc, NpcObservation observation)
        {
            if (npc == null || observation == null)
            {
                return new List<string>();
            }

            var plan = new List<string>();
            foreach (var visibleObject in observation.VisibleObjects)
            {
                if (visibleObject.Type.Equals("cup", System.StringComparison.OrdinalIgnoreCase))
                {
                    plan.Add($"move_to:{visibleObject.Id}");
                    plan.Add($"pick_up:{visibleObject.Id}");
                    return plan;
                }
            }

            plan.Add("wait:1.0");
            return plan;
        }
    }
}
