using System.Collections.Generic;
using AIGames.Common;
using UnityEngine;

namespace AIGames.Swarm
{
    /// <summary>
    /// Spawns the boids and, every frame, computes what each boid perceives from its
    /// neighbours (alignment, cohesion and separation data) before letting it steer.
    /// </summary>
    public class SwarmManager : MonoBehaviour
    {
        public SwarmSettings settings = new SwarmSettings();
        public int boidCount = 150;
        public float spawnRadius = 6f;
        public Material boidMaterial;
        [Tooltip("Optional 3D models (e.g. spaceships) used instead of the capsule; they keep their own materials.")]
        public GameObject[] boidModels = new GameObject[0];
        public Gradient colors;
        public Transform target;
        public Transform predator;

        readonly List<Boid> boids = new List<Boid>();

        public IReadOnlyList<Boid> Boids => boids;

        void Start()
        {
            for (int i = 0; i < boidCount; i++)
            {
                Vector3 pos = transform.position + Random.insideUnitSphere * spawnRadius;
                var boid = CreateBoid(i, pos, Random.rotation);
                boid.Initialize(settings, boidModels.Length > 0 ? (Color?)null : colors.Evaluate(Random.value));
                boids.Add(boid);
            }
        }

        Boid CreateBoid(int index, Vector3 position, Quaternion rotation)
        {
            var go = new GameObject($"Boid {index}");
            go.transform.SetParent(transform);
            go.transform.SetPositionAndRotation(position, rotation);

            if (boidModels.Length > 0)
            {
                Instantiate(boidModels[index % boidModels.Length], go.transform, false);
                return go.AddComponent<Boid>();
            }

            // Capsule body stretched along the forward axis.
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            Destroy(body.GetComponent<Collider>());
            body.transform.SetParent(go.transform, false);
            body.transform.localRotation = Quaternion.Euler(90, 0, 0);
            body.transform.localScale = new Vector3(0.35f, 0.55f, 0.25f);
            body.GetComponent<Renderer>().sharedMaterial = boidMaterial;

            return go.AddComponent<Boid>();
        }

        void Update()
        {
            float radiusSqr = settings.perceptionRadius * settings.perceptionRadius;
            float avoidSqr = settings.avoidanceRadius * settings.avoidanceRadius;
            int totalNeighbours = 0;
            int fleeing = 0;
            int avoiding = 0;

            // O(n^2) neighbour search, fine for a few hundred boids.
            for (int i = 0; i < boids.Count; i++)
            {
                Boid boid = boids[i];
                boid.flockHeading = Vector3.zero;
                boid.flockCentre = Vector3.zero;
                boid.separationHeading = Vector3.zero;
                boid.perceivedFlockmates = 0;

                for (int j = 0; j < boids.Count; j++)
                {
                    if (i == j)
                        continue;
                    Boid other = boids[j];
                    Vector3 offset = other.Position - boid.Position;
                    float sqrDst = offset.sqrMagnitude;
                    if (sqrDst >= radiusSqr)
                        continue;

                    boid.perceivedFlockmates++;
                    boid.flockHeading += other.Forward;
                    boid.flockCentre += other.Position;
                    if (sqrDst < avoidSqr)
                        boid.separationHeading -= offset / Mathf.Max(sqrDst, 0.0001f);
                }
                totalNeighbours += boid.perceivedFlockmates;
            }

            foreach (Boid boid in boids)
            {
                boid.UpdateBoid(target, predator, Time.deltaTime);
                if (boid.IsFleeing)
                    fleeing++;
                else if (boid.IsAvoiding)
                    avoiding++;
            }

            DemoHUD.Show("Swarm - boids flocking",
                $"Boids: {boids.Count}, avg. neighbours: {(boids.Count == 0 ? 0 : (float)totalNeighbours / boids.Count):F1}\n" +
                $"Weights: separation {settings.separationWeight}, alignment {settings.alignmentWeight}, cohesion {settings.cohesionWeight}\n" +
                $"Avoiding obstacles (pink): {avoiding}\n" +
                $"Fleeing from predator (yellow): {fleeing}");
        }

        public Vector3 SwarmCentre()
        {
            if (boids.Count == 0)
                return transform.position;
            Vector3 sum = Vector3.zero;
            foreach (Boid b in boids)
                sum += b.Position;
            return sum / boids.Count;
        }
    }
}
