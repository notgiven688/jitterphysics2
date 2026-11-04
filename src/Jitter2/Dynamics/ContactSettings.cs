/*
 * Jitter2 Physics Library
 * Copyright (c) Thorben Linneweber and others
 * SPDX-License-Identifier: MIT
 */

using System.Runtime.InteropServices;

namespace Jitter2.Dynamics;

/// <summary>
/// Solver settings shared by all contacts of a world. Access and modify them through
/// <see cref="World.ContactSettings"/>; changes take effect for existing and new contacts alike.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct ContactSettings
{
    /// <summary>
    /// Maximum separation velocity, in world units per second, that penetration correction adds to a
    /// contact. Default is 100.
    /// </summary>
    public Real MaximumBias;

    /// <summary>Fraction of the penetration corrected per step (Baumgarte stabilization). Default is 0.2.</summary>
    public Real BiasFactor;

    /// <summary>Penetration depth below which no position correction is applied. Default is 0.01.</summary>
    public Real AllowedPenetration;

    /// <summary>
    /// Maximum tangential drift before a cached contact point is dropped; normal separation is limited
    /// to one tenth of this value. Default is 0.02.
    /// </summary>
    public Real BreakThreshold;

    /// <summary>
    /// Minimum approach speed, in world units per second, at which a contact applies restitution.
    /// Slower impacts are resolved as fully inelastic, which keeps resting and stacked bodies calm.
    /// Lower it for scenes where slow bodies must still bounce, such as billiard balls that have
    /// nearly rolled out. CCD contacts do not apply restitution. Default is 1.
    /// </summary>
    public Real RestitutionThreshold;

    /// <summary>
    /// Fraction of the predicted gap that CCD contacts allow to close during one step, from 0 (no closing
    /// motion) to 1 (allow the full gap to close). Values below 1 leave a small separation margin. Default is 0.9.
    /// </summary>
    public Real CcdRelaxationFactor;

    /// <summary>The default settings of a new world.</summary>
    public static ContactSettings Default => new()
    {
        MaximumBias = (Real)100.0,
        BiasFactor = (Real)0.2,
        AllowedPenetration = (Real)0.01,
        BreakThreshold = (Real)0.02,
        RestitutionThreshold = (Real)1.0,
        CcdRelaxationFactor = (Real)0.9,
    };
}
