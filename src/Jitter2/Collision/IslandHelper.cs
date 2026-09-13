/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Jitter2.Dynamics;
using Jitter2.Dynamics.Constraints;
using IslandSet = Jitter2.DataStructures.PartitionedSet<Jitter2.Collision.Island>;

namespace Jitter2.Collision;

/// <summary>
/// Helper class to update islands. Methods must not be called concurrently for the same world.
/// </summary>
/// <remarks>
/// Search scratch data is stored per thread. Pooled islands are stored by the owning world.
/// Separate worlds may use this helper concurrently on different external threads as long as
/// each individual world is not used concurrently.
/// </remarks>
internal static class IslandHelper
{
    [ThreadStatic] private static Queue<RigidBody>? leftSearchQueue;
    [ThreadStatic] private static Queue<RigidBody>? rightSearchQueue;
    [ThreadStatic] private static List<RigidBody>? visitedBodiesLeft;
    [ThreadStatic] private static List<RigidBody>? visitedBodiesRight;

    private static Queue<RigidBody> LeftSearchQueue => leftSearchQueue ??= new Queue<RigidBody>();
    private static Queue<RigidBody> RightSearchQueue => rightSearchQueue ??= new Queue<RigidBody>();
    private static List<RigidBody> VisitedBodiesLeft => visitedBodiesLeft ??= [];
    private static List<RigidBody> VisitedBodiesRight => visitedBodiesRight ??= [];

    private static Island GetFromPool(Stack<Island> pool)
    {
        if (!pool.TryPop(out var island))
        {
            island = new Island();
        }

        island.MarkedAsActive = true;
        island.NeedsUpdate = false;

        return island;
    }

    private static void ReturnToPool(Stack<Island> pool, Island island)
    {
        pool.Push(island);
    }

    public static void ArbiterCreated(IslandSet islands, Stack<Island> islandPool, Arbiter arbiter)
    {
        RigidBody b1 = arbiter.Body1;
        RigidBody b2 = arbiter.Body2;

        b1.InternalContacts.Add(arbiter);
        b2.InternalContacts.Add(arbiter);

        AddConnection(islands, islandPool, b1, b2);
    }

    public static void ArbiterRemoved(IslandSet islands, Stack<Island> islandPool, Arbiter arbiter)
    {
        arbiter.Body1.InternalContacts.Remove(arbiter);
        arbiter.Body2.InternalContacts.Remove(arbiter);

        RemoveConnection(islands, islandPool, arbiter.Body1, arbiter.Body2);
    }

    public static void ConstraintCreated(IslandSet islands, Stack<Island> islandPool, Constraint constraint)
    {
        constraint.Body1.InternalConstraints.Add(constraint);
        constraint.Body2.InternalConstraints.Add(constraint);

        AddConnection(islands, islandPool, constraint.Body1, constraint.Body2);
    }

    public static void ConstraintRemoved(IslandSet islands, Stack<Island> islandPool, Constraint constraint)
    {
        constraint.Body1.InternalConstraints.Remove(constraint);
        constraint.Body2.InternalConstraints.Remove(constraint);

        RemoveConnection(islands, islandPool, constraint.Body1, constraint.Body2);
    }

    public static void BodyAdded(IslandSet islands, Stack<Island> islandPool, RigidBody body)
    {
        body.InternalIsland = GetFromPool(islandPool);
        islands.Add(body.InternalIsland, true);
        body.InternalIsland.InternalBodies.Add(body);
    }

    public static void BodyRemoved(IslandSet islands, Stack<Island> islandPool, RigidBody body)
    {
        body.InternalIsland.ClearLists();
        ReturnToPool(islandPool, body.InternalIsland);
        islands.Remove(body.InternalIsland);
    }

    public static void AddConnection(IslandSet islands, Stack<Island> islandPool, RigidBody body1, RigidBody body2)
    {
        bool needsUpdate = (!islands.IsActive(body1.Island) || !islands.IsActive(body2.Island));
        bool bothNotStatic = body1.Data.MotionType != MotionType.Static && body2.Data.MotionType != MotionType.Static;

        if (bothNotStatic)
        {
            MergeIslands(islands, islandPool, body1, body2);
            body1.InternalConnections.Add(body2);
            body2.InternalConnections.Add(body1);
        }

        if (needsUpdate)
        {
            if(body1.Data.MotionType != MotionType.Static) body1.Island.NeedsUpdate = true;
            if(body2.Data.MotionType != MotionType.Static) body2.Island.NeedsUpdate = true;
        }
    }

    public static void RemoveConnection(IslandSet islands, Stack<Island> islandPool, RigidBody body1, RigidBody body2)
    {
        static void RemoveRef(List<RigidBody> list, RigidBody body)
        {
            int index = list.IndexOf(body);
            if (index < 0) return;

            int last = list.Count - 1;
            list[index] = list[last];
            list.RemoveAt(last);
        }

        RemoveRef(body1.InternalConnections, body2);
        RemoveRef(body2.InternalConnections, body1);

        if (body1.InternalIsland == body2.InternalIsland)
        {
            SplitIslands(islands, islandPool, body1, body2);
        }
    }

    private static void SplitIslands(IslandSet islands, Stack<Island> islandPool, RigidBody body1, RigidBody body2)
    {
        Debug.Assert(body1.InternalIsland == body2.InternalIsland, "Islands not the same or null.");

        Queue<RigidBody> leftSearchQueue = LeftSearchQueue;
        Queue<RigidBody> rightSearchQueue = RightSearchQueue;
        List<RigidBody> visitedBodiesLeft = VisitedBodiesLeft;
        List<RigidBody> visitedBodiesRight = VisitedBodiesRight;

        bool sourceIslandActive = islands.IsActive(body1.InternalIsland);
        bool sourceNeedsUpdate = body1.InternalIsland.NeedsUpdate;
        bool sourceMarkedAsActive = body1.InternalIsland.MarkedAsActive;

        leftSearchQueue.Enqueue(body1);
        rightSearchQueue.Enqueue(body2);

        visitedBodiesLeft.Add(body1);
        visitedBodiesRight.Add(body2);

        body1.InternalIslandMarker = 1;
        body2.InternalIslandMarker = 2;

        try
        {
            while (leftSearchQueue.Count > 0 && rightSearchQueue.Count > 0)
            {
                RigidBody currentNode = leftSearchQueue.Dequeue();
                if (currentNode.Data.MotionType != MotionType.Static)
                {
                    for (int i = 0; i < currentNode.InternalConnections.Count; i++)
                    {
                        RigidBody connectedNode = currentNode.InternalConnections[i];

                        if (connectedNode.InternalIslandMarker == 0)
                        {
                            leftSearchQueue.Enqueue(connectedNode);
                            visitedBodiesLeft.Add(connectedNode);
                            connectedNode.InternalIslandMarker = 1;
                        }
                        else if (connectedNode.InternalIslandMarker == 2)
                        {
                            return;
                        }
                    }
                }

                currentNode = rightSearchQueue.Dequeue();
                if (currentNode.Data.MotionType != MotionType.Static)
                {
                    for (int i = 0; i < currentNode.InternalConnections.Count; i++)
                    {
                        RigidBody connectedNode = currentNode.InternalConnections[i];

                        if (connectedNode.InternalIslandMarker == 0)
                        {
                            rightSearchQueue.Enqueue(connectedNode);
                            visitedBodiesRight.Add(connectedNode);
                            connectedNode.InternalIslandMarker = 2;
                        }
                        else if (connectedNode.InternalIslandMarker == 1)
                        {
                            return;
                        }
                    }
                }
            }

            Island island = GetFromPool(islandPool);
            island.NeedsUpdate = sourceNeedsUpdate;
            island.MarkedAsActive = sourceMarkedAsActive;
            islands.Add(island, sourceIslandActive);

            if (leftSearchQueue.Count == 0)
            {
                for (int i = 0; i < visitedBodiesLeft.Count; i++)
                {
                    RigidBody body = visitedBodiesLeft[i];
                    body2.InternalIsland.InternalBodies.Remove(body);
                    island.InternalBodies.Add(body);
                    body.InternalIsland = island;
                }
            }
            else if (rightSearchQueue.Count == 0)
            {
                for (int i = 0; i < visitedBodiesRight.Count; i++)
                {
                    RigidBody body = visitedBodiesRight[i];
                    body1.InternalIsland.InternalBodies.Remove(body);
                    island.InternalBodies.Add(body);
                    body.InternalIsland = island;
                }
            }
        }
        finally
        {
            for (int i = 0; i < visitedBodiesLeft.Count; i++)
            {
                visitedBodiesLeft[i].InternalIslandMarker = 0;
            }

            for (int i = 0; i < visitedBodiesRight.Count; i++)
            {
                visitedBodiesRight[i].InternalIslandMarker = 0;
            }

            leftSearchQueue.Clear();
            rightSearchQueue.Clear();
            visitedBodiesLeft.Clear();
            visitedBodiesRight.Clear();
        }
    }

    // Both bodies must be !static
    private static void MergeIslands(IslandSet islands, Stack<Island> islandPool, RigidBody body1, RigidBody body2)
    {
        if (body1.InternalIsland == body2.InternalIsland) return;

        bool needsUpdate = body1.InternalIsland.NeedsUpdate || body2.InternalIsland.NeedsUpdate;
        bool markedAsActive = body1.InternalIsland.MarkedAsActive || body2.InternalIsland.MarkedAsActive;

        // merge smaller into larger
        RigidBody smallIslandOwner, largeIslandOwner;

        if (body1.InternalIsland.InternalBodies.Count > body2.InternalIsland.InternalBodies.Count)
        {
            smallIslandOwner = body2;
            largeIslandOwner = body1;
        }
        else
        {
            smallIslandOwner = body1;
            largeIslandOwner = body2;
        }

        Island giveBackIsland = smallIslandOwner.InternalIsland;

        ReturnToPool(islandPool, giveBackIsland);
        islands.Remove(giveBackIsland);

        foreach (RigidBody b in giveBackIsland.InternalBodies)
        {
            b.InternalIsland = largeIslandOwner.InternalIsland;
            largeIslandOwner.InternalIsland.InternalBodies.Add(b);
        }

        largeIslandOwner.InternalIsland.NeedsUpdate |= needsUpdate;
        largeIslandOwner.InternalIsland.MarkedAsActive |= markedAsActive;

        giveBackIsland.ClearLists();
    }
}
