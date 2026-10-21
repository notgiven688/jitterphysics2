/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

using System;
using System.Collections.Generic;
using Jitter2.Collision;
using Jitter2.Collision.Shapes;
using Jitter2.DataStructures;
using Jitter2.Dynamics;
using Jitter2.LinearMath;
using Jitter2.Parallelization;

namespace Jitter2;

public sealed partial class World
{
    private int ccdIterations = 4;
    // Enabled bodies stay registered between steps. Discovery temporarily appends impulse recipients.
    private readonly List<RigidBody> ccdBodies = [];
    private readonly HashSet<RigidBody> ccdBodySet = [];
    private readonly List<RigidBodyShape> ccdShapes = [];
    private readonly List<CcdQuery> ccdQueries = [];
    private readonly List<Arbiter> ccdContacts = [];
    private readonly Dictionary<ArbiterKey, Arbiter> ccdPairs = [];

    private struct CcdHit
    {
        public RigidBodyShape Candidate;
        public JVector PointA, PointB, Normal;
        public Real Time;
    }

    private sealed class CcdQuery(World world) : ISink<IDynamicTreeProxy>
    {
        public RigidBodyShape Shape = null!;
        public readonly List<CcdHit> Hits = [];

        public void Add(in IDynamicTreeProxy candidate) => world.FindCcdHit(this, candidate);

        public void Clear()
        {
            Shape = null!;
            Hits.Clear();
        }
    }

    internal bool IsDiscoveringCcdContacts { get; private set; }

    /// <summary>
    /// Number of CCD contact discovery passes before the regular solver. Default: 4.
    /// </summary>
    /// <remarks>
    /// Each pass predicts translation and rotation, then applies contact impulses so the next pass
    /// can discover collisions caused by the changed motion. Bodies stay at their starting poses.
    /// Higher values cost more sweeps and do not guarantee that every collision will be found.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">The value is less than one.</exception>
    public int CcdIterations
    {
        get => ccdIterations;
        set
        {
            if (value < 1) throw new ArgumentOutOfRangeException(nameof(value));
            ccdIterations = value;
        }
    }

    internal void AddCcdBody(RigidBody body)
    {
        if (ccdBodySet.Add(body)) ccdBodies.Add(body);
    }

    internal void RemoveCcdBody(RigidBody body)
    {
        if (ccdBodySet.Remove(body)) ccdBodies.Remove(body);
    }

    internal bool NeedsCcdBounds(RigidBody body) =>
        body.EnableCcd || (IsDiscoveringCcdContacts && ccdBodySet.Contains(body));

    private void DiscoverCcdContacts(bool multiThread)
    {
        if (ccdBodies.Count == 0) return;

        IsDiscoveringCcdContacts = true;
        try
        {
            for (int pass = 0; pass < ccdIterations; pass++)
            {
                ccdShapes.Clear();
                foreach (RigidBody body in ccdBodies)
                {
                    ref RigidBodyData data = ref body.Data;
                    // Discovery impulses can move a sleeping body before activation is processed.
                    if (!data.IsActive && data.Velocity == JVector.Zero &&
                        data.AngularVelocity == JVector.Zero) continue;

                    foreach (RigidBodyShape shape in body.Shapes)
                    {
                        // Discovery can change velocities of previously sleeping bodies as well.
                        if (pass > 0) DynamicTree.Update(shape, timeStep.StepDt);
                        ccdShapes.Add(shape);
                    }
                }
                ccdShapes.Sort(static (a, b) => a.ShapeId.CompareTo(b.ShapeId));

                for (int i = 0; i < ccdShapes.Count; i++)
                {
                    if (i == ccdQueries.Count) ccdQueries.Add(new CcdQuery(this));
                    ccdQueries[i].Shape = ccdShapes[i];
                }

                if (multiThread && ccdShapes.Count >= 64)
                {
                    int tasks = Math.Min(ThreadPool.Instance.ThreadCount * 4, (ccdShapes.Count + 15) / 16);
                    Parallel.ForBatch(0, ccdShapes.Count, tasks, findCcdHits);
                }
                else FindCcdHits(new Parallel.Batch(0, ccdShapes.Count));

                // Registration changes duplicate selection for later sources. Keep all predicted
                // hits until this ordered pass, while workers only read the tree and body state.
                for (int i = 0; i < ccdShapes.Count; i++)
                {
                    RegisterCcdContact(ccdQueries[i]);
                    ccdQueries[i].Clear();
                }

                if (ccdContacts.Count == 0) break;

                // Keep the geometry of contacts that have already applied impulses. A later sweep
                // can add another point, including another corner of the same box against a wall.
                foreach (Arbiter arbiter in ccdContacts)
                {
                    ref ContactData contact = ref arbiter.Handle.Data;
                    contact.SkipWarmStart();
                    contact.PrepareForIteration(timeStep);
                }

                foreach (Arbiter arbiter in ccdContacts) arbiter.Handle.Data.Iterate(false);
            }
        }
        finally
        {
            // These impulses are already in the velocities. The regular solver must retain them
            // as its accumulated solution without applying them a second time during preparation.
            foreach (Arbiter arbiter in ccdContacts) arbiter.Handle.Data.SkipWarmStart();
            IsDiscoveringCcdContacts = false;
            for (int i = ccdBodies.Count - 1; i >= 0; i--)
            {
                RigidBody body = ccdBodies[i];
                if (body.EnableCcd) continue;
                ccdBodySet.Remove(body);
                ccdBodies.RemoveAt(i);
                foreach (RigidBodyShape shape in body.Shapes) DynamicTree.Update(shape);
            }
            for (int i = 0; i < Math.Min(ccdShapes.Count, ccdQueries.Count); i++) ccdQueries[i].Clear();
            ccdShapes.Clear();
            ccdContacts.Clear();
            ccdPairs.Clear();
        }
    }

    private void FindCcdHits(Parallel.Batch batch)
    {
        for (int i = batch.Start; i < batch.End; i++)
        {
            CcdQuery query = ccdQueries[i];
            DynamicTree.Query(ref query, query.Shape.WorldBoundingBox);
        }
    }

    private void FindCcdHit(CcdQuery query, IDynamicTreeProxy proxy)
    {
        RigidBodyShape shape = query.Shape;
        if (!DynamicTree.Filter(shape, proxy)) return;
        if (proxy is not RigidBodyShape candidate) return;
        if (candidate.RigidBody == shape.RigidBody) return;

        RigidBodyShape a = shape, b = candidate;
        if (b.ShapeId < a.ShapeId) (a, b) = (b, a);
        ccdPairs.TryGetValue(new ArbiterKey(a.ShapeId, b.ShapeId), out Arbiter? knownPair);
        if (knownPair != null &&
            (knownPair.Handle.Data.UsageMask & ContactData.MaskContactAll) == ContactData.MaskContactAll) return;
        if (BroadPhaseFilter != null && !BroadPhaseFilter.Filter(a, b)) return;

        ref RigidBodyData dataA = ref a.RigidBody.Data;
        ref RigidBodyData dataB = ref b.RigidBody.Data;
        if (dataA.MotionType != MotionType.Dynamic && dataB.MotionType != MotionType.Dynamic) return;

        Real radiusA = AngularExtent(a), radiusB = AngularExtent(b);
        Real speed = (dataB.Velocity - dataA.Velocity).Length()
            + radiusA * dataA.AngularVelocity.Length() + radiusB * dataB.AngularVelocity.Length();
        if (speed < CcdVelocityThreshold) return;

        NarrowPhase.SweepStatus status = NarrowPhase.Sweep(a, b, dataA.Orientation, dataB.Orientation,
            dataA.Position, dataB.Position, dataA.Velocity, dataB.Velocity,
            dataA.AngularVelocity, dataB.AngularVelocity, radiusA, radiusB, timeStep.StepDt,
            out JVector pointA, out JVector pointB, out JVector normal, out Real toi);
        if (status != NarrowPhase.SweepStatus.Hit || toi == 0) return;

        query.Hits.Add(new CcdHit
        {
            Candidate = candidate, PointA = pointA, PointB = pointB, Normal = normal, Time = toi
        });
    }

    private void RegisterCcdContact(CcdQuery query)
    {
        RigidBodyShape? hitA = null, hitB = null;
        JVector hitPointA = default, hitPointB = default, hitNormal = default;
        Real firstTime = Real.PositiveInfinity;
        ulong firstCandidateId = ulong.MaxValue;

        foreach (CcdHit hit in query.Hits)
        {
            RigidBodyShape a = query.Shape, b = hit.Candidate;
            if (b.ShapeId < a.ShapeId) (a, b) = (b, a);
            ccdPairs.TryGetValue(new ArbiterKey(a.ShapeId, b.ShapeId), out Arbiter? knownPair);
            if (knownPair != null &&
                (knownPair.Handle.Data.UsageMask & ContactData.MaskContactAll) == ContactData.MaskContactAll) continue;
            if (hit.Time > firstTime || (hit.Time == firstTime && hit.Candidate.ShapeId >= firstCandidateId)) continue;

            JVector pointA = hit.PointA, pointB = hit.PointB, normal = hit.Normal;
            Real penetration = JVector.Dot(normal, pointA - pointB) * CcdRelaxationFactor;
            if (NarrowPhaseFilter != null &&
                !NarrowPhaseFilter.Filter(a, b, ref pointA, ref pointB, ref normal, ref penetration)) continue;
            if (knownPair != null && !IsNewCcdPoint(knownPair, pointA)) continue;

            firstTime = hit.Time;
            firstCandidateId = hit.Candidate.ShapeId;
            hitA = a;
            hitB = b;
            hitPointA = pointA;
            hitPointB = pointB;
            hitNormal = normal;
        }

        if (hitA == null || hitB == null) return;

        ArbiterKey key = new(hitA.ShapeId, hitB.ShapeId);
        if (ccdPairs.TryGetValue(key, out Arbiter? arbiter))
        {
            // Persistence controls caching between frames. Even without it, retain this step's
            // discovered points and impulses instead of clearing them in RegisterContact.
            arbiter.Handle.Data.AddContact(hitPointA, hitPointB, hitNormal);
        }
        else
        {
            GetOrCreateArbiter(hitA.ShapeId, hitB.ShapeId, hitA.RigidBody, hitB.RigidBody, out arbiter);
            RegisterContact(arbiter, hitPointA, hitPointB, hitNormal);
            arbiter.Handle.Data.ResetImpulses();
            ccdPairs.Add(key, arbiter);
            ccdContacts.Add(arbiter);
        }

        // Follow the recipients, rather than requiring users to anticipate which bodies an impact moves.
        if (arbiter.Body1.MotionType == MotionType.Dynamic) AddCcdBody(arbiter.Body1);
        if (arbiter.Body2.MotionType == MotionType.Dynamic) AddCcdBody(arbiter.Body2);
    }

    private static bool IsNewCcdPoint(Arbiter arbiter, in JVector point)
    {
        ref ContactData data = ref arbiter.Handle.Data;
        JVector relative = point - arbiter.Body1.Position;
        const Real thresholdSquared = ContactData.Contact.BreakThreshold * ContactData.Contact.BreakThreshold;
        return ((data.UsageMask & ContactData.MaskContact0) == 0 ||
                (data.Contact0.RelativePosition1 - relative).LengthSquared() >= thresholdSquared)
            && ((data.UsageMask & ContactData.MaskContact1) == 0 ||
                (data.Contact1.RelativePosition1 - relative).LengthSquared() >= thresholdSquared)
            && ((data.UsageMask & ContactData.MaskContact2) == 0 ||
                (data.Contact2.RelativePosition1 - relative).LengthSquared() >= thresholdSquared)
            && ((data.UsageMask & ContactData.MaskContact3) == 0 ||
                (data.Contact3.RelativePosition1 - relative).LengthSquared() >= thresholdSquared);
    }

    private static Real AngularExtent(RigidBodyShape shape)
    {
        ref RigidBodyData data = ref shape.RigidBody.Data;
        shape.CalculateBoundingBox(data.Orientation, data.Position, out JBoundingBox box);
        return JVector.Max(JVector.Abs(box.Min - data.Position), JVector.Abs(box.Max - data.Position)).Length();
    }
}
