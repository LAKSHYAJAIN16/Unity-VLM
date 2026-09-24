namespace UnityVLM.NPCs
{
    public interface IVLMProvider
    {
        VLMDecision Decide(VLMNpc npc, NpcObservation observation, string prompt);
    }

    public class HeuristicVLMProvider : IVLMProvider
    {
        public VLMDecision Decide(VLMNpc npc, NpcObservation observation, string prompt)
        {
            if (npc == null || observation == null)
            {
                return new VLMDecision("wait", "", "", "", 1f, "No observation available.");
            }

            foreach (var visibleObject in observation.VisibleObjects)
            {
                if (visibleObject.Type.Equals("cup", System.StringComparison.OrdinalIgnoreCase))
                {
                    return new VLMDecision("move_to", visibleObject.Id, "", "", 0f, "The cup appears misplaced. Return it to a proper location.");
                }
            }

            return new VLMDecision("wait", "", "", "", 1f, "No immediate problem detected.");
        }
    }
}
