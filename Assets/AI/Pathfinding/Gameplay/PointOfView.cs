using System.Collections.Generic;
using UnityEngine;

namespace AIGames.Pathfinding.Gameplay
{
    /// <summary>
    /// Exercise 3: the enemy's field of view. The player is seen when it is within the view
    /// radius, inside the view angle and no wall blocks the line of sight. The view cone is
    /// drawn as a mesh on the floor, clipped by the walls, and turns red while the player is seen.
    /// </summary>
    public class PointOfView : MonoBehaviour
    {
        public Transform target;
        public float viewRadius = 7f;
        [Range(10, 360)] public float viewAngle = 100f;
        public float eyeHeight = 1.4f;
        public LayerMask obstacleMask;

        [Header("Visualisation")]
        public MeshFilter viewMeshFilter;
        public float degreesPerRay = 2f;
        public Color calmColor = new Color(1f, 0.85f, 0.3f, 0.25f);
        public Color alertColor = new Color(1f, 0.2f, 0.15f, 0.4f);

        Mesh viewMesh;
        MaterialPropertyBlock block;
        MeshRenderer viewRenderer;
        readonly List<Vector3> points = new List<Vector3>();

        public bool CanSeeTarget { get; private set; }
        public Vector3 LastSeenPosition { get; private set; }
        /// <summary>Forces the alert colour (e.g. while chasing) even without sight.</summary>
        public bool Alert { get; set; }

        void Start()
        {
            if (viewMeshFilter != null)
            {
                viewMesh = new Mesh { name = "View Mesh" };
                viewMeshFilter.mesh = viewMesh;
                viewRenderer = viewMeshFilter.GetComponent<MeshRenderer>();
                block = new MaterialPropertyBlock();
            }
        }

        void Update()
        {
            CanSeeTarget = CheckTarget();
            if (CanSeeTarget)
                LastSeenPosition = target.position;
        }

        bool CheckTarget()
        {
            if (target == null)
                return false;
            Vector3 eye = transform.position + Vector3.up * eyeHeight;
            Vector3 toTarget = target.position + Vector3.up * eyeHeight - eye;
            if (toTarget.sqrMagnitude > viewRadius * viewRadius)
                return false;
            Vector3 flat = new Vector3(toTarget.x, 0, toTarget.z);
            if (Vector3.Angle(transform.forward, flat) > viewAngle / 2)
                return false;
            return !Physics.Raycast(eye, toTarget.normalized, toTarget.magnitude, obstacleMask);
        }

        void LateUpdate()
        {
            if (viewMesh == null)
                return;
            DrawViewMesh();
            block.SetColor("_BaseColor", CanSeeTarget || Alert ? alertColor : calmColor);
            viewRenderer.SetPropertyBlock(block);
        }

        void DrawViewMesh()
        {
            int rayCount = Mathf.Max(2, Mathf.RoundToInt(viewAngle / degreesPerRay));
            float step = viewAngle / rayCount;
            Vector3 origin = transform.position + Vector3.up * 0.5f;

            points.Clear();
            for (int i = 0; i <= rayCount; i++)
            {
                float angle = transform.eulerAngles.y - viewAngle / 2 + step * i;
                Vector3 dir = new Vector3(Mathf.Sin(angle * Mathf.Deg2Rad), 0, Mathf.Cos(angle * Mathf.Deg2Rad));
                float distance = Physics.Raycast(origin, dir, out RaycastHit hit, viewRadius, obstacleMask) ? hit.distance : viewRadius;
                points.Add(origin + dir * distance);
            }

            // Triangle fan in the local space of the mesh object, slightly above the floor.
            Transform meshTransform = viewMeshFilter.transform;
            var vertices = new Vector3[points.Count + 1];
            var triangles = new int[(points.Count - 1) * 3];
            vertices[0] = Flatten(meshTransform.InverseTransformPoint(origin));
            for (int i = 0; i < points.Count; i++)
            {
                vertices[i + 1] = Flatten(meshTransform.InverseTransformPoint(points[i]));
                if (i < points.Count - 1)
                {
                    triangles[i * 3] = 0;
                    triangles[i * 3 + 1] = i + 1;
                    triangles[i * 3 + 2] = i + 2;
                }
            }
            viewMesh.Clear();
            viewMesh.vertices = vertices;
            viewMesh.triangles = triangles;
            viewMesh.RecalculateBounds();
        }

        static Vector3 Flatten(Vector3 local) => new Vector3(local.x, 0.03f, local.z);

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, viewRadius);
            Vector3 a = Quaternion.Euler(0, -viewAngle / 2, 0) * transform.forward;
            Vector3 b = Quaternion.Euler(0, viewAngle / 2, 0) * transform.forward;
            Gizmos.DrawLine(transform.position, transform.position + a * viewRadius);
            Gizmos.DrawLine(transform.position, transform.position + b * viewRadius);
        }
    }
}
