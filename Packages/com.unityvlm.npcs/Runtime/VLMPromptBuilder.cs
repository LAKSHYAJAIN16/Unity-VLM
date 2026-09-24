using System.Text;

namespace UnityVLM.NPCs
{
    public static class VLMPromptBuilder
    {
        public static string BuildPrompt(VLMNpc npc, NpcObservation observation)
        {
            if (npc == null)
            {
                return "No NPC context available.";
            }

            var sb = new StringBuilder();
            sb.AppendLine("IDENTITY");
            sb.AppendLine($"You are {npc.Identity}.");
            sb.AppendLine();
            sb.AppendLine("DESCRIPTION");
            sb.AppendLine(npc.Description);
            sb.AppendLine();
            sb.AppendLine("GOALS");
            sb.AppendLine(npc.Goals);
            sb.AppendLine();
            sb.AppendLine("PERSONALITY");
            sb.AppendLine(npc.Personality);
            sb.AppendLine();
            sb.AppendLine("CURRENT OBSERVATION");
            sb.AppendLine(observation != null ? observation.Summary : "No observation available.");
            if (observation != null)
            {
                sb.AppendLine("VISIBLE OBJECTS:");
                foreach (var visible in observation.VisibleObjects)
                {
                    sb.AppendLine($"- {visible.Id} ({visible.Type}) at distance {visible.Distance:F1}");
                }
            }

            sb.AppendLine();
            sb.AppendLine("AVAILABLE ACTIONS");
            sb.AppendLine("move_to(target)");
            sb.AppendLine("look_at(target)");
            sb.AppendLine("pick_up(target)");
            sb.AppendLine("place(target)");
            sb.AppendLine("speak(text)");
            sb.AppendLine("wait(seconds)");
            sb.AppendLine();
            sb.AppendLine("Choose the next best action based on the scene and goals.");
            return sb.ToString();
        }
    }
}
