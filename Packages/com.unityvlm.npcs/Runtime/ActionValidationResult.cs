using System;
using UnityEngine;

namespace UnityVLM.NPCs
{
    [Serializable]
    public class ActionValidationResult
    {
        [SerializeField] private bool success;
        [SerializeField] private string reason;

        public bool Success => success;
        public string Reason => reason ?? string.Empty;

        public ActionValidationResult(bool isSuccess, string failureReason = "")
        {
            success = isSuccess;
            reason = failureReason;
        }
    }
}
