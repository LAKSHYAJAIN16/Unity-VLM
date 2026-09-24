using UnityEngine;
using UnityEngine.AI;
using UnityVLM.NPCs;

namespace UnityVLM.NPCs.Samples
{
    public static class ApartmentDemoSetup
    {
        public static GameObject CreateNpc(Vector3 position)
        {
            var npcObject = new GameObject("Bob");
            npcObject.transform.position = position;

            var agent = npcObject.AddComponent<NavMeshAgent>();
            agent.speed = 3f;
            agent.stoppingDistance = 0.6f;

            var npc = npcObject.AddComponent<VLMNpc>();
            var cameraObject = new GameObject("NPC Camera");
            cameraObject.transform.SetParent(npcObject.transform);
            cameraObject.transform.localPosition = new Vector3(0f, 1.5f, 0.2f);

            var camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 60f;

            return npcObject;
        }

        public static GameObject CreateSemanticObject(string name, string type, Vector3 position, bool movable = true)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = name;
            obj.transform.position = position;
            obj.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            var semantic = obj.AddComponent<SemanticObject>();
            semantic.SetIdentity(name, type, $"A {type} object.");
            var renderer = obj.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = Color.gray;
            }

            return obj;
        }
    }
}
