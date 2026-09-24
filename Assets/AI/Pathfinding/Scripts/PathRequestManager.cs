using System;
using System.Collections.Generic;
using UnityEngine;

namespace AIGames.Pathfinding
{
    /// <summary>
    /// Queues path requests from many units and processes one per frame, so a crowd of
    /// units asking for paths at the same time does not cause a frame spike.
    /// </summary>
    [RequireComponent(typeof(AStarPathfinder))]
    public class PathRequestManager : MonoBehaviour
    {
        struct PathRequest
        {
            public Vector3 start;
            public Vector3 end;
            public Action<Vector3[], bool> callback;
        }

        static PathRequestManager instance;

        public int requestsPerFrame = 1;

        readonly Queue<PathRequest> queue = new Queue<PathRequest>();
        AStarPathfinder pathfinder;

        public static int PendingRequests => instance == null ? 0 : instance.queue.Count;
        public static int ProcessedRequests { get; private set; }

        void Awake()
        {
            instance = this;
            ProcessedRequests = 0;
            pathfinder = GetComponent<AStarPathfinder>();
        }

        public static void RequestPath(Vector3 start, Vector3 end, Action<Vector3[], bool> callback)
        {
            instance.queue.Enqueue(new PathRequest { start = start, end = end, callback = callback });
        }

        void Update()
        {
            for (int i = 0; i < requestsPerFrame && queue.Count > 0; i++)
            {
                PathRequest request = queue.Dequeue();
                List<Node> path = pathfinder.FindPath(request.start, request.end);
                bool success = path != null && path.Count > 0;
                ProcessedRequests++;
                request.callback(success ? AStarPathfinder.SimplifyPath(path) : new Vector3[0], success);
            }
        }
    }
}
