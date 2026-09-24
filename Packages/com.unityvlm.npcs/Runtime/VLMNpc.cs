using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace UnityVLM.NPCs
{
    public class VLMNpc : MonoBehaviour
    {
        [Header("Identity")]
        [SerializeField] private string identity = "Bob";
        [SerializeField] [TextArea(2, 4)] private string description = "You are Bob, the player's roommate.";
        [SerializeField] [TextArea(3, 6)] private string goals = "Keep the apartment organized and help the player when reasonable.";
        [SerializeField] [TextArea(2, 4)] private string personality = "Friendly, slightly sarcastic.";

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
        private IVLMProvider llmProvider;

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
        public NavMeshAgent NavMeshAgent => navMeshAgent;

        public void ConfigureModelProvider(IVLMProvider provider)
        {
            llmProvider = provider;
        }

        private void Awake()
        {
            memory = new AgentMemory();
            memory.SetFact("identity", identity);
            memory.SetFact("goals", goals);
            navMeshAgent = GetComponent<NavMeshAgent>();
            llmProvider = new HeuristicVLMProvider();
            RegisterDefaultActions();
        }

        private void Start()
        {
            if (npcCamera == null)
            {
                npcCamera = GetComponentInChildren<Camera>();
            }

            if (navMeshAgent == null)
            {
                navMeshAgent = GetComponent<NavMeshAgent>();
            }

            nextDecisionTime = Time.time + decisionInterval;
            memory.AddWorkingMemory("Agent initialized and ready to observe.");
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

            var prompt = VLMPromptBuilder.BuildPrompt(this, observation);
            var decision = llmProvider != null ? llmProvider.Decide(this, observation, prompt) : new VLMDecision("wait", "", "", "", 1f, "No model provider configured.");

            memory.AddWorkingMemory($"Decision: {decision.ActionName} {decision.TargetId}. Reason: {decision.Reason}");

            if (string.IsNullOrEmpty(decision.ActionName))
            {
                return;
            }

            var targetObject = FindGameObjectById(decision.TargetId);
            var target2Object = FindGameObjectById(decision.Target2Id);

            if (decision.ActionName == "move_to")
            {
                ExecuteAction(decision.ActionName, targetObject, target2Object, decision.Text, decision.Seconds);
                return;
            }

            if (decision.ActionName == "pick_up")
            {
                ExecuteAction(decision.ActionName, targetObject, target2Object, decision.Text, decision.Seconds);
                return;
            }

            if (decision.ActionName == "place")
            {
                ExecuteAction(decision.ActionName, targetObject, target2Object, decision.Text, decision.Seconds);
                return;
            }

            if (decision.ActionName == "speak")
            {
                ExecuteAction(decision.ActionName, targetObject, target2Object, decision.Text, decision.Seconds);
                return;
            }

            if (decision.ActionName == "wait")
            {
                ExecuteAction(decision.ActionName, targetObject, target2Object, decision.Text, decision.Seconds);
            }
        }

        public NpcObservation Observe()
        {
            var observation = new NpcObservation();
            observation.SetSummary("Scanning the apartment for relevant objects and state changes.");

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
                    if (semanticObject == null || semanticObject.gameObject == null)
                    {
                        continue;
                    }

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

                var validation = ActionValidator.ValidateAction(this, actionName, target, target2, text, seconds);
                if (!validation.Success)
                {
                    memory.AddWorkingMemory($"Action rejected: {actionName}. {validation.Reason}");
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
            if (string.IsNullOrWhiteSpace(id))
            {
                return null;
            }

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

            var direction = target.transform.position - npc.transform.position;
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
