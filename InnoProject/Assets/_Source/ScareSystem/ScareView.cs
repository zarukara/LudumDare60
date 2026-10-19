using PlayerSystem;
using UnityEngine;

namespace ScareSystem
{

    public static class ScareView
    {

        public static void TurnTowards(PlayerController player, Camera view, Vector3 target, float sharpness)
        {
            float k = 1f - Mathf.Exp(-sharpness * Time.deltaTime);
            Vector3 to = target - view.transform.position;
            Vector3 flat = Vector3.ProjectOnPlane(to, Vector3.up);
            if (flat.sqrMagnitude > 1e-4f)
                player.transform.rotation = Quaternion.Slerp(player.transform.rotation, Quaternion.LookRotation(flat, Vector3.up), k);
            player.Pitch = Mathf.Lerp(player.Pitch, -Mathf.Atan2(to.y, flat.magnitude) * Mathf.Rad2Deg, k);
        }

        public static float Smooth(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }
    }
}
