using UnityEngine;

namespace Mudfront
{
    /// <summary>Keeps the flat zone disc matched to the zone radius and tinted to the owner.</summary>
    public class ZoneMarkerScaler : MonoBehaviour
    {
        CaptureZone _zone;
        Renderer _r;
        MaterialPropertyBlock _mpb;

        void Awake()
        {
            _zone = GetComponentInParent<CaptureZone>();
            _r = GetComponent<Renderer>();
            _mpb = new MaterialPropertyBlock();
        }

        void LateUpdate()
        {
            if (_zone == null) return;
            float d = _zone.Radius * 2f;
            transform.localScale = new Vector3(d, 0.02f, d);
            _mpb.SetColor("_BaseColor", _zone.Owner >= 0 ? TeamColors.Get(_zone.Owner) : new Color(0.95f, 0.79f, 0.3f));
            _r.SetPropertyBlock(_mpb);
        }
    }
}
