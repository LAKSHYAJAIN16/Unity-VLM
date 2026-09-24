using UnityEngine;

namespace UnityVLM.NPCs
{
    public static class ActionValidator
    {
        public static ActionValidationResult ValidateAction(VLMNpc npc, string actionName, GameObject target, GameObject target2 = null, string text = "", float seconds = 0f)
        {
            if (npc == null)
            {
                return new ActionValidationResult(false, "NPC reference is missing.");
            }

            if (string.IsNullOrWhiteSpace(actionName))
            {
                return new ActionValidationResult(false, "Action name is missing.");
            }

            if (target == null && actionName != "wait" && actionName != "speak")
            {
                return new ActionValidationResult(false, $"Target is required for {actionName}.");
            }

            if (actionName == "pick_up")
            {
                var semantic = target != null ? target.GetComponent<SemanticObject>() : null;
                if (semantic != null && !semantic.Movable)
                {
                    return new ActionValidationResult(false, "Target is not movable.");
                }

                if (!npc.IsCloseEnough(target, 1.5f))
                {
                    return new ActionValidationResult(false, "Object is too far away.");
                }
            }

            if (actionName == "move_to")
            {
                if (npc.NavMeshAgent == null)
                {
                    return new ActionValidationResult(false, "Movement is not available.");
                }
            }

            if (actionName == "speak" && string.IsNullOrWhiteSpace(text))
            {
                return new ActionValidationResult(false, "Speech text is required.");
            }

            return new ActionValidationResult(true);
        }
    }
}
