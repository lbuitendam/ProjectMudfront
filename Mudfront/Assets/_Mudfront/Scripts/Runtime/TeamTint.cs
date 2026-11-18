using UnityEngine;

namespace Mudfront
{
    /// <summary>
    /// Recolours the faction materials (named *tidewatch*) of a character model to its team colour at runtime.
    /// MaterialPropertyBlocks are not saved in scenes, so this must run on Awake.
    /// </summary>
    public class TeamTint : MonoBehaviour
    {
        void Awake() => Apply();

        public void Apply()
        {
            var tm = GetComponentInParent<TeamMember>();
            if (tm == null) return;
            var col = TeamColors.Get(tm.TeamId);
            foreach (var r in GetComponentsInChildren<Renderer>())
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] == null || !mats[i].name.Contains("tidewatch")) continue;
                    string n = mats[i].name;
                    float k = n.Contains("dark") ? 0.62f : n.Contains("accent") ? 1.25f : 1f;
                    if (n.EndsWith("_dk")) k *= 0.8f; else if (n.EndsWith("_lt")) k *= 1.13f;
                    var mpb = new MaterialPropertyBlock();
                    r.GetPropertyBlock(mpb, i);
                    mpb.SetColor("_BaseColor", new Color(Mathf.Clamp01(col.r * k), Mathf.Clamp01(col.g * k), Mathf.Clamp01(col.b * k)));
                    r.SetPropertyBlock(mpb, i);
                }
            }
        }
    }
}
