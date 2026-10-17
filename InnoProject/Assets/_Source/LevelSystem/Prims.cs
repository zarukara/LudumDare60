using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace LevelSystem
{
    /// <summary>
    /// Building blocks for the level. Boxes get their own meshes whose UVs are in metres (cached per size), so a
    /// texture keeps the same scale on every floor, wall and desk. Cylinders and spheres are Unity primitives.
    /// </summary>
    public static class Prims
    {
        static readonly Dictionary<Vector3Int, Mesh> boxMeshes = new Dictionary<Vector3Int, Mesh>();
        static readonly Dictionary<Vector3Int, Mesh> frustumMeshes = new Dictionary<Vector3Int, Mesh>();

        /// <summary>Forgets the cached meshes (the editor saves them into an asset before reusing them).</summary>
        public static void ClearCache()
        {
            boxMeshes.Clear();
            frustumMeshes.Clear();
        }

        public static Transform Group(string name, Transform parent, Vector3 position = default, float yaw = 0f)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.localPosition = position;
            t.localRotation = Quaternion.Euler(0f, yaw, 0f);
            return t;
        }

        public static GameObject Box(string name, Transform parent, Vector3 center, Vector3 size, Material mat, bool collider = true, Vector3? euler = null)
        {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            var t = go.transform;
            t.SetParent(parent, false);
            t.localPosition = center;
            t.localRotation = euler.HasValue ? Quaternion.Euler(euler.Value) : Quaternion.identity;
            go.GetComponent<MeshFilter>().sharedMesh = BoxMesh(size);
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            if (collider) go.AddComponent<BoxCollider>().size = size;
            return go;
        }

        /// <summary>Box without shadows or collider (glowing panels, thin decals).</summary>
        public static GameObject Flat(string name, Transform parent, Vector3 center, Vector3 size, Material mat, Vector3? euler = null)
        {
            var go = Box(name, parent, center, size, mat, false, euler);
            go.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            return go;
        }

        /// <summary>Upright cylinder (Unity primitive) given radius and height; set euler to lay it down.</summary>
        public static GameObject Cylinder(string name, Transform parent, Vector3 center, float radius, float height, Material mat, bool collider = false, Vector3? euler = null) =>
            Primitive(PrimitiveType.Cylinder, name, parent, center, new Vector3(radius * 2f, height / 2f, radius * 2f), mat, collider, euler);

        public static GameObject Sphere(string name, Transform parent, Vector3 center, Vector3 size, Material mat, bool collider = false) =>
            Primitive(PrimitiveType.Sphere, name, parent, center, size, mat, collider, null);

        public static GameObject Capsule(string name, Transform parent, Vector3 center, Vector3 size, Material mat, bool collider = false, Vector3? euler = null) =>
            Primitive(PrimitiveType.Capsule, name, parent, center, size, mat, collider, euler);

        /// <summary>Thin box from a to b (rods, tripod legs, wires).</summary>
        public static GameObject Segment(string name, Transform parent, Vector3 a, Vector3 b, float thickness, Material mat)
        {
            Vector3 d = b - a;
            return Box(name, parent, (a + b) / 2f, new Vector3(thickness, thickness, d.magnitude), mat, false, Quaternion.LookRotation(d).eulerAngles);
        }

        /// <summary>
        /// Open, double-sided truncated cone (a gramophone horn, a lamp shade): radius r0 at the base point, r1 at
        /// <paramref name="height"/> along the local Y axis turned by <paramref name="euler"/>.
        /// </summary>
        public static GameObject Frustum(string name, Transform parent, Vector3 basePoint, float r0, float r1, float height, Material mat, Vector3 euler)
        {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(parent, false);
            go.transform.localPosition = basePoint;
            go.transform.localRotation = Quaternion.Euler(euler);
            go.GetComponent<MeshFilter>().sharedMesh = FrustumMesh(r0, r1, height, 16);
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            return go;
        }

        static Mesh FrustumMesh(float r0, float r1, float height, int segments)
        {
            var key = new Vector3Int(Mathf.RoundToInt(r0 * 1000f), Mathf.RoundToInt(r1 * 1000f), Mathf.RoundToInt(height * 1000f));
            if (frustumMeshes.TryGetValue(key, out var cached) && cached != null) return cached;
            var verts = new List<Vector3>();
            var tris = new List<int>();
            for (int i = 0; i < segments; i++)
            {
                float a0 = 2f * Mathf.PI * i / segments, a1 = 2f * Mathf.PI * (i + 1) / segments;
                var b0 = new Vector3(Mathf.Cos(a0) * r0, 0f, Mathf.Sin(a0) * r0);
                var b1 = new Vector3(Mathf.Cos(a1) * r0, 0f, Mathf.Sin(a1) * r0);
                var t0 = new Vector3(Mathf.Cos(a0) * r1, height, Mathf.Sin(a0) * r1);
                var t1 = new Vector3(Mathf.Cos(a1) * r1, height, Mathf.Sin(a1) * r1);
                int k = verts.Count;
                verts.AddRange(new[] { b0, t0, t1, b1, b0, b1, t1, t0 }); // outside, then inside
                tris.AddRange(new[] { k, k + 1, k + 2, k, k + 2, k + 3, k + 4, k + 5, k + 6, k + 4, k + 6, k + 7 });
            }
            var mesh = new Mesh { name = $"Frustum_{key.x}_{key.y}_{key.z}" };
            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.SetUVs(0, new List<Vector2>(new Vector2[verts.Count]));
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            frustumMeshes[key] = mesh;
            return mesh;
        }

        /// <summary>Flat picture whose visible side faces <paramref name="facing"/> (screens, posters, signs).</summary>
        public static GameObject Quad(string name, Transform parent, Vector3 center, Vector2 size, Vector3 facing, Material mat, float roll = 0f)
        {
            var go = Primitive(PrimitiveType.Quad, name, parent, center, new Vector3(size.x, size.y, 1f), mat, false, null);
            go.transform.localRotation = Quaternion.LookRotation(-facing, Vector3.up) * Quaternion.Euler(0f, 0f, roll);
            go.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            return go;
        }

        /// <summary>3D text readable from the <paramref name="facing"/> side; lineHeight is in metres.</summary>
        public static TextMesh Text(string name, Transform parent, Vector3 position, Vector3 facing, string text, float lineHeight,
            Color color, Material mat, TextAnchor anchor = TextAnchor.MiddleCenter)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localRotation = Quaternion.LookRotation(-facing, Vector3.up);
            var tm = go.AddComponent<TextMesh>();
            tm.font = FontBinder.DefaultFont;
            tm.fontSize = 64;
            tm.characterSize = lineHeight / 6.4f;
            tm.anchor = anchor;
            tm.alignment = anchor == TextAnchor.UpperLeft || anchor == TextAnchor.MiddleLeft || anchor == TextAnchor.LowerLeft
                ? TextAlignment.Left
                : TextAlignment.Center;
            tm.color = color;
            tm.text = text;
            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = mat;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            go.AddComponent<FontBinder>();
            return tm;
        }

        public static Light PointLight(string name, Transform parent, Vector3 position, float range, float intensity, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = range;
            light.intensity = intensity;
            light.color = color;
            light.shadows = LightShadows.None;
            return light;
        }

        public static Light SpotLight(string name, Transform parent, Vector3 position, Vector3 direction, float angle, float range, float intensity,
            Color color, bool shadows)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localRotation = Quaternion.LookRotation(direction, Mathf.Abs(direction.y) > 0.9f ? Vector3.forward : Vector3.up);
            var light = go.AddComponent<Light>();
            light.type = LightType.Spot;
            light.spotAngle = angle;
            light.innerSpotAngle = angle * 0.45f;
            light.range = range;
            light.intensity = intensity;
            light.color = color;
            light.shadows = shadows ? LightShadows.Soft : LightShadows.None;
            light.shadowStrength = 0.75f;
            light.shadowNearPlane = 0.1f;
            return light;
        }

        static GameObject Primitive(PrimitiveType type, string name, Transform parent, Vector3 center, Vector3 scale, Material mat, bool collider, Vector3? euler)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            var t = go.transform;
            t.SetParent(parent, false);
            t.localPosition = center;
            t.localRotation = euler.HasValue ? Quaternion.Euler(euler.Value) : Quaternion.identity;
            t.localScale = scale;
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        /// <summary>Centred box mesh with flat faces; each face is mapped 1 UV unit = 1 metre, V pointing up on walls.</summary>
        public static Mesh BoxMesh(Vector3 size)
        {
            var key = new Vector3Int(Mathf.RoundToInt(size.x * 1000f), Mathf.RoundToInt(size.y * 1000f), Mathf.RoundToInt(size.z * 1000f));
            if (boxMeshes.TryGetValue(key, out var cached) && cached != null) return cached;

            var h = size / 2f;
            var verts = new List<Vector3>(24);
            var normals = new List<Vector3>(24);
            var uvs = new List<Vector2>(24);
            var tris = new List<int>(36);

            // For every face: outward normal n and in-face axes u, v with Cross(v, u) == n, so the winding faces out.
            void Face(Vector3 n, Vector3 u, Vector3 v)
            {
                float hn = Mathf.Abs(Vector3.Dot(h, n)), hu = Mathf.Abs(Vector3.Dot(h, u)), hv = Mathf.Abs(Vector3.Dot(h, v));
                Vector3 c = n * hn;
                int i = verts.Count;
                foreach (var (su, sv) in new[] { (-1f, -1f), (-1f, 1f), (1f, 1f), (1f, -1f) })
                {
                    var p = c + u * (su * hu) + v * (sv * hv);
                    verts.Add(p);
                    normals.Add(n);
                    uvs.Add(new Vector2(Vector3.Dot(p, u), Vector3.Dot(p, v)));
                }
                tris.AddRange(new[] { i, i + 1, i + 2, i, i + 2, i + 3 });
            }

            Face(Vector3.up, Vector3.right, Vector3.forward);
            Face(Vector3.down, Vector3.right, Vector3.back);
            Face(Vector3.right, Vector3.forward, Vector3.up);
            Face(Vector3.left, Vector3.back, Vector3.up);
            Face(Vector3.forward, Vector3.left, Vector3.up);
            Face(Vector3.back, Vector3.right, Vector3.up);

            var mesh = new Mesh { name = $"Box_{key.x}x{key.y}x{key.z}" };
            mesh.SetVertices(verts);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            boxMeshes[key] = mesh;
            return mesh;
        }
    }
}
