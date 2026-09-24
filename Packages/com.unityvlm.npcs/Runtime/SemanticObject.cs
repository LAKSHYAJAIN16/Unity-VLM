using System.Collections.Generic;
using UnityEngine;

namespace UnityVLM.NPCs
{
    public class SemanticObject : MonoBehaviour
    {
        [Header("Identity")]
        [SerializeField] private string objectName = "Unnamed Object";
        [SerializeField] private string objectType = "object";
        [SerializeField] [TextArea(2, 4)] private string description = "A generic object.";

        [Header("Properties")]
        [SerializeField] private bool movable = true;
        [SerializeField] private bool breakable = false;
        [SerializeField] private bool container = false;

        [Header("Interactions")]
        [SerializeField] private List<string> availableInteractions = new List<string> { "Inspect", "PickUp" };

        public string ObjectName => objectName;
        public string ObjectType => objectType;
        public string Description => description;
        public bool Movable => movable;
        public bool Breakable => breakable;
        public bool Container => container;
        public IReadOnlyList<string> AvailableInteractions => availableInteractions;

        public void SetIdentity(string name, string type, string descriptionText)
        {
            objectName = name;
            objectType = type;
            description = descriptionText;
        }

        public IEnumerable<string> GetAvailableInteractions()
        {
            return availableInteractions;
        }
    }
}
