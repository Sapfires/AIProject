using UnityEngine;

namespace AIGames.Pathfinding.Gameplay
{
    /// <summary>
    /// A loop of patrol points; the points are the child transforms.
    /// </summary>
    public class PatrolPath : MonoBehaviour
    {
        public Color gizmoColor = new Color(1f, 0.8f, 0.2f);

        public int Count => transform.childCount;

        public Vector3 this[int i] => transform.GetChild(i).position;

        public int NearestIndex(Vector3 position)
        {
            int best = 0;
            float bestDst = float.MaxValue;
            for (int i = 0; i < Count; i++)
            {
                float d = (this[i] - position).sqrMagnitude;
                if (d < bestDst)
                {
                    bestDst = d;
                    best = i;
                }
            }
            return best;
        }

        void OnDrawGizmos()
        {
            Gizmos.color = gizmoColor;
            for (int i = 0; i < Count; i++)
            {
                Gizmos.DrawSphere(this[i], 0.25f);
                Gizmos.DrawLine(this[i], this[(i + 1) % Count]);
            }
        }
    }
}
