using UnityEngine;

namespace PlayerSystem
{
    /// <summary>
    /// A patch of floor with its own step sounds (blood, a puddle): put it on an object with a trigger collider
    /// over that patch. While the player's feet are inside, <see cref="PlayerController"/> steps with these clips
    /// instead of the floor's.
    /// </summary>
    public class FootstepSurface : MonoBehaviour
    {
        public AudioClip[] clips = new AudioClip[0];
    }
}
