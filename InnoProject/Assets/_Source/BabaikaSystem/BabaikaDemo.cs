using System.Collections;
using UnityEngine;

namespace BabaikaSystem
{
    /// <summary>
    /// Test-scene driver: stands, walks the route, stands, runs the route, and over again.
    /// Keys 1 / 2 / 3: stand, walk, run on demand.
    /// </summary>
    public class BabaikaDemo : MonoBehaviour
    {
        public Babaika babaika;
        public BabaikaRoute route;
        public float pause = 2.5f;

        IEnumerator Start()
        {
            while (true)
            {
                yield return new WaitForSeconds(pause);
                yield return Go(BabaikaGait.Walk);
                yield return new WaitForSeconds(pause);
                yield return Go(BabaikaGait.Run);
            }
        }

        IEnumerator Go(BabaikaGait gait)
        {
            babaika.Follow(route, gait);
            while (babaika.IsMoving) yield return null;
        }

        void Update()
        {
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard == null) return;
            if (keyboard.digit1Key.wasPressedThisFrame)
            {
                StopAllCoroutines();
                babaika.Stand();
            }
            else if (keyboard.digit2Key.wasPressedThisFrame || keyboard.digit3Key.wasPressedThisFrame)
            {
                StopAllCoroutines();
                babaika.Follow(route, keyboard.digit3Key.wasPressedThisFrame ? BabaikaGait.Run : BabaikaGait.Walk);
            }
        }
    }
}
