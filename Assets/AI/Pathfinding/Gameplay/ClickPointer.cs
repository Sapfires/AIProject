using UnityEngine;

namespace AIGames.Pathfinding.Gameplay
{
    /// <summary>
    /// Target marker shown where the player clicked: pulses and hides once the player arrives.
    /// </summary>
    public class ClickPointer : MonoBehaviour
    {
        public Transform player;
        public float pulseSpeed = 4f;
        public float hideDistance = 0.4f;

        Vector3 baseScale;

        void Awake()
        {
            baseScale = transform.localScale;
            gameObject.SetActive(false);
        }

        void Update()
        {
            float pulse = 1f + 0.15f * Mathf.Sin(Time.time * pulseSpeed);
            transform.localScale = new Vector3(baseScale.x * pulse, baseScale.y, baseScale.z * pulse);

            Vector3 d = player.position - transform.position;
            d.y = 0;
            if (d.sqrMagnitude < hideDistance * hideDistance)
                gameObject.SetActive(false);
        }
    }
}
