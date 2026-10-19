/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

namespace Jitter2.Dynamics.Constraints;

/// <summary>Selects the optional drive created with a joint.</summary>
public enum JointDrive
{
    /// <summary>Creates the mechanical joint without a drive.</summary>
    None,

    /// <summary>Adds a motor that drives relative velocity.</summary>
    Motor,

    /// <summary>Adds a servo that drives toward a target position or angle.</summary>
    Servo
}
