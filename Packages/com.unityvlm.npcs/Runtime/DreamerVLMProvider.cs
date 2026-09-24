using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;

namespace UnityVLM.NPCs
{
    public class ActionPlan
    {
        public List<VLMDecision> Actions { get; set; }
        public string Goal { get; set; }
        public float Confidence { get; set; }
        public int CurrentStep { get; set; }

        public ActionPlan()
        {
            Actions = new List<VLMDecision>();
            CurrentStep = 0;
            Confidence = 1f;
        }

        public VLMDecision GetCurrentAction()
        {
            return CurrentStep < Actions.Count ? Actions[CurrentStep] : null;
        }

        public bool IsComplete()
        {
            return CurrentStep >= Actions.Count;
        }

        public void AdvanceStep()
        {
            CurrentStep++;
        }
    }

    public class DreamerVLMProvider : IVLMProvider
    {
        private const string ApiUrl = "https://api.anthropic.com/v1/messages";
        private const string Model = "claude-3-5-sonnet-20241022";

        private string apiKey;
        private MonoBehaviour coroutineRunner;
        private ActionPlan currentPlan;
        private NpcObservation lastObservation;
        private bool isWaitingForResponse;
        private float planExpirationTime;
        private const float PlanValidityDuration = 10f;

        public DreamerVLMProvider(string anthropicApiKey, MonoBehaviour runner = null)
        {
            apiKey = anthropicApiKey;
            coroutineRunner = runner;
            currentPlan = new ActionPlan();
        }

        public VLMDecision Decide(VLMNpc npc, NpcObservation observation, string prompt)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                Debug.LogError("Claude API key not configured.");
                return new VLMDecision("wait", "", "", "", 1f, "API key missing.");
            }

            lastObservation = observation;

            if (currentPlan.Actions.Count > 0 && !currentPlan.IsComplete() && Time.time < planExpirationTime)
            {
                var action = currentPlan.GetCurrentAction();
                currentPlan.AdvanceStep();
                return action;
            }

            if (isWaitingForResponse)
            {
                return new VLMDecision("wait", "", "", "", 0.5f, "Generating new plan...");
            }

            if (coroutineRunner != null)
            {
                coroutineRunner.StartCoroutine(PlanAsync(npc, observation, prompt));
            }
            else
            {
                var plan = PlanSynchronous(npc, observation, prompt);
                currentPlan = plan;
                planExpirationTime = Time.time + PlanValidityDuration;

                if (currentPlan.Actions.Count > 0)
                {
                    var action = currentPlan.GetCurrentAction();
                    currentPlan.AdvanceStep();
                    return action;
                }
            }

            return new VLMDecision("wait", "", "", "", 1f, "Initializing plan...");
        }

        private ActionPlan PlanSynchronous(VLMNpc npc, NpcObservation observation, string prompt)
        {
            var planPrompt = BuildPlanningPrompt(npc, observation, prompt);
            var requestBody = BuildRequestBody(observation, planPrompt);

            using (var request = new UnityWebRequest(ApiUrl, "POST"))
            {
                var bodyBytes = Encoding.UTF8.GetBytes(requestBody);
                request.uploadHandler = new UploadHandlerRaw(bodyBytes);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("x-api-key", apiKey);
                request.SetRequestHeader("anthropic-version", "2023-06-01");

                request.SendWebRequest();

                while (!request.isDone)
                {
                    System.Threading.Thread.Sleep(10);
                }

                if (request.result == UnityWebRequest.Result.Success)
                {
                    return ParsePlanResponse(request.downloadHandler.text);
                }

                Debug.LogError($"Claude API error: {request.error}");
                return new ActionPlan();
            }
        }

        private IEnumerator PlanAsync(VLMNpc npc, NpcObservation observation, string prompt)
        {
            isWaitingForResponse = true;

            var planPrompt = BuildPlanningPrompt(npc, observation, prompt);
            var requestBody = BuildRequestBody(observation, planPrompt);

            using (var request = new UnityWebRequest(ApiUrl, "POST"))
            {
                var bodyBytes = Encoding.UTF8.GetBytes(requestBody);
                request.uploadHandler = new UploadHandlerRaw(bodyBytes);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("x-api-key", apiKey);
                request.SetRequestHeader("anthropic-version", "2023-06-01");

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    currentPlan = ParsePlanResponse(request.downloadHandler.text);
                    planExpirationTime = Time.time + PlanValidityDuration;
                }
                else
                {
                    Debug.LogError($"Claude API error: {request.error}");
                    currentPlan = new ActionPlan();
                }

                isWaitingForResponse = false;
            }
        }

        private string BuildPlanningPrompt(VLMNpc npc, NpcObservation observation, string basePrompt)
        {
            var sb = new StringBuilder();
            sb.AppendLine("You are a planner. Based on the scene and agent goals, generate a SHORT plan (2-5 steps max).");
            sb.AppendLine();
            sb.AppendLine(basePrompt);
            sb.AppendLine();
            sb.AppendLine("Generate a plan as a JSON array of actions:");
            sb.AppendLine("[");
            sb.AppendLine("  {\"action\": \"move_to\", \"target\": \"cup\", \"reason\": \"Cup is misplaced\"},");
            sb.AppendLine("  {\"action\": \"pick_up\", \"target\": \"cup\", \"reason\": \"Pick up the cup\"},");
            sb.AppendLine("  {\"action\": \"move_to\", \"target\": \"table\", \"reason\": \"Move to proper location\"},");
            sb.AppendLine("  {\"action\": \"place\", \"target\": \"table\", \"reason\": \"Place cup on table\"}");
            sb.AppendLine("]");
            sb.AppendLine();
            sb.AppendLine("Keep plans SHORT. Respond with only valid JSON array.");

            return sb.ToString();
        }

        private string BuildRequestBody(NpcObservation observation, string prompt)
        {
            var contentArray = new StringBuilder();
            contentArray.Append("[");

            contentArray.Append("{\"type\": \"text\", \"text\": \"");
            contentArray.Append(EscapeJson(prompt));
            contentArray.Append("\"}");

            if (observation?.Image != null)
            {
                var imageBase64 = Convert.ToBase64String(observation.Image.EncodeToPNG());
                contentArray.Append(", {\"type\": \"image\", \"source\": {\"type\": \"base64\", \"media_type\": \"image/png\", \"data\": \"");
                contentArray.Append(imageBase64);
                contentArray.Append("\"}}");
            }

            contentArray.Append("]");

            var json = new StringBuilder();
            json.Append("{");
            json.Append($"\"model\": \"{Model}\",");
            json.Append($"\"max_tokens\": 512,");
            json.Append($"\"messages\": [{{\"role\": \"user\", \"content\": {contentArray}}}]");
            json.Append("}");

            return json.ToString();
        }

        private ActionPlan ParsePlanResponse(string responseText)
        {
            try
            {
                var json = SimpleJsonParse(responseText);
                if (!json.TryGetValue("content", out var content) || content.Count == 0)
                {
                    return new ActionPlan();
                }

                var textContent = content[0];
                if (!textContent.TryGetValue("text", out var text))
                {
                    return new ActionPlan();
                }

                return ExtractPlanFromText(text);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to parse plan response: {e.Message}");
                return new ActionPlan();
            }
        }

        private ActionPlan ExtractPlanFromText(string text)
        {
            var plan = new ActionPlan();

            var jsonMatch = Regex.Match(text, @"\[[\s\S]*\]");
            if (!jsonMatch.Success)
            {
                Debug.LogWarning($"Could not find JSON array in response: {text}");
                return plan;
            }

            var jsonArray = jsonMatch.Value;
            var actions = ParseJsonArray(jsonArray);

            foreach (var action in actions)
            {
                var actionName = action.TryGetValue("action", out var a) ? a : "wait";
                var target = action.TryGetValue("target", out var t) ? t : "";
                var reason = action.TryGetValue("reason", out var r) ? r : "";

                plan.Actions.Add(new VLMDecision(actionName, target, "", "", 0f, reason));
            }

            plan.Goal = $"Execute {plan.Actions.Count} step plan";
            return plan;
        }

        private List<Dictionary<string, string>> ParseJsonArray(string arrayJson)
        {
            var result = new List<Dictionary<string, string>>();
            var objects = Regex.Matches(arrayJson, @"\{[^{}]*\}");

            foreach (Match obj in objects)
            {
                result.Add(SimpleJsonParse(obj.Value));
            }

            return result;
        }

        private Dictionary<string, dynamic> SimpleJsonParse(string json)
        {
            var result = new Dictionary<string, dynamic>();
            var depth = 0;
            var currentKey = "";
            var currentValue = new StringBuilder();
            var inString = false;
            var escaped = false;

            for (int i = 0; i < json.Length; i++)
            {
                var c = json[i];

                if (escaped)
                {
                    currentValue.Append(c);
                    escaped = false;
                    continue;
                }

                if (c == '\\')
                {
                    escaped = true;
                    currentValue.Append(c);
                    continue;
                }

                if (c == '"')
                {
                    inString = !inString;
                    currentValue.Append(c);
                    continue;
                }

                if (inString)
                {
                    currentValue.Append(c);
                    continue;
                }

                if (c == ':' && depth == 1)
                {
                    currentKey = currentValue.ToString().Trim().Trim('"');
                    currentValue.Clear();
                    continue;
                }

                if (c == ',' && depth == 1)
                {
                    var val = currentValue.ToString().Trim();
                    if (!string.IsNullOrEmpty(currentKey))
                    {
                        result[currentKey] = UnquoteValue(val);
                    }
                    currentKey = "";
                    currentValue.Clear();
                    continue;
                }

                if (c == '{')
                {
                    depth++;
                    if (depth > 1) currentValue.Append(c);
                }
                else if (c == '}')
                {
                    depth--;
                    if (depth > 0) currentValue.Append(c);
                    else if (depth == 0 && !string.IsNullOrEmpty(currentKey))
                    {
                        var val = currentValue.ToString().Trim();
                        result[currentKey] = UnquoteValue(val);
                    }
                }
                else if (c != '[' && c != ']')
                {
                    currentValue.Append(c);
                }
            }

            return result;
        }

        private string UnquoteValue(string value)
        {
            value = value.Trim();
            if (value.StartsWith("\"") && value.EndsWith("\""))
            {
                value = value.Substring(1, value.Length - 2);
            }
            return value;
        }

        private string EscapeJson(string input)
        {
            return input
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r")
                .Replace("\t", "\\t");
        }
    }
}
