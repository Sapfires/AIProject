using UnityEngine;

namespace AIGames.Swarm
{
    /// <summary>
    /// Periodically hunts: chases the boid closest to it, then rests and wanders.
    /// Boids inside its flee radius steer away from it.
    /// </summary>
    public class Predator : MonoBehaviour
    {
        public SwarmManager swarm;
        public float huntSpeed = 8f;
        public float wanderSpeed = 2.5f;
        public float huntDuration = 6f;
        public float restDuration = 4f;
        public float turnSpeed = 3f;
        public Vector3 arenaHalfSize = new Vector3(18, 5, 18);
        public Vector3 arenaCentre = new Vector3(0, 6, 0);

        float stateTimer;
        bool hunting;
        Vector3 wanderPoint;

        void Start()
        {
            stateTimer = restDuration;
            PickWanderPoint();
        }

        void Update()
        {
            stateTimer -= Time.deltaTime;
            if (stateTimer <= 0)
            {
                hunting = !hunting;
                stateTimer = hunting ? huntDuration : restDuration;
                PickWanderPoint();
            }

            Vector3 goal = hunting ? ClosestBoidPosition() : wanderPoint;
            if (!hunting && (goal - transform.position).sqrMagnitude < 1f)
                PickWanderPoint();

            Vector3 dir = goal - transform.position;
            if (dir.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), turnSpeed * Time.deltaTime);
            transform.position += transform.forward * (hunting ? huntSpeed : wanderSpeed) * Time.deltaTime;
        }

        Vector3 ClosestBoidPosition()
        {
            Vector3 best = swarm.SwarmCentre();
            float bestDst = float.MaxValue;
            foreach (Boid b in swarm.Boids)
            {
                float d = (b.Position - transform.position).sqrMagnitude;
                if (d < bestDst)
                {
                    bestDst = d;
                    best = b.Position;
                }
            }
            return best;
        }

        void PickWanderPoint()
        {
            wanderPoint = arenaCentre + Vector3.Scale(Random.insideUnitSphere, arenaHalfSize);
        }
    }
}
