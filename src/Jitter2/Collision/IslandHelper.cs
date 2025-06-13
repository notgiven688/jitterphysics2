/*
 * Copyright (c) Thorben Linneweber and others
 *
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to
 * permit persons to whom the Software is furnished to do so, subject to
 * the following conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
 * LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
 * OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
 * WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

using System.Collections.Generic;
using System.Diagnostics;
using Jitter2.Dynamics;
using Jitter2.Dynamics.Constraints;
using IslandSet = Jitter2.DataStructures.PartitionedSet<Jitter2.Collision.Island>;

namespace Jitter2.Collision;

/// <summary>
/// Helper class to update islands. The methods here are not thread-safe.
/// </summary>
internal static class IslandHelper
{
    private static readonly Stack<Island> pool = new();

    private static Island GetFromPool()
    {
        if (pool.Count > 0)
        {
            return pool.Pop();
        }

        return new Island();
    }

    private static void ReturnToPool(Island island)
    {
        pool.Push(island);
    }

    public static void ArbiterCreated(IslandSet islands, Arbiter arbiter)
    {
        RigidBody b1 = arbiter.Body1;
        RigidBody b2 = arbiter.Body2;
        b1.InternalContacts.Add(arbiter);
        b2.InternalContacts.Add(arbiter);

        if (b1.Data.IsStatic || b2.Data.IsStatic) return;

        AddConnection(islands, b1, b2);
    }

    public static void ArbiterRemoved(IslandSet islands, Arbiter arbiter)
    {
        arbiter.Body1.InternalContacts.Remove(arbiter);
        arbiter.Body2.InternalContacts.Remove(arbiter);

        RemoveConnection(islands, arbiter.Body1, arbiter.Body2);
    }

    public static void ConstraintCreated(IslandSet islands, Constraint constraint)
    {
        constraint.Body1.InternalConstraints.Add(constraint);
        constraint.Body2.InternalConstraints.Add(constraint);

        if (constraint.Body1.Data.IsStatic || constraint.Body2.Data.IsStatic) return;

        AddConnection(islands, constraint.Body1, constraint.Body2);
    }

    public static void ConstraintRemoved(IslandSet islands, Constraint constraint)
    {
        constraint.Body1.InternalConstraints.Remove(constraint);
        constraint.Body2.InternalConstraints.Remove(constraint);

        RemoveConnection(islands, constraint.Body1, constraint.Body2);
    }

    public static void BodyAdded(IslandSet islands, RigidBody body)
    {
        body.InternalIsland = GetFromPool();
        islands.Add(body.InternalIsland, true);
        body.InternalIsland.InternalBodies.Add(body);
    }

    public static void BodyRemoved(IslandSet islands, RigidBody body)
    {
        body.InternalIsland.ClearLists();
        ReturnToPool(body.InternalIsland);
        islands.Remove(body.InternalIsland);
    }

    public static void AddConnection(IslandSet islands, RigidBody body1, RigidBody body2)
    {
        MergeIslands(islands, body1, body2);

        body1.InternalConnections.Add(body2);
        body2.InternalConnections.Add(body1);
    }

    public static void RemoveConnection(IslandSet islands, RigidBody body1, RigidBody body2)
    {
        body1.InternalConnections.Remove(body2);
        body2.InternalConnections.Remove(body1);

        if (body1.InternalIsland == body2.InternalIsland)
            SplitIslands(islands, body1, body2);
    }

    private static readonly Queue<RigidBody> leftSearchQueue = [];
    private static readonly Queue<RigidBody> rightSearchQueue = [];

    private static readonly List<RigidBody> visitedBodiesLeft = [];
    private static readonly List<RigidBody> visitedBodiesRight = [];

    private static void SplitIslands(IslandSet islands, RigidBody body1, RigidBody body2)
    {
        Debug.Assert(body1.InternalIsland == body2.InternalIsland, "Islands not the same or null.");

        leftSearchQueue.Enqueue(body1);
        rightSearchQueue.Enqueue(body2);

        visitedBodiesLeft.Add(body1);
        visitedBodiesRight.Add(body2);

        body1.InternalIslandMarker = 1;
        body2.InternalIslandMarker = 2;

        while (leftSearchQueue.Count > 0 && rightSearchQueue.Count > 0)
        {
            RigidBody currentNode = leftSearchQueue.Dequeue();
            if (!currentNode.Data.IsStatic)
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
                        leftSearchQueue.Clear();
                        rightSearchQueue.Clear();
                        goto ResetSearchStates;
                    }
                }
            }

            currentNode = rightSearchQueue.Dequeue();
            if (!currentNode.Data.IsStatic)
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
                        leftSearchQueue.Clear();
                        rightSearchQueue.Clear();
                        goto ResetSearchStates;
                    }
                }
            }
        }

        Island island = GetFromPool();
        islands.Add(island, true);

        if (leftSearchQueue.Count == 0)
        {
            for (int i = 0; i < visitedBodiesLeft.Count; i++)
            {
                RigidBody body = visitedBodiesLeft[i];
                body2.InternalIsland.InternalBodies.Remove(body);
                island.InternalBodies.Add(body);
                body.InternalIsland = island;
            }

            rightSearchQueue.Clear();
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

            leftSearchQueue.Clear();
        }

        ResetSearchStates:

        for (int i = 0; i < visitedBodiesLeft.Count; i++)
        {
            visitedBodiesLeft[i].InternalIslandMarker = 0;
        }

        for (int i = 0; i < visitedBodiesRight.Count; i++)
        {
            visitedBodiesRight[i].InternalIslandMarker = 0;
        }

        visitedBodiesLeft.Clear();
        visitedBodiesRight.Clear();
    }

    // Both bodies must be !static
    private static void MergeIslands(IslandSet islands, RigidBody body1, RigidBody body2)
    {
        if (body1.InternalIsland == body2.InternalIsland) return;

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

        ReturnToPool(giveBackIsland);
        islands.Remove(giveBackIsland);

        foreach (RigidBody b in giveBackIsland.InternalBodies)
        {
            b.InternalIsland = largeIslandOwner.InternalIsland;
            largeIslandOwner.InternalIsland.InternalBodies.Add(b);
        }

        giveBackIsland.ClearLists();
    }
}