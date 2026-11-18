using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Mudfront.Tests
{
    /// <summary>Step 1 "movement & camera feel": acceleration, sprint rules, crouch + headroom, aim, jump grace, camera modes.</summary>
    public class MovementFeelTests
    {
        GameObject _world;

        [SetUp]
        public void SetUp()
        {
            _world = new GameObject("MoveWorld");
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Ground";
            ground.transform.SetParent(_world.transform);
            ground.transform.localScale = new Vector3(200, 1, 200);
            ground.transform.position = new Vector3(0, -0.5f, 0);
        }

        [TearDown]
        public void TearDown() => Object.Destroy(_world);

        ThirdPersonController Player(Vector3 pos)
        {
            var go = new GameObject("Player");
            go.transform.SetParent(_world.transform);
            go.transform.position = pos;
            var cc = go.AddComponent<CharacterController>();
            cc.height = 1.8f; cc.radius = 0.4f; cc.center = new Vector3(0, 0.9f, 0);
            var p = go.AddComponent<ThirdPersonController>();
            p.UseLocalInput = false;
            p.Yaw = 0;
            return p;
        }

        static IEnumerator Settle() { yield return new WaitForSeconds(0.25f); }

        [UnityTest]
        public IEnumerator Accelerates_ThenReachesRunSpeed()
        {
            var p = Player(new Vector3(0, 0.1f, 0));
            yield return Settle();
            p.MoveInput = Vector2.up;
            yield return null; yield return null;
            Assert.Less(p.Speed, p.RunSpeed, "does not jump to full speed in one frame");
            yield return new WaitForSeconds(0.4f);
            Assert.AreEqual(p.RunSpeed, p.Speed, 0.05f, "reaches run speed");
        }

        [UnityTest]
        public IEnumerator Decelerates_QuicklyWhenReleased()
        {
            var p = Player(new Vector3(0, 0.1f, 0));
            yield return Settle();
            p.MoveInput = Vector2.up;
            yield return new WaitForSeconds(0.5f);
            p.MoveInput = Vector2.zero;
            yield return new WaitForSeconds(0.15f);
            Assert.Less(p.Speed, 0.05f, "stops within ~0.1 s");
        }

        [UnityTest]
        public IEnumerator Sprint_OnlyForward_NotWhileAimingOrCrouching()
        {
            var p = Player(new Vector3(0, 0.1f, 0));
            yield return Settle();
            p.SprintInput = true; p.MoveInput = Vector2.up;
            yield return new WaitForSeconds(0.5f);
            Assert.AreEqual(p.SprintSpeed, p.Speed, 0.05f, "sprints forward");
            p.MoveInput = Vector2.right;
            yield return new WaitForSeconds(0.5f);
            Assert.AreEqual(p.RunSpeed, p.Speed, 0.05f, "no sideways sprint");
            p.MoveInput = Vector2.up; p.AimInput = true;
            yield return new WaitForSeconds(0.5f);
            Assert.IsFalse(p.IsSprinting);
            Assert.AreEqual(p.RunSpeed * p.AimSpeedMultiplier, p.Speed, 0.05f, "aiming walks slower");
        }

        [UnityTest]
        public IEnumerator Crouch_LowersCapsuleAndSpeed()
        {
            var p = Player(new Vector3(0, 0.1f, 0));
            var cc = p.GetComponent<CharacterController>();
            yield return Settle();
            p.CrouchInput = true; p.MoveInput = Vector2.up;
            yield return new WaitForSeconds(0.5f);
            Assert.IsTrue(p.IsCrouching);
            Assert.AreEqual(p.CrouchHeight, cc.height, 0.01f);
            Assert.AreEqual(p.CrouchSpeed, p.Speed, 0.05f);
            p.CrouchInput = false;
            yield return new WaitForSeconds(0.3f);
            Assert.IsFalse(p.IsCrouching);
            Assert.AreEqual(p.StandHeight, cc.height, 0.01f, "stands back up in the open");
        }

        [UnityTest]
        public IEnumerator Crouch_CannotStandUpUnderLowCeiling()
        {
            var p = Player(new Vector3(0, 0.1f, 0));
            yield return Settle();
            p.CrouchInput = true;
            yield return new WaitForSeconds(0.3f);
            var ceiling = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ceiling.transform.SetParent(_world.transform);
            ceiling.transform.position = new Vector3(0, 1.5f, 0); ceiling.transform.localScale = new Vector3(4, 0.2f, 4); // underside at 1.4 m
            yield return new WaitForFixedUpdate();
            p.CrouchInput = false;
            yield return new WaitForSeconds(0.3f);
            Assert.IsTrue(p.IsCrouching, "stays crouched with 1.4 m headroom");
            Object.Destroy(ceiling);
            yield return new WaitForSeconds(0.3f);
            Assert.IsFalse(p.IsCrouching, "stands once the ceiling is gone");
        }

        [UnityTest]
        public IEnumerator Jump_RisesAboutJumpHeight()
        {
            var p = Player(new Vector3(0, 0.1f, 0));
            yield return Settle();
            float y0 = p.transform.position.y, peak = y0;
            p.JumpInput = true;
            float t = 0;
            while (t < 0.7f) { t += Time.deltaTime; peak = Mathf.Max(peak, p.transform.position.y); yield return null; }
            Assert.AreEqual(p.JumpHeight, peak - y0, 0.15f, "jump apex");
        }

        [UnityTest]
        public IEnumerator Jump_CoyoteTime_AllowsJumpJustAfterLedge()
        {
            // platform edge at z = 0, player runs off and presses jump 0.06 s later
            var plat = GameObject.CreatePrimitive(PrimitiveType.Cube);
            plat.transform.SetParent(_world.transform);
            plat.transform.position = new Vector3(0, 1.5f, -5); plat.transform.localScale = new Vector3(4, 1, 10);
            var p = Player(new Vector3(0, 2.1f, -1.5f));
            yield return Settle();
            p.MoveInput = Vector2.up;
            while (p.IsGrounded) yield return null;     // just left the ledge
            yield return new WaitForSeconds(0.06f);
            float y = p.transform.position.y;
            p.JumpInput = true;
            yield return new WaitForSeconds(0.15f);
            Assert.Greater(p.transform.position.y, y + 0.2f, "coyote jump fired");
        }

        [UnityTest]
        public IEnumerator Jump_BufferedPressBeforeLanding_Fires()
        {
            var p = Player(new Vector3(0, 0.6f, 0)); // drop 0.5 m
            yield return null;
            while (!p.IsGrounded)
            {
                if (p.transform.position.y < 0.12f) p.JumpInput = true; // pressed a few cm before touching down
                yield return null;
            }
            float y = p.transform.position.y;
            yield return new WaitForSeconds(0.2f);
            Assert.Greater(p.transform.position.y, y + 0.3f, "buffered jump fired on landing");
        }

        [UnityTest]
        public IEnumerator Camera_AimZoomsIn_FirstPersonHidesModel()
        {
            var p = Player(new Vector3(0, 0.1f, 0));
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            Object.Destroy(body.GetComponent<Collider>());
            body.transform.SetParent(p.transform, false); body.transform.localPosition = Vector3.up;
            var camGo = new GameObject("Cam"); camGo.transform.SetParent(_world.transform);
            var cam = camGo.AddComponent<Camera>();
            var tpc = camGo.AddComponent<ThirdPersonCamera>(); tpc.Target = p;
            yield return Settle();
            Assert.AreEqual(tpc.Fov, cam.fieldOfView, 0.1f);
            float d3 = Vector3.Distance(cam.transform.position, p.transform.position);
            p.AimInput = true;
            yield return new WaitForSeconds(0.4f);
            Assert.AreEqual(tpc.AimFov, cam.fieldOfView, 0.1f, "aim FOV");
            Assert.Less(Vector3.Distance(cam.transform.position, p.transform.position), d3, "camera moves closer when aiming");
            p.AimInput = false; p.FirstPerson = true;
            yield return null; yield return null;
            Assert.AreEqual(UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly, body.GetComponent<Renderer>().shadowCastingMode, "own model hidden in 1st person");
            Assert.AreEqual(tpc.EyeHeight, cam.transform.position.y - p.transform.position.y, 0.1f, "camera at eye height");
            p.FirstPerson = false;
            yield return null;
            Assert.AreEqual(UnityEngine.Rendering.ShadowCastingMode.On, body.GetComponent<Renderer>().shadowCastingMode);
        }

        [UnityTest]
        public IEnumerator Camera_PullsInFrontOfWall()
        {
            var p = Player(new Vector3(0, 0.1f, 0));
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.transform.SetParent(_world.transform);
            wall.transform.position = new Vector3(0, 1.5f, -2.5f); wall.transform.localScale = new Vector3(6, 3, 0.3f);
            var camGo = new GameObject("Cam"); camGo.transform.SetParent(_world.transform);
            camGo.AddComponent<Camera>();
            camGo.AddComponent<ThirdPersonCamera>().Target = p;
            p.Pitch = 0;
            yield return Settle();
            Assert.Greater(camGo.transform.position.z, -2.35f, "camera stays in front of the wall (no clipping)");
        }
    }
}
