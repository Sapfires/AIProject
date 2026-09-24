using UnityEngine;

namespace AIGames.Pathfinding.Gameplay
{
    /// <summary>
    /// Moves a character along A* paths from the PathRequestManager, turns it towards the
    /// next waypoint, feeds the animator with the current speed and draws the remaining path.
    /// Shared by the enemy and the player.
    /// </summary>
    public class PathMover : MonoBehaviour
    {
        public float speed = 3f;
        public float turnSpeed = 10f;
        public LineRenderer pathLine;
        public Animator animator;

        Vector3[] path = new Vector3[0];
        int index;
        Vector3 lastPosition;
        bool requestPending;

        public bool IsMoving => index < path.Length || requestPending;
        public Vector3 Destination { get; private set; }

        void Start()
        {
            lastPosition = transform.position;
        }

        public void MoveTo(Vector3 target)
        {
            Destination = target;
            requestPending = true;
            PathRequestManager.RequestPath(transform.position, target, OnPathFound);
        }

        public void Stop()
        {
            path = new Vector3[0];
            index = 0;
            requestPending = false;
        }

        void OnPathFound(Vector3[] newPath, bool success)
        {
            if (this == null)
                return;
            requestPending = false;
            path = success ? newPath : new Vector3[0];
            index = 0;
        }

        void Update()
        {
            if (index < path.Length)
            {
                Vector3 waypoint = path[index];
                waypoint.y = transform.position.y;
                Vector3 toWaypoint = waypoint - transform.position;
                float step = speed * Time.deltaTime;

                if (toWaypoint.magnitude <= step)
                {
                    transform.position = waypoint;
                    index++;
                }
                else
                {
                    transform.position += toWaypoint.normalized * step;
                    transform.rotation = Quaternion.Slerp(transform.rotation,
                        Quaternion.LookRotation(toWaypoint), turnSpeed * Time.deltaTime);
                }
            }

            if (animator != null && Time.deltaTime > 0)
            {
                float currentSpeed = (transform.position - lastPosition).magnitude / Time.deltaTime;
                animator.SetFloat("Speed", currentSpeed, 0.1f, Time.deltaTime);
            }
            lastPosition = transform.position;
            DrawPath();
        }

        void DrawPath()
        {
            if (pathLine == null)
                return;
            int remaining = Mathf.Max(0, path.Length - index);
            pathLine.positionCount = remaining == 0 ? 0 : remaining + 1;
            if (remaining == 0)
                return;
            pathLine.SetPosition(0, transform.position + Vector3.up * 0.05f);
            for (int i = 0; i < remaining; i++)
                pathLine.SetPosition(i + 1, new Vector3(path[index + i].x, 0.05f, path[index + i].z));
        }
    }
}
