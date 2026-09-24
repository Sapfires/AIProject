using UnityEngine;

namespace AIGames.Swarm
{
    /// <summary>
    /// One member of the swarm. The SwarmManager fills in what the boid perceives
    /// (neighbour averages) and the boid turns that into a steering force.
    /// </summary>
    public class Boid : MonoBehaviour
    {
        // Perception, written by the SwarmManager each frame.
        [HideInInspector] public Vector3 flockHeading;
        [HideInInspector] public Vector3 flockCentre;
        [HideInInspector] public Vector3 separationHeading;
        [HideInInspector] public int perceivedFlockmates;

        public Vector3 Velocity { get; private set; }
        public Vector3 Position => cachedTransform.position;
        public Vector3 Forward => cachedTransform.forward;
        public bool IsFleeing { get; private set; }
        public bool IsAvoiding { get; private set; }

        SwarmSettings settings;
        Transform cachedTransform;
        Renderer[] renderers;
        MaterialPropertyBlock block;
        Color? baseColor;
        int shownState = -1;
        readonly Collider[] nearbyObstacles = new Collider[8];

        /// <param name="color">Tint in the normal state, or null to keep the model's own materials.</param>
        public void Initialize(SwarmSettings swarmSettings, Color? color)
        {
            settings = swarmSettings;
            cachedTransform = transform;
            renderers = GetComponentsInChildren<Renderer>();
            block = new MaterialPropertyBlock();
            baseColor = color;
            float startSpeed = (settings.minSpeed + settings.maxSpeed) / 2;
            Velocity = cachedTransform.forward * startSpeed;
            ShowState(0);
        }

        public void UpdateBoid(Transform target, Transform predator, float deltaTime)
        {
            Vector3 acceleration = Vector3.zero;

            if (target != null)
                acceleration += SteerTowards(target.position - Position) * settings.targetWeight;

            if (perceivedFlockmates > 0)
            {
                Vector3 centreOffset = flockCentre / perceivedFlockmates - Position;
                acceleration += SteerTowards(flockHeading) * settings.alignmentWeight;
                acceleration += SteerTowards(centreOffset) * settings.cohesionWeight;
                acceleration += SteerTowards(separationHeading) * settings.separationWeight;
            }

            IsFleeing = false;
            if (predator != null)
            {
                Vector3 away = Position - predator.position;
                if (away.sqrMagnitude < settings.fleeRadius * settings.fleeRadius)
                {
                    acceleration += SteerTowards(away) * settings.fleeWeight;
                    IsFleeing = true;
                }
            }

            // Look ahead: steer to a free direction before hitting an obstacle.
            IsAvoiding = IsHeadingForCollision();
            if (IsAvoiding)
                acceleration += SteerTowards(ObstacleRays()) * settings.avoidCollisionWeight;

            // Safety net: push away from surfaces that are already very close
            // (e.g. when the target or the predator pulls the boid towards a rock).
            Vector3 repulsion = ObstacleRepulsion();
            if (repulsion != Vector3.zero)
            {
                acceleration += SteerTowards(repulsion) * settings.avoidCollisionWeight;
                IsAvoiding = true;
            }

            Vector3 velocity = Velocity + acceleration * deltaTime;
            float speed = Mathf.Clamp(velocity.magnitude, settings.minSpeed, IsFleeing ? settings.maxSpeed * 1.5f : settings.maxSpeed);
            Vector3 dir = velocity.sqrMagnitude > 0 ? velocity.normalized : Forward;
            Velocity = dir * speed;

            cachedTransform.position += Velocity * deltaTime;
            cachedTransform.forward = dir;

            ShowState(IsFleeing ? 2 : IsAvoiding ? 1 : 0);
        }

        Vector3 SteerTowards(Vector3 vector)
        {
            if (vector.sqrMagnitude < 0.0001f)
                return Vector3.zero;
            Vector3 v = vector.normalized * settings.maxSpeed - Velocity;
            return Vector3.ClampMagnitude(v, settings.maxSteerForce);
        }

        bool IsHeadingForCollision()
        {
            return Physics.SphereCast(Position, settings.boundsRadius, Forward, out _,
                settings.collisionAvoidDistance, settings.obstacleMask);
        }

        Vector3 ObstacleRepulsion()
        {
            int count = Physics.OverlapSphereNonAlloc(Position, settings.repelRadius, nearbyObstacles, settings.obstacleMask);
            Vector3 push = Vector3.zero;
            for (int i = 0; i < count; i++)
            {
                Vector3 away = Position - nearbyObstacles[i].ClosestPoint(Position);
                if (away.sqrMagnitude < 0.0001f)
                    away = Position - nearbyObstacles[i].bounds.center; // already inside
                push += away.normalized / Mathf.Max(away.magnitude, 0.1f);
            }
            return push;
        }

        /// <summary>
        /// Tests directions spread over a sphere (ordered from straight ahead outwards)
        /// and returns the first one that is not blocked.
        /// </summary>
        Vector3 ObstacleRays()
        {
            Vector3[] directions = BoidDirections.Directions;
            for (int i = 0; i < directions.Length; i++)
            {
                Vector3 dir = cachedTransform.TransformDirection(directions[i]);
                var ray = new Ray(Position, dir);
                if (!Physics.SphereCast(ray, settings.boundsRadius, settings.collisionAvoidDistance, settings.obstacleMask))
                    return dir;
            }
            return Forward;
        }

        /// <summary>0 = normal, 1 = avoiding an obstacle, 2 = fleeing from the predator.</summary>
        void ShowState(int state)
        {
            if (state == shownState)
                return;
            shownState = state;
            Color? color = state == 2 ? settings.fleeColor : state == 1 ? settings.avoidColor : baseColor;
            if (color.HasValue)
                block.SetColor("_BaseColor", color.Value);
            foreach (var r in renderers)
                r.SetPropertyBlock(color.HasValue ? block : null);
        }
    }

    /// <summary>
    /// Directions evenly distributed on a sphere using the golden spiral.
    /// Index 0 points forward (+Z), later indices point further away from forward.
    /// </summary>
    public static class BoidDirections
    {
        const int NumViewDirections = 150;
        public static readonly Vector3[] Directions;

        static BoidDirections()
        {
            Directions = new Vector3[NumViewDirections];
            float goldenRatio = (1 + Mathf.Sqrt(5)) / 2;
            float angleIncrement = Mathf.PI * 2 * goldenRatio;

            for (int i = 0; i < NumViewDirections; i++)
            {
                float t = (float)i / NumViewDirections;
                float inclination = Mathf.Acos(1 - 2 * t);
                float azimuth = angleIncrement * i;
                float x = Mathf.Sin(inclination) * Mathf.Cos(azimuth);
                float y = Mathf.Sin(inclination) * Mathf.Sin(azimuth);
                float z = Mathf.Cos(inclination);
                Directions[i] = new Vector3(x, y, z);
            }
        }
    }
}
