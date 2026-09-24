using UnityEngine;

namespace AIGames.Pathfinding.Gameplay
{
    /// <summary>
    /// Enemy guard.
    /// Exercise 1: walks along its patrol path, using A* between the points.
    /// Exercise 3/4: notices the player with its field of view or the detection collider;
    /// when alerted it stops and turns towards the player.
    /// </summary>
    [RequireComponent(typeof(PathMover))]
    public class EnemyAI : MonoBehaviour
    {
        public enum State { Patrol, Alert }

        public PatrolPath patrol;
        public PointOfView pointOfView;
        public Detection detection;
        public float patrolSpeed = 2.2f;
        public float waitAtPoint = 1.5f;
        public float turnSpeed = 6f;

        PathMover mover;
        int currentPoint;
        float waitUntil;
        bool goingToPoint;

        public State CurrentState { get; private set; } = State.Patrol;

        public bool PlayerNoticed =>
            (pointOfView != null && pointOfView.CanSeeTarget) ||
            (detection != null && detection.PlayerDetected);

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
            if (PlayerNoticed)
            {
                if (CurrentState != State.Alert)
                {
                    CurrentState = State.Alert;
                    mover.Stop();
                    goingToPoint = false;
                }
                FacePlayer();
            }
            else
            {
                CurrentState = State.Patrol;
                Patrol();
            }
            if (pointOfView != null)
                pointOfView.Alert = CurrentState != State.Patrol;
        }

        void FacePlayer()
        {
            Vector3 d = pointOfView.target.position - transform.position;
            d.y = 0;
            if (d.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(d), turnSpeed * Time.deltaTime);
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
