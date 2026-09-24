using UnityEngine;

namespace AIGames.Pathfinding.Gameplay
{
    /// <summary>
    /// Enemy guard. Exercise 1: walks along its patrol path, using A* between the points
    /// and waiting a moment at each one.
    /// </summary>
    [RequireComponent(typeof(PathMover))]
    public class EnemyAI : MonoBehaviour
    {
        public PatrolPath patrol;
        public float patrolSpeed = 2.2f;
        public float waitAtPoint = 1.5f;

        PathMover mover;
        int currentPoint;
        float waitUntil;
        bool goingToPoint;

        void Start()
        {
            mover = GetComponent<PathMover>();
            if (patrol == null || patrol.Count == 0)
            {
                Debug.LogError($"{name}: patrol path is missing");
                enabled = false;
                return;
            }
            currentPoint = patrol.NearestIndex(transform.position);
        }

        void Update()
        {
            Patrol();
        }

        void Patrol()
        {
            mover.speed = patrolSpeed;
            if (goingToPoint)
            {
                if (mover.IsMoving)
                    return;
                // Arrived: wait, then continue with the next point of the loop.
                goingToPoint = false;
                waitUntil = Time.time + waitAtPoint;
                currentPoint = (currentPoint + 1) % patrol.Count;
            }
            else if (Time.time >= waitUntil)
            {
                mover.MoveTo(patrol[currentPoint]);
                goingToPoint = true;
            }
        }
    }
}
