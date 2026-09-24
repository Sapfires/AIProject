using UnityEngine;
using UnityEngine.InputSystem;

namespace AIGames.Pathfinding.Gameplay
{
    /// <summary>
    /// Exercise 2: the player moves with the same pathfinding as the enemies. A left click
    /// on the floor places the pointer and the player walks there on an A* path.
    /// </summary>
    [RequireComponent(typeof(PathMover))]
    public class PlayerController : MonoBehaviour
    {
        public Transform pointer;
        public LayerMask floorMask = 1;
        public float speed = 3.5f;

        PathMover mover;

        void Awake()
        {
            mover = GetComponent<PathMover>();
            mover.speed = speed;
        }

        void Update()
        {
            var mouse = Mouse.current;
            if (mouse == null || !mouse.leftButton.wasPressedThisFrame)
                return;

            Ray ray = Camera.main.ScreenPointToRay(mouse.position.ReadValue());
            if (Physics.Raycast(ray, out RaycastHit hit, 200, floorMask))
                GoTo(hit.point);
        }

        public void GoTo(Vector3 point)
        {
            point.y = 0;
            if (pointer != null)
            {
                pointer.position = point;
                pointer.gameObject.SetActive(true);
            }
            mover.MoveTo(point);
        }

        public void Teleport(Vector3 position)
        {
            mover.Stop();
            transform.position = position;
            if (pointer != null)
                pointer.gameObject.SetActive(false);
        }
    }
}
