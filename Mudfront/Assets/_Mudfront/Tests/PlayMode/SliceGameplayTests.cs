using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Mudfront.Tests
{
    /// <summary>Local (single-process) gameplay checks: capture, contest, scoring, shooting, respawn, vehicle state, door clearance.</summary>
    public class SliceGameplayTests
    {
        GameObject _world;

        [SetUp]
        public void SetUp()
        {
            _world = new GameObject("TestWorld");
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Ground";
            ground.transform.SetParent(_world.transform);
            ground.transform.localScale = new Vector3(200, 1, 200);
            ground.transform.position = new Vector3(0, -0.5f, 0);
            new GameObject("MatchManager").AddComponent<MatchManager>().transform.SetParent(_world.transform);
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(_world);
        }

        GameObject Trooper(int team, Vector3 pos, bool controller = true)
        {
            var go = new GameObject("Trooper_T" + team);
            go.transform.SetParent(_world.transform);
            go.transform.position = pos;
            var cc = go.AddComponent<CharacterController>();
            cc.height = 1.8f; cc.radius = 0.4f; cc.center = new Vector3(0, 0.9f, 0);
            go.AddComponent<TeamMember>().TeamId = team;
            go.AddComponent<Health>();
            go.AddComponent<Respawner>().RespawnDelay = 0.3f;
            if (controller) go.AddComponent<ThirdPersonController>().UseLocalInput = false;
            return go;
        }

        SpawnPoint Spawn(int team, Vector3 pos)
        {
            var sp = new GameObject("Spawn").AddComponent<SpawnPoint>();
            sp.transform.SetParent(_world.transform);
            sp.transform.position = pos; sp.TeamId = team;
            return sp;
        }

        CaptureZone Zone(float captureTime = 0.5f)
        {
            var z = new GameObject("Zone").AddComponent<CaptureZone>();
            z.transform.SetParent(_world.transform);
            z.Radius = 8; z.CaptureTime = captureTime; z.PointsPerSecond = 10;
            return z;
        }

        [UnityTest]
        public IEnumerator Capture_SingleTeamTakesZone_AndScores()
        {
            var zone = Zone();
            Trooper(0, new Vector3(1, 0.1f, 1));
            yield return new WaitForSeconds(0.8f);
            Assert.AreEqual(0, zone.Owner, "team 0 should own the zone");
            float before = MatchManager.Instance.Scores[0];
            yield return new WaitForSeconds(0.5f);
            Assert.Greater(MatchManager.Instance.Scores[0], before, "owner should gain points");
        }

        [UnityTest]
        public IEnumerator Capture_ContestedZone_DoesNotChangeOwner()
        {
            var zone = Zone();
            Trooper(0, new Vector3(1, 0.1f, 1));
            Trooper(1, new Vector3(-1, 0.1f, -1));
            yield return new WaitForSeconds(1.0f);
            Assert.IsTrue(zone.Contested);
            Assert.AreEqual(-1, zone.Owner);
        }

        [UnityTest]
        public IEnumerator Capture_OutsideRadius_DoesNotCount()
        {
            var zone = Zone();
            Trooper(0, new Vector3(9, 0.1f, 0));
            yield return new WaitForSeconds(0.8f);
            Assert.AreEqual(-1, zone.Owner);
        }

        [UnityTest]
        public IEnumerator Shooting_DamagesEnemy_NotTeammate()
        {
            var shooterGo = Trooper(0, new Vector3(0, 0.1f, -5));
            var sh = shooterGo.AddComponent<Shooter>();
            sh.UseLocalInput = false;
            var enemy = Trooper(1, new Vector3(0, 0.1f, 5), false).GetComponent<Health>();
            var friend = Trooper(0, new Vector3(3, 0.1f, 5), false).GetComponent<Health>();
            yield return new WaitForFixedUpdate();
            Physics.SyncTransforms();
            sh.TryFire(new Ray(new Vector3(0, 1.2f, -3), Vector3.forward));
            Assert.AreEqual(75f, enemy.Current, 0.01f, "enemy took one hit");
            yield return new WaitForSeconds(0.2f);
            sh.TryFire(new Ray(new Vector3(3, 1.2f, -3), Vector3.forward));
            Assert.AreEqual(100f, friend.Current, 0.01f, "no friendly fire");
        }

        [UnityTest]
        public IEnumerator Respawn_DeadPlayerReturnsAtTeamSpawn()
        {
            var sp = Spawn(0, new Vector3(20, 0.1f, 20));
            var p = Trooper(0, new Vector3(0, 0.1f, 0));
            var h = p.GetComponent<Health>();
            h.Damage(500, 1);
            Assert.IsTrue(h.IsDead);
            Assert.IsFalse(p.GetComponent<TeamMember>().IsAlive, "dead player must not count for zones");
            yield return new WaitForSeconds(0.6f);
            Assert.IsFalse(h.IsDead, "respawned");
            Assert.AreEqual(1, p.GetComponent<Respawner>().Deaths);
            Assert.Less(Vector3.Distance(p.transform.position, sp.transform.position), 0.5f, "at spawn point");
        }

        GameObject Car(Vector3 pos)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Car";
            go.transform.SetParent(_world.transform);
            go.transform.position = pos;
            go.transform.localScale = Vector3.one; // collider on root
            Object.Destroy(go.GetComponent<BoxCollider>());
            var col = go.AddComponent<BoxCollider>(); col.size = new Vector3(1.8f, 1f, 3.4f); col.center = new Vector3(0, 0.5f, 0);
            var rb = go.AddComponent<Rigidbody>(); rb.mass = 900; rb.centerOfMass = new Vector3(0, 0.3f, 0);
            go.AddComponent<Health>().Max = 300;
            var v = go.AddComponent<Vehicle>(); v.RespawnDelay = 0.5f;
            return go;
        }

        [UnityTest]
        public IEnumerator Vehicle_EnterDriveExit()
        {
            var car = Car(new Vector3(0, 0.1f, 0)).GetComponent<Vehicle>();
            var p = Trooper(0, new Vector3(2, 0.1f, 0)).GetComponent<ThirdPersonController>();
            yield return new WaitForFixedUpdate();
            p.Interact();
            Assert.AreEqual(car, p.CurrentVehicle, "entered");
            Assert.AreEqual(p, car.Driver);
            p.MoveInput = new Vector2(0, 1);
            var start = car.transform.position;
            yield return new WaitForSeconds(1.5f);
            float moved = Vector3.Distance(start, car.transform.position);
            Assert.Greater(moved, 3f, $"vehicle should drive forward (moved {moved:0.0} m)");
            Assert.Less(Vector3.Distance(p.transform.position, car.transform.position), 2f, "driver rides along");
            p.Interact();
            Assert.IsNull(p.CurrentVehicle, "exited");
            Assert.IsNull(car.Driver);
            Assert.IsTrue(p.GetComponent<CharacterController>().enabled);
        }

        [UnityTest]
        public IEnumerator Vehicle_DestroyedEjectsDriver_ThenRespawns()
        {
            var carGo = Car(new Vector3(0, 0.1f, 0));
            var car = carGo.GetComponent<Vehicle>();
            var p = Trooper(0, new Vector3(2, 0.1f, 0)).GetComponent<ThirdPersonController>();
            yield return new WaitForFixedUpdate();
            p.Interact();
            p.MoveInput = new Vector2(0, 1);
            yield return new WaitForSeconds(0.6f);
            carGo.GetComponent<Health>().Damage(1000);
            yield return null;
            Assert.IsTrue(car.IsDestroyed);
            Assert.IsNull(p.CurrentVehicle, "driver ejected");
            Assert.IsFalse(car.TryEnter(p), "cannot enter a wreck");
            yield return new WaitForSeconds(0.8f);
            Assert.IsFalse(car.IsDestroyed, "respawned");
            Assert.AreEqual(1, car.Respawns);
            Assert.Less(Vector3.Distance(carGo.transform.position, new Vector3(0, 0.1f, 0)), 0.2f, "back at its spawn");
        }

        [UnityTest]
        public IEnumerator Movement_RunsAtRunSpeedOnFlatGround()
        {
            var p = Trooper(0, new Vector3(0, 0.1f, -3)).GetComponent<ThirdPersonController>();
            p.Yaw = 0;
            yield return new WaitForSeconds(0.3f); // settle on the ground
            p.MoveInput = new Vector2(0, 1);
            float z0 = p.transform.position.z, t0 = Time.time;
            int frames = 0;
            while (Time.time - t0 < 1f) { frames++; yield return null; }
            float speed = (p.transform.position.z - z0) / (Time.time - t0);
            Debug.Log($"[MoveTest] speed {speed:0.00} m/s over {frames} frames, dt≈{(Time.time - t0) / frames:0.000}");
            Assert.AreEqual(p.RunSpeed, speed, 0.6f, "run speed on flat ground");
        }

#if UNITY_EDITOR
        [UnityTest]
        public IEnumerator TeamTint_EnemyModelGetsTeamColour()
        {
            var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Mudfront/Art/Models/SM_Char_Trooper_Placeholder.fbx");
            var t = Trooper(1, new Vector3(0, 0.1f, 0), false);
            var model = Object.Instantiate(asset, t.transform);
            model.AddComponent<TeamTint>();
            yield return null;
            bool found = false;
            foreach (var r in model.GetComponentsInChildren<Renderer>())
                for (int i = 0; i < r.sharedMaterials.Length; i++)
                    if (r.sharedMaterials[i].name.Contains("tidewatch_blue_main"))
                    {
                        var mpb = new MaterialPropertyBlock(); r.GetPropertyBlock(mpb, i);
                        var c = mpb.GetColor("_BaseColor");
                        Assert.Greater(c.r, c.b, "team 1 (Ironclads red) tint applied");
                        found = true;
                    }
            Assert.IsTrue(found, "model has faction material");
        }

        [UnityTest]
        public IEnumerator DoorClearance_ControllerWalksThroughDoorWall()
        {
            var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Mudfront/Art/Models/SM_Kit_WallDoor_4m.fbx");
            Assert.IsNotNull(asset, "door wall imported");
            var wall = Object.Instantiate(asset, _world.transform);
            wall.transform.position = Vector3.zero;
            Assert.IsNotNull(wall.GetComponentInChildren<MeshCollider>(), "door wall has collider");
            var p = Trooper(0, new Vector3(0, 0.1f, -3)).GetComponent<ThirdPersonController>();
            p.Yaw = 0;
            p.MoveInput = new Vector2(0, 1);
            yield return new WaitForSeconds(1.6f);
            Assert.Greater(p.transform.position.z, 2f, $"walked through the doorway (z={p.transform.position.z:0.00})");
        }

        [UnityTest]
        public IEnumerator DoorClearance_SolidWallBlocks()
        {
            var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Mudfront/Art/Models/SM_Kit_Wall_4m.fbx");
            Object.Instantiate(asset, _world.transform).transform.position = Vector3.zero;
            var p = Trooper(0, new Vector3(0, 0.1f, -3)).GetComponent<ThirdPersonController>();
            p.Yaw = 0;
            p.MoveInput = new Vector2(0, 1);
            yield return new WaitForSeconds(1.6f);
            Assert.Less(p.transform.position.z, 0f, "solid wall stops the player");
        }
#endif
    }
}
