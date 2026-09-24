using UnityEngine;

namespace AIGames.Pathfinding.Gameplay
{
    /// <summary>
    /// Exercise 4: player detection by collision. A trigger sphere around the enemy
    /// ("hearing" range) detects the player even behind the enemy's back.
    /// The ring on the floor shows the range and lights up when the player is inside.
    /// </summary>
    [RequireComponent(typeof(SphereCollider))]
    public class Detection : MonoBehaviour
    {
        public Renderer rangeIndicator;
        public Color idleColor = new Color(1f, 1f, 1f, 0.12f);
        public Color detectedColor = new Color(1f, 0.2f, 0.15f, 0.45f);

        MaterialPropertyBlock block;

        public bool PlayerDetected { get; private set; }
        public Transform Player { get; private set; }

        void Awake()
        {
            GetComponent<SphereCollider>().isTrigger = true;
            block = new MaterialPropertyBlock();
            SetColor(idleColor);
        }

        void OnTriggerEnter(Collider other)
        {
            var player = other.GetComponentInParent<PlayerController>();
            if (player == null)
                return;
            PlayerDetected = true;
            Player = player.transform;
            SetColor(detectedColor);
        }

        void OnTriggerExit(Collider other)
        {
            if (other.GetComponentInParent<PlayerController>() == null)
                return;
            PlayerDetected = false;
            SetColor(idleColor);
        }

        void SetColor(Color color)
        {
            if (rangeIndicator == null)
                return;
            block.SetColor("_BaseColor", color);
            rangeIndicator.SetPropertyBlock(block);
        }
    }
}
