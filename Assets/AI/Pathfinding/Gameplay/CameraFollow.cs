using UnityEngine;
using UnityEngine.InputSystem;

namespace AIGames.Pathfinding.Gameplay
{
    /// <summary>
    /// Exercise 2: gameplay camera.
    /// Overview mode (default) fits the whole level into the view for any screen aspect;
    /// follow mode smoothly follows the player. Tab switches the modes, the mouse wheel zooms.
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        public Transform target;
        public Vector3 offset = new Vector3(0, 15, -10);
        public float smoothTime = 0.25f;
        [Range(0.4f, 2.5f)] public float zoom = 1f;

        [Header("Overview")]
        public bool overview = true;
        public Bounds levelBounds = new Bounds(Vector3.zero, new Vector3(32, 2, 32));
        [Range(30, 90)] public float overviewPitch = 60f;
        [Range(0f, 0.2f)] public float overviewMargin = 0.03f;

        Vector3 velocity;
        Camera cam;

        void Awake()
        {
            cam = GetComponent<Camera>();
        }

        void LateUpdate()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.tabKey.wasPressedThisFrame)
                overview = !overview;

            var mouse = Mouse.current;
            if (mouse != null)
                zoom = Mathf.Clamp(zoom - mouse.scroll.ReadValue().y * 0.001f, 0.4f, 2.5f);

            if (overview)
            {
                Vector3 desiredOverview = OverviewPosition();
                transform.rotation = Quaternion.Euler(overviewPitch, 0, 0);
                transform.position = Vector3.SmoothDamp(transform.position, desiredOverview, ref velocity, smoothTime);
                return;
            }

            if (target == null)
                return;
            Vector3 desired = target.position + offset * zoom;
            transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothTime);
            transform.LookAt(target.position + Vector3.up);
        }

        /// <summary>
        /// Looks at the level centre from the overview angle and finds (binary search) the
        /// smallest distance at which all corners of the level are inside the viewport.
        /// </summary>
        Vector3 OverviewPosition()
        {
            var rotation = Quaternion.Euler(overviewPitch, 0, 0);
            Vector3 back = rotation * Vector3.back;
            Vector3 saved = transform.position;
            Quaternion savedRotation = transform.rotation;
            transform.rotation = rotation;

            float near = 1f, far = 500f;
            for (int i = 0; i < 24; i++)
            {
                float mid = (near + far) / 2;
                transform.position = levelBounds.center + back * mid;
                if (AllCornersVisible())
                    far = mid;
                else
                    near = mid;
            }

            transform.SetPositionAndRotation(saved, savedRotation);
            return levelBounds.center + back * far * zoom;
        }

        bool AllCornersVisible()
        {
            Vector3 min = levelBounds.min, max = levelBounds.max;
            for (int i = 0; i < 8; i++)
            {
                var corner = new Vector3((i & 1) == 0 ? min.x : max.x, (i & 2) == 0 ? min.y : max.y, (i & 4) == 0 ? min.z : max.z);
                Vector3 vp = cam.WorldToViewportPoint(corner);
                if (vp.z <= 0 || vp.x < overviewMargin || vp.x > 1 - overviewMargin || vp.y < overviewMargin || vp.y > 1 - overviewMargin)
                    return false;
            }
            return true;
        }

        public void SnapToTarget()
        {
            if (cam == null)
                cam = GetComponent<Camera>();
            if (overview)
            {
                transform.rotation = Quaternion.Euler(overviewPitch, 0, 0);
                transform.position = OverviewPosition();
                return;
            }
            transform.position = target.position + offset * zoom;
            transform.LookAt(target.position + Vector3.up);
        }
    }
}
