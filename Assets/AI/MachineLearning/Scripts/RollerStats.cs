using AIGames.Common;
using Unity.MLAgents.Policies;
using UnityEngine;

namespace AIGames.MachineLearning
{
    /// <summary>
    /// Counts episode outcomes of all RollerAgents in the scene and shows them on the HUD.
    /// </summary>
    public class RollerStats : MonoBehaviour
    {
        public BehaviorParameters sampleAgent;

        static int reached;
        static int fell;

        public static void Record(bool success)
        {
            if (success)
                reached++;
            else
                fell++;
        }

        void Awake()
        {
            reached = 0;
            fell = 0;
        }

        void Update()
        {
            int total = reached + fell;
            string mode = sampleAgent == null ? "?" :
                sampleAgent.Model != null && sampleAgent.BehaviorType != BehaviorType.HeuristicOnly
                    ? $"inference ({sampleAgent.Model.name}.onnx)"
                    : sampleAgent.BehaviorType == BehaviorType.HeuristicOnly ? "heuristic (keyboard)" : "training / no model";

            DemoHUD.Show("ML-Agents - RollerBall",
                $"Mode: {mode}\n" +
                $"Episodes: {total}, target reached: {reached}, fell off: {fell}\n" +
                $"Success rate: {(total == 0 ? 0 : 100f * reached / total):F0}%\n" +
                "Obs: 8 floats, actions: 2 continuous forces");
        }
    }
}
