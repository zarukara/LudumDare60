using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Random = System.Random;

namespace ScareSystem
{
    /// <summary>
    /// A dead office worker with the skin gone, sitting in an office chair: bare muscle with white tendons at the
    /// joints, staring eyes, bared teeth, blood on the seat and the floor. Built from rounded, tapering limb meshes
    /// on a pose given in chair space (+Z towards the desk, the seat top at 0.5 m).
    /// </summary>
    public static class FlayedBody
    {
        public enum Pose
        {
            /// <summary>Fallen forward, cheek on the desk, arms around the head.</summary>
            OnDesk,
            /// <summary>Thrown back over the backrest, face up, arms hanging.</summary>
            ThrownBack,
            /// <summary>Slumped to one side, head on the shoulder, one arm hanging.</summary>
            Slumped,
        }

        /// <summary>
        /// Seats a body in <paramref name="chair"/>; <paramref name="deskEdge"/> is how far ahead of the chair (along
        /// its +Z) the desk's front edge is, so a body falling forward lands on the desk.
        /// </summary>
        public static void Seat(Transform root, GoreBuilder.Kit kit, Random rng, Transform chair, float deskEdge, Pose pose)
        {
            var body = new GameObject("Body_" + pose).transform;
            body.SetParent(root, false);
            body.position = chair.position;
            body.rotation = chair.rotation;

            // Legs: the same in every pose, a little apart and uneven.
            float spread = R(rng, -0.02f, 0.03f);
            Vector3 hipL = new Vector3(-0.095f, 0.56f, 0f), hipR = new Vector3(0.095f, 0.56f, 0f);
            Vector3 kneeL = new Vector3(-0.11f - spread, 0.55f, 0.43f), kneeR = new Vector3(0.11f + spread, 0.55f, 0.42f);
            Vector3 ankleL = new Vector3(-0.12f - spread, 0.09f, 0.47f + R(rng, -0.05f, 0.05f)), ankleR = new Vector3(0.12f + spread, 0.09f, 0.46f + R(rng, -0.05f, 0.05f));

            Vector3 spine = new Vector3(0f, 0.66f, -0.04f), chest, neck, head, headUp, face;
            Vector3 shoulderL, shoulderR, elbowL, elbowR, wristL, wristR, tipL, tipR;
            switch (pose)
            {
                case Pose.OnDesk:
                {
                    var d = new Vector3(0f, 0f, Mathf.Clamp(deskEdge - 0.28f, -0.05f, 0.25f)); // reach the desk
                    chest = new Vector3(0f, 0.9f, 0.2f) + d * 0.6f;
                    neck = new Vector3(0.01f, 0.95f, 0.34f) + d * 0.8f;
                    head = new Vector3(0.04f, 0.84f, 0.5f) + d;
                    headUp = new Vector3(0.25f, 0.1f, 1f);
                    face = new Vector3(-1f, -0.3f, 0.1f);
                    shoulderL = new Vector3(-0.18f, 0.94f, 0.26f) + d * 0.6f;
                    shoulderR = new Vector3(0.18f, 0.94f, 0.26f) + d * 0.6f;
                    elbowL = new Vector3(-0.31f, 0.8f, 0.48f) + d;
                    elbowR = new Vector3(0.3f, 0.8f, 0.45f) + d;
                    wristL = new Vector3(-0.2f, 0.78f, 0.7f) + d;
                    wristR = new Vector3(0.24f, 0.78f, 0.68f) + d;
                    tipL = new Vector3(-0.12f, 0.775f, 0.8f) + d;
                    tipR = new Vector3(0.2f, 0.775f, 0.84f) + d;
                    break;
                }
                case Pose.ThrownBack:
                    chest = new Vector3(0f, 0.93f, -0.15f);
                    neck = new Vector3(0f, 1.06f, -0.2f);
                    head = new Vector3(0f, 1.16f, -0.22f);
                    headUp = new Vector3(0f, 0.75f, -0.66f); // fallen back, but the face still towards the room
                    face = new Vector3(R(rng, -0.2f, 0.2f), 0.55f, 1f);
                    shoulderL = new Vector3(-0.19f, 0.99f, -0.15f);
                    shoulderR = new Vector3(0.19f, 0.99f, -0.15f);
                    elbowL = new Vector3(-0.29f, 0.74f, -0.1f);
                    elbowR = new Vector3(0.29f, 0.74f, -0.08f);
                    wristL = new Vector3(-0.31f, 0.5f, -0.02f);
                    wristR = new Vector3(0.31f, 0.5f, 0f);
                    tipL = new Vector3(-0.31f, 0.36f, 0.01f);
                    tipR = new Vector3(0.31f, 0.36f, 0.03f);
                    break;
                default:
                    chest = new Vector3(0.07f, 0.92f, 0.02f);
                    neck = new Vector3(0.13f, 1.02f, 0.04f);
                    head = new Vector3(0.2f, 1.08f, 0.08f);
                    headUp = new Vector3(0.6f, 0.8f, 0.05f);
                    face = new Vector3(0.15f, -0.25f, 1f);
                    shoulderL = new Vector3(-0.12f, 0.98f, 0.02f);
                    shoulderR = new Vector3(0.24f, 0.95f, 0.02f);
                    elbowL = new Vector3(-0.18f, 0.72f, 0.2f);
                    elbowR = new Vector3(0.32f, 0.7f, 0.05f);
                    wristL = new Vector3(-0.1f, 0.62f, 0.38f);
                    wristR = new Vector3(0.34f, 0.46f, 0.1f);
                    tipL = new Vector3(-0.04f, 0.6f, 0.48f);
                    tipR = new Vector3(0.34f, 0.33f, 0.12f);
                    break;
            }

            var muscle = kit.muscle;
            Vector3 side = Vector3.right;
            Vector3 waist = Vector3.Lerp(spine, chest, 0.45f);
            // Trunk.
            Part(body, muscle, spine + new Vector3(0f, -0.06f, 0.01f), spine + new Vector3(0f, 0.03f, 0f), 0.12f, 0.12f, 0f, side, 1.45f, 0.95f, 0.25f, 0.75f); // pelvis
            Part(body, muscle, spine, waist, 0.115f, 0.125f, 0.05f, side, 1.25f, 0.8f);
            Part(body, muscle, waist, neck - (neck - chest).normalized * 0.04f, 0.135f, 0.115f, 0.08f, side, 1.4f, 0.85f);
            Part(body, muscle, shoulderL, shoulderR, 0.05f, 0.05f, 0.2f, Vector3.up, 1f, 1f); // across the shoulders
            Part(body, muscle, neck - (neck - chest).normalized * 0.06f, neck + (head - neck).normalized * 0.03f, 0.05f, 0.045f, 0.1f, side, 1.1f, 0.9f);
            // The muscles that make it read as a body without skin: chest, belly, shoulders.
            Vector3 up = (neck - spine).normalized;
            Vector3 across = Vector3.ProjectOnPlane(side, up).normalized;
            Vector3 front = Vector3.Cross(across, up).normalized;
            Vector3 breast = Vector3.Lerp(spine, neck, 0.76f);
            foreach (float s in new[] { -1f, 1f })
            {
                var pec = breast + across * 0.075f * s + front * 0.085f;
                Part(body, muscle, pec - up * 0.035f, pec + up * 0.035f, 0.06f, 0.055f, 0f, across, 1.3f, 0.7f);
                for (int row = 0; row < 3; row++)
                {
                    var ab = Vector3.Lerp(spine, neck, 0.2f + row * 0.13f) + across * 0.038f * s + front * 0.095f;
                    Part(body, muscle, ab - up * 0.025f, ab + up * 0.025f, 0.03f, 0.03f, 0f, across, 1.15f, 0.55f);
                }
            }
            foreach (var (shoulder, elbow) in new[] { (shoulderL, elbowL), (shoulderR, elbowR) })
                Part(body, muscle, shoulder - up * 0.02f, shoulder + (elbow - shoulder).normalized * 0.07f, 0.056f, 0.045f, 0.1f, front, 1f, 1f);
            // Legs.
            foreach (var (hip, knee, ankle) in new[] { (hipL, kneeL, ankleL), (hipR, kneeR, ankleR) })
            {
                Part(body, muscle, hip, knee, 0.078f, 0.05f, 0.15f, side, 1f, 0.95f);
                Part(body, muscle, knee, ankle, 0.05f, 0.032f, 0.2f, side, 0.9f, 1f);
                Part(body, muscle, ankle, ankle + new Vector3(0f, -0.055f, 0.19f), 0.035f, 0.03f, 0f, side, 1.25f, 0.7f);
            }
            // Arms.
            foreach (var (shoulder, elbow, wrist, tip) in new[] { (shoulderL, elbowL, wristL, tipL), (shoulderR, elbowR, wristR, tipR) })
            {
                Part(body, muscle, shoulder, elbow, 0.047f, 0.036f, 0.14f, side, 1f, 1f);
                Part(body, muscle, elbow, wrist, 0.04f, 0.026f, 0.12f, side, 1.1f, 0.9f);
                Part(body, muscle, wrist, tip, 0.028f, 0.02f, 0f, side, 1.6f, 0.6f);
            }
            Head(body, kit, head, headUp.normalized, face.normalized);

            // Blood on the seat and under the chair.
            var seat = root.InverseTransformPoint(body.TransformPoint(new Vector3(0f, 0.5f, 0.06f)));
            GoreBuilder.Puddle(root, kit, rng, seat, 0.22f, root.InverseTransformDirection(body.up));
            var floor = root.InverseTransformPoint(body.TransformPoint(new Vector3(0f, 0f, 0.3f)));
            GoreBuilder.Puddle(root, kit, rng, new Vector3(floor.x, 0f, floor.z), R(rng, 0.35f, 0.55f), Vector3.up, true);
        }

        /// <summary>A skull-shaped head of muscle with lidless eyes and lipless teeth, crown along up, face along face.</summary>
        static void Head(Transform body, GoreBuilder.Kit kit, Vector3 center, Vector3 up, Vector3 face)
        {
            face = Vector3.ProjectOnPlane(face, up).normalized;
            var frame = Quaternion.LookRotation(face, up);
            Part(body, kit.muscle, center - up * 0.055f, center + up * 0.04f, 0.082f, 0.088f, 0.05f, frame * Vector3.right, 0.95f, 1.15f, 0.2f, 0.8f);
            foreach (float x in new[] { -0.032f, 0.032f })
            {
                var eye = center + frame * new Vector3(x, 0.018f, 0.083f);
                Ball(body, kit.bone, eye, 0.019f);
                Ball(body, kit.organ, eye + face * 0.016f + frame * new Vector3(x * 0.1f, 0f, 0f), 0.0085f);
            }
            Ball(body, kit.organ, center + frame * new Vector3(0f, -0.012f, 0.09f), 0.012f); // where the nose was
            var teeth = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.DestroyImmediate(teeth.GetComponent<Collider>());
            teeth.name = "Teeth";
            teeth.transform.SetParent(body, false);
            teeth.transform.localPosition = center + frame * new Vector3(0f, -0.048f, 0.082f);
            teeth.transform.localRotation = frame;
            teeth.transform.localScale = new Vector3(0.062f, 0.026f, 0.016f);
            teeth.GetComponent<MeshRenderer>().sharedMaterial = kit.bone;
        }

        static void Ball(Transform body, Material material, Vector3 at, float radius)
        {
            var ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Object.DestroyImmediate(ball.GetComponent<Collider>());
            ball.name = "Eye";
            ball.transform.SetParent(body, false);
            ball.transform.localPosition = at;
            ball.transform.localScale = Vector3.one * radius * 2f;
            var renderer = ball.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
        }

        /// <summary>
        /// One rounded, tapering part from a to b (radii ra, rb) with a muscle belly in the middle; the cross section
        /// is sx wide along <paramref name="side"/> and sy deep. The texture runs along it from v0 to v1, so the
        /// white tendon ends of the muscle texture land on the joints.
        /// </summary>
        static void Part(Transform body, Material material, Vector3 a, Vector3 b, float ra, float rb, float bulge, Vector3 side,
            float sx, float sy, float v0 = 0f, float v1 = 1f)
        {
            Vector3 axis = b - a;
            float length = axis.magnitude;
            if (length < 1e-4f) return;
            var go = new GameObject("Muscle", typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(body, false);
            go.transform.localPosition = a;
            Vector3 up = Vector3.Cross(axis, side);
            if (up.sqrMagnitude < 1e-6f) up = Vector3.Cross(axis, Vector3.forward);
            go.transform.localRotation = Quaternion.LookRotation(axis, up);
            go.GetComponent<MeshFilter>().sharedMesh = PartMesh(length, ra, rb, bulge, sx, sy, v0, v1);
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
        }

        /// <summary>Along +Z from 0 to length, with half-round ends (local x is the wide side).</summary>
        static Mesh PartMesh(float length, float ra, float rb, float bulge, float sx, float sy, float v0, float v1)
        {
            const int sides = 12, capRings = 4, bodyRings = 8;
            var rings = new List<(float z, float r, float nz)>(); // position along the axis, radius, axial part of the normal
            for (int k = 0; k <= capRings; k++)
            {
                float phi = Mathf.PI / 2f * (1f - (float)k / capRings); // 90° (the tip) .. 0°
                rings.Add((-ra * Mathf.Sin(phi), ra * Mathf.Cos(phi), -Mathf.Sin(phi)));
            }
            for (int k = 1; k < bodyRings; k++)
            {
                float t = (float)k / bodyRings;
                float r = Mathf.Lerp(ra, rb, t) * (1f + bulge * Mathf.Sin(Mathf.PI * t));
                rings.Add((t * length, r, 0f));
            }
            for (int k = 0; k <= capRings; k++)
            {
                float phi = Mathf.PI / 2f * k / capRings; // 0° .. 90° (the tip)
                rings.Add((length + rb * Mathf.Sin(phi), rb * Mathf.Cos(phi), Mathf.Sin(phi)));
            }

            float zMin = -ra, zMax = length + rb;
            var verts = new List<Vector3>();
            var normals = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();
            for (int i = 0; i < rings.Count; i++)
            {
                var (z, r, nz) = rings[i];
                float v = Mathf.Lerp(v0, v1, (z - zMin) / (zMax - zMin));
                for (int s = 0; s <= sides; s++)
                {
                    float a = 2f * Mathf.PI * s / sides;
                    float cx = Mathf.Cos(a), cy = Mathf.Sin(a);
                    verts.Add(new Vector3(cx * r * sx, cy * r * sy, z));
                    var radial = new Vector3(cx / sx, cy / sy, 0f).normalized;
                    normals.Add((radial * Mathf.Sqrt(Mathf.Max(0f, 1f - nz * nz)) + Vector3.forward * nz).normalized);
                    uvs.Add(new Vector2((float)s / sides, v));
                }
            }
            for (int i = 0; i < rings.Count - 1; i++)
            {
                for (int s = 0; s < sides; s++)
                {
                    int a = i * (sides + 1) + s, b = a + sides + 1;
                    tris.AddRange(new[] { a, a + 1, b, a + 1, b + 1, b });
                }
            }
            var mesh = new Mesh { name = "Muscle" };
            mesh.SetVertices(verts);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateTangents(); // the muscle relief is a normal map
            mesh.RecalculateBounds();
            return mesh;
        }

        static float R(Random rng, float min, float max) => min + (float)rng.NextDouble() * (max - min);
    }
}
