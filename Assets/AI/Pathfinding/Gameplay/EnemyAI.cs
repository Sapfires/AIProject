using UnityEngine;

namespace AIGames.Pathfinding.Gameplay
{
    /// <summary>
    /// Enemy guard - a small state machine on top of the A* PathMover.
    ///   Patrol (exercise 1): walks along its patrol path, using A* between the points.
    ///   Chase  (exercise 5): the player was noticed by the field of view (exercise 3) or the
    ///                        detection collider (exercise 4) - run to the player, re-planning
    ///                        the path while the player moves.
    ///   Search (own extension): the player was lost - go to the last known position and look
    ///                        around, then return to the patrol.
    /// </summary>
    [RequireComponent(typeof(PathMover))]
    public class EnemyAI : MonoBehaviour
    {
        public enum State { Patrol, Chase, Search }

        public PatrolPath patrol;
        public PointOfView pointOfView;
        public Detection detection;

        [Header("Patrol")]
        public float patrolSpeed = 2.2f;
        public float waitAtPoint = 1.5f;

        [Header("Chase")]
        public float chaseSpeed = 3.8f;
        public float repathInterval = 0.3f;
        public float loseSightTime = 1.2f;
        public float catchDistance = 0.9f;

        [Header("Search")]
        public float searchTime = 3f;
        public float lookAroundSpeed = 120f;

        PathMover mover;
        Transform player;
        int currentPoint;
        float waitUntil;
        bool goingToPoint;
        float nextRepath;
        float lastNoticed;
        float searchUntil;
        bool searching;
        Vector3 lastKnownPosition;

        public State CurrentState { get; private set; } = State.Patrol;
        public event System.Action<EnemyAI> CaughtPlayer;

        public bool PlayerNoticed =>
            (pointOfView != null && pointOfView.CanSeeTarget) ||
            (detection != null && detection.PlayerDetected);

        void Start()
        {
            mover = GetComponent<PathMover>();
            player = pointOfView != null ? pointOfView.target : null;
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
                lastNoticed = Time.time;
                lastKnownPosition = player.position;
                if (CurrentState != State.Chase)
                    Enter(State.Chase);
            }

            switch (CurrentState)
            {
                case State.Patrol: Patrol(); break;
                case State.Chase: Chase(); break;
                case State.Search: Search(); break;
            }

            if (pointOfView != null)
                pointOfView.Alert = CurrentState != State.Patrol;
        }

        void Enter(State state)
        {
            CurrentState = state;
            mover.Stop();
            goingToPoint = false;
            searching = false;
            nextRepath = 0;
            if (state == State.Patrol)
                currentPoint = patrol.NearestIndex(transform.position);
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

        void Chase()
        {
            mover.speed = chaseSpeed;

            if (Vector3.Distance(transform.position, player.position) < catchDistance)
            {
                CaughtPlayer?.Invoke(this);
                Enter(State.Search);
                return;
            }

            if (Time.time - lastNoticed > loseSightTime)
            {
                Enter(State.Search);
                return;
            }

            // Exercise 5: follow the player - request a new path to its current position.
            if (Time.time >= nextRepath)
            {
                nextRepath = Time.time + repathInterval;
                mover.MoveTo(lastKnownPosition);
            }
        }

        void Search()
        {
            mover.speed = patrolSpeed * 1.3f;
            if (!searching)
            {
                if (!mover.IsMoving && !goingToPoint)
                {
                    mover.MoveTo(lastKnownPosition);
                    goingToPoint = true;
                }
                else if (goingToPoint && !mover.IsMoving)
                {
                    searching = true;
                    searchUntil = Time.time + searchTime;
                }
                return;
            }

            // Look around at the last known position, then give up.
            transform.Rotate(0, lookAroundSpeed * Time.deltaTime, 0);
            if (Time.time >= searchUntil)
                Enter(State.Patrol);
        }
    }
}
