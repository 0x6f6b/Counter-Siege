using UnityEngine;

namespace CounterSiege
{
    [ExecuteAlways]
    public class Billboard : MonoBehaviour
    {
        public bool yAxisOnly = true;

        void LateUpdate()
        {
            var cam = Camera.main;
#if UNITY_EDITOR
            if (cam == null && !Application.isPlaying)
            {
                var sv = UnityEditor.SceneView.lastActiveSceneView;
                if (sv != null) cam = sv.camera;
            }
#endif
            if (cam == null) return;

            if (yAxisOnly)
            {
                var fwd = cam.transform.position - transform.position;
                fwd.y = 0f;
                if (fwd.sqrMagnitude < 0.0001f) return;
                transform.rotation = Quaternion.LookRotation(-fwd.normalized, Vector3.up);
            }
            else
            {
                transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position);
            }
        }
    }
}
