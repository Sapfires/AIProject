using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AIGames.MachineLearning
{
    /// <summary>
    /// ML-Agents RollerBall: a ball learns to roll to a randomly placed target without
    /// falling off the platform.
    ///   Observations (8): target position (3), own position (3), velocity x/z (2)
    ///   Actions: 2 continuous values = force along x and z
    ///   Rewards: +1 for reaching the target, episode ends when falling off
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class RollerAgent : Agent
    {
        public Transform target;
        public float forceMultiplier = 10f;
        public float platformHalfSize = 4f;

        Rigidbody rb;

        public override void Initialize()
        {
            rb = GetComponent<Rigidbody>();
        }

        public override void OnEpisodeBegin()
        {
            // Reset the ball only when it fell off, otherwise continue from where it is.
            if (transform.localPosition.y < 0)
            {
                rb.angularVelocity = Vector3.zero;
                rb.linearVelocity = Vector3.zero;
                transform.localPosition = new Vector3(0, 0.5f, 0);
            }

            target.localPosition = new Vector3(
                Random.Range(-platformHalfSize, platformHalfSize), 0.5f,
                Random.Range(-platformHalfSize, platformHalfSize));
        }

        public override void CollectObservations(VectorSensor sensor)
        {
            sensor.AddObservation(target.localPosition);
            sensor.AddObservation(transform.localPosition);
            sensor.AddObservation(rb.linearVelocity.x);
            sensor.AddObservation(rb.linearVelocity.z);
        }

        public override void OnActionReceived(ActionBuffers actions)
        {
            var control = new Vector3(actions.ContinuousActions[0], 0, actions.ContinuousActions[1]);
            rb.AddForce(control * forceMultiplier);

            float distanceToTarget = Vector3.Distance(transform.localPosition, target.localPosition);
            if (distanceToTarget < 1.42f)
            {
                SetReward(1.0f);
                RollerStats.Record(true);
                EndEpisode();
            }
            else if (transform.localPosition.y < 0)
            {
                RollerStats.Record(false);
                EndEpisode();
            }
        }

        /// <summary>Manual control (Behavior Type = Heuristic Only) with WASD / arrow keys.</summary>
        public override void Heuristic(in ActionBuffers actionsOut)
        {
            var continuous = actionsOut.ContinuousActions;
            var keyboard = Keyboard.current;
            if (keyboard == null)
                return;
            continuous[0] = (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1 : 0)
                          - (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ? 1 : 0);
            continuous[1] = (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed ? 1 : 0)
                          - (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed ? 1 : 0);
        }
    }
}
