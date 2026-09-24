using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityVLM.NPCs
{
    [Serializable]
    public class VisibleObject
    {
        [SerializeField] private string id;
        [SerializeField] private string type;
        [SerializeField] private float distance;

        public string Id => id ?? string.Empty;
        public string Type => type ?? string.Empty;
        public float Distance => distance;

        public VisibleObject(string objectId, string objectType, float objectDistance)
        {
            id = objectId;
            type = objectType;
            distance = objectDistance;
        }
    }

    [Serializable]
    public class NpcObservation
    {
        [SerializeField] private string summary;
        [SerializeField] private List<VisibleObject> visibleObjects = new List<VisibleObject>();
        [SerializeField] private Texture2D image;

        public string Summary => summary ?? string.Empty;
        public List<VisibleObject> VisibleObjects => visibleObjects;
        public Texture2D Image => image;

        public void SetSummary(string text)
        {
            summary = text;
        }

        public void AddVisibleObject(string id, string type, float distance)
        {
            visibleObjects.Add(new VisibleObject(id, type, distance));
        }

        public void SetImage(Texture2D texture)
        {
            image = texture;
        }
    }
}
