using UnityEngine;

namespace Mudfront
{
    public class SliceHud : MonoBehaviour
    {
        public ThirdPersonController Player;
        public CaptureZone Zone;
        GUIStyle _big, _small;

        void OnGUI()
        {
            if (_big == null)
            {
                _big = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold };
                _small = new GUIStyle(GUI.skin.label) { fontSize = 16 };
            }
            float w = Screen.width, h = Screen.height;
            GUI.Label(new Rect(w / 2 - 6, h / 2 - 14, 20, 30), "+", _big);

            var m = MatchManager.Instance;
            if (m != null)
            {
                for (int i = 0; i < m.Scores.Length; i++)
                {
                    GUI.color = TeamColors.Get(i);
                    GUI.Label(new Rect(20 + i * 170, 14, 170, 30), $"{TeamColors.Names[i]} {m.Scores[i]:0}", _big);
                }
                GUI.color = Color.white;
                if (m.Winner >= 0) GUI.Label(new Rect(w / 2 - 150, 60, 400, 40), $"{TeamColors.Names[m.Winner]} WIN!", _big);
            }
            if (Zone != null)
            {
                string s = Zone.Owner >= 0 ? $"Zone: {TeamColors.Names[Zone.Owner]}" : "Zone: neutral";
                if (Zone.Contested) s += "  (CONTESTED)";
                else if (Zone.CapturingTeam >= 0) s += $"  capturing {Zone.Progress * 100:0}%";
                GUI.Label(new Rect(20, 48, 600, 30), s, _small);
            }
            if (Player != null)
            {
                var hp = Player.GetComponent<Health>();
                var sh = Player.GetComponent<Shooter>();
                var rs = Player.GetComponent<Respawner>();
                string line = hp != null ? $"HP {hp.Current:0}" : "";
                if (sh != null) line += sh.Reloading ? "   reloading..." : $"   Ammo {sh.Ammo}/{sh.MagazineSize}";
                GUI.Label(new Rect(20, h - 40, 400, 30), line, _big);
                if (rs != null && rs.IsWaiting) GUI.Label(new Rect(w / 2 - 80, h / 2 + 40, 300, 40), "Respawning...", _big);
                string hint = Player.CurrentVehicle != null ? "W/S drive  A/D steer  E exit" : "WASD move  Shift sprint  Space jump  LMB shoot  R reload  E vehicle";
                GUI.Label(new Rect(w - 560, h - 32, 560, 30), hint, _small);
            }
        }
    }
}
