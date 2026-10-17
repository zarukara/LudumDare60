using UnityEngine;
using UnityEngine.Events;

namespace BabaikaSystem
{
    /// <summary>
    /// A scripted moment: when the player walks into this trigger, the Babaika appears at the start of the route,
    /// runs along it and, if asked, vanishes at the end. <see cref="Fire"/> starts it from code or a UnityEvent too.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class BabaikaTrigger : MonoBehaviour
    {
        public Babaika babaika;
        public BabaikaRoute route;
        public BabaikaGait gait = BabaikaGait.Run;
        [Tooltip("Show the Babaika at the start of the route first.")]
        public bool teleportToStart = true;
        [Tooltip("Hide the Babaika when it reaches the end of the route.")]
        public bool hideAtEnd = true;
        [Tooltip("Fire only the first time the player comes in.")]
        public bool once = true;
        public UnityEvent onFired;
        public UnityEvent onFinished;

        bool fired;

        void Reset() => GetComponent<Collider>().isTrigger = true;

        void OnTriggerEnter(Collider other)
        {
            if (once && fired) return;
            if (other.GetComponentInParent<CharacterController>() == null) return; // only the player walks with one
            Fire();
        }

        public void Fire()
        {
            if (babaika == null || route == null || route.Count == 0) return;
            fired = true;
            babaika.gameObject.SetActive(true);
            if (teleportToStart) babaika.Teleport(route[0], route.StartRotation());
            babaika.Arrived -= Finish;
            babaika.Arrived += Finish;
            babaika.Follow(route, gait);
            onFired.Invoke();
        }

        void Finish()
        {
            babaika.Arrived -= Finish;
            if (hideAtEnd) babaika.gameObject.SetActive(false);
            onFinished.Invoke();
        }
    }
}
