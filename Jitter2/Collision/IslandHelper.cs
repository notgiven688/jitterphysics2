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
        b1.contacts.Add(arbiter);
        b2.contacts.Add(arbiter);

        if (b1.Data.IsStatic || b2.Data.IsStatic) return;

        AddConnection(islands, b1, b2);
    }

    public static void ArbiterRemoved(IslandSet islands, Arbiter arbiter)
    {
        arbiter.Body1.contacts.Remove(arbiter);
        arbiter.Body2.contacts.Remove(arbiter);

        RemoveConnection(islands, arbiter.Body1, arbiter.Body2);
    }

    public static void ConstraintCreated(IslandSet islands, Constraint constraint)
    {
        constraint.Body1.constraints.Add(constraint);
        constraint.Body2.constraints.Add(constraint);

        if (constraint.Body1.Data.IsStatic || constraint.Body2.Data.IsStatic) return;

        AddConnection(islands, constraint.Body1, constraint.Body2);
    }

    public static void ConstraintRemoved(IslandSet islands, Constraint constraint)
    {
        constraint.Body1.constraints.Remove(constraint);
        constraint.Body2.constraints.Remove(constraint);

        RemoveConnection(islands, constraint.Body1, constraint.Body2);
    }

    public static void BodyAdded(IslandSet islands, RigidBody body)
    {
        body.island = GetFromPool();
        islands.Add(body.island, true);
        body.island.bodies.Add(body);
    }

    public static void BodyRemoved(IslandSet islands, RigidBody body)
    {
        body.island.ClearLists();
        ReturnToPool(body.island);
        islands.Remove(body.island);
    }

    public static void AddConnection(IslandSet islands, RigidBody body1, RigidBody body2)
    {
        MergeIslands(islands, body1, body2);

        body1.connections.Add(body2);
        body2.connections.Add(body1);
    }

    public static void RemoveConnection(IslandSet islands, RigidBody body1, RigidBody body2)
    {
        body1.connections.Remove(body2);
        body2.connections.Remove(body1);

        if (body1.island == body2.island)
            SplitIslands(islands, body1, body2);
    }

    private static readonly Queue<RigidBody> leftSearchQueue = new();
    private static readonly Queue<RigidBody> rightSearchQueue = new();

    private static readonly List<RigidBody> visitedBodiesLeft = new();
    private static readonly List<RigidBody> visitedBodiesRight = new();

    private static void SplitIslands(IslandSet islands, RigidBody body1, RigidBody body2)
    {
        Debug.Assert(body1.island == body2.island, "Islands not the same or null.");

        leftSearchQueue.Enqueue(body1);
        rightSearchQueue.Enqueue(body2);

        visitedBodiesLeft.Add(body1);
        visitedBodiesRight.Add(body2);

        body1.islandMarker = 1;
        body2.islandMarker = 2;

        while (leftSearchQueue.Count > 0 && rightSearchQueue.Count > 0)
        {
            RigidBody currentNode = leftSearchQueue.Dequeue();
            if (!currentNode.Data.IsStatic)
            {
                for (int i = 0; i < currentNode.connections.Count; i++)
                {
                    RigidBody connectedNode = currentNode.connections[i];

                    if (connectedNode.islandMarker == 0)
                    {
                        leftSearchQueue.Enqueue(connectedNode);
                        visitedBodiesLeft.Add(connectedNode);
                        connectedNode.islandMarker = 1;
                    }
                    else if (connectedNode.islandMarker == 2)
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
                for (int i = 0; i < currentNode.connections.Count; i++)
                {
                    RigidBody connectedNode = currentNode.connections[i];

                    if (connectedNode.islandMarker == 0)
                    {
                        rightSearchQueue.Enqueue(connectedNode);
                        visitedBodiesRight.Add(connectedNode);
                        connectedNode.islandMarker = 2;
                    }
                    else if (connectedNode.islandMarker == 1)
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
                body2.island.bodies.Remove(body);
                island.bodies.Add(body);
                body.island = island;
            }

            rightSearchQueue.Clear();
        }
        else if (rightSearchQueue.Count == 0)
        {
            for (int i = 0; i < visitedBodiesRight.Count; i++)
            {
                RigidBody body = visitedBodiesRight[i];
                body1.island.bodies.Remove(body);
                island.bodies.Add(body);
                body.island = island;
            }

            leftSearchQueue.Clear();
        }

        ResetSearchStates:

        for (int i = 0; i < visitedBodiesLeft.Count; i++)
        {
            visitedBodiesLeft[i].islandMarker = 0;
        }

        for (int i = 0; i < visitedBodiesRight.Count; i++)
        {
            visitedBodiesRight[i].islandMarker = 0;
        }

        visitedBodiesLeft.Clear();
        visitedBodiesRight.Clear();
    }

    // Both bodies must be !static
    private static void MergeIslands(IslandSet islands, RigidBody body1, RigidBody body2)
    {
        if (body1.island == body2.island) return;

        // merge smaller into larger
        RigidBody smallIslandOwner, largeIslandOwner;

        if (body1.island.bodies.Count > body2.island.bodies.Count)
        {
            smallIslandOwner = body2;
            largeIslandOwner = body1;
        }
        else
        {
            smallIslandOwner = body1;
            largeIslandOwner = body2;
        }

        Island giveBackIsland = smallIslandOwner.island;

        ReturnToPool(giveBackIsland);
        islands.Remove(giveBackIsland);

        foreach (RigidBody b in giveBackIsland.bodies)
        {
            b.island = largeIslandOwner.island;
            largeIslandOwner.island.bodies.Add(b);
        }

        giveBackIsland.ClearLists();
    }
}