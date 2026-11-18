using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Mudfront.EditorTools
{
    /// <summary>
    /// Builds the playable test slice from the Blender kit (original layout, see docs/TARGETS.md) and validates it.
    /// Menu: Mudfront/Build Slice Scene, Mudfront/Validate Slice.
    /// Batch: Unity -batchmode -projectPath Mudfront -executeMethod Mudfront.EditorTools.SliceSceneBuilder.BuildAndValidate -quit
    /// </summary>
    public static class SliceSceneBuilder
    {
        const string Models = "Assets/_Mudfront/Art/Models/";
        const string ScenePath = "Assets/_Mudfront/Scenes/Slice_Test.unity";
        const string MatFolder = "Assets/_Mudfront/Art/Materials";
        internal static string ReportDir => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "docs", "verification"));

        internal static Transform _root;

        // ------------------------------------------------------------------ build
        [MenuItem("Mudfront/Build Slice Scene")]
        public static void Build()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            _root = new GameObject("Slice").transform;
            Lighting();

            // ground, backdrop hills
            Block("Ground", new Vector3(0, -0.5f, 0), new Vector3(240, 1, 240), "#6DA33F", true);
            foreach (var (p, s) in new[] { (new Vector3(-95, -6, 60), new Vector3(70, 26, 60)), (new Vector3(95, -8, 70), new Vector3(80, 30, 70)),
                                           (new Vector3(0, -10, 110), new Vector3(140, 34, 50)), (new Vector3(-70, -8, -95), new Vector3(70, 22, 50)) })
                Hill(p, s, "#4E7D2E");

            // vehicle lane (team 0) + side roads, purely visual strips
            Strip("Road_VehicleLane", new Vector3(0, 0, -38), new Vector3(8, 1, 52), 0, "#A97E52");
            Strip("Road_East", new Vector3(30, 0, 13), new Vector3(7, 1, 44), -62, "#A97E52");
            Strip("Road_West", new Vector3(-30, 0, 13), new Vector3(7, 1, 44), 62, "#A97E52");
            Strip("Courtyard", new Vector3(0, 0, 0), new Vector3(23, 1, 23), 0, "#C8A877");

            Courtyard();
            Tower(new Vector3(18, 0, 18));
            // west infantry route: hut lane
            Hut(new Vector3(-22, 0, -38), 0);
            Hut(new Vector3(-27, 0, -21), 0);
            Place("SM_Cover_CrateStack", new Vector3(-15, 0, -31), 20);
            Place("SM_Cover_SandbagsLow_4m", new Vector3(-13, 0, -47), -10);
            Place("SM_Cover_CrateStack", new Vector3(-18, 0, -12), -35);
            Place("SM_Cover_SandbagsLow_4m", new Vector3(-19, 0, -28), 90);
            // east infantry route: barricade zig-zag
            Place("SM_Cover_BarricadeHigh_4m", new Vector3(19, 0, -46), 15);
            Place("SM_Cover_SandbagsLow_4m", new Vector3(24, 0, -37), -20);
            Place("SM_Cover_BarricadeHigh_4m", new Vector3(18, 0, -28), -10);
            Place("SM_Cover_SandbagsLow_4m", new Vector3(23, 0, -19), 25);
            Place("SM_Cover_CrateStack", new Vector3(17, 0, -14), 0);

            Routes();
            Gameplay();

            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            EditorSceneManager.SaveScene(scene, ScenePath);
            var list = EditorBuildSettings.scenes.Where(s => s.path != ScenePath).ToList();
            list.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = list.ToArray();
            Debug.Log("[Mudfront] Slice scene built: " + ScenePath);
        }

        internal static void Lighting()
        {
            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.transform.SetParent(_root);
            sun.type = LightType.Directional;
            sun.color = Hex("#FFF1DA");
            sun.intensity = 1.35f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.75f;
            sun.transform.rotation = Quaternion.Euler(48, -35, 0);
            RenderSettings.sun = sun;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = Hex("#9CC6EC");
            RenderSettings.ambientEquatorColor = Hex("#CFE0D0");
            RenderSettings.ambientGroundColor = Hex("#6B6A4E");
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = Hex("#CFE8F5");
            RenderSettings.fogStartDistance = 70;
            RenderSettings.fogEndDistance = 320;
            RenderSettings.skybox = null;
        }

        internal static void Courtyard()
        {
            var parent = Group("Objective_Courtyard");
            string W = "SM_Kit_Wall_4m", D = "SM_Kit_WallDoor_4m", N = "SM_Kit_WallWindow_4m";
            float[] seg = { -10, -6, -2, 2, 6, 10 };
            string[] south = { W, N, null, null, N, W };   // 8 m gap = vehicle lane entrance
            string[] north = { W, D, W, N, D, W };
            string[] west = { W, N, D, W, N, W };
            string[] east = { W, N, W, D, N, W };
            for (int i = 0; i < 6; i++)
            {
                if (south[i] != null) Place(south[i], new Vector3(seg[i], 0, -12), 0, parent);
                Place(north[i], new Vector3(seg[i], 0, 12), 180, parent);
                Place(west[i], new Vector3(-12, 0, seg[i]), 90, parent);
                Place(east[i], new Vector3(12, 0, seg[i]), -90, parent);
            }
            foreach (var c in new[] { new Vector3(-12, 0, -12), new Vector3(12, 0, -12), new Vector3(-12, 0, 12), new Vector3(12, 0, 12),
                                      new Vector3(-4, 0, -12), new Vector3(4, 0, -12) })
                Place("SM_Kit_CornerPost", c, 0, parent);
            Place("SM_Cover_SandbagsLow_4m", new Vector3(-4.5f, 0, 3), 30, parent);
            Place("SM_Cover_CrateStack", new Vector3(4.5f, 0, -2.5f), -15, parent);
            Place("SM_Cover_SandbagsLow_4m", new Vector3(3, 0, 6.5f), -5, parent);
        }

        internal static void Tower(Vector3 c)
        {
            var parent = Group("Landmark_Tower");
            for (int floor = 0; floor < 3; floor++)
            {
                float y = floor * 3.2f;
                string front = floor == 0 ? "SM_Kit_WallDoor_4m" : "SM_Kit_WallWindow_4m";
                Place(front, c + new Vector3(0, y, -2), 0, parent);
                Place("SM_Kit_WallWindow_4m", c + new Vector3(0, y, 2), 180, parent);
                Place(floor == 1 ? "SM_Kit_WallWindow_4m" : "SM_Kit_Wall_4m", c + new Vector3(-2, y, 0), 90, parent);
                Place("SM_Kit_WallWindow_4m", c + new Vector3(2, y, 0), -90, parent);
                Place("SM_Kit_Floor_4m", c + new Vector3(0, y + 3.2f - 0.2f, 0), 0, parent);
            }
            foreach (var o in new[] { new Vector3(-2, 0, -2), new Vector3(2, 0, -2), new Vector3(-2, 0, 2), new Vector3(2, 0, 2) })
                for (int floor = 0; floor < 3; floor++)
                    Place("SM_Kit_CornerPost", c + o + Vector3.up * floor * 3.2f, 0, parent);
            Block("FlagPole", c + new Vector3(0, 9.6f + 3f, 0), new Vector3(0.2f, 6f, 0.2f), "#6E4E33", false, parent);
            Block("Flag", c + new Vector3(1.1f, 9.6f + 5.2f, 0), new Vector3(2.2f, 1.3f, 0.08f), "#F2C94C", false, parent);
        }

        internal static void Hut(Vector3 c, float yaw)
        {
            var parent = Group("Hut");
            parent.position = c; parent.rotation = Quaternion.Euler(0, yaw, 0);
            Place("SM_Kit_WallDoor_4m", new Vector3(0, 0, -2), 0, parent, true);
            Place("SM_Kit_Wall_4m", new Vector3(0, 0, 2), 180, parent, true);
            Place("SM_Kit_WallWindow_4m", new Vector3(-2, 0, 0), 90, parent, true);
            Place("SM_Kit_WallWindow_4m", new Vector3(2, 0, 0), -90, parent, true);
            Place("SM_Kit_RoofSlope_4m", new Vector3(-1.2f, 3.2f, 0), 0, parent, true);
            Place("SM_Kit_RoofSlope_4m", new Vector3(1.2f, 3.2f, 0), 180, parent, true);
        }

        // Routes are data for the validator (and later for bots). Points at ground level.
        internal static readonly Dictionary<string, Vector3[]> RouteDefs = new Dictionary<string, Vector3[]>
        {
            // west: hut lane, enters the courtyard through the west door (x=-12, z=-2)
            { "Route_Infantry_West", new[] { new Vector3(-3, 0, -60), new Vector3(-14, 0, -52), new Vector3(-17.5f, 0, -45), new Vector3(-17.5f, 0, -16),
                                             new Vector3(-15, 0, -9), new Vector3(-16, 0, -2), new Vector3(-12, 0, -2), new Vector3(-4, 0, -2) } },
            // east: flank outside the barricade line, enters through the east door (x=12, z=2)
            { "Route_Infantry_East", new[] { new Vector3(3, 0, -60), new Vector3(14, 0, -55), new Vector3(27, 0, -48), new Vector3(28, 0, -30),
                                             new Vector3(28, 0, -14), new Vector3(20, 0, 2), new Vector3(12, 0, 2), new Vector3(4, 0, 2) } },
            { "Route_Vehicle_Lane",  new[] { new Vector3(0, 0, -60), new Vector3(0, 0, -14), new Vector3(0, 0, -6) } },
        };

        internal static void Routes()
        {
            var parent = Group("Routes");
            foreach (var kv in RouteDefs)
            {
                var r = new GameObject(kv.Key).transform; r.SetParent(parent);
                for (int i = 0; i < kv.Value.Length; i++)
                {
                    var p = new GameObject("P" + i).transform; p.SetParent(r); p.position = kv.Value[i];
                }
            }
        }

        internal static void Gameplay()
        {
            var parent = Group("Gameplay");
            new GameObject("MatchManager").AddComponent<MatchManager>().transform.SetParent(parent);
            var zoneGo = new GameObject("CaptureZone"); zoneGo.transform.SetParent(parent);
            var zone = zoneGo.AddComponent<CaptureZone>();
            zone.Radius = 8f; zone.CaptureTime = 3f;
            Disc("ZoneMarker", Vector3.up * 0.03f, 16f, "#F2C94C", zoneGo.transform);

            var spawns = new (int team, Vector3 pos, float yaw)[]
            {
                (0, new Vector3(-4, 0, -62), 0), (0, new Vector3(0, 0, -64), 0), (0, new Vector3(4, 0, -62), 0),
                (1, new Vector3(54, 0, 32), -122), (1, new Vector3(57, 0, 28), -122),
                (2, new Vector3(-54, 0, 32), 122), (2, new Vector3(-57, 0, 28), 122),
            };
            foreach (var s in spawns)
            {
                var go = new GameObject($"Spawn_T{s.team}");
                go.transform.SetParent(parent);
                go.transform.SetPositionAndRotation(s.pos + Vector3.up * 0.1f, Quaternion.Euler(0, s.yaw, 0));
                go.AddComponent<SpawnPoint>().TeamId = s.team;
                Block("SpawnPad", s.pos + Vector3.up * 0.02f, new Vector3(3, 0.04f, 3), ColorUtility.ToHtmlStringRGB(TeamColors.Get(s.team)), false, go.transform, true);
            }

            MakeVehicle(new Vector3(7, 0.1f, -58), 0, parent);
            MakeVehicle(new Vector3(47, 0.1f, 25), -122, parent);
            MakeVehicle(new Vector3(-47, 0.1f, 25), 122, parent);

            var player = MakeTrooper("Player", 0, new Vector3(0, 0.1f, -60), 0, parent);
            var ctrl = player.AddComponent<ThirdPersonController>();
            player.AddComponent<Shooter>();
            player.GetComponent<Respawner>().RespawnDelay = 6f;

            // enemy dummies (outside the zone so the player can capture solo)
            foreach (var d in new (int t, Vector3 p, float yaw)[] { (1, new Vector3(22, 0.1f, -26), -150), (1, new Vector3(4, 0.1f, 17), 180),
                                                                    (2, new Vector3(-21, 0.1f, -16), 150), (2, new Vector3(-8, 0.1f, 18), 160) })
            {
                var dummy = MakeTrooper("Dummy_T" + d.t, d.t, d.p, d.yaw, parent);
                dummy.GetComponent<Respawner>().RespawnDelay = 5f;
                // dummies respawn where they stood: give them a private spawn point
                var sp = new GameObject("DummySpawn").AddComponent<SpawnPoint>();
                sp.TeamId = d.t; sp.transform.SetParent(parent); sp.transform.SetPositionAndRotation(d.p, Quaternion.Euler(0, d.yaw, 0));
            }

            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Hex("#9CCBF0");
            cam.fieldOfView = 65; cam.farClipPlane = 600;
            camGo.AddComponent<AudioListener>();
            camGo.AddComponent<ThirdPersonCamera>().Target = ctrl;
            camGo.transform.SetParent(parent);
            var hud = new GameObject("HUD").AddComponent<SliceHud>();
            hud.Player = ctrl; hud.Zone = zone; hud.transform.SetParent(parent);
        }

        internal static GameObject MakeTrooper(string name, int team, Vector3 pos, float yaw, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0, yaw, 0));
            var cc = go.AddComponent<CharacterController>();
            cc.height = 1.8f; cc.radius = 0.4f; cc.center = new Vector3(0, 0.9f, 0); cc.stepOffset = 0.4f; cc.slopeLimit = 45;
            go.AddComponent<TeamMember>().TeamId = team;
            go.AddComponent<Health>().Max = 100;
            go.AddComponent<Respawner>();
            // the CharacterController capsule doubles as the hitbox (raycasts hit it)
            var model = Instance("SM_Char_Trooper_Placeholder", go.transform);
            FaceForward(model, 1.66f, 1.78f, front: true);
            TintTeam(model, team);
            return go;
        }

        internal static void MakeVehicle(Vector3 pos, float yaw, Transform parent)
        {
            var go = new GameObject("Vehicle_Mudskipper");
            go.transform.SetParent(parent);
            go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0, yaw, 0));
            var model = Instance("SM_Veh_Mudskipper", go.transform);
            FaceForward(model, 1.95f, 2.2f, front: false);   // roll bar sits at the back
            var b = LocalBounds(go);
            var col = go.AddComponent<BoxCollider>();
            col.center = b.center + Vector3.up * 0.15f; col.size = b.size - Vector3.up * 0.3f;
            var rb = go.AddComponent<Rigidbody>();
            rb.mass = 900; rb.linearDamping = 0.05f; rb.angularDamping = 2f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.centerOfMass = new Vector3(0, 0.45f, 0);
            go.AddComponent<Health>().Max = 300;
            var v = go.AddComponent<Vehicle>();
            var seat = new GameObject("Seat").transform; seat.SetParent(go.transform, false);
            seat.localPosition = new Vector3(-0.35f, 0.75f, 0.1f);
            v.Seat = seat;
        }

        // ------------------------------------------------------------------ helpers
        internal static Transform Group(string name)
        {
            var t = new GameObject(name).transform; t.SetParent(_root); return t;
        }

        internal static GameObject Instance(string model, Transform parent)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(Models + model + ".fbx");
            if (asset == null) { Debug.LogError("[Mudfront] Missing model " + model); return new GameObject(model + "_MISSING"); }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(asset, parent);
            go.transform.localPosition = Vector3.zero; go.transform.localRotation = Quaternion.identity;
            return go;
        }

        internal static GameObject Place(string model, Vector3 pos, float yaw, Transform parent = null, bool local = false)
        {
            var go = Instance(model, parent != null ? parent : _root);
            if (local) { go.transform.localPosition = pos; go.transform.localRotation = Quaternion.Euler(0, yaw, 0); }
            else go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0, yaw, 0));
            go.isStatic = true;
            foreach (Transform c in go.GetComponentsInChildren<Transform>()) c.gameObject.isStatic = true;
            return go;
        }

        /// <summary>Rotates the model 180° if the identifying feature (between heights y0..y1) is on the wrong side.</summary>
        internal static void FaceForward(GameObject model, float y0, float y1, bool front)
        {
            float sum = 0; int n = 0;
            foreach (var mf in model.GetComponentsInChildren<MeshFilter>())
                foreach (var v in mf.sharedMesh.vertices)
                {
                    var p = model.transform.InverseTransformPoint(mf.transform.TransformPoint(v));
                    if (p.y >= y0 && p.y <= y1) { sum += p.z; n++; }
                }
            if (n == 0) return;
            bool featureAtPlusZ = sum / n > 0;
            if (featureAtPlusZ != front) model.transform.localRotation = Quaternion.Euler(0, 180, 0) * model.transform.localRotation;
        }

        internal static void TintTeam(GameObject model, int team)
        {
            var col = TeamColors.Get(team);
            foreach (var r in model.GetComponentsInChildren<Renderer>())
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] == null || !mats[i].name.Contains("tidewatch")) continue;
                    var mpb = new MaterialPropertyBlock();
                    float k = mats[i].name.Contains("dark") ? 0.62f : mats[i].name.Contains("accent") ? 1.25f : 1f;
                    mpb.SetColor("_BaseColor", new Color(col.r * k, col.g * k, col.b * k));
                    r.SetPropertyBlock(mpb, i);
                }
            }
        }

        internal static Bounds LocalBounds(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>();
            var b = new Bounds(go.transform.InverseTransformPoint(rs[0].bounds.center), Vector3.zero);
            foreach (var r in rs)
                foreach (var mf in r.GetComponents<MeshFilter>())
                    foreach (var v in mf.sharedMesh.vertices)
                        b.Encapsulate(go.transform.InverseTransformPoint(mf.transform.TransformPoint(v)));
            return b;
        }

        internal static Material Mat(string hex)
        {
            Directory.CreateDirectory(MatFolder);
            string path = $"{MatFolder}/MAT_{hex.TrimStart('#')}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m != null) return m;
            m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            m.SetColor("_BaseColor", Hex(hex));
            m.SetFloat("_Smoothness", 0.05f);
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        internal static GameObject Block(string name, Vector3 pos, Vector3 size, string hex, bool collider, Transform parent = null, bool noCollider = false)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent != null ? parent : _root);
            go.transform.position = pos; go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = Mat(hex.StartsWith("#") ? hex : "#" + hex);
            if (!collider || noCollider) Object.DestroyImmediate(go.GetComponent<Collider>());
            go.isStatic = true;
            return go;
        }

        internal static void Strip(string name, Vector3 pos, Vector3 size, float yaw, string hex)
        {
            var go = Block(name, pos + Vector3.up * 0.015f, new Vector3(size.x, 0.03f, size.z), hex, false);
            go.transform.rotation = Quaternion.Euler(0, yaw, 0);
        }

        internal static void Hill(Vector3 pos, Vector3 size, string hex)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Hill"; go.transform.SetParent(_root);
            go.transform.position = pos; go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = Mat(hex);
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.AddComponent<MeshCollider>();
            go.isStatic = true;
        }

        internal static void Disc(string name, Vector3 localPos, float diameter, string hex, Transform parent)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name; go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos; go.transform.localScale = new Vector3(diameter, 0.01f, diameter);
            var m = Mat(hex);
            go.GetComponent<Renderer>().sharedMaterial = m;
            Object.DestroyImmediate(go.GetComponent<Collider>());
        }

        internal static Color Hex(string hex) { ColorUtility.TryParseHtmlString(hex, out var c); return c; }

        // ------------------------------------------------------------------ validate
        [MenuItem("Mudfront/Validate Slice")]
        public static int Validate()
        {
            var sb = new StringBuilder();
            int fails = 0;
            void Check(bool ok, string msg) { sb.AppendLine((ok ? "- PASS " : "- **FAIL** ") + msg); if (!ok) fails++; }

            sb.AppendLine("# Slice verification report");
            sb.AppendLine($"Generated {System.DateTime.Now:yyyy-MM-dd HH:mm} by `SliceSceneBuilder.Validate` (Unity {Application.unityVersion}).\n");

            // 1. asset imports
            sb.AppendLine("## 1. Asset imports");
            foreach (var path in Directory.GetFiles(Path.Combine(Application.dataPath, "_Mudfront/Art/Models"), "*.fbx").OrderBy(p => p))
            {
                string name = Path.GetFileNameWithoutExtension(path);
                var asset = AssetDatabase.LoadAssetAtPath<GameObject>(Models + name + ".fbx");
                bool ok = asset != null;
                string info = "";
                if (ok)
                {
                    var rs = asset.GetComponentsInChildren<Renderer>();
                    var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
                    int mats = rs.Sum(r => r.sharedMaterials.Length);
                    bool needsCol = name.StartsWith("SM_Kit_") || name.StartsWith("SM_Cover_");
                    bool hasCol = asset.GetComponentInChildren<MeshCollider>() != null;
                    ok = rs.Length > 0 && mats > 0 && Mathf.Abs(b.min.y) < 0.05f && (!needsCol || hasCol);
                    info = $"size {b.size.x:0.00}×{b.size.y:0.00}×{b.size.z:0.00} m, {mats} materials, pivot→bottom {b.min.y:+0.00;-0.00}, collider {(needsCol ? (hasCol ? "mesh" : "MISSING") : "added in code")}";
                }
                Check(ok, $"`{name}` {info}");
            }

            // 2. controller clearance
            sb.AppendLine("\n## 2. Controller clearance (capsule r=0.4, h=1.8)");
            Physics.SyncTransforms();
            var probe = new Vector3(5000, 0, 5000);
            foreach (var (model, expectClear) in new[] { ("SM_Kit_WallDoor_4m", true), ("SM_Kit_Wall_4m", false), ("SM_Kit_WallWindow_4m", false) })
            {
                var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Models + model + ".fbx"));
                go.transform.position = probe;
                Physics.SyncTransforms();
                var start = probe + new Vector3(0, 0, -2.5f);
                bool hit = Physics.CapsuleCast(start + Vector3.up * 0.45f, start + Vector3.up * 1.35f, 0.4f, Vector3.forward, out var h, 5f);
                Check(hit != expectClear, $"`{model}`: capsule {(hit ? "blocked" : "passes")} (expected {(expectClear ? "pass" : "block")})");
                Object.DestroyImmediate(go);
            }

            // 3. route times + obstruction, in the built scene
            sb.AppendLine("\n## 3. Routes (sprint 8 m/s, run 5.5 m/s, vehicle ~14 m/s avg)");
            if (!File.Exists(Path.Combine(Application.dataPath, "..", ScenePath))) Build();
            EditorSceneManager.OpenScene(ScenePath);
            Physics.SyncTransforms();
            foreach (var kv in RouteDefs)
            {
                float len = 0; int blocked = 0;
                var pts = kv.Value;
                for (int i = 1; i < pts.Length; i++)
                {
                    var a = pts[i - 1] + Vector3.up * 0.05f; var d = pts[i] - pts[i - 1]; float dist = d.magnitude; len += dist;
                    bool vehicle = kv.Key.Contains("Vehicle");
                    float r = vehicle ? 1.2f : 0.4f;
                    var hits = Physics.CapsuleCastAll(a + Vector3.up * (r + 0.45f), a + Vector3.up * 1.35f, r, d / dist, dist);
                    foreach (var h in hits)
                    {
                        if (h.collider.GetComponentInParent<TeamMember>() || h.collider.GetComponentInParent<Vehicle>() || h.collider.name == "Ground") continue;
                        blocked++; sb.AppendLine($"  - segment {i - 1}->{i} blocked by `{h.collider.transform.root.name}/{h.collider.name}` at {h.point}");
                    }
                }
                string t = kv.Key.Contains("Vehicle") ? $"{len / 14f:0.0} s by vehicle" : $"{len / 8f:0.0} s sprint / {len / 5.5f:0.0} s run";
                Check(blocked == 0, $"`{kv.Key}` length {len:0} m → {t}, {(blocked == 0 ? "clear" : blocked + " blocked segment(s)")}");
            }

            // 4. readability (colour contrast vs environment)
            sb.AppendLine("\n## 4. Readability (WCAG-style luminance contrast, team colour vs background)");
            foreach (var (bgName, bg) in new[] { ("grass", Hex("#6DA33F")), ("plaster", Hex("#EADDBF")), ("dirt road", Hex("#A97E52")) })
                for (int t = 0; t < 3; t++)
                {
                    float cr = Contrast(TeamColors.Get(t), bg);
                    sb.AppendLine($"- {TeamColors.Names[t]} vs {bgName}: {cr:0.00}:1 {(cr >= 1.5f ? "" : " ← weak, relies on silhouette/outline")}");
                }
            sb.AppendLine("\nScreenshots from the gameplay camera: `shot_spawn.png`, `shot_zone.png`, `shot_tower.png`.");
            Screenshots();

            sb.AppendLine($"\n**Result: {(fails == 0 ? "all checks passed" : fails + " check(s) failed")}.**");
            Directory.CreateDirectory(ReportDir);
            File.WriteAllText(Path.Combine(ReportDir, "slice_report.md"), sb.ToString());
            Debug.Log("[Mudfront] Validation done, fails=" + fails + "\n" + sb);
            return fails;
        }

        internal static float Lum(Color c)
        {
            float f(float x) => x <= 0.03928f ? x / 12.92f : Mathf.Pow((x + 0.055f) / 1.055f, 2.4f);
            return 0.2126f * f(c.r) + 0.7152f * f(c.g) + 0.0722f * f(c.b);
        }
        internal static float Contrast(Color a, Color b) { float la = Lum(a) + 0.05f, lb = Lum(b) + 0.05f; return Mathf.Max(la, lb) / Mathf.Min(la, lb); }

        internal static void Screenshots()
        {
            var camGo = GameObject.Find("Main Camera");
            if (camGo == null) return;
            var cam = camGo.GetComponent<Camera>();
            var shots = new (string file, Vector3 pivot, float yaw, float pitch, float dist)[]
            {
                ("shot_spawn.png", new Vector3(0, 1.6f, -60), 0, 8, 4f),
                ("shot_zone.png", new Vector3(-2, 1.6f, -9), 15, 12, 4f),
                ("shot_tower.png", new Vector3(-20, 1.6f, -30), 40, -2, 4f),
            };
            foreach (var s in shots)
            {
                var rot = Quaternion.Euler(s.pitch, s.yaw, 0);
                cam.transform.SetPositionAndRotation(s.pivot + rot * new Vector3(0.6f, 0, -s.dist), rot);
                var rt = new RenderTexture(1600, 900, 24);
                cam.targetTexture = rt;
                cam.Render();
                RenderTexture.active = rt;
                var tex = new Texture2D(1600, 900, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0); tex.Apply();
                Directory.CreateDirectory(ReportDir);
                File.WriteAllBytes(Path.Combine(ReportDir, s.file), tex.EncodeToPNG());
                cam.targetTexture = null; RenderTexture.active = null;
                Object.DestroyImmediate(rt); Object.DestroyImmediate(tex);
            }
        }

        public static void BuildAndValidate()
        {
            AssetDatabase.Refresh();
            Build();
            int fails = Validate();
            EditorApplication.Exit(fails == 0 ? 0 : 2);
        }
    }
}
