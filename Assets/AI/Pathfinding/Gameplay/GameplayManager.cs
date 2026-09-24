using AIGames.Common;
using UnityEngine;

namespace AIGames.Pathfinding.Gameplay
{
    /// <summary>
    /// Game rules of the Pathfinder gameplay scene: reach the treasure chest without being
    /// caught by the robot guards. Being caught or collecting the treasure respawns the player.
    /// </summary>
    public class GameplayManager : MonoBehaviour
    {
        public PlayerController player;
        public Transform treasure;
        public EnemyAI[] guards;
        public float treasureRadius = 1.2f;

        Vector3 startPosition;
        int caught;
        int treasures;
        string lastEvent = "";

        void Start()
        {
            startPosition = player.transform.position;
            foreach (var guard in guards)
                guard.CaughtPlayer += OnCaught;
        }

        void OnCaught(EnemyAI guard)
        {
            caught++;
            lastEvent = $"Caught by {guard.name}!";
            player.Teleport(startPosition);
        }

        void Update()
        {
            if (treasure != null && Vector3.Distance(player.transform.position, treasure.position) < treasureRadius)
            {
                treasures++;
                lastEvent = "Treasure collected!";
                player.Teleport(startPosition);
            }
            if (treasure != null)
                treasure.Rotate(0, 60 * Time.deltaTime, 0);

            string states = "";
            foreach (var g in guards)
                states += $"{g.name}: {g.CurrentState}\n";
            DemoHUD.Show("Pathfinder 5 - Patrol, detect, chase",
                states +
                $"Treasures: {treasures}, caught: {caught}  {lastEvent}\n" +
                "Click: move the ninja to the chest, Tab: camera");
        }
    }
}
