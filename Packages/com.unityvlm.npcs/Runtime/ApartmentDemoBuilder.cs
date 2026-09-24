using UnityEngine;

namespace UnityVLM.NPCs
{
    public static class ApartmentDemoBuilder
    {
        public static GameObject BuildApartment(Vector3 origin)
        {
            var root = new GameObject("Apartment");
            root.transform.position = origin;

            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor";
            floor.transform.SetParent(root.transform);
            floor.transform.localScale = new Vector3(10f, 0.2f, 10f);
            floor.transform.localPosition = new Vector3(0f, -0.1f, 0f);

            var table = GameObject.CreatePrimitive(PrimitiveType.Cube);
            table.name = "Table";
            table.transform.SetParent(root.transform);
            table.transform.localPosition = new Vector3(0f, 0.5f, 2.5f);
            table.transform.localScale = new Vector3(2f, 0.5f, 1f);
            table.AddComponent<SemanticObject>().SetIdentity("Table", "table", "A dining table.");

            var chair = GameObject.CreatePrimitive(PrimitiveType.Cube);
            chair.name = "Chair";
            chair.transform.SetParent(root.transform);
            chair.transform.localPosition = new Vector3(-2.5f, 0.5f, 2.5f);
            chair.transform.localScale = new Vector3(0.7f, 1.2f, 0.7f);
            chair.AddComponent<SemanticObject>().SetIdentity("Chair", "chair", "A dining chair.");

            var door = GameObject.CreatePrimitive(PrimitiveType.Cube);
            door.name = "Door";
            door.transform.SetParent(root.transform);
            door.transform.localPosition = new Vector3(4.5f, 1.2f, 0f);
            door.transform.localScale = new Vector3(0.2f, 2.4f, 1.6f);
            door.AddComponent<SemanticObject>().SetIdentity("Door", "door", "The apartment door.");

            var cup = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cup.name = "Cup";
            cup.transform.SetParent(root.transform);
            cup.transform.localPosition = new Vector3(0.2f, 0.9f, 2.7f);
            cup.transform.localScale = new Vector3(0.35f, 0.4f, 0.35f);
            cup.AddComponent<SemanticObject>().SetIdentity("Cup", "cup", "A ceramic drinking cup.");

            var apple = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            apple.name = "Apple";
            apple.transform.SetParent(root.transform);
            apple.transform.localPosition = new Vector3(-1.8f, 0.6f, 1.5f);
            apple.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
            apple.AddComponent<SemanticObject>().SetIdentity("Apple", "apple", "A ripe red apple.");

            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = "Box";
            box.transform.SetParent(root.transform);
            box.transform.localPosition = new Vector3(2.7f, 0.35f, -1.2f);
            box.transform.localScale = new Vector3(0.8f, 0.7f, 0.8f);
            box.AddComponent<SemanticObject>().SetIdentity("Box", "box", "A storage box.");

            var trash = GameObject.CreatePrimitive(PrimitiveType.Cube);
            trash.name = "TrashCan";
            trash.transform.SetParent(root.transform);
            trash.transform.localPosition = new Vector3(-3.5f, 0.5f, -3f);
            trash.transform.localScale = new Vector3(0.8f, 1f, 0.8f);
            trash.AddComponent<SemanticObject>().SetIdentity("TrashCan", "trash_can", "A small trash can.");

            var player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "Player";
            player.transform.SetParent(root.transform);
            player.transform.localPosition = new Vector3(3.2f, 1f, 2.8f);
            player.transform.localScale = new Vector3(0.8f, 1f, 0.8f);
            player.AddComponent<SemanticObject>().SetIdentity("Player", "player", "The human player.");

            return root;
        }
    }
}
