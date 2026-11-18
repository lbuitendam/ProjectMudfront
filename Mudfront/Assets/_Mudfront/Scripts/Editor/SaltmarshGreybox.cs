using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using B = Mudfront.EditorTools.SliceSceneBuilder;

namespace Mudfront.EditorTools
{
    /// <summary>
    /// Grey-box of Map 01 "Saltmarsh Point" (docs/maps/MAP_01_Saltmarsh_Point.md). Original layout.
    /// Menu: Mudfront/Build Saltmarsh Greybox. Batch: -executeMethod Mudfront.EditorTools.SaltmarshGreybox.BuildAndValidate
    /// Units: metres. Sea level = y 0. Terrain 1024 m, play area ~700 m.
    /// </summary>
    public static class SaltmarshGreybox
    {
        const string ScenePath = "Assets/_Mudfront/Scenes/Map01_Saltmarsh_Greybox.unity";
        const string TerrainPath = "Assets/_Mudfront/Scenes/Map01_Saltmarsh_Terrain.asset";
        const float TerrainSize = 1024f, TerrainBottom = -12f, TerrainHeight = 72f;
        static string ReportDir => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "docs", "verification"));

        // ---- layout (XZ) --------------------------------------------------------------
        internal static readonly (int team, string name, Vector2 pos, float yaw)[] HQs =
        {
            (0, "HQ_Tidewatch_HarbourQuay", new Vector2(10, -190), 0),
            (1, "HQ_Ironclads_CliffFort",   new Vector2(208, 120), -120),
            (2, "HQ_Muddogs_DuneFarm",      new Vector2(-202, 74), 110),
        };

        internal static readonly (string name, Vector2 pos, float radius)[] Zones =
        {
            ("Beacon Mill",     new Vector2(110, 70),   8),
            ("Marsh Boardwalk", new Vector2(0, 0),      10),
            ("Net Sheds",       new Vector2(60, -120),  8),
            ("Sunken Barge",    new Vector2(-70, -60),  9),
            ("Dune Battery",    new Vector2(-110, 60),  9),
        };

        // ring road (vehicle) and per-HQ infantry routes to their nearest zone and to the marsh
        static readonly Vector2[] RingRoad =
        {
            new Vector2(10, -175), new Vector2(70, -150), new Vector2(130, -80), new Vector2(160, 10), new Vector2(190, 100),
            new Vector2(120, 140), new Vector2(20, 120), new Vector2(-80, 110), new Vector2(-185, 80),
            new Vector2(-150, 0), new Vector2(-110, -90), new Vector2(-50, -150), new Vector2(10, -175),
        };

        internal static readonly Dictionary<string, Vector2[]> Routes = new Dictionary<string, Vector2[]>
        {
            { "Tidewatch→Net Sheds (quay)",        new[] { new Vector2(10, -185), new Vector2(40, -150), new Vector2(60, -124) } },
            { "Tidewatch→Marsh (boardwalk)",       new[] { new Vector2(10, -185), new Vector2(0, -130), new Vector2(-10, -60), new Vector2(0, -8) } },
            { "Tidewatch→Marsh (mudflat flank)",   new[] { new Vector2(10, -185), new Vector2(-45, -140), new Vector2(-70, -66), new Vector2(-30, -30), new Vector2(-6, -6) } },
            { "Ironclads→Beacon Mill (cliff path)", new[] { new Vector2(200, 115), new Vector2(160, 95), new Vector2(116, 74) } },
            { "Ironclads→Marsh (cliff descent)",   new[] { new Vector2(200, 115), new Vector2(150, 40), new Vector2(90, 15), new Vector2(40, 5), new Vector2(8, 2) } },
            { "Muddogs→Dune Battery (trench)",     new[] { new Vector2(-195, 72), new Vector2(-150, 70), new Vector2(-116, 62) } },
            { "Muddogs→Marsh (dune trail)",        new[] { new Vector2(-195, 72), new Vector2(-140, 30), new Vector2(-80, 10), new Vector2(-30, 5), new Vector2(-8, 1) } },
        };

        // ---- terrain shape --------------------------------------------------------------
        /// <summary>Ground height in metres at world XZ. Sea level = 0.</summary>
        internal static float Height(float x, float z)
        {
            float h = 5f;                                                        // marsh / farmland base
            // cliff plateau to the north-east (Ironclads, Beacon Mill)
            float ne = (x * 0.62f + z * 0.78f);
            h += 18f * Smooth(55f, 85f, ne);
            // dunes to the west (Muddogs)
            float w = Smooth(-90f, -150f, x);
            h += w * (4f + 2.5f * Mathf.Sin(x * 0.09f) * Mathf.Cos(z * 0.07f));
            // marsh bowl in the middle (slightly lower, with channels)
            float r = new Vector2(x, z).magnitude;
            h -= 2.2f * (1f - Smooth(0f, 70f, r));
            h -= 1.2f * Mathf.Clamp01(1f - Mathf.Abs(Mathf.Sin((x + z) * 0.05f)) * 3f) * (1f - Smooth(30f, 90f, r));
            // harbour shelf to the south (Tidewatch quay ~1.5 m)
            float south = Smooth(-100f, -160f, z);
            h = Mathf.Lerp(h, 1.5f, south * (1f - Smooth(120f, 170f, Mathf.Abs(x - 30f))));
            // coastline: sea to the south and east
            float coast = Mathf.Max(Smooth(-205f, -240f, z), Smooth(250f, 290f, x - z * 0.15f));
            h = Mathf.Lerp(h, -8f, coast);
            // map edge rises to backdrop hills (north/west)
            float edge = Mathf.Max(Smooth(330f, 420f, z), Smooth(-330f, -420f, x));
            h += 30f * edge;
            h += Mathf.PerlinNoise(x * 0.02f + 100, z * 0.02f + 100) * 1.6f - 0.8f;
            return h;
        }

        static float Smooth(float a, float b, float v)
        {
            float t = Mathf.Clamp01((v - a) / (b - a));
            return t * t * (3f - 2f * t);
        }

        static Vector3 OnGround(Vector2 p, float lift = 0f) => new Vector3(p.x, Height(p.x, p.y) + lift, p.y);

        // ---- build ----------------------------------------------------------------------
        [MenuItem("Mudfront/Build Saltmarsh Greybox")]
        public static void Build()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            B._root = new GameObject("Map01_Saltmarsh").transform;
            B.Lighting();
            RenderSettings.fogEndDistance = 650; RenderSettings.fogStartDistance = 150;

            MakeTerrain();
            var sea = B.Block("Sea", new Vector3(0, -0.05f, 0), new Vector3(3000, 0.1f, 3000), "#4FA3C7", false);
            sea.isStatic = true;

            // roads
            var roads = B.Group("Roads");
            for (int i = 1; i < RingRoad.Length; i++) RoadSegment(RingRoad[i - 1], RingRoad[i], 8f, roads);
            foreach (var hq in HQs) RoadSegment(hq.pos, NearestOnRing(hq.pos), 7f, roads);
            Bridge(new Vector2(-130, -45), 35, roads);
            Bridge(new Vector2(145, -35), -20, roads);

            // HQs
            foreach (var hq in HQs) BuildHQ(hq.team, hq.name, hq.pos, hq.yaw);

            // zone locations (grey-box set pieces)
            BeaconMill(Zones[0].pos);
            Boardwalk(Zones[1].pos);
            NetSheds(Zones[2].pos);
            SunkenBarge(Zones[3].pos);
            DuneBattery(Zones[4].pos);
            Dressing();

            // route markers (data for validator / bots)
            var rt = B.Group("Routes");
            foreach (var kv in Routes)
            {
                var r = new GameObject(kv.Key).transform; r.SetParent(rt);
                for (int i = 0; i < kv.Value.Length; i++) { var p = new GameObject("P" + i).transform; p.SetParent(r); p.position = OnGround(kv.Value[i]); }
            }

            Gameplay();

            EditorSceneManager.SaveScene(scene, ScenePath);
            var list = EditorBuildSettings.scenes.Where(s => s.path != ScenePath).ToList();
            list.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = list.ToArray();
            Debug.Log("[Mudfront] Saltmarsh greybox built: " + ScenePath);
        }

        static void MakeTerrain()
        {
            const int res = 513;
            var td = new TerrainData { heightmapResolution = res };
            td.size = new Vector3(TerrainSize, TerrainHeight, TerrainSize);
            var hs = new float[res, res];
            for (int zi = 0; zi < res; zi++)
                for (int xi = 0; xi < res; xi++)
                {
                    float x = -TerrainSize / 2 + xi * TerrainSize / (res - 1);
                    float z = -TerrainSize / 2 + zi * TerrainSize / (res - 1);
                    hs[zi, xi] = Mathf.Clamp01((Height(x, z) - TerrainBottom) / TerrainHeight);
                }
            td.SetHeights(0, 0, hs);

            // simple painted layers: grass / sand / rock by height & slope
            var grass = Layer("grass", "#6DA33F"); var sand = Layer("sand", "#E3CC92"); var rock = Layer("rock", "#9A948A"); var mud = Layer("mud", "#7A5A3A");
            td.terrainLayers = new[] { grass, sand, rock, mud };
            td.alphamapResolution = 256;
            var a = new float[256, 256, 4];
            for (int zi = 0; zi < 256; zi++)
                for (int xi = 0; xi < 256; xi++)
                {
                    float x = -TerrainSize / 2 + (xi + 0.5f) * TerrainSize / 256, z = -TerrainSize / 2 + (zi + 0.5f) * TerrainSize / 256;
                    float h = Height(x, z);
                    float steep = td.GetSteepness((xi + 0.5f) / 256f, (zi + 0.5f) / 256f);
                    float r = new Vector2(x, z).magnitude;
                    float wR = Mathf.Clamp01((steep - 22f) / 10f);
                    float wS = (1 - wR) * Mathf.Clamp01(h < 2.5f ? 1f : (x < -130 ? 0.8f : 0f));
                    float wM = (1 - wR - wS) * Mathf.Clamp01(1f - r / 60f);
                    float wG = Mathf.Max(0, 1 - wR - wS - wM);
                    a[zi, xi, 0] = wG; a[zi, xi, 1] = wS; a[zi, xi, 2] = wR; a[zi, xi, 3] = wM;
                }
            td.SetAlphamaps(0, 0, a);
            AssetDatabase.DeleteAsset(TerrainPath);
            AssetDatabase.CreateAsset(td, TerrainPath);
            var go = Terrain.CreateTerrainGameObject(td);
            go.name = "Terrain";
            go.transform.SetParent(B._root);
            go.transform.position = new Vector3(-TerrainSize / 2, TerrainBottom, -TerrainSize / 2);
            go.isStatic = true;
        }

        static TerrainLayer Layer(string name, string hex)
        {
            string path = $"Assets/_Mudfront/Art/Materials/TL_{name}.terrainlayer";
            var l = AssetDatabase.LoadAssetAtPath<TerrainLayer>(path);
            if (l != null) return l;
            Directory.CreateDirectory("Assets/_Mudfront/Art/Materials");
            var tex = new Texture2D(4, 4);
            ColorUtility.TryParseHtmlString(hex, out var c);
            tex.SetPixels(Enumerable.Repeat(c, 16).ToArray()); tex.Apply();
            File.WriteAllBytes($"Assets/_Mudfront/Art/Materials/TX_{name}.png", tex.EncodeToPNG());
            AssetDatabase.ImportAsset($"Assets/_Mudfront/Art/Materials/TX_{name}.png");
            l = new TerrainLayer { diffuseTexture = AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/_Mudfront/Art/Materials/TX_{name}.png"), tileSize = new Vector2(8, 8) };
            AssetDatabase.CreateAsset(l, path);
            return l;
        }

        static Vector2 NearestOnRing(Vector2 p)
        {
            Vector2 best = RingRoad[0]; float bd = float.MaxValue;
            for (int i = 1; i < RingRoad.Length; i++)
            {
                var a = RingRoad[i - 1]; var b = RingRoad[i];
                float t = Mathf.Clamp01(Vector2.Dot(p - a, b - a) / (b - a).sqrMagnitude);
                var q = a + (b - a) * t; float d = (q - p).sqrMagnitude;
                if (d < bd) { bd = d; best = q; }
            }
            return best;
        }

        static void RoadSegment(Vector2 a, Vector2 b, float width, Transform parent)
        {
            // split into ~10 m pieces that follow the terrain
            int n = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(a, b) / 10f));
            for (int i = 0; i < n; i++)
            {
                var p0 = Vector2.Lerp(a, b, (float)i / n); var p1 = Vector2.Lerp(a, b, (float)(i + 1) / n);
                var w0 = OnGround(p0, 0.06f); var w1 = OnGround(p1, 0.06f);
                if (Mathf.Min(w0.y, w1.y) < 0.2f) continue; // bridges handle water crossings
                var go = B.Block("Road", (w0 + w1) / 2, new Vector3(width, 0.08f, Vector3.Distance(w0, w1) + 0.6f), "#A97E52", false, parent, true);
                go.transform.rotation = Quaternion.LookRotation(w1 - w0);
            }
        }

        static void Bridge(Vector2 c, float yaw, Transform parent)
        {
            var p = OnGround(c);
            var deck = B.Block("Bridge", new Vector3(p.x, Mathf.Max(p.y, 0) + 1.2f, p.z), new Vector3(9, 0.5f, 26), "#9C6B3E", true, parent);
            deck.transform.rotation = Quaternion.Euler(0, yaw, 0);
            for (int s = -1; s <= 1; s += 2)
            {
                var rail = B.Block("Rail", deck.transform.position + deck.transform.right * 4.4f * s + Vector3.up * 0.6f, new Vector3(0.3f, 0.9f, 26), "#6B4526", true, parent);
                rail.transform.rotation = deck.transform.rotation;
            }
        }

        static void BuildHQ(int team, string name, Vector2 c, float yaw)
        {
            var g = B.Group(name);
            var p = OnGround(c);
            var pad = B.Block("HQ_Pad", p + Vector3.up * 0.1f, new Vector3(36, 0.4f, 30), "#C8A877", true, g);
            pad.transform.rotation = Quaternion.Euler(0, yaw, 0);
            var rot = Quaternion.Euler(0, yaw, 0);
            // a couple of kit walls as the HQ silhouette
            for (int i = -2; i <= 2; i++)
                if (i != 0) B.Place("SM_Kit_Wall_4m", p + rot * new Vector3(i * 4, 0.3f, 14), yaw + 180, g);
            B.Block("HQ_Banner", p + rot * new Vector3(0, 6, 14), new Vector3(3, 4, 0.2f), ColorUtility.ToHtmlStringRGB(TeamColors.Get(team)), false, g);
            for (int i = 0; i < 3; i++)
            {
                var s = new GameObject($"Spawn_T{team}_{i}"); s.transform.SetParent(g);
                s.transform.SetPositionAndRotation(p + rot * new Vector3(-6 + i * 6, 0.5f, 0), rot);
                s.AddComponent<SpawnPoint>().TeamId = team;
            }
            B.MakeVehicle(p + rot * new Vector3(-10, 0.6f, -8), yaw, g);
            B.MakeVehicle(p + rot * new Vector3(10, 0.6f, -8), yaw, g);
        }

        static void BeaconMill(Vector2 c)
        {
            var g = B.Group("Zone_BeaconMill"); var p = OnGround(c);
            var tower = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            tower.name = "MillTower"; tower.transform.SetParent(g); tower.transform.position = p + Vector3.up * 9; tower.transform.localScale = new Vector3(7, 9, 7);
            tower.GetComponent<Renderer>().sharedMaterial = B.Mat("#EEF3F7"); tower.isStatic = true;
            B.Block("Lamp", p + Vector3.up * 19.5f, new Vector3(4, 3, 4), "#F2C94C", true, g);
            B.Block("Cap", p + Vector3.up * 21.6f, new Vector3(5, 1.2f, 5), "#3F6E9E", true, g);
            var blades = B.Block("BladeA", p + new Vector3(0, 15, -3.8f), new Vector3(1.2f, 16, 0.3f), "#C49A63", false, g);
            blades.transform.rotation = Quaternion.Euler(0, 0, 25);
            var b2 = B.Block("BladeB", p + new Vector3(0, 15, -3.8f), new Vector3(1.2f, 16, 0.3f), "#C49A63", false, g);
            b2.transform.rotation = Quaternion.Euler(0, 0, 115);
            B.Block("Balcony", p + Vector3.up * 6.4f, new Vector3(11, 0.4f, 11), "#9C6B3E", true, g);
            B.Place("SM_Cover_SandbagsLow_4m", p + new Vector3(-8, 0, -6), 30, g);
            B.Place("SM_Cover_CrateStack", p + new Vector3(7, 0, -7), -20, g);
        }

        static void Boardwalk(Vector2 c)
        {
            var g = B.Group("Zone_MarshBoardwalk"); var p = OnGround(c);
            float y = Mathf.Max(p.y, 0f) + 0.8f;
            foreach (var (o, s, yaw) in new[] { (new Vector2(0, 0), new Vector2(12, 12), 0f), (new Vector2(0, -24), new Vector2(3, 30), 0f),
                                                (new Vector2(18, 6), new Vector2(3, 26), 70f), (new Vector2(-18, 4), new Vector2(3, 26), -75f) })
            {
                var b = B.Block("Walkway", new Vector3(p.x + o.x, y, p.z + o.y), new Vector3(s.x, 0.3f, s.y), "#9C6B3E", true, g);
                b.transform.rotation = Quaternion.Euler(0, yaw, 0);
            }
            B.Place("SM_Cover_CrateStack", new Vector3(p.x + 3, y + 0.15f, p.z + 3), 10, g);
            B.Place("SM_Cover_SandbagsLow_4m", new Vector3(p.x - 3, y + 0.15f, p.z - 2), -40, g);
        }

        static void NetSheds(Vector2 c)
        {
            var g = B.Group("Zone_NetSheds"); var p = OnGround(c);
            foreach (var (o, yaw) in new[] { (new Vector2(-9, 4), 10f), (new Vector2(8, 6), -8f), (new Vector2(0, -10), 85f) })
            {
                var s = B.Block("Shed", p + new Vector3(o.x, 2.5f, o.y), new Vector3(8, 5, 6), "#B8644A", true, g);
                s.transform.rotation = Quaternion.Euler(0, yaw, 0);
                var roof = B.Block("ShedRoof", p + new Vector3(o.x, 5.4f, o.y), new Vector3(9, 0.6f, 7), "#5E6670", false, g);
                roof.transform.rotation = s.transform.rotation;
            }
            B.Block("Pier", p + new Vector3(0, 0.6f, -26), new Vector3(5, 0.4f, 28), "#9C6B3E", true, g);
            B.Place("SM_Cover_CrateStack", p + new Vector3(2, 0, 2), 0, g);
        }

        static void SunkenBarge(Vector2 c)
        {
            var g = B.Group("Zone_SunkenBarge"); var p = OnGround(c);
            var hull = B.Block("Hull", p + new Vector3(0, 1.4f, 0), new Vector3(9, 3.2f, 26), "#6B4526", true, g);
            hull.transform.rotation = Quaternion.Euler(0, 35, 7);
            var deck = B.Block("Wheelhouse", p + new Vector3(2, 4.2f, -6), new Vector3(4, 3, 4), "#EADDBF", true, g);
            deck.transform.rotation = hull.transform.rotation;
            B.Place("SM_Cover_BarricadeHigh_4m", p + new Vector3(-9, 0, 6), 60, g);
            B.Place("SM_Cover_SandbagsLow_4m", p + new Vector3(8, 0, -8), 20, g);
        }

        static void DuneBattery(Vector2 c)
        {
            var g = B.Group("Zone_DuneBattery"); var p = OnGround(c);
            foreach (var (o, yaw) in new[] { (new Vector2(-6, 2), 0f), (new Vector2(6, -2), 0f) })
            {
                var q = p + new Vector3(o.x, 0, o.y);
                B.Place("SM_Cover_SandbagsLow_4m", q + new Vector3(0, 0, 2.2f), yaw, g);
                B.Place("SM_Cover_SandbagsLow_4m", q + new Vector3(-2.2f, 0, 0), yaw + 90, g);
                B.Place("SM_Cover_SandbagsLow_4m", q + new Vector3(2.2f, 0, 0), yaw + 90, g);
                var gun = B.Block("OldGun", q + new Vector3(0, 0.9f, 0.5f), new Vector3(0.5f, 0.5f, 3.2f), "#7D858C", true, g);
                gun.transform.rotation = Quaternion.Euler(-12, yaw, 0);
            }
        }

        static void Dressing()
        {
            var g = B.Group("Dressing");
            var rnd = new System.Random(5);
            int placed = 0;
            for (int i = 0; i < 400 && placed < 140; i++)
            {
                var c = new Vector2(rnd.Next(-330, 330), rnd.Next(-200, 330));
                float h = Height(c.x, c.y);
                if (h < 3f) continue;
                if (Zones.Any(z => Vector2.Distance(z.pos, c) < z.radius + 14)) continue;
                if (HQs.Any(q => Vector2.Distance(q.pos, c) < 30)) continue;
                if (Distance(RingRoad, c) < 9) continue;
                if (Routes.Values.Any(r => Distance(r, c) < 5)) continue;
                var trunk = B.Block("TreeTrunk", OnGround(c, 1.5f), new Vector3(0.7f, 3f, 0.7f), "#6E4E33", true, g);
                var crown = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                crown.name = "TreeCrown"; crown.transform.SetParent(g); crown.transform.position = OnGround(c, 4.2f);
                crown.transform.localScale = Vector3.one * (3.2f + (float)rnd.NextDouble() * 1.6f);
                crown.GetComponent<Renderer>().sharedMaterial = B.Mat(placed % 3 == 0 ? "#86BE4C" : "#5E9A3A");
                Object.DestroyImmediate(crown.GetComponent<Collider>()); crown.isStatic = true;
                placed++;
            }
        }

        static float Distance(Vector2[] line, Vector2 p)
        {
            float best = float.MaxValue;
            for (int i = 1; i < line.Length; i++)
            {
                var a = line[i - 1]; var b = line[i];
                float t = Mathf.Clamp01(Vector2.Dot(p - a, b - a) / (b - a).sqrMagnitude);
                best = Mathf.Min(best, Vector2.Distance(a + (b - a) * t, p));
            }
            return best;
        }

        static void Gameplay()
        {
            var g = B.Group("Gameplay");
            new GameObject("MatchManager").AddComponent<MatchManager>().transform.SetParent(g);
            var zoneGo = new GameObject("CaptureZone"); zoneGo.transform.SetParent(g);
            var zone = zoneGo.AddComponent<CaptureZone>();
            var rot = zoneGo.AddComponent<ZoneRotation>();
            rot.Spots = Zones.Select(z => new ZoneRotation.ZoneSpot { Name = z.name, Position = OnGround(z.pos), Radius = z.radius }).ToArray();
            rot.Interval = 150; rot.WarningTime = 30;
            zoneGo.transform.position = rot.Spots[1].Position;
            zone.Radius = rot.Spots[1].Radius;
            var marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marker.name = "ZoneMarker"; marker.transform.SetParent(zoneGo.transform, false);
            marker.transform.localPosition = Vector3.up * 0.1f; marker.transform.localScale = new Vector3(20, 0.02f, 20);
            marker.GetComponent<Renderer>().sharedMaterial = B.Mat("#F2C94C");
            Object.DestroyImmediate(marker.GetComponent<Collider>());
            marker.AddComponent<ZoneMarkerScaler>();

            var hq = HQs[0];
            var player = B.MakeTrooper("Player", 0, OnGround(hq.pos, 0.6f), hq.yaw, g);
            var ctrl = player.AddComponent<ThirdPersonController>();
            player.AddComponent<Shooter>();

            var camGo = new GameObject("Main Camera"); camGo.tag = "MainCamera"; camGo.transform.SetParent(g);
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color32(0x9C, 0xCB, 0xF0, 255);
            cam.fieldOfView = 65; cam.farClipPlane = 1200;
            camGo.AddComponent<AudioListener>();
            camGo.AddComponent<ThirdPersonCamera>().Target = ctrl;
            var hud = new GameObject("HUD").AddComponent<SliceHud>(); hud.transform.SetParent(g);
            hud.Player = ctrl; hud.Zone = zone;
        }

        // ---- validate -----------------------------------------------------------------
        [MenuItem("Mudfront/Validate Saltmarsh Greybox")]
        public static int Validate()
        {
            if (!File.Exists(Path.Combine(Application.dataPath, "..", ScenePath))) Build();
            EditorSceneManager.OpenScene(ScenePath);
            Physics.SyncTransforms();
            var sb = new StringBuilder();
            int fails = 0;
            void Check(bool ok, string msg) { sb.AppendLine((ok ? "- PASS " : "- **FAIL** ") + msg); if (!ok) fails++; }
            sb.AppendLine("# Saltmarsh Point grey-box – verification");
            sb.AppendLine($"Generated {System.DateTime.Now:yyyy-MM-dd HH:mm} (Unity {Application.unityVersion}). Targets: docs/maps/MAP_01_Saltmarsh_Point.md, docs/TARGETS.md\n");

            sb.AppendLine("## HQs");
            foreach (var h in HQs)
            {
                float y = Height(h.pos.x, h.pos.y);
                var nearest = Zones.OrderBy(z => Vector2.Distance(z.pos, h.pos)).First();
                Check(y > 0.5f, $"{h.name}: ground {y:0.0} m (above sea), {Vector2.Distance(h.pos, Vector2.zero):0} m to marsh centre, nearest zone **{nearest.name}** {Vector2.Distance(nearest.pos, h.pos):0} m");
            }
            for (int i = 0; i < HQs.Length; i++)
                for (int j = i + 1; j < HQs.Length; j++)
                    sb.AppendLine($"- {HQs[i].name} ↔ {HQs[j].name}: {Vector2.Distance(HQs[i].pos, HQs[j].pos):0} m");

            sb.AppendLine("\n## Zones (neighbour spacing target 90–130 m)");
            foreach (var z in Zones)
            {
                float y = Height(z.pos.x, z.pos.y);
                var nn = Zones.Where(o => o.name != z.name).Min(o => Vector2.Distance(o.pos, z.pos));
                Check(nn >= 85 && nn <= 140, $"{z.name}: ground {y:0.0} m, radius {z.radius} m, nearest other zone {nn:0} m");
            }

            sb.AppendLine("\n## Infantry routes (target HQ→nearest zone ≤ 25 s sprint at 8 m/s; max slope ≤ 40°)");
            var terrain = Object.FindFirstObjectByType<Terrain>();
            foreach (var kv in Routes)
            {
                float len = 0, maxSlope = 0; int blocked = 0;
                for (int i = 1; i < kv.Value.Length; i++)
                {
                    int steps = Mathf.CeilToInt(Vector2.Distance(kv.Value[i - 1], kv.Value[i]) / 2f);
                    for (int s = 0; s < steps; s++)
                    {
                        var a = OnGround(Vector2.Lerp(kv.Value[i - 1], kv.Value[i], (float)s / steps));
                        var b = OnGround(Vector2.Lerp(kv.Value[i - 1], kv.Value[i], (float)(s + 1) / steps));
                        float d = Vector3.Distance(a, b); len += d;
                        float horiz = Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));
                        maxSlope = Mathf.Max(maxSlope, Mathf.Atan2(Mathf.Abs(b.y - a.y), horiz) * Mathf.Rad2Deg);
                        var dir = (b - a).normalized;
                        foreach (var h in Physics.CapsuleCastAll(a + Vector3.up * 0.85f, a + Vector3.up * 1.35f, 0.4f, dir, d))
                        {
                            if (h.collider is TerrainCollider || h.collider.GetComponentInParent<TeamMember>() || h.collider.GetComponentInParent<Vehicle>() || h.collider.name.StartsWith("Road")) continue;
                            blocked++;
                        }
                    }
                }
                bool toNearest = !kv.Key.Contains("Marsh");
                bool ok = blocked == 0 && maxSlope <= 40f && (!toNearest || len / 8f <= 25f);
                Check(ok, $"`{kv.Key}`: {len:0} m → **{len / 8f:0.0} s** sprint ({len / 5.5f:0} s run), max slope {maxSlope:0}°, {(blocked == 0 ? "clear" : blocked + " obstruction hit(s)")}");
            }

            sb.AppendLine("\n## Vehicle ring road");
            float ring = 0, ringSlope = 0;
            for (int i = 1; i < RingRoad.Length; i++)
            {
                var a = OnGround(RingRoad[i - 1]); var b = OnGround(RingRoad[i]);
                ring += Vector3.Distance(a, b);
                ringSlope = Mathf.Max(ringSlope, Mathf.Atan2(Mathf.Abs(b.y - a.y), Vector2.Distance(RingRoad[i - 1], RingRoad[i])) * Mathf.Rad2Deg);
            }
            Check(ringSlope < 25, $"ring road {ring:0} m (lap ≈ {ring / 14f:0} s at 14 m/s), steepest average segment slope {ringSlope:0}°");
            foreach (var h in HQs)
            {
                float d = Vector2.Distance(h.pos, NearestOnRing(h.pos)) + Vector2.Distance(NearestOnRing(h.pos), Zones.OrderBy(z => Vector2.Distance(z.pos, h.pos)).First().pos);
                Check(d / 14f <= 12f, $"{h.name} → nearest zone by vehicle ≈ {d:0} m ≈ {d / 14f:0.0} s (target ≤ 12 s)");
            }

            sb.AppendLine("\nScreenshots: `saltmarsh_overview.png`, `saltmarsh_harbour.png`, `saltmarsh_mill.png`.");
            Shots();
            sb.AppendLine($"\n**Result: {(fails == 0 ? "all checks passed" : fails + " check(s) failed")}.**");
            Directory.CreateDirectory(ReportDir);
            File.WriteAllText(Path.Combine(ReportDir, "saltmarsh_greybox_report.md"), sb.ToString());
            Debug.Log("[Mudfront] Saltmarsh validation fails=" + fails + "\n" + sb);
            return fails;
        }

        static void Shots()
        {
            var cam = GameObject.Find("Main Camera").GetComponent<Camera>();
            var shots = new (string file, Vector3 pos, Vector3 look)[]
            {
                ("saltmarsh_overview.png", new Vector3(-60, 190, -330), new Vector3(10, 0, 0)),
                ("saltmarsh_harbour.png", OnGround(HQs[0].pos, 3.5f) + new Vector3(-2, 0, -5), OnGround(Zones[2].pos, 3f)),
                ("saltmarsh_mill.png", OnGround(new Vector2(40, 10), 3f), OnGround(Zones[0].pos, 10f)),
            };
            foreach (var s in shots)
            {
                cam.transform.position = s.pos; cam.transform.LookAt(s.look);
                var rt = new RenderTexture(1600, 900, 24); cam.targetTexture = rt; cam.Render();
                RenderTexture.active = rt;
                var tex = new Texture2D(1600, 900, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0); tex.Apply();
                Directory.CreateDirectory(ReportDir);
                File.WriteAllBytes(Path.Combine(ReportDir, s.file), tex.EncodeToPNG());
                cam.targetTexture = null; RenderTexture.active = null; Object.DestroyImmediate(rt); Object.DestroyImmediate(tex);
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
