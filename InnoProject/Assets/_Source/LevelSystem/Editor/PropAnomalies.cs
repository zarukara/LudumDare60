using System.Collections.Generic;
using System.IO;
using System.Linq;
using AnomalySystem;
using AnomalySystem.EditorTools;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LevelSystem.EditorTools
{

    public static class PropAnomalies
    {
        const string ModelPath = OfficeMenu.AnomalyFolder + "/More.fbx";
        const float DeskTop = 0.75f;

        sealed class Prop
        {
            public string File, Title;

            public Vector3 Center, Front;

            public float Size;
            public bool ByLength;

            public string Desk;

            public Vector3 Place, Facing;
            public string[] Replaces = new string[0];
        }

        static readonly Prop[] Props =
        {
            new Prop { File = "Old_Boombox", Title = "Старый кассетник", Center = new Vector3(2f, 2f, -41.6f), Front = Vector3.left, Size = 0.5f, ByLength = true,
                Place = new Vector3(7.82f, 0f, 2.15f), Facing = Vector3.left },
            new Prop { File = "Desk_Fan", Title = "Вентилятор", Center = new Vector3(0f, 1.5f, -10f), Front = Vector3.back, Size = 0.36f,
                Desk = "Desk_E1", Place = new Vector3(-0.5f, DeskTop, 0.22f) },
            new Prop { File = "Old_Heater", Title = "Обогреватель", Center = new Vector3(-1f, 1.1f, 18.2f), Front = Vector3.right, Size = 0.7f, ByLength = true,
                Place = new Vector3(2.35f, 0f, 0.16f), Facing = Vector3.forward },
            new Prop { File = "Dot_Printer", Title = "Матричный принтер", Center = new Vector3(0f, 0.6f, 3.6f), Front = Vector3.right, Size = 0.42f, ByLength = true,
                Desk = "Desk_E5", Place = new Vector3(-0.48f, DeskTop, 0.24f) },
            new Prop { File = "Desk_Lamp", Title = "Настольная лампа", Center = new Vector3(0.6f, 2.5f, 7f), Front = Vector3.back, Size = 0.5f,
                Desk = "Desk_W3", Place = new Vector3(0.56f, DeskTop, 0.2f),
                Replaces = new[] { "Workstations/Desk_W3/Lamp_Base", "Workstations/Desk_W3/Lamp_Arm", "Workstations/Desk_W3/Lamp_Head", "Workstations/Desk_W3/Lamp_Bulb" } },
            new Prop { File = "Walkie_Talkie", Title = "Рация", Center = new Vector3(-9.6f, 2.5f, -4f), Front = Vector3.right, Size = 0.32f,
                Desk = "Desk_W1", Place = new Vector3(-0.52f, DeskTop, 0.24f) },
        };

        [MenuItem("Office/Rebuild Prop Anomalies")]
        public static void Rebuild() => Build(OfficeMenu.BuildPalette(), true);

        public static void Build(OfficePalette palette, bool overwrite = false)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            if (model == null) return;
            var office = OfficeRoomBuilder.Build(palette);
            var scene = (GameObject)PrefabUtility.InstantiatePrefab(model);
            try
            {
                PrefabUtility.UnpackPrefabInstance(scene, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                scene.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                var parts = Group(scene.GetComponentsInChildren<Renderer>(true));
                foreach (var prop in Props)
                {
                    string path = $"{OfficeMenu.AnomalyFolder}/{prop.File}.prefab";
                    if (File.Exists(path) && !overwrite) continue;
                    if (!parts.TryGetValue(prop, out var own) || own.Count == 0) continue;
                    var root = Make(prop, own, office.transform);
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    Object.DestroyImmediate(root);
                }
            }
            finally
            {
                Object.DestroyImmediate(scene);
                Object.DestroyImmediate(office);
                Prims.ClearCache();
            }
            var library = AssetDatabase.LoadAssetAtPath<AnomalyLibrary>(OfficeMenu.AnomalyFolder + "/AnomalyLibrary.asset");
            AnomalyLibrarySync.Sync(library);
        }

        static Dictionary<Prop, List<Renderer>> Group(Renderer[] renderers)
        {
            var groups = Props.ToDictionary(p => p, p => new List<Renderer>());
            foreach (var r in renderers)
            {
                var nearest = Props.OrderBy(p => Vector3.Distance(p.Center, r.bounds.center)).First();
                groups[nearest].Add(r);
            }
            return groups;
        }

        static GameObject Make(Prop prop, List<Renderer> parts, Transform office)
        {
            var bounds = parts[0].bounds;
            foreach (var r in parts) bounds.Encapsulate(r.bounds);

            var root = new GameObject(prop.File);
            root.transform.position = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            foreach (var r in parts)
            {
                var filter = r.GetComponent<MeshFilter>();
                if (filter == null || filter.sharedMesh == null) continue;
                var part = new GameObject(r.name, typeof(MeshFilter), typeof(MeshRenderer));
                part.transform.SetPositionAndRotation(r.transform.position, r.transform.rotation);
                part.transform.localScale = r.transform.lossyScale;
                part.transform.SetParent(root.transform, true);
                part.GetComponent<MeshFilter>().sharedMesh = filter.sharedMesh;
                part.GetComponent<MeshRenderer>().sharedMaterials = r.sharedMaterials;
            }
            var box = root.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, bounds.size.y / 2f, 0f);
            box.size = bounds.size;

            float measured = prop.ByLength ? Mathf.Max(bounds.size.x, bounds.size.z) : bounds.size.y;
            root.transform.localScale = Vector3.one * (prop.Size / measured);
            Vector3 place = prop.Place, facing = prop.Facing;
            var desk = prop.Desk != null ? office.Find("Workstations/" + prop.Desk) : null;
            if (desk != null)
            {
                place = desk.TransformPoint(place);
                var chair = desk.Find("Chair");
                facing = chair != null ? Vector3.ProjectOnPlane(chair.position - place, Vector3.up) : desk.forward;
            }
            root.transform.rotation = Quaternion.Euler(0f, Vector3.SignedAngle(prop.Front, facing, Vector3.up), 0f);
            root.transform.position = place;

            var anomaly = root.AddComponent<AnomalyObject>();
            anomaly.anomalyName = prop.Title;
            anomaly.replaces = prop.Replaces;
            OfficeMenu.AddHighlight(root.transform);
            return root;
        }
    }
}
