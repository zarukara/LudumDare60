using System.Collections;
using System.Collections.Generic;
using BabaikaSystem;
using LevelSystem;
using UnityEngine;

namespace ScareSystem
{
    /// <summary>
    /// The escape (see the main file), in three parts:
    /// - Down: the <see cref="chaseFloor"/> has no cart; as the player comes onto its landing the view turns back up
    ///   the flight they came down and she comes round the middle wall above and down its ceiling. She chases them
    ///   down along the ceilings, upside down, on their own track; the numbers on the landings are anything at all,
    ///   and the stairs go on longer than they are (on the half landing above the 1st floor the player is lifted a
    ///   floor without a seam).
    /// - Up: <see cref="descentLandings"/> landings down, the door of the landing below bursts out of its frame and
    ///   she comes out of the office corridor behind it, on her feet now, and up the stairs; the way down is shut.
    /// - Out: <see cref="climbLandings"/> landings up, on the <see cref="ExitFloor"/>, the door under the exit sign is
    ///   open. Through it, it slams shut in her face; the glass doors at the end of the hall behind it end the run.
    /// A slip going down or up rewinds the tape to where that part began.
    /// </summary>
    public partial class StairsHunt
    {
        enum Escape
        {
            None,
            Down,
            Up,
        }

        const int BurstMark = 100;
        /// <summary>The landing whose door she breaks (a floor of the building, not a number on a wall).</summary>
        const int BurstFloor = 1;

        readonly List<Vector3> track = new List<Vector3>();
        Vector3 lastSample;
        /// <summary>The landing the player last came to (a floor of the building) and how many they have passed.</summary>
        int lastLanding, landings;
        float chaseTime, headHeight, pressure;
        Escape escape;
        // Built on the way: the corridor behind the door she breaks, the wall that keeps the player off the flight
        // down to it, and the hall behind the door out.
        Transform corridor, burstDoor, exitHall;
        GameObject barrier;
        AutoDoor exitDoor;
        Vector3 doorPosition;
        Quaternion doorRotation;
        bool doorSaved;

        /// <summary>The floor with the door out, as many landings up from the burst door as the player has to run.</summary>
        int ExitFloor => BurstFloor + climbLandings;

        // ------------------------------------------------------------------ down

        IEnumerator Descent()
        {
            State = Phase.Cutscene;
            escape = Escape.Down;
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
            yield return ScreamAtPlayer(0.7f);
            for (float t = 0f; t < 0.25f; t += Time.deltaTime)
            {
                view.fieldOfView = Mathf.Lerp(zoomFov, normalFov, ScareView.Smooth(t / 0.25f));
                yield return null;
            }
            view.fieldOfView = normalFov;

            BeginRun(Phase.Chasing, chaseHeadHeight, "hint.run");
            lastLanding = Mathf.CeilToInt(well.Level(player.transform.position) - 0.05f); // the landing at or above the player
            landings = 0;
            ShuffleNumbers(lastLanding);
            while (true)
            {
                if (Time.deltaTime > 0f)
                {
                    LandingsDown();
                    if (OnHalfLandingOverFirst())
                    {
                        if (landings >= descentLandings)
                        {
                            yield return Burst();
                            yield break;
                        }
                        Stretch();
                    }
                    pressure = Mathf.Clamp01((float)landings / descentLandings);
                    if (!ChaseStep(Time.deltaTime)) yield break;
                }
                yield return null;
            }
        }

        /// <summary>The player has come down to the next landing.</summary>
        void LandingsDown()
        {
            int below = lastLanding - 1;
            if (below < 1 || !well.OnLanding(below, player.transform.position)) return;
            lastLanding = below;
            landings++;
            ShuffleNumbers(lastLanding);
        }

        /// <summary>On the half landing between the 2nd floor and the 1st, having come down from the 2nd.</summary>
        bool OnHalfLandingOverFirst()
        {
            var feet = player.transform.position;
            return lastLanding == 2 && well.Level(feet) <= 1.6f && well.transform.InverseTransformPoint(feet).z >= Stairwell.FlightEnd;
        }

        /// <summary>
        /// The player, she and her track go up a floor, and the numbers go up with them, so nothing changes in view:
        /// the next landing down is the 2nd floor again.
        /// </summary>
        void Stretch()
        {
            Vector3 lift = well.transform.TransformVector(0f, Stairwell.FloorHeight, 0f);
            well.ShiftNumbersUp(3);
            MovePlayerBy(lift);
            babaika.Teleport(babaika.transform.position + lift, babaika.transform.rotation);
            for (int i = 0; i < track.Count; i++) track[i] += lift;
            lastSample += lift;
            lastLanding = 3;
            ShuffleNumbers(3);
        }

        // ------------------------------------------------------------------ up

        /// <summary>
        /// The door of the 1st floor's landing bursts out of its frame and she comes out of the corridor behind it and
        /// up the stairs at the player, who now has to run up; the door out on the <see cref="ExitFloor"/> is open now.
        /// Also where the tape comes back to after a slip on the way up: the door is back in its frame and it happens
        /// again.
        /// </summary>
        IEnumerator Burst()
        {
            escape = Escape.Up;
            State = Phase.Fleeing;
            Debug.Log("[StairsHunt] The door bursts: run up");
            if (tape != null) tape.Mark(BurstMark);
            BuildEscape();
            if (exitDoor != null) exitDoor.Unlock();
            well.ShutWaysUp(ExitFloor);
            Lock(false);
            if (hide != null) hide.allowed = false;

            // She is in the corridor, out of sight, and comes for the door.
            var cor = corridor;
            Spawn(cor.TransformPoint(new Vector3(0f, 1.45f, -6.5f)), cor.TransformPoint(new Vector3(0f, 1.45f, 0f)), true);
            if (presence == null) presence = LoopOn(BabaikaChase.SoundChaseLoop, babaika.gameObject, true, 1f);
            babaika.Follow(new[] { cor.TransformPoint(new Vector3(0f, 1.45f, -0.4f)) }, BabaikaGait.Run);
            while (babaika.IsMoving) yield return null;

            StartCoroutine(FlingDoor());
            if (sounds != null) sounds.PlayAt(BabaikaChase.SoundSlam, burstDoor.position + Vector3.up);
            player.Shake(0.8f);
            babaika.Scream(1f);

            BeginRun(Phase.Fleeing, 1.45f, "hint.runUp");
            babaika.bodyUp = Vector3.up;
            // Across the landing and up the flight to the half landing the player stands on.
            float y = Stairwell.FloorY(BurstFloor) + 1.45f;
            foreach (var p in new[]
                     {
                         new Vector3(OfficeRoomBuilder.EntranceX, y, 1.4f), new Vector3(Stairwell.WestFlight, y, 3f),
                         new Vector3(Stairwell.WestFlight, y + 0.2f, 3.9f), new Vector3(Stairwell.WestFlight, y + Stairwell.FlightDrop, 7f),
                         new Vector3(Stairwell.WestFlight + 0.3f, y + Stairwell.FlightDrop, 8.2f),
                     })
                track.Insert(track.Count - 1, well.transform.TransformPoint(p));
            lastLanding = BurstFloor;
            landings = 0;
            yield return Flee();
        }

        /// <summary>A test jump: on the exit floor's landing, the door out open, her coming up the last flight.</summary>
        IEnumerator BackToExit()
        {
            escape = Escape.Up;
            BuildEscape();
            if (exitDoor != null) exitDoor.Unlock();
            well.ShutWaysUp(ExitFloor);
            float y = Stairwell.FloorY(ExitFloor);
            Spawn(well.transform.TransformPoint(new Vector3(Stairwell.EastFlight, y - Stairwell.FlightDrop + 1.45f, 8.2f)),
                well.transform.TransformPoint(new Vector3(Stairwell.EastFlight, y + 1.45f, 4f)), true);
            if (presence == null) presence = LoopOn(BabaikaChase.SoundChaseLoop, babaika.gameObject, true, 1f);
            BeginRun(Phase.Fleeing, 1.45f, "hint.runUp");
            track.Insert(track.Count - 1, well.transform.TransformPoint(new Vector3(Stairwell.EastFlight, y + 1.45f, 3.4f)));
            lastLanding = ExitFloor;
            landings = climbLandings;
            yield return Flee();
        }

        /// <summary>Up the stairs with her behind, until the player is through the door out.</summary>
        IEnumerator Flee()
        {
            while (true)
            {
                if (Time.deltaTime > 0f)
                {
                    LandingsUp();
                    if (PastExitDoor(player.transform.position))
                    {
                        yield return Out();
                        yield break;
                    }
                    pressure = Mathf.Clamp01((float)landings / climbLandings);
                    if (!ChaseStep(Time.deltaTime)) yield break;
                }
                yield return null;
            }
        }

        /// <summary>The player has come up to the next landing.</summary>
        void LandingsUp()
        {
            int above = lastLanding + 1;
            if (!well.OnLanding(above, player.transform.position)) return;
            lastLanding = above;
            landings++;
            ShuffleNumbers(lastLanding);
        }

        // ------------------------------------------------------------------ out

        /// <summary>
        /// Through the door out: it slams shut behind the player and she throws herself at it a few times, then is
        /// quiet. The siren is gone and the morning shows through the glass doors at the end of the hall; walking out
        /// through them ends the run.
        /// </summary>
        IEnumerator Out()
        {
            escape = Escape.None;
            State = Phase.Escaped;
            hintUntil = 0f;
            Debug.Log("[StairsHunt] Through the door out");
            if (exitDoor != null) exitDoor.Slam();
            Vector3 door = ExitDoorPoint(0f);
            if (sounds != null) sounds.PlayAt(BabaikaChase.SoundSlam, door);
            player.Shake(0.5f);
            StopSound(ref siren);
            StopSound(ref music);
            if (ambience != null) ambience.Duck(0.5f);

            // She runs on along the player's way up to the door, and at it.
            var path = new List<Vector3> { babaika.transform.position };
            foreach (var point in track)
            {
                if (PastExitDoor(point)) break;
                path.Add(point);
            }
            Vector3 before = ExitDoorPoint(0.75f);
            path.Add(before);
            float length = PathLength(path);
            for (float s = 0f; s < length; s += chaseMaxSpeed * Time.deltaTime)
            {
                babaika.DriveTo(PointAlong(path, s), BabaikaGait.Run);
                yield return null;
            }
            for (int i = 0; i < 3; i++)
            {
                yield return Drive(ExitDoorPoint(0.4f), 0.12f, true);
                if (exitDoor != null) exitDoor.Knock(1.2f, babaika.transform.position);
                if (sounds != null) sounds.PlayAt(BabaikaChase.SoundBang, door);
                player.Shake(0.35f);
                yield return Drive(before, 0.3f, true);
                yield return Wait(0.3f + 0.25f * i);
            }
            Park();
            StopSound(ref heartbeat);
        }

        /// <summary>A point on the stairwell side of the door out, this far in front of it, at her head's height (world).</summary>
        Vector3 ExitDoorPoint(float before) =>
            well.transform.TransformPoint(new Vector3(OfficeRoomBuilder.EntranceX, Stairwell.FloorY(ExitFloor) + 1.45f, before));

        /// <summary>The point is through the door out, in the hall behind it.</summary>
        bool PastExitDoor(Vector3 world)
        {
            if (exitHall == null) return false;
            var local = exitHall.InverseTransformPoint(world);
            return local.z > 1.2f && local.y > -0.5f && local.y < 3f;
        }

        /// <summary>At the glass doors at the end of the hall: out.</summary>
        bool AtTheWayOut(Vector3 world)
        {
            if (exitHall == null) return false;
            var local = exitHall.InverseTransformPoint(world);
            return local.z > EscapeBuilder.ExitLength - 1.3f && Mathf.Abs(local.x) < 1.6f && local.y > -0.5f && local.y < 3f;
        }

        /// <summary>On the half landing over the 1st floor, come down to it, turned to go on down (world): where the door bursts.</summary>
        Pose BurstLanding() =>
            new Pose(well.transform.TransformPoint(new Vector3(Stairwell.Split, Stairwell.FloorY(BurstFloor) + Stairwell.FlightDrop + 0.05f, 8.3f)),
                well.transform.rotation * Quaternion.Euler(0f, 180f, 0f));

        /// <summary>The top of the flight up to the exit floor's landing, just come up it (world): where a test jump puts the player.</summary>
        Pose ExitLanding() =>
            new Pose(well.transform.TransformPoint(new Vector3(Stairwell.EastFlight - 0.1f, Stairwell.FloorY(ExitFloor) + 0.05f, 3.3f)),
                well.transform.rotation * Quaternion.Euler(0f, 180f, 0f));

        // ------------------------------------------------------------------ building the end

        /// <summary>
        /// The corridor, the wall across the flight down to it, the hall behind the door out and the sign over that
        /// door, once; the burst door back in its frame and the door out shut again.
        /// </summary>
        void BuildEscape()
        {
            var root = well.transform;
            var p = well.palette;
            const float back = -OfficeRoomBuilder.Wall;
            if (corridor == null)
                corridor = EscapeBuilder.Corridor(root, p, new Vector3(OfficeRoomBuilder.EntranceX, Stairwell.FloorY(BurstFloor), back), 0f);
            if (barrier == null)
                barrier = EscapeBuilder.Barrier(root, "No_Way_Down", new Vector3(Stairwell.WestFlight, Stairwell.FloorY(BurstFloor + 1) - 1.35f, 5.8f),
                    new Vector3(2.3f, 3.3f, 0.1f));
            if (exitHall == null)
            {
                // The hall's open side is the stairwell's south wall, the door in it at the entrance's place.
                exitHall = EscapeBuilder.Exit(root, p, new Vector3(Stairwell.Split, Stairwell.FloorY(ExitFloor), back), 180f);
                EscapeBuilder.ExitSign(root, p, new Vector3(OfficeRoomBuilder.EntranceX, Stairwell.FloorY(ExitFloor) + OfficeRoomBuilder.DoorHeight + 0.3f, 0f), 0f);
                var frame = well.FloorDoor(ExitFloor);
                exitDoor = frame != null ? frame.GetComponentInChildren<AutoDoor>(true) : null;
            }
            if (exitDoor != null) exitDoor.locked = true; // until she bursts out below

            var burstFrame = well.FloorDoor(BurstFloor);
            burstDoor = burstFrame != null ? burstFrame.Find("Hinge") : null;
            if (burstDoor == null) return;
            if (!doorSaved)
            {
                doorPosition = burstDoor.localPosition;
                doorRotation = burstDoor.localRotation;
                doorSaved = true;
            }
            burstDoor.localPosition = doorPosition;
            burstDoor.localRotation = doorRotation;
            var hinge = burstDoor.GetComponent<AutoDoor>();
            if (hinge != null) hinge.enabled = true;
        }

        /// <summary>The broken door flies across the landing, turning over, and lands flat.</summary>
        IEnumerator FlingDoor()
        {
            if (burstDoor == null) yield break;
            var hinge = burstDoor.GetComponent<AutoDoor>();
            if (hinge != null) hinge.enabled = false;
            foreach (var c in burstDoor.GetComponentsInChildren<Collider>()) c.enabled = false;
            Vector3 from = burstDoor.position;
            Quaternion turnFrom = burstDoor.rotation;
            Vector3 to = well.transform.TransformPoint(new Vector3(OfficeRoomBuilder.EntranceX - 0.4f, Stairwell.FloorY(BurstFloor) + 0.04f, 2.9f));
            Quaternion turnTo = well.transform.rotation * Quaternion.Euler(-90f, 160f, 0f);
            for (float t = 0f; t < 0.6f; t += Time.deltaTime)
            {
                float u = t / 0.6f;
                burstDoor.SetPositionAndRotation(Vector3.Lerp(from, to, u) + Vector3.up * (1.1f * Mathf.Sin(u * Mathf.PI)), Quaternion.Slerp(turnFrom, turnTo, u));
                yield return null;
            }
            burstDoor.SetPositionAndRotation(to, turnTo);
            if (sounds != null) sounds.PlayAt(BabaikaChase.SoundBang, to);
        }

        // ------------------------------------------------------------------ running

        /// <summary>Control back, the run's sounds and hint, her track starting where she is.</summary>
        void BeginRun(Phase phase, float height, string hint)
        {
            State = phase;
            Breath = null;
            Lock(false);
            if (hide != null)
            {
                hide.Release();
                hide.allowed = false;
            }
            if (normalFov <= 0f) normalFov = view.fieldOfView;
            if (heartbeat == null) heartbeat = LoopOn(BabaikaChase.SoundHeartbeat, gameObject, false, 0.5f);
            if (music == null) music = LoopOn(BabaikaChase.SoundMusic, gameObject, false, 0.6f);
            Hint(hint, 3f);
            babaika.StopLooking();
            headHeight = height;
            track.Clear();
            track.Add(babaika.transform.position);
            lastSample = ChasePoint();
            track.Add(lastSample);
            chaseTime = 0f;
        }

        /// <summary>Where her head follows the player: high over their feet, near the ceiling, or at head height.</summary>
        Vector3 ChasePoint() => player.transform.position + Vector3.up * headHeight;

        /// <summary>One frame of a chase. False when she has the player (the screamer has begun).</summary>
        bool ChaseStep(float dt)
        {
            chaseTime += dt;
            Vector3 target = ChasePoint();
            if ((target - lastSample).sqrMagnitude > 0.0625f)
            {
                track.Add(target);
                lastSample = target;
            }

            // Like the chase in the offices: she keeps a gap that shrinks as it goes on.
            Vector3 position = babaika.transform.position;
            float gap = TrackLength(position, target);
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
            if (player.Speed < 3.2f && chaseTime > 1.5f && gap < 3f) Hint(State == Phase.Fleeing ? "hint.runUp" : "hint.run", 1f);
            return true;
        }

        static float PathLength(List<Vector3> path)
        {
            float length = 0f;
            for (int i = 1; i < path.Count; i++) length += Vector3.Distance(path[i - 1], path[i]);
            return length;
        }

        /// <summary>The point this far along the path (its end past that).</summary>
        static Vector3 PointAlong(List<Vector3> path, float distance)
        {
            for (int i = 1; i < path.Count; i++)
            {
                float step = Vector3.Distance(path[i - 1], path[i]);
                if (distance <= step) return step > 0.0001f ? Vector3.Lerp(path[i - 1], path[i], distance / step) : path[i];
                distance -= step;
            }
            return path[path.Count - 1];
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

        /// <summary>Every landing but the one in view shows a number at random: the floors are all mixed up.</summary>
        void ShuffleNumbers(int keep)
        {
            for (int floor = 1; floor <= Stairwell.Top; floor++)
                if (floor != keep) well.SetNumber(floor, Random.Range(1, 10).ToString());
        }

        // ------------------------------------------------------------------ helpers

        /// <summary>She turns her face on the player and screams; the view is held on her.</summary>
        IEnumerator ScreamAtPlayer(float seconds)
        {
            babaika.LookAt(view.transform.position);
            var scream = sounds != null ? sounds.PlayAt(BabaikaChase.SoundScream, babaika.transform.position) : null;
            babaika.Scream(scream != null ? Mathf.Clamp(scream.length * 0.9f, 1f, 2.5f) : 1f);
            player.Shake(0.6f);
            for (float t = 0f; t < seconds; t += Time.deltaTime)
            {
                ScareView.TurnTowards(player, view, babaika.transform.position, 8f);
                yield return null;
            }
        }

        /// <summary>Steers her head to a point in the given time, smoothly.</summary>
        IEnumerator Drive(Vector3 to, float seconds, bool running)
        {
            Vector3 from = babaika.transform.position;
            for (float t = 0f; t < seconds; t += Time.deltaTime)
            {
                babaika.DriveTo(Vector3.Lerp(from, to, ScareView.Smooth(t / seconds)), running ? BabaikaGait.Run : BabaikaGait.Walk);
                yield return null;
            }
            babaika.DriveTo(to, running ? BabaikaGait.Run : BabaikaGait.Walk);
        }

        static IEnumerator Wait(float seconds)
        {
            for (float t = 0f; t < seconds; t += Time.deltaTime) yield return null;
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
