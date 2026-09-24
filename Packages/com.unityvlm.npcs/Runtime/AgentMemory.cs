using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityVLM.NPCs
{
    [Serializable]
    public class AgentMemoryEntry
    {
        [SerializeField] private string timestamp;
        [SerializeField] private string content;

        public string Timestamp => timestamp ?? DateTime.UtcNow.ToString("O");
        public string Content => content ?? string.Empty;

        public AgentMemoryEntry(string contentText)
        {
            timestamp = DateTime.UtcNow.ToString("O");
            content = contentText;
        }
    }

    [Serializable]
    public class AgentMemory
    {
        [SerializeField] private List<AgentMemoryEntry> workingMemory = new List<AgentMemoryEntry>();
        [SerializeField] private Dictionary<string, string> semanticMemory = new Dictionary<string, string>();

        [SerializeField] private int maxWorkingEntries = 20;

        public IReadOnlyList<AgentMemoryEntry> WorkingMemory => workingMemory;
        public IReadOnlyDictionary<string, string> SemanticMemory => semanticMemory;

        public void AddWorkingMemory(string entry)
        {
            workingMemory.Add(new AgentMemoryEntry(entry));

            while (workingMemory.Count > maxWorkingEntries)
            {
                workingMemory.RemoveAt(0);
            }
        }

        public void SetFact(string key, string value)
        {
            semanticMemory[key] = value;
        }

        public string GetFact(string key)
        {
            return semanticMemory.TryGetValue(key, out var value) ? value : string.Empty;
        }
    }
}
