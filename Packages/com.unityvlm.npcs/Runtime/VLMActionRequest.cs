using System;
using UnityEngine;

namespace UnityVLM.NPCs
{
    [Serializable]
    public class VLMActionRequest
    {
        [SerializeField] private string action;
        [SerializeField] private string target;
        [SerializeField] private string target2;
        [SerializeField] private string text;
        [SerializeField] private float seconds = 0f;

        public string Action
        {
            get => action ?? string.Empty;
            set => action = value;
        }

        public string Target
        {
            get => target ?? string.Empty;
            set => target = value;
        }

        public string Target2
        {
            get => target2 ?? string.Empty;
            set => target2 = value;
        }

        public string Text
        {
            get => text ?? string.Empty;
            set => text = value;
        }

        public float Seconds
        {
            get => seconds;
            set => seconds = value;
        }
    }
}
