using System;
using System.Collections.Generic;
using PlayerSystem;
using UnityEngine;
using UnityEngine.Rendering;
using Random = System.Random;

namespace ScareSystem
{
    /// <summary>
    /// Fills an office with what the Babaika left behind: the people who worked there, flayed, still in their
    /// chairs (<see cref="FlayedBody"/>), blood on the floor, a few desks and the walls, a smear dragged towards the
    /// far door, and, if asked for, heaps and mountains of guts. Built in code from a <see cref="Kit"/>. Positions
    /// are in office space: x 0..8 across from the west wall, z 0..12 from the entrance wall; desks are found by
    /// name. Nothing blocks the way; the bloody floor has trigger patches (<see cref="FootstepSurface"/>) so steps
    /// there squelch.
    /// </summary>
    public static class GoreBuilder
    {
        /// <summary>What the scene is made of.</summary>
        [Serializable]
        public class Kit
        {
            public Material guts;
            [Tooltip("A darker, bloodier tone some of the guts get.")]
            public Material gutsDark;
            public Material organ;
            [Tooltip("Bare muscle of the bodies at the desks.")]
            public Material muscle;
            [Tooltip("Their eyes and teeth.")]
            public Material bone;
            public Material pool;
            public Material splatter;
            public Material smear;
            [Tooltip("Steps in blood and on guts.")]
            public AudioClip[] steps = new AudioClip[0];
        }

        const float DeskTop = 0.75f; // OfficeRoomBuilder's desk height
        static readonly HashSet<string> OwnNames = new HashSet<string> { "Gut", "Pool", "Smear", "Lump", "Muscle" };
        static Mesh quad;

        /// <summary>
        /// Builds everything under <paramref name="room"/>, with heaps and mountains of guts only if
        /// <paramref name="guts"/>; returns the point (world) the eye goes to first.
        /// </summary>
        public static Vector3 Build(Transform room, Kit kit, Random rng, bool guts)
        {
            var root = new GameObject("Gore").transform;
            root.SetParent(room, false);

            // The people who worked here, still at their desks. The first one has turned to face the door: under the
            // same warm light from above that marks anomalies, the eye finds it at once.
            var look = Sitter(root, kit, rng, room, "Desk_W2", FlayedBody.Pose.ThrownBack) ?? room.TransformPoint(new Vector3(4f, 1f, 5f));
            Sitter(root, kit, rng, room, "Desk_W3", FlayedBody.Pose.OnDesk);
            Sitter(root, kit, rng, room, "Desk_W4", FlayedBody.Pose.Slumped);
            Sitter(root, kit, rng, room, "Desk_E2", FlayedBody.Pose.OnDesk);
            Sitter(root, kit, rng, room, "Desk_E3", FlayedBody.Pose.ThrownBack);
            Sitter(root, kit, rng, room, "Desk_E5", FlayedBody.Pose.Slumped);
            var light = root.InverseTransformPoint(look);
            LevelSystem.Prims.SpotLight("Gore_Light", root, new Vector3(light.x, 2.75f, light.z), Vector3.down, 70f, 3.4f, 2.4f, new Color(1f, 0.82f, 0.6f), false);

            // Blood: a few pools, splashes, a smear dragged to the far door, some on desks and walls.
            for (int i = 0; i < 3; i++)
            {
                var at = new Vector3(R(rng, 2.6f, 5.4f), 0f, R(rng, 2.5f, 10.5f));
                float size = R(rng, 0.35f, 0.65f);
                Pool(root, kit.pool, rng, at, size, 0.0015f);
                Squelch(root, kit, at, size);
            }
            for (int i = 0; i < 16; i++)
                Splat(root, kit.splatter, rng, new Vector3(R(rng, 1.8f, 6.2f), 0f, R(rng, 1.5f, 11.4f)), Vector3.up, R(rng, 0.1f, 0.35f));
            Drag(root, kit, rng, new[] { new Vector3(2.4f, 0f, 3.6f), new Vector3(3.2f, 0f, 6.5f), new Vector3(2.9f, 0f, 9.4f), new Vector3(2.2f, 0f, 11.4f) }, 0.3f);
            foreach (var name in new[] { "Desk_W1", "Desk_W5", "Desk_E1" })
                Desk(root, kit, rng, room.Find("Workstations/" + name), false, guts);
            Splat(root, kit.splatter, rng, new Vector3(8f, 1.55f, 3.2f), Vector3.left, 0.7f); // up the walls, where nothing stands in front
            Splat(root, kit.splatter, rng, new Vector3(8f, 1.15f, 2.6f), Vector3.left, 0.35f);
            Splat(root, kit.splatter, rng, new Vector3(0f, 1.7f, 10.8f), Vector3.right, 0.6f);

            if (guts)
            {
                // A heap in the middle of the aisle and mountains along both sides; the middle stays clear enough.
                Heap(root, kit, rng, new Vector3(4.3f, 0f, 4.8f), 0.6f);
                foreach (float z in new[] { 2.6f, 5.1f, 7.5f, 9.7f })
                    Mound(root, kit, rng, new Vector3(R(rng, 2.05f, 2.35f), 0f, z + R(rng, -0.3f, 0.3f)), R(rng, 0.6f, 0.8f));
                foreach (float z in new[] { 3.9f, 6.3f, 8.7f, 10.9f })
                    Mound(root, kit, rng, new Vector3(R(rng, 5.65f, 5.95f), 0f, z + R(rng, -0.3f, 0.3f)), R(rng, 0.6f, 0.8f));
                Desk(root, kit, rng, room.Find("Workstations/Desk_W3"), true, true);
                Desk(root, kit, rng, room.Find("Workstations/Desk_E3"), true, true);
                Desk(root, kit, rng, room.Find("Workstations/Desk_E4"), false, true);
            }

            // Everything above was built as new meshes (the shared splash quad and Unity's primitives aside).
            var owned = root.gameObject.AddComponent<OwnedMeshes>();
            foreach (var filter in root.GetComponentsInChildren<MeshFilter>())
                if (filter.sharedMesh != null && filter.sharedMesh != quad && OwnNames.Contains(filter.sharedMesh.name))
                    owned.meshes.Add(filter.sharedMesh);

            return look;
        }

        // ------------------------------------------------------------------ pieces

        /// <summary>Two or three coiled strands and a few lumps in a pool, with splashes around.</summary>
        static void Heap(Transform root, Kit kit, Random rng, Vector3 center, float size)
        {
            Pool(root, kit.pool, rng, center, size * 1.7f, 0.002f);
            Squelch(root, kit, center, size * 1.7f);
            int strands = size > 0.4f ? 3 : 2;
            for (int s = 0; s < strands; s++)
            {
                float tube = s == 0 ? R(rng, 0.05f, 0.06f) : R(rng, 0.03f, 0.042f); // one thick, the rest thin
                var at = center + new Vector3(R(rng, -0.15f, 0.15f), tube * s * 0.9f, R(rng, -0.15f, 0.15f)) * size / 0.5f;
                Gut(root, GutTone(kit, rng), rng, Coil(rng, at, size * R(rng, 0.6f, 1f), tube, rng.Next(22, 34), true), tube);
            }
            for (int k = size > 0.4f ? 3 : 1; k > 0; k--)
            {
                float a = R(rng, 0f, 2f * Mathf.PI);
                Organ(root, kit.organ, rng, center + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * size * R(rng, 0.7f, 1.2f), R(rng, 0.1f, 0.16f));
            }
            for (int k = 0; k < 7; k++)
            {
                float a = R(rng, 0f, 2f * Mathf.PI), d = size * R(rng, 1.2f, 2.8f);
                Splat(root, kit.splatter, rng, center + new Vector3(Mathf.Cos(a) * d, 0f, Mathf.Sin(a) * d), Vector3.up, R(rng, 0.1f, 0.4f));
            }
        }

        /// <summary>
        /// A mountain of guts: a dozen strands winding over a cone, lumps on its slopes, a wide pool under it. Too many
        /// pieces for shadows, so it casts none.
        /// </summary>
        static void Mound(Transform root, Kit kit, Random rng, Vector3 center, float size)
        {
            float height = size * R(rng, 0.6f, 0.85f);
            Pool(root, kit.pool, rng, center, size * 2.1f, 0.002f);
            Squelch(root, kit, center, size * 1.8f);
            for (int s = rng.Next(10, 15); s > 0; s--)
            {
                float tube = rng.NextDouble() < 0.35 ? R(rng, 0.05f, 0.065f) : R(rng, 0.03f, 0.045f);
                Gut(root, GutTone(kit, rng), rng, OverMound(rng, center, size, height, tube, rng.Next(16, 26)), tube, 8, 4, false);
            }
            for (int k = rng.Next(4, 7); k > 0; k--)
            {
                float a = R(rng, 0f, 2f * Mathf.PI), r = size * R(rng, 0.1f, 0.8f);
                float y = height * Mathf.Pow(1f - r / size, 0.7f);
                Organ(root, kit.organ, rng, center + new Vector3(Mathf.Cos(a) * r, y * 0.85f, Mathf.Sin(a) * r), R(rng, 0.08f, 0.14f));
            }
            for (int k = 0; k < 10; k++)
            {
                float a = R(rng, 0f, 2f * Mathf.PI), d = size * R(rng, 1.1f, 2.6f);
                Splat(root, kit.splatter, rng, center + new Vector3(Mathf.Cos(a) * d, 0f, Mathf.Sin(a) * d), Vector3.up, R(rng, 0.15f, 0.5f));
            }
        }

        /// <summary>A strand winding over a mound of the given size and height, close under its surface so it shows.</summary>
        static List<Vector3> OverMound(Random rng, Vector3 center, float size, float height, float tube, int count)
        {
            var points = new List<Vector3>(count);
            float angle = R(rng, 0f, 2f * Mathf.PI), radius = size * R(rng, 0.2f, 0.9f);
            float turn = R(rng, 0.35f, 0.6f) * (rng.Next(2) == 0 ? 1f : -1f);
            for (int i = 0; i < count; i++)
            {
                if (rng.NextDouble() < 0.15) turn = -turn;
                angle += turn * R(rng, 0.7f, 1.3f);
                radius = Mathf.Clamp(radius + R(rng, -0.15f, 0.15f) * size, size * 0.05f, size);
                float surface = height * Mathf.Pow(1f - radius / size, 0.7f);
                points.Add(center + new Vector3(Mathf.Cos(angle) * radius, tube + surface * R(rng, 0.75f, 1f), Mathf.Sin(angle) * radius));
            }
            return points;
        }

        /// <summary>A dragged smear along the points that squelches underfoot.</summary>
        static void Drag(Transform root, Kit kit, Random rng, Vector3[] through, float width)
        {
            Smear(root, kit.smear, rng, through, width);
            foreach (var point in Spline(new List<Vector3>(through), 2)) Squelch(root, kit, point, width);
        }

        /// <summary>Blood on a desk; with guts a heap of them, and with spill a strand sliding over the edge to the floor.</summary>
        static void Desk(Transform root, Kit kit, Random rng, Transform desk, bool spill, bool guts)
        {
            if (desk == null) return;
            // Desk space: x along the desk, +z away from the wall, the top at DeskTop.
            Vector3 P(float x, float y, float z) => root.InverseTransformPoint(desk.TransformPoint(new Vector3(x, y, z)));
            var up = root.InverseTransformDirection(desk.up);

            Pool(root, kit.pool, rng, P(-0.3f, DeskTop, 0.5f), guts ? 0.26f : 0.2f, 0.003f, up);
            for (int k = 0; k < 3; k++) Splat(root, kit.splatter, rng, P(R(rng, -0.6f, 0.5f), DeskTop, R(rng, 0.25f, 0.7f)), up, R(rng, 0.08f, 0.2f));
            if (!guts)
            {
                Pool(root, kit.pool, rng, P(0f, 0f, 0.95f), 0.14f, 0.002f); // drips off the front edge
                return;
            }
            float tube = 0.03f;
            Gut(root, GutTone(kit, rng), rng, Coil(rng, P(-0.36f, DeskTop, 0.48f), 0.17f, tube, 16, false), tube);

            if (spill)
            {
                var hang = new List<Vector3>
                {
                    P(-0.3f, DeskTop + 0.05f, 0.6f), P(-0.27f, DeskTop + 0.03f, 0.74f), P(-0.25f, DeskTop - 0.06f, 0.8f),
                    P(-0.24f, 0.45f, 0.81f), P(-0.22f, 0.2f, 0.84f), P(-0.18f, 0.05f, 0.94f), P(-0.05f, tube, 1.06f),
                    P(0.12f, tube, 1.1f), P(0.22f, tube, 1.02f),
                };
                Gut(root, GutTone(kit, rng), rng, hang, tube);
                Pool(root, kit.pool, rng, P(-0.1f, 0f, 1.02f), 0.38f, 0.002f);
                Squelch(root, kit, P(-0.1f, 0f, 1.02f), 0.38f);
            }
            else
            {
                Organ(root, kit.organ, rng, P(0.05f, DeskTop, 0.55f), R(rng, 0.08f, 0.11f));
                Pool(root, kit.pool, rng, P(0f, 0f, 0.95f), 0.16f, 0.002f); // drips off the front edge
            }
        }

        /// <summary>A flayed body in the chair of <paramref name="desk"/>; returns its upper body (world), or null.</summary>
        static Vector3? Sitter(Transform root, Kit kit, Random rng, Transform room, string desk, FlayedBody.Pose pose)
        {
            var d = room.Find("Workstations/" + desk);
            var chair = d != null ? d.Find("Chair") : null;
            if (chair == null || kit.muscle == null) return null;
            // Only the ones fallen onto the desk keep facing it; the rest have turned round towards the aisle.
            if (pose != FlayedBody.Pose.OnDesk) chair.Rotate(0f, 180f + R(rng, -25f, 25f), 0f, Space.World);
            // How far ahead of the chair the desk's front edge is.
            var inDesk = d.InverseTransformPoint(chair.position);
            var edge = chair.InverseTransformPoint(d.TransformPoint(new Vector3(inDesk.x, DeskTop, 0.775f)));
            return FlayedBody.Seat(root, kit, rng, chair, edge.z, pose);
        }

        /// <summary>A pool of blood for other builders; on the floor it can squelch underfoot.</summary>
        internal static void Puddle(Transform root, Kit kit, Random rng, Vector3 center, float radius, Vector3 up, bool squelch = false)
        {
            Pool(root, kit.pool, rng, center, radius, 0.003f, up);
            if (squelch) Squelch(root, kit, center, radius);
        }

        /// <summary>A trigger over a bloody patch of floor, so steps there squelch.</summary>
        static void Squelch(Transform root, Kit kit, Vector3 center, float radius)
        {
            if (kit.steps == null || kit.steps.Length == 0) return;
            var go = new GameObject("Squelch", typeof(BoxCollider), typeof(FootstepSurface)) { layer = 2 }; // Ignore Raycast: sight checks look past it
            go.transform.SetParent(root, false);
            go.transform.localPosition = new Vector3(center.x, 0.15f, center.z);
            var box = go.GetComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(radius * 1.8f, 0.3f, radius * 1.8f);
            go.GetComponent<FootstepSurface>().clips = kit.steps;
        }

        /// <summary>
        /// A strand heaped in loops: it curls, bends back now and then, wanders in and out, and later loops lie on
        /// the earlier ones. With a tail its end slides off the heap across the floor.
        /// </summary>
        static List<Vector3> Coil(Random rng, Vector3 center, float spread, float tube, int count, bool tail)
        {
            var points = new List<Vector3>(count + 3);
            float angle = R(rng, 0f, 2f * Mathf.PI), radius = spread * 0.35f;
            float turn = R(rng, 0.45f, 0.75f) * (rng.Next(2) == 0 ? 1f : -1f);
            for (int i = 0; i < count; i++)
            {
                if (rng.NextDouble() < 0.12) turn = -turn; // an S-bend
                angle += turn * R(rng, 0.7f, 1.3f);
                radius = Mathf.Clamp(radius + R(rng, -0.18f, 0.2f) * spread, spread * 0.15f, spread);
                float height = tube * (1f + 2.2f * i / count * R(rng, 0.5f, 1f));
                points.Add(center + new Vector3(Mathf.Cos(angle) * radius, height, Mathf.Sin(angle) * radius));
            }
            if (!tail) return points;
            Vector3 last = points[points.Count - 1];
            Vector3 outward = Vector3.ProjectOnPlane(last - center, Vector3.up);
            outward = outward.sqrMagnitude > 1e-4f ? outward.normalized : Vector3.forward;
            Vector3 side = Vector3.Cross(Vector3.up, outward);
            for (int k = 1; k <= 3; k++)
                points.Add(new Vector3(last.x, center.y + tube, last.z) + outward * spread * 0.45f * k + side * R(rng, -0.12f, 0.12f) * k);
            return points;
        }

        static void Gut(Transform root, Material material, Random rng, List<Vector3> points, float radius, int sides = 10, int steps = 6, bool shadows = true) =>
            Piece(root, "Gut", Tube(Spline(points, steps), radius, sides, R(rng, 0f, 10f)), material, shadows);

        /// <summary>The usual guts, or now and then the darker ones.</summary>
        static Material GutTone(Kit kit, Random rng) => kit.gutsDark != null && rng.NextDouble() < 0.4 ? kit.gutsDark : kit.guts;

        static void Organ(Transform root, Material material, Random rng, Vector3 at, float size)
        {
            var go = Piece(root, "Organ", Lump(rng), material, true);
            go.transform.localPosition = at + Vector3.up * size * 0.4f;
            go.transform.localRotation = Quaternion.Euler(R(rng, -15f, 15f), R(rng, 0f, 360f), R(rng, -15f, 15f));
            go.transform.localScale = new Vector3(size * R(rng, 1.1f, 1.7f), size * R(rng, 0.4f, 0.65f), size * R(rng, 0.8f, 1.3f));
        }

        /// <summary>A flat puddle with a ragged edge, lying on the surface facing <paramref name="up"/>.</summary>
        static void Pool(Transform root, Material material, Random rng, Vector3 center, float radius, float lift, Vector3? up = null)
        {
            const int n = 40;
            var harmonics = new (float amount, float phase)[5];
            for (int k = 0; k < harmonics.Length; k++) harmonics[k] = (R(rng, 0.04f, 0.13f) / Mathf.Sqrt(k + 1), R(rng, 0f, 6.3f));
            var verts = new List<Vector3> { Vector3.zero };
            var tris = new List<int>();
            for (int i = 0; i < n; i++)
            {
                float a = 2f * Mathf.PI * i / n, r = 1f;
                for (int k = 0; k < harmonics.Length; k++) r += harmonics[k].amount * Mathf.Sin((k + 2) * a + harmonics[k].phase);
                verts.Add(new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * radius * r);
                tris.AddRange(new[] { 0, 1 + (i + 1) % n, 1 + i });
            }
            var mesh = new Mesh { name = "Pool" };
            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.SetNormals(verts.ConvertAll(_ => Vector3.up));
            mesh.SetUVs(0, verts.ConvertAll(v => new Vector2(v.x, v.z)));
            mesh.RecalculateBounds();
            var go = Piece(root, "Pool", mesh, material, false);
            var normal = up ?? Vector3.up;
            go.transform.localPosition = center + normal * lift;
            go.transform.localRotation = Quaternion.FromToRotation(Vector3.up, normal) * Quaternion.Euler(0f, R(rng, 0f, 360f), 0f);
        }

        /// <summary>A splash (cut-out texture on a quad) on a surface facing <paramref name="facing"/>.</summary>
        static void Splat(Transform root, Material material, Random rng, Vector3 at, Vector3 facing, float size)
        {
            var go = Piece(root, "Splash", Quad(), material, false);
            go.transform.localPosition = at + facing * R(rng, 0.003f, 0.007f); // a little apart, so overlaps never flicker
            go.transform.localRotation = Quaternion.FromToRotation(Vector3.up, facing) * Quaternion.Euler(0f, R(rng, 0f, 360f), 0f);
            go.transform.localScale = new Vector3(size, 1f, size * R(rng, 0.8f, 1.25f));
        }

        /// <summary>A dragged smear along a curve over the floor (the smear texture runs along it).</summary>
        static void Smear(Transform root, Material material, Random rng, Vector3[] through, float width)
        {
            var path = Spline(new List<Vector3>(through), 10);
            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();
            float length = 0f;
            for (int i = 0; i < path.Count; i++)
            {
                if (i > 0) length += Vector3.Distance(path[i - 1], path[i]);
                Vector3 dir = (i < path.Count - 1 ? path[i + 1] - path[i] : path[i] - path[i - 1]).normalized;
                Vector3 side = Vector3.Cross(Vector3.up, dir) * width * 0.5f * Mathf.Lerp(1f, 0.55f, (float)i / path.Count);
                verts.Add(path[i] - side + Vector3.up * 0.0025f);
                verts.Add(path[i] + side + Vector3.up * 0.0025f);
                uvs.Add(new Vector2(length / (width * 3f), 0f));
                uvs.Add(new Vector2(length / (width * 3f), 1f));
                if (i == 0) continue;
                int a = (i - 1) * 2;
                tris.AddRange(new[] { a, a + 2, a + 1, a + 1, a + 2, a + 3 });
            }
            var mesh = new Mesh { name = "Smear" };
            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.SetNormals(verts.ConvertAll(_ => Vector3.up));
            mesh.RecalculateBounds();
            Piece(root, "Smear", mesh, material, false);
        }

        // ------------------------------------------------------------------ meshes

        static GameObject Piece(Transform root, string name, Mesh mesh, Material material, bool shadows)
        {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(root, false);
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = shadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            return go;
        }

        /// <summary>Catmull-Rom through the points, <paramref name="steps"/> points per span.</summary>
        static List<Vector3> Spline(List<Vector3> p, int steps)
        {
            var result = new List<Vector3>();
            for (int i = 0; i < p.Count - 1; i++)
            {
                Vector3 p0 = p[Mathf.Max(i - 1, 0)], p1 = p[i], p2 = p[i + 1], p3 = p[Mathf.Min(i + 2, p.Count - 1)];
                for (int s = 0; s < steps; s++)
                {
                    float t = (float)s / steps, t2 = t * t, t3 = t2 * t;
                    result.Add(0.5f * (2f * p1 + (p2 - p0) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (3f * p1 - p0 - 3f * p2 + p3) * t3));
                }
            }
            result.Add(p[p.Count - 1]);
            return result;
        }

        /// <summary>A round tube along the path that swells and narrows like a gut, thinner at both ends, capped.</summary>
        static Mesh Tube(List<Vector3> path, float radius, int sides, float phase)
        {
            var verts = new List<Vector3>();
            var normals = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();
            Vector3 first = (path[1] - path[0]).normalized;
            Vector3 normal = Vector3.Cross(first, Mathf.Abs(first.y) < 0.9f ? Vector3.up : Vector3.right).normalized;
            float length = 0f;
            int rings = path.Count;
            for (int i = 0; i < rings; i++)
            {
                if (i > 0) length += Vector3.Distance(path[i - 1], path[i]);
                Vector3 t = (i < rings - 1 ? path[i + 1] - path[i] : path[i] - path[i - 1]).normalized;
                normal = Vector3.ProjectOnPlane(normal, t).normalized; // carried along, so the tube does not twist
                Vector3 binormal = Vector3.Cross(t, normal);
                float end = Mathf.Clamp01(Mathf.Min(i, rings - 1 - i) / 4f);
                float r = radius * (0.86f + 0.14f * Mathf.Sin(length / radius * 1.25f + phase)) * Mathf.Lerp(0.5f, 1f, end);
                for (int s = 0; s <= sides; s++)
                {
                    float a = 2f * Mathf.PI * s / sides;
                    Vector3 dir = normal * Mathf.Cos(a) + binormal * Mathf.Sin(a);
                    verts.Add(path[i] + dir * r);
                    normals.Add(dir);
                    uvs.Add(new Vector2((float)s / sides, length / (2f * Mathf.PI * radius)));
                }
            }
            for (int i = 0; i < rings - 1; i++)
            {
                for (int s = 0; s < sides; s++)
                {
                    int a = i * (sides + 1) + s, b = a + sides + 1;
                    tris.AddRange(new[] { a, a + 1, b, a + 1, b + 1, b });
                }
            }
            Cap(verts, normals, uvs, tris, path[0], -(path[1] - path[0]).normalized, 0, sides, true);
            Cap(verts, normals, uvs, tris, path[rings - 1], (path[rings - 1] - path[rings - 2]).normalized, (rings - 1) * (sides + 1), sides, false);

            var mesh = new Mesh { name = "Gut" };
            if (verts.Count > 65000) mesh.indexFormat = IndexFormat.UInt32;
            mesh.SetVertices(verts);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        static void Cap(List<Vector3> verts, List<Vector3> normals, List<Vector2> uvs, List<int> tris, Vector3 center, Vector3 facing, int ring, int sides, bool start)
        {
            int c = verts.Count;
            verts.Add(center);
            normals.Add(facing);
            uvs.Add(new Vector2(0.5f, 0f));
            for (int s = 0; s <= sides; s++)
            {
                verts.Add(verts[ring + s]);
                normals.Add(facing);
                uvs.Add(new Vector2((float)s / sides, 0f));
            }
            for (int s = 0; s < sides; s++)
            {
                if (start) tris.AddRange(new[] { c, c + 2 + s, c + 1 + s });
                else tris.AddRange(new[] { c, c + 1 + s, c + 2 + s });
            }
        }

        /// <summary>A bumpy blob of radius about 1 with smooth normals (no UV seam).</summary>
        static Mesh Lump(Random rng)
        {
            const int rings = 10, segments = 14;
            var bumps = new Vector3[5];
            for (int k = 0; k < bumps.Length; k++) bumps[k] = new Vector3(R(rng, -1f, 1f), R(rng, -1f, 1f), R(rng, -1f, 1f)).normalized;
            float Radius(Vector3 dir)
            {
                float r = 1f;
                foreach (var b in bumps) r += 0.16f * Mathf.Pow(Mathf.Max(0f, Vector3.Dot(dir, b)), 3f);
                return r;
            }
            var verts = new List<Vector3> { Vector3.up * Radius(Vector3.up) };
            for (int j = 1; j < rings; j++)
            {
                float v = Mathf.PI * j / rings;
                for (int i = 0; i < segments; i++)
                {
                    float u = 2f * Mathf.PI * i / segments;
                    var dir = new Vector3(Mathf.Sin(v) * Mathf.Cos(u), Mathf.Cos(v), Mathf.Sin(v) * Mathf.Sin(u));
                    verts.Add(dir * Radius(dir));
                }
            }
            verts.Add(Vector3.down * Radius(Vector3.down));
            int bottom = verts.Count - 1;
            int At(int j, int i) => 1 + (j - 1) * segments + (i % segments);
            var tris = new List<int>();
            for (int i = 0; i < segments; i++) tris.AddRange(new[] { 0, At(1, i + 1), At(1, i) });
            for (int j = 1; j < rings - 1; j++)
                for (int i = 0; i < segments; i++)
                    tris.AddRange(new[] { At(j, i), At(j, i + 1), At(j + 1, i), At(j, i + 1), At(j + 1, i + 1), At(j + 1, i) });
            for (int i = 0; i < segments; i++) tris.AddRange(new[] { bottom, At(rings - 1, i), At(rings - 1, i + 1) });
            var mesh = new Mesh { name = "Lump" };
            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>A unit quad lying flat, facing up, UVs 0..1 (shared).</summary>
        static Mesh Quad()
        {
            if (quad != null) return quad;
            quad = new Mesh { name = "GoreQuad" };
            quad.SetVertices(new List<Vector3> { new Vector3(-0.5f, 0f, -0.5f), new Vector3(0.5f, 0f, -0.5f), new Vector3(-0.5f, 0f, 0.5f), new Vector3(0.5f, 0f, 0.5f) });
            quad.SetUVs(0, new List<Vector2> { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f) });
            quad.SetNormals(new List<Vector3> { Vector3.up, Vector3.up, Vector3.up, Vector3.up });
            quad.SetTriangles(new[] { 0, 2, 1, 1, 2, 3 }, 0);
            quad.RecalculateBounds();
            return quad;
        }

        static float R(Random rng, float min, float max) => min + (float)rng.NextDouble() * (max - min);
    }
}
