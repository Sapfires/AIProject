using UnityEngine;

namespace AIGames.Swarm
{
    /// <summary>
    /// Moves the point the swarm is attracted to along a Lissajous curve around the arena.
    /// </summary>
    public class SwarmTarget : MonoBehaviour
    {
        public Vector3 centre = new Vector3(0, 6, 0);
        public Vector3 amplitude = new Vector3(14, 3, 14);
        public Vector3 frequency = new Vector3(0.11f, 0.23f, 0.17f);

        void Update()
        {
            float t = Time.time;
            transform.position = centre + new Vector3(
                amplitude.x * Mathf.Sin(t * frequency.x * Mathf.PI * 2),
                amplitude.y * Mathf.Sin(t * frequency.y * Mathf.PI * 2),
                amplitude.z * Mathf.Sin(t * frequency.z * Mathf.PI * 2 + 1.3f));
        }
    }
}
