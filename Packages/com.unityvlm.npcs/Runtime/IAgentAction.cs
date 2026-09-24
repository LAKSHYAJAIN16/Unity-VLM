using UnityEngine;

namespace UnityVLM.NPCs
{
    public interface IAgentAction
    {
        string ActionName { get; }
        bool Validate(VLMNpc npc, GameObject target, GameObject target2 = null, string text = "", float seconds = 0f);
        bool Execute(VLMNpc npc, GameObject target, GameObject target2 = null, string text = "", float seconds = 0f);
    }
}
