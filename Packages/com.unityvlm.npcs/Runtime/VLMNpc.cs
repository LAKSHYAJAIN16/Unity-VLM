using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace UnityVLM.NPCs
{
    public class VLMNpc : MonoBehaviour
    {
        [Header("Identity")]
        [SerializeField] private string identity = "NPC";
        [SerializeField] [TextArea(2, 4)] private string description = "A helpful NPC.";
        [SerializeField] [TextArea(3, 6)] private string goals = "Keep the area organized.";
        [SerializeField] [TextArea(2, 4)] private string personality = "Friendly.";

        [Header("Perception")]
        [SerializeField] private Camera npcCamera;
        [SerializeField] private bool visionEnabled = true;
        [SerializeField] private bool structuredPerceptionEnabled = true;
        [SerializeField] private float decisionInterval = 2f;

        [Header("Model")]
        [SerializeField] private string model = "Gemini";

        [Header("Actions")]
        [SerializeField] private bool canMove = true;
        [SerializeField] private bool canLook = true;
        [SerializeField] private bool canSpeak = true;
        [SerializeField] private bool canPickUp = true;
        [SerializeField] private bool canDrop = true;
        [SerializeField] private bool canPlace = true;
        [SerializeField] private bool canOpen = true;
        [SerializeField] private bool canClose = true;

        private readonly List<IAgentAction> actions = new List<IAgentAction>();
        private AgentMemory memory;
        private NpcObservation currentObservation;
        private float nextDecisionTime;
        private NavMeshAgent navMeshAgent;
        private GameObject heldObject;

        public string Identity => identity;
        public string Description => description;
        public string Goals => goals;
        public string Personality => personality;
        public Camera NpcCamera => npcCamera;
        public bool VisionEnabled => visionEnabled;
        public bool StructuredPerceptionEnabled => structuredPerceptionEnabled;
        public float DecisionInterval => decisionInterval;
        public string Model => model;
        public AgentMemory Memory => memory;
        public NpcObservation CurrentObservation => currentObservation;
        public GameObject HeldObject => heldObject;

        private void Awake()
        {
            memory = new AgentMemory();
            memory.SetFact("identity", identity);
            memory.SetFact("goals", goals);
            navMeshAgent = GetComponent<NavMeshAgent>();
            RegisterDefaultActions();
        }

        private void Start()
        {
            if (npcCamera == null)
            {
                npcCamera = GetComponentInChildren<Camera>();
            }

            nextDecisionTime = Time.time + decisionInterval;
            memory.AddWorkingMemory("Agent initialized.");
        }

        private void Update()
        {
            if (Time.time < nextDecisionTime)
            {
                return;
            }

            nextDecisionTime = Time.time + decisionInterval;
            Think();
        }

        private void RegisterDefaultActions()
        {
            actions.Clear();
            actions.Add(new MoveToAction());
            actions.Add(new LookAtAction());
            actions.Add(new SpeakAction());
            actions.Add(new PickUpAction());
            actions.Add(new DropAction());
            actions.Add(new PlaceAction());
            actions.Add(new OpenAction());
            actions.Add(new CloseAction());
            actions.Add(new WaitAction());
        }

        public void Think()
        {
            var observation = Observe();
            currentObservation = observation;
            memory.AddWorkingMemory($"Observed: {observation.Summary}");

            if (observation.VisibleObjects.Count == 0)
            {
                memory.AddWorkingMemory("No relevant target detected.");
                return;
            }

            var targetObject = observation.VisibleObjects[0];
            if (targetObject.Type == "cup" || targetObject.Type == "Cup")
            {
                var cup = FindGameObjectById(targetObject.Id);
                if (cup != null)
                {
                    ExecuteAction("move_to", cup);
                }
            }
        }

        public NpcObservation Observe()
        {
            var observation = new NpcObservation();
            observation.SetSummary("Scanning the living area for objects of interest.");

            if (visionEnabled && npcCamera != null)
            {
                var renderTexture = new RenderTexture(256, 256, 24);
                var previous = npcCamera.targetTexture;
                npcCamera.targetTexture = renderTexture;
                npcCamera.Render();
                RenderTexture.active = renderTexture;

                var texture = new Texture2D(256, 256, TextureFormat.RGB24, false);
                texture.ReadPixels(new Rect(0, 0, 256, 256), 0, 0);
                texture.Apply();
                observation.SetImage(texture);

                npcCamera.targetTexture = previous;
                RenderTexture.active = null;
                Destroy(renderTexture);
            }

            if (structuredPerceptionEnabled)
            {
                var objects = FindObjectsOfType<SemanticObject>();
                foreach (var semanticObject in objects)
                {
                    var distance = Vector3.Distance(transform.position, semanticObject.transform.position);
                    observation.AddVisibleObject(semanticObject.name, semanticObject.ObjectType, distance);
                }
            }

            return observation;
        }

        public bool ExecuteAction(string actionName, GameObject target, GameObject target2 = null, string text = "", float seconds = 0f)
        {
            foreach (var action in actions)
            {
                if (action.ActionName != actionName)
                {
                    continue;
                }

                if (!action.Validate(this, target, target2, text, seconds))
                {
                    memory.AddWorkingMemory($"Action rejected: {actionName} ({target != null ? target.name : "null"})");
                    return false;
                }

                var success = action.Execute(this, target, target2, text, seconds);
                memory.AddWorkingMemory(success
                    ? $"Action succeeded: {actionName}."
                    : $"Action failed: {actionName}.");
                return success;
            }

            memory.AddWorkingMemory($"Action not found: {actionName}");
            return false;
        }

        public void SetHeldObject(GameObject newHeldObject)
        {
            heldObject = newHeldObject;
        }

        public GameObject FindGameObjectById(string id)
        {
            var allObjects = FindObjectsOfType<GameObject>();
            foreach (var candidate in allObjects)
            {
                if (string.Equals(candidate.name, id, StringComparison.OrdinalIgnoreCase))
                {
                    return candidate;
                }
            }

            return null;
        }

        public bool IsCloseEnough(GameObject target, float maxDistance = 1.5f)
        {
            if (target == null)
            {
                return false;
            }

            return Vector3.Distance(transform.position, target.transform.position) <= maxDistance;
        }

        public NavMeshAgent NavMeshAgent => navMeshAgent;
    }

    public class MoveToAction : IAgentAction
    {
        public string ActionName => "move_to";

        public bool Validate(VLMNpc npc, GameObject target, GameObject target2 = null, string text = "", float seconds = 0f)
        {
            return npc != null && target != null && npc.NavMeshAgent != null;
        }

        public bool Execute(VLMNpc npc, GameObject target, GameObject target2 = null, string text = "", float seconds = 0f)
        {
            if (!Validate(npc, target, target2, text, seconds))
            {
                return false;
            }

            npc.NavMeshAgent.SetDestination(target.transform.position);
            return true;
        }
    }

    public class LookAtAction : IAgentAction
    {
        public string ActionName => "look_at";

        public bool Validate(VLMNpc npc, GameObject target, GameObject target2 = null, string text = "", float seconds = 0f)
        {
            return npc != null && target != null && npc.NpcCamera != null;
        }

        public bool Execute(VLMNpc npc, GameObject target, GameObject target2 = null, string text = "", float seconds = 0f)
        {
            if (!Validate(npc, target, target2, text, seconds))
            {
                return false;
            }

            var position = target.transform.position;
            var direction = position - npc.transform.position;
            if (direction.sqrMagnitude > 0.0001f)
            {
                npc.transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            }

            return true;
        }
    }

    public class SpeakAction : IAgentAction
    {
        public string ActionName => "speak";

        public bool Validate(VLMNpc npc, GameObject target, GameObject target2 = null, string text = "", float seconds = 0f)
        {
            return npc != null && !string.IsNullOrWhiteSpace(text);
        }

        public bool Execute(VLMNpc npc, GameObject target, GameObject target2 = null, string text = "", float seconds = 0f)
        {
            if (!Validate(npc, target, target2, text, seconds))
            {
                return false;
            }

            Debug.Log($"{npc.Identity}: {text}");
            return true;
        }
    }

    public class PickUpAction : IAgentAction
    {
        public string ActionName => "pick_up";

        public bool Validate(VLMNpc npc, GameObject target, GameObject target2 = null, string text = "", float seconds = 0f)
        {
            if (npc == null || target == null)
            {
                return false;
            }

            var semantic = target.GetComponent<SemanticObject>();
            return semantic == null || semantic.Movable;
        }

        public bool Execute(VLMNpc npc, GameObject target, GameObject target2 = null, string text = "", float seconds = 0f)
        {
            if (!Validate(npc, target, target2, text, seconds))
            {
                return false;
            }

            if (npc.IsCloseEnough(target, 1.5f))
            {
                npc.SetHeldObject(target);
                target.transform.SetParent(npc.transform);
                return true;
            }

            return false;
        }
    }

    public class DropAction : IAgentAction
    {
        public string ActionName => "drop";

        public bool Validate(VLMNpc npc, GameObject target, GameObject target2 = null, string text = "", float seconds = 0f)
        {
            return npc != null && npc.HeldObject != null;
        }

        public bool Execute(VLMNpc npc, GameObject target, GameObject target2 = null, string text = "", float seconds = 0f)
        {
            if (!Validate(npc, target, target2, text, seconds))
            {
                return false;
            }

            npc.HeldObject.transform.SetParent(null);
            npc.SetHeldObject(null);
            return true;
        }
    }

    public class PlaceAction : IAgentAction
    {
        public string ActionName => "place";

        public bool Validate(VLMNpc npc, GameObject target, GameObject target2 = null, string text = "", float seconds = 0f)
        {
            return npc != null && target != null && npc.HeldObject != null;
        }

        public bool Execute(VLMNpc npc, GameObject target, GameObject target2 = null, string text = "", float seconds = 0f)
        {
            if (!Validate(npc, target, target2, text, seconds))
            {
                return false;
            }

            npc.HeldObject.transform.SetParent(null);
            npc.HeldObject.transform.position = target.transform.position + Vector3.up * 0.5f;
            npc.SetHeldObject(null);
            return true;
        }
    }

    public class OpenAction : IAgentAction
    {
        public string ActionName => "open";

        public bool Validate(VLMNpc npc, GameObject target, GameObject target2 = null, string text = "", float seconds = 0f)
        {
            return npc != null && target != null;
        }

        public bool Execute(VLMNpc npc, GameObject target, GameObject target2 = null, string text = "", float seconds = 0f)
        {
            if (!Validate(npc, target, target2, text, seconds))
            {
                return false;
            }

            Debug.Log($"Opening {target.name}");
            return true;
        }
    }

    public class CloseAction : IAgentAction
    {
        public string ActionName => "close";

        public bool Validate(VLMNpc npc, GameObject target, GameObject target2 = null, string text = "", float seconds = 0f)
        {
            return npc != null && target != null;
        }

        public bool Execute(VLMNpc npc, GameObject target, GameObject target2 = null, string text = "", float seconds = 0f)
        {
            if (!Validate(npc, target, target2, text, seconds))
            {
                return false;
            }

            Debug.Log($"Closing {target.name}");
            return true;
        }
    }

    public class WaitAction : IAgentAction
    {
        public string ActionName => "wait";

        public bool Validate(VLMNpc npc, GameObject target, GameObject target2 = null, string text = "", float seconds = 0f)
        {
            return npc != null && seconds >= 0f;
        }

        public bool Execute(VLMNpc npc, GameObject target, GameObject target2 = null, string text = "", float seconds = 0f)
        {
            if (!Validate(npc, target, target2, text, seconds))
            {
                return false;
            }

            return true;
        }
    }
}
