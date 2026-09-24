using UnityEngine;

namespace AIGames.Swarm
{
    /// <summary>
    /// Tunable parameters shared by all boids of a swarm.
    /// </summary>
    [System.Serializable]
    public class SwarmSettings
    {
        [Header("Movement")]
        public float minSpeed = 3f;
        public float maxSpeed = 7f;
        public float maxSteerForce = 6f;

        [Header("Perception")]
        public float perceptionRadius = 3f;
        public float avoidanceRadius = 1.2f;

        [Header("Rule weights")]
        public float separationWeight = 2.5f;
        public float alignmentWeight = 1f;
        public float cohesionWeight = 1f;
        public float targetWeight = 0.6f;

        [Header("Obstacles")]
        public LayerMask obstacleMask;
        public float boundsRadius = 0.3f;
        public float collisionAvoidDistance = 4f;
        public float avoidCollisionWeight = 12f;
        [Tooltip("Distance at which boids are pushed away from obstacle surfaces.")]
        public float repelRadius = 1f;
        public Color avoidColor = new Color(1f, 0.30f, 0.75f);

        [Header("Predator")]
        public float fleeRadius = 6f;
        public float fleeWeight = 8f;
        public Color fleeColor = new Color(1f, 0.85f, 0.2f);
    }
}
