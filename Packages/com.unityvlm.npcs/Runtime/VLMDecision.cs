using System;
using UnityEngine;

namespace UnityVLM.NPCs
{
    [Serializable]
    public class VLMDecision
    {
        [SerializeField] private string actionName = "wait";
        [SerializeField] private string targetId = string.Empty;
        [SerializeField] private string target2Id = string.Empty;
        [SerializeField] private string text = string.Empty;
        [SerializeField] private float seconds = 1f;
        [SerializeField] private string reason = string.Empty;

        public VLMDecision(string action, string target = "", string target2 = "", string speechText = "", float waitSeconds = 1f, string explanation = "")
        {
            actionName = action;
            targetId = target;
            target2Id = target2;
            text = speechText;
            seconds = waitSeconds;
            reason = explanation;
        }

        public string ActionName => actionName;
        public string TargetId => targetId;
        public string Target2Id => target2Id;
        public string Text => text;
        public float Seconds => seconds;
        public string Reason => reason;
    }
}
