using AIGames.Common;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AIGames.Pathfinding
{
    /// <summary>
    /// Moves the target between patrol points on its own, walking around the walls on an
    /// A* path; a left mouse click on the floor places the target manually. Units re-plan
    /// their paths whenever it moves.
    /// </summary>
    public class TargetMover : MonoBehaviour
    {
        public Transform[] patrolPoints;
        public float speed = 3f;
        public float waitTime = 2f;
        public LayerMask floorMask = ~0;

        int index;
        float waitUntil;
        bool manual;
        bool waitingForPath;
        Vector3[] route = new Vector3[0];
        int routeIndex;

        void Update()
        {
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
                if (Physics.Raycast(ray, out RaycastHit hit, 200, floorMask))
                {
                    transform.position = new Vector3(hit.point.x, transform.position.y, hit.point.z);
                    manual = true;
                }
            }

            if (!manual && patrolPoints.Length > 0)
                Patrol();

            DemoHUD.Show("Pathfinder 4 - Units and weights",
                "Units follow simplified A* paths to a moving target\n" +
                $"Path requests processed: {PathRequestManager.ProcessedRequests}, queued: {PathRequestManager.PendingRequests}\n" +
                "Brown = mud and wall margins (higher cost)\n" +
                "Left click places the target");
        }

        void Patrol()
        {
            if (routeIndex < route.Length)
            {
                Vector3 goal = route[routeIndex];
                goal.y = transform.position.y;
                transform.position = Vector3.MoveTowards(transform.position, goal, speed * Time.deltaTime);
                if ((transform.position - goal).sqrMagnitude < 0.001f && ++routeIndex == route.Length)
                    waitUntil = Time.time + waitTime;
                return;
            }

            if (!waitingForPath && Time.time >= waitUntil)
            {
                // The target walks around walls too: ask A* for a route to the next patrol point.
                waitingForPath = true;
                PathRequestManager.RequestPath(transform.position, patrolPoints[index].position, OnRouteFound);
            }
        }

        void OnRouteFound(Vector3[] path, bool success)
        {
            waitingForPath = false;
            index = (index + 1) % patrolPoints.Length;
            route = success ? path : new Vector3[0];
            routeIndex = 0;
        }
    }
}
