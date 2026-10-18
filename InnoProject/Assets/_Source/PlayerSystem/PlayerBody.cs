using UnityEngine;

namespace PlayerSystem
{
    /// <summary>
    /// The player's own body, seen when looking down. The legs walk in step with the <see cref="PlayerController"/>
    /// (one step per stepLength), the arms swing against them, and the body's right arm hides while the torch hand
    /// (<see cref="Flashlight.viewmodel"/>) is up. Joints are found by name: Pelvis, Leg_L/R (_Knee, _Ankle),
    /// Arm_L/R (_Elbow). Lives under the player root, a little behind the camera, so looking down shows the
    /// shirt, the tie and the shoes instead of the inside of the chest. Hidden while crouching behind cover, where
    /// the eyes would be inside it.
    /// </summary>
    public class PlayerBody : MonoBehaviour
    {
        public PlayerController controller;
        public Flashlight flashlight;
        [Tooltip("Thigh swing at walking pace, degrees.")]
        public float legSwing = 26f;
        public float kneeBend = 50f;
        public float armSwing = 16f;
        public float elbowBend = 12f;

        Transform pelvis, legL, legR, kneeL, kneeR, ankleL, ankleR, armL, armR, elbowL, elbowR;
        Quaternion pelvisRest, legLRest, legRRest, kneeLRest, kneeRRest, ankleLRest, ankleRRest, armLRest, armRRest, elbowLRest, elbowRRest;
        Renderer[] rightArm = new Renderer[0], all = new Renderer[0];

        void Awake()
        {
            if (controller == null) controller = GetComponentInParent<PlayerController>();
            if (flashlight == null && controller != null) flashlight = controller.GetComponentInChildren<Flashlight>();
            pelvis = Find("Pelvis", out pelvisRest);
            legL = Find("Leg_L", out legLRest);
            legR = Find("Leg_R", out legRRest);
            kneeL = Find("Leg_L_Knee", out kneeLRest);
            kneeR = Find("Leg_R_Knee", out kneeRRest);
            ankleL = Find("Leg_L_Ankle", out ankleLRest);
            ankleR = Find("Leg_R_Ankle", out ankleRRest);
            armL = Find("Arm_L", out armLRest);
            armR = Find("Arm_R", out armRRest);
            elbowL = Find("Arm_L_Elbow", out elbowLRest);
            elbowR = Find("Arm_R_Elbow", out elbowRRest);
            if (armR != null) rightArm = armR.GetComponentsInChildren<Renderer>(true);
            all = GetComponentsInChildren<Renderer>(true);
        }

        Transform Find(string jointName, out Quaternion rest)
        {
            foreach (var t in GetComponentsInChildren<Transform>(true))
            {
                if (t.name != jointName) continue;
                rest = t.localRotation;
                return t;
            }
            rest = Quaternion.identity;
            return null;
        }

        void LateUpdate()
        {
            float pace = 0f, run = 0f, phase = 0f;
            if (controller != null)
            {
                pace = controller.WalkWeight;
                run = Mathf.InverseLerp(controller.walkSpeed, controller.runSpeed, controller.Speed);
                phase = controller.Travelled / Mathf.Max(controller.stepLength, 0.01f) * Mathf.PI;
            }
            float amount = pace * (1f + 0.45f * run);
            float s = Mathf.Sin(phase), c = Mathf.Cos(phase);

            // Negative X swings a hanging limb forward, positive back.
            float thighL = -legSwing * amount * s, thighR = legSwing * amount * s;
            float bendL = kneeBend * amount * Mathf.Max(0f, c), bendR = kneeBend * amount * Mathf.Max(0f, -c);
            Pose(legL, legLRest, thighL);
            Pose(legR, legRRest, thighR);
            Pose(kneeL, kneeLRest, bendL);
            Pose(kneeR, kneeRRest, bendR);
            Pose(ankleL, ankleLRest, -(thighL + bendL) * 0.8f);
            Pose(ankleR, ankleRRest, -(thighR + bendR) * 0.8f);
            Pose(armL, armLRest, armSwing * amount * s);
            Pose(armR, armRRest, -armSwing * amount * s);
            Pose(elbowL, elbowLRest, -elbowBend * (0.6f + 0.6f * amount));
            Pose(elbowR, elbowRRest, -elbowBend * (0.6f + 0.6f * amount));
            if (pelvis != null) pelvis.localRotation = pelvisRest * Quaternion.Euler(0f, 4f * amount * s, 0f);

            bool handUp = flashlight != null && flashlight.viewmodel != null && flashlight.Raise > 0.3f;
            bool crouched = controller != null && controller.headDrop > 0.3f;
            foreach (var r in all) r.enabled = !crouched;
            if (!crouched)
                foreach (var r in rightArm) r.enabled = !handUp;
        }

        static void Pose(Transform joint, Quaternion rest, float angle)
        {
            if (joint != null) joint.localRotation = rest * Quaternion.Euler(angle, 0f, 0f);
        }
    }
}
