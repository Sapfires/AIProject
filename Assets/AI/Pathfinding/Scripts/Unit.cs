using System.Collections;
using UnityEngine;

namespace AIGames.Pathfinding
{
    /// <summary>
    /// A unit that asks the PathRequestManager for a path to its target and follows the
    /// waypoints. When the target moves far enough the path is requested again.
    /// </summary>
    public class Unit : MonoBehaviour
    {
        public Transform target;
        public float speed = 6f;
        public float turnSpeed = 8f;
        public float pathUpdateMoveThreshold = 0.5f;
        public float minPathUpdateTime = 0.25f;
        public LineRenderer pathLine;
        [Tooltip("Optional character animator, receives the current speed as the Speed parameter.")]
        public Animator animator;
        [Tooltip("Height of the unit's pivot above the floor (capsule centre = 1, character feet = 0).")]
        public float pivotHeight = 1f;

        Vector3 lastPosition;

        Vector3[] path = new Vector3[0];
        int targetIndex;
        Coroutine followRoutine;

        void Start()
        {
            lastPosition = transform.position;
            StartCoroutine(UpdatePath());
        }

        IEnumerator UpdatePath()
        {
            // Small random delay so all units do not request on the same frame.
            yield return new WaitForSeconds(Random.Range(0f, 0.3f));
            PathRequestManager.RequestPath(transform.position, target.position, OnPathFound);

            float sqrThreshold = pathUpdateMoveThreshold * pathUpdateMoveThreshold;
            Vector3 targetPosOld = target.position;
            while (true)
            {
                yield return new WaitForSeconds(minPathUpdateTime);
                if ((target.position - targetPosOld).sqrMagnitude > sqrThreshold)
                {
                    PathRequestManager.RequestPath(transform.position, target.position, OnPathFound);
                    targetPosOld = target.position;
                }
            }
        }

        void OnPathFound(Vector3[] newPath, bool success)
        {
            if (!success || this == null)
                return;
            path = newPath;
            targetIndex = 0;
            if (followRoutine != null)
                StopCoroutine(followRoutine);
            followRoutine = StartCoroutine(FollowPath());
        }

        IEnumerator FollowPath()
        {
            while (targetIndex < path.Length)
            {
                Vector3 waypoint = path[targetIndex];
                waypoint.y = transform.position.y;
                Vector3 toWaypoint = waypoint - transform.position;

                if (toWaypoint.sqrMagnitude < 0.01f)
                {
                    targetIndex++;
                    continue;
                }

                Quaternion look = Quaternion.LookRotation(toWaypoint);
                transform.rotation = Quaternion.Slerp(transform.rotation, look, turnSpeed * Time.deltaTime);
                transform.position = Vector3.MoveTowards(transform.position, waypoint, speed * Time.deltaTime);
                yield return null;
            }
        }

        void LateUpdate()
        {
            if (animator != null && Time.deltaTime > 0)
                animator.SetFloat("Speed", (transform.position - lastPosition).magnitude / Time.deltaTime, 0.1f, Time.deltaTime);
            lastPosition = transform.position;

            if (pathLine == null)
                return;
            int remaining = Mathf.Max(0, path.Length - targetIndex);
            pathLine.positionCount = remaining + 1;
            pathLine.SetPosition(0, transform.position + Vector3.down * (pivotHeight - 0.1f));
            for (int i = 0; i < remaining; i++)
                pathLine.SetPosition(i + 1, path[targetIndex + i] + Vector3.up * 0.1f);
        }

        void OnDrawGizmos()
        {
            for (int i = targetIndex; i < path.Length; i++)
            {
                Gizmos.color = Color.black;
                Gizmos.DrawCube(path[i], Vector3.one * 0.3f);
                Gizmos.DrawLine(i == targetIndex ? transform.position : path[i - 1], path[i]);
            }
        }
    }
}
