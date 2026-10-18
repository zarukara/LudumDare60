using System.Collections;
using System.Collections.Generic;
using BabaikaSystem;
using LevelSystem;
using UnityEngine;

namespace ScareSystem
{
    /// <summary>
    /// The chase down the stairs (see the main file). The <see cref="chaseFloor"/> has no cart: as the player comes onto
    /// its landing the view turns back up the flight they came down, and she rounds the middle wall above and runs
    /// down its ceiling at them and screams. Then she chases them along the ceilings, upside down, on their own track;
    /// she keeps a few metres back at first and closes in on the way down, so only running keeps her off. The stairs
    /// go on longer than they should: the landings show <see cref="chaseLandings"/> one after another, and while more
    /// than one is still to come the player is lifted a floor without a seam on the half landing above the 1st, so
    /// the 1st floor and the way out under it come only at the very end.
    /// </summary>
    public partial class StairsHunt
    {
        readonly List<Vector3> track = new List<Vector3>();
        Vector3 lastSample;
        /// <summary>The landing the player last came to (a real floor) and how many of chaseLandings they have passed.</summary>
        int lastLanding, passedLandings;
        float chaseTime;

        IEnumerator Descent()
        {
            State = Phase.Cutscene;
            cutsceneDone = true;
            RaiseAlarm();
            Debug.Log("[StairsHunt] She comes down at the player: the chase");
            Lock(true);
            if (hide != null) hide.allowed = false;
            if (normalFov <= 0f) normalFov = view.fieldOfView;
            if (ambience != null) ambience.Duck(0.25f);

            // She waits round the middle wall at the half landing above, out of sight; the player turns back up
            // the flight they came down, and she comes round and runs down its ceiling.
            var route = well.Route(chaseFloor, out _);
            int last = route.Length - 1;
            Spawn(route[last], route[last - 1]);
            presence = LoopOn(BabaikaChase.SoundChaseLoop, babaika.gameObject, true, 1f);
            yield return Glide(new Pose(player.transform.position, well.transform.rotation), -22f, 0.45f);
            babaika.Follow(new[] { route[last - 1], route[last - 2], Vector3.Lerp(route[last - 2], route[last - 3], 0.5f) }, BabaikaGait.Run);
            for (float t = 0f; babaika.IsMoving || t < 0.6f; t += Time.deltaTime)
            {
                ScareView.TurnTowards(player, view, babaika.transform.position, 7f);
                view.fieldOfView = Mathf.Lerp(normalFov, zoomFov, ScareView.Smooth(t / 0.6f));
                yield return null;
            }

            babaika.LookAt(view.transform.position);
            var scream = sounds != null ? sounds.PlayAt(BabaikaChase.SoundScream, babaika.transform.position) : null;
            babaika.Scream(scream != null ? Mathf.Clamp(scream.length * 0.9f, 1f, 2.5f) : 1f);
            player.Shake(0.6f);
            for (float t = 0f; t < 0.7f; t += Time.deltaTime)
            {
                ScareView.TurnTowards(player, view, babaika.transform.position, 8f);
                yield return null;
            }
            for (float t = 0f; t < 0.25f; t += Time.deltaTime)
            {
                view.fieldOfView = Mathf.Lerp(zoomFov, normalFov, ScareView.Smooth(t / 0.25f));
                yield return null;
            }
            view.fieldOfView = normalFov;
            yield return Chase();
        }

        IEnumerator Chase()
        {
            State = Phase.Chasing;
            Breath = null;
            Lock(false);
            if (heartbeat == null) heartbeat = LoopOn(BabaikaChase.SoundHeartbeat, gameObject, false, 0.5f);
            if (presence == null) presence = LoopOn(BabaikaChase.SoundChaseLoop, babaika.gameObject, true, 1f);
            music = LoopOn(BabaikaChase.SoundMusic, gameObject, false, 0.6f);
            Hint("hint.run", 3f);
            babaika.StopLooking();

            lastLanding = Mathf.CeilToInt(well.Level(player.transform.position) - 0.05f); // the landing at or above the player
            passedLandings = 0;
            ShowComingNumber();
            track.Clear();
            track.Add(babaika.transform.position);
            lastSample = ChasePoint();
            track.Add(lastSample);
            chaseTime = 0f;

            while (true)
            {
                if (Time.deltaTime > 0f && !ChaseStep(Time.deltaTime)) yield break;
                yield return null;
            }
        }

        /// <summary>Where her head follows the player: high over their feet, near the ceiling.</summary>
        Vector3 ChasePoint() => player.transform.position + Vector3.up * chaseHeadHeight;

        /// <summary>One frame of the chase. False when she has the player (the screamer has begun).</summary>
        bool ChaseStep(float dt)
        {
            chaseTime += dt;
            Landings();
            Stretch();

            Vector3 target = ChasePoint();
            if ((target - lastSample).sqrMagnitude > 0.0625f)
            {
                track.Add(target);
                lastSample = target;
            }

            // Like the chase in the offices: she keeps a gap that shrinks on the way down.
            Vector3 position = babaika.transform.position;
            float gap = TrackLength(position, target);
            float pressure = Mathf.Clamp01((float)passedLandings / Mathf.Max(1, chaseLandings.Length));
            float gapGoal = Mathf.Lerp(chaseGap.x, chaseGap.y, pressure);
            float slowest = Mathf.Lerp(chaseMinSpeed.x, chaseMinSpeed.y, pressure);
            float speed = Mathf.Clamp(player.Speed + 2.2f * (gap - gapGoal), slowest, chaseMaxSpeed);
            if (chaseTime < 1.5f) speed = Mathf.Min(speed, 1f + 1.2f * chaseTime); // a moment to turn round and run
            float step = speed * dt;
            while (step > 0f && track.Count > 0)
            {
                Vector3 to = track[0] - position;
                float distance = to.magnitude;
                if (distance <= step)
                {
                    position = track[0];
                    step -= distance;
                    track.RemoveAt(0);
                }
                else
                {
                    position += to / distance * step;
                    step = 0f;
                }
            }
            babaika.DriveTo(position, BabaikaGait.Run);

            gap = TrackLength(position, target);
            if (gap < chaseCatchGap) return Caught();

            float near = Mathf.InverseLerp(7f, chaseGap.y * 0.8f, gap);
            if (heartbeat != null)
            {
                heartbeat.volume = Mathf.Lerp(0.3f, 0.95f, near);
                heartbeat.pitch = Mathf.Lerp(1f, 1.5f, near);
            }
            if (near > 0.6f) player.Shake(0.25f * near);
            if (player.Speed < 3.2f && chaseTime > 1.5f && gap < 3f) Hint("hint.run", 1f);
            return true;
        }

        float TrackLength(Vector3 from, Vector3 target)
        {
            float length = 0f;
            Vector3 previous = from;
            foreach (var point in track)
            {
                length += Vector3.Distance(previous, point);
                previous = point;
            }
            return length + Vector3.Distance(previous, target);
        }

        /// <summary>The player has come down to the next landing: that is the next of the chase's numbers.</summary>
        void Landings()
        {
            int below = lastLanding - 1;
            if (below < 1 || !well.OnLanding(below, player.transform.position)) return;
            lastLanding = below;
            if (passedLandings < chaseLandings.Length) Floor = chaseLandings[passedLandings];
            passedLandings++;
            ShowComingNumber();
        }

        /// <summary>Every landing below the player shows the number of the next one they will reach.</summary>
        void ShowComingNumber()
        {
            if (passedLandings >= chaseLandings.Length) return;
            string next = chaseLandings[passedLandings].ToString();
            for (int floor = 1; floor < lastLanding; floor++) well.SetNumber(floor, next);
        }

        /// <summary>
        /// On the half landing above the 1st floor, unless the landing to come is the last one, the player, she and
        /// her track go up a floor, and the numbers go up with them, so nothing changes in view.
        /// </summary>
        void Stretch()
        {
            if (lastLanding != 2 || passedLandings >= chaseLandings.Length - 1) return;
            var feet = player.transform.position;
            if (well.Level(feet) > 1.6f || well.transform.InverseTransformPoint(feet).z < Stairwell.FlightEnd) return;

            Vector3 lift = well.transform.TransformVector(0f, Stairwell.FloorHeight, 0f);
            well.ShiftNumbersUp(3);
            MovePlayerBy(lift);
            babaika.Teleport(babaika.transform.position + lift, babaika.transform.rotation);
            for (int i = 0; i < track.Count; i++) track[i] += lift;
            lastSample += lift;
            lastLanding = 3;
            ShowComingNumber();
        }

        void MovePlayerBy(Vector3 offset)
        {
            var body = player.GetComponent<CharacterController>();
            if (body != null) body.enabled = false;
            player.transform.position += offset;
            if (body != null) body.enabled = true;
        }
    }
}
