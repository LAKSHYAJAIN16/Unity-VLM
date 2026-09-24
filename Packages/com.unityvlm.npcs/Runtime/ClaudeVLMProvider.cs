using System;
using System.Collections;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;

namespace UnityVLM.NPCs
{
    public class ClaudeVLMProvider : IVLMProvider
    {
        private const string ApiUrl = "https://api.anthropic.com/v1/messages";
        private const string Model = "claude-3-5-sonnet-20241022";

        private string apiKey;
        private MonoBehaviour coroutineRunner;
        private VLMDecision lastDecision;
        private bool isWaitingForResponse;

        public ClaudeVLMProvider(string anthropicApiKey, MonoBehaviour runner = null)
        {
            apiKey = anthropicApiKey;
            coroutineRunner = runner;
        }

        public VLMDecision Decide(VLMNpc npc, NpcObservation observation, string prompt)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                Debug.LogError("Claude API key not configured. Set via ClaudeVLMProvider constructor.");
                return new VLMDecision("wait", "", "", "", 1f, "API key missing.");
            }

            if (isWaitingForResponse)
            {
                return lastDecision ?? new VLMDecision("wait", "", "", "", 1f, "Waiting for previous response...");
            }

            if (coroutineRunner == null)
            {
                Debug.LogWarning("No coroutine runner available. Using synchronous fallback (blocking).");
                return DecideSynchronous(observation, prompt);
            }

            coroutineRunner.StartCoroutine(DecideAsync(observation, prompt));
            return lastDecision ?? new VLMDecision("wait", "", "", "", 1f, "Initializing...");
        }

        private VLMDecision DecideSynchronous(NpcObservation observation, string prompt)
        {
            var requestBody = BuildRequestBody(observation, prompt);
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
                    return ParseResponse(request.downloadHandler.text);
                }

                Debug.LogError($"Claude API error: {request.error} - {request.downloadHandler.text}");
                return new VLMDecision("wait", "", "", "", 1f, "API request failed.");
            }
        }

        private IEnumerator DecideAsync(NpcObservation observation, string prompt)
        {
            isWaitingForResponse = true;
            lastDecision = new VLMDecision("wait", "", "", "", 1f, "Processing request...");

            var requestBody = BuildRequestBody(observation, prompt);
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
                    lastDecision = ParseResponse(request.downloadHandler.text);
                }
                else
                {
                    Debug.LogError($"Claude API error: {request.error}");
                    Debug.LogError($"Response: {request.downloadHandler.text}");
                    lastDecision = new VLMDecision("wait", "", "", "", 1f, "API request failed.");
                }

                isWaitingForResponse = false;
            }
        }

        private string BuildRequestBody(NpcObservation observation, string prompt)
        {
            var contentArray = new StringBuilder();
            contentArray.Append("[");

            var textContent = new StringBuilder();
            textContent.Append("Respond with a JSON object containing exactly these fields: {\"action\": \"<action_name>\", \"target\": \"<target_id>\", \"text\": \"<speech_text>\", \"seconds\": <number>, \"reason\": \"<explanation>\"}\n\n");
            textContent.Append(prompt);

            contentArray.Append("{\"type\": \"text\", \"text\": \"");
            contentArray.Append(EscapeJson(textContent.ToString()));
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
            json.Append($"\"max_tokens\": 1024,");
            json.Append($"\"messages\": [{{\"role\": \"user\", \"content\": {contentArray}}}]");
            json.Append("}");

            return json.ToString();
        }

        private VLMDecision ParseResponse(string responseText)
        {
            try
            {
                var json = SimpleJsonParse(responseText);
                if (!json.TryGetValue("content", out var content) || content.Count == 0)
                {
                    return new VLMDecision("wait", "", "", "", 1f, "No content in response.");
                }

                var textContent = content[0];
                if (!textContent.TryGetValue("text", out var text))
                {
                    return new VLMDecision("wait", "", "", "", 1f, "No text in response.");
                }

                return ExtractDecisionFromText(text);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to parse Claude response: {e.Message}");
                return new VLMDecision("wait", "", "", "", 1f, $"Parse error: {e.Message}");
            }
        }

        private VLMDecision ExtractDecisionFromText(string text)
        {
            var jsonMatch = Regex.Match(text, @"\{[^{}]*\}");
            if (!jsonMatch.Success)
            {
                Debug.LogWarning($"Could not find JSON in response: {text}");
                return new VLMDecision("wait", "", "", "", 1f, "No JSON found in response.");
            }

            var json = SimpleJsonParse(jsonMatch.Value);

            var action = json.TryGetValue("action", out var a) ? a : "wait";
            var target = json.TryGetValue("target", out var t) ? t : "";
            var speechText = json.TryGetValue("text", out var txt) ? txt : "";
            var seconds = json.TryGetValue("seconds", out var sec) ? float.TryParse(sec, out var s) ? s : 1f : 1f;
            var reason = json.TryGetValue("reason", out var r) ? r : "Decision made by Claude.";

            return new VLMDecision(action, target, "", speechText, seconds, reason);
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

        private Dictionary<string, List<Dictionary<string, string>>> SimpleJsonParseArray(string json)
        {
            var result = new Dictionary<string, List<Dictionary<string, string>>>();
            var currentKey = "";
            var arrayContent = new StringBuilder();
            var inString = false;
            var depth = 0;

            for (int i = 0; i < json.Length; i++)
            {
                var c = json[i];

                if (c == '"' && (i == 0 || json[i - 1] != '\\'))
                {
                    inString = !inString;
                }

                if (!inString)
                {
                    if (c == ':')
                    {
                        currentKey = arrayContent.ToString().Trim().Trim('"');
                        arrayContent.Clear();
                        continue;
                    }

                    if (c == '[')
                    {
                        depth++;
                        if (depth > 1) arrayContent.Append(c);
                        continue;
                    }

                    if (c == ']')
                    {
                        depth--;
                        if (depth == 0)
                        {
                            result[currentKey] = ParseJsonArray(arrayContent.ToString());
                            arrayContent.Clear();
                            currentKey = "";
                        }
                        else
                        {
                            arrayContent.Append(c);
                        }
                        continue;
                    }

                    if (c == ',' && depth == 0)
                    {
                        continue;
                    }
                }

                if (depth > 0 || (inString && depth == 0))
                {
                    arrayContent.Append(c);
                }
            }

            return result;
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
