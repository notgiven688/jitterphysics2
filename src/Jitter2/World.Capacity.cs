/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

using System;

namespace Jitter2;

public partial class World
{
    public struct Capacity : IEquatable<Capacity>
    {
        // Offsets for customizable counts
        private int bodyCountOffset;
        private int contactCountOffset;
        private int constraintCountOffset;
        private int smallConstraintCountOffset;

        // Default values for each property
        public const int DefaultBodyCount = 32768;
        public const int DefaultContactCount = 65536;
        public const int DefaultConstraintCount = 32768;
        public const int DefaultSmallConstraintCount = 32768;

        /// <summary>
        /// Gets or sets the total number of bodies.
        /// The value will always be at least <see cref="DefaultBodyCount"/>.
        /// </summary>
        public int BodyCount
        {
            readonly get => DefaultBodyCount + bodyCountOffset;
            set
            {
                ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value, nameof(value));
                bodyCountOffset = value - DefaultBodyCount;
            }
        }

        /// <summary>
        /// Gets or sets the total number of contacts.
        /// </summary>
        public int ContactCount
        {
            readonly get => DefaultContactCount + contactCountOffset;
            set
            {
                ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value, nameof(value));
                contactCountOffset = value - DefaultContactCount;
            }
        }

        /// <summary>
        /// Gets or sets the total number of constraints.
        /// </summary>
        public int ConstraintCount
        {
            readonly get => DefaultConstraintCount + constraintCountOffset;
            set
            {
                ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value, nameof(value));
                constraintCountOffset = value - DefaultConstraintCount;
            }
        }

        /// <summary>
        /// Gets or sets the total number of small constraints.
        /// </summary>
        public int SmallConstraintCount
        {
            readonly get => DefaultSmallConstraintCount + smallConstraintCountOffset;
            set
            {
                ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value, nameof(value));
                smallConstraintCountOffset = value - DefaultSmallConstraintCount;
            }
        }

        /// <summary>
        /// Uses the default values for world creation.
        /// </summary>
        /// <remarks>
        /// The default values are:
        /// <list type="bullet">
        /// <item>BodyCount: 32,768</item>
        /// <item>ContactCount: 65,536</item>
        /// <item>ConstraintCount: 32,768</item>
        /// <item>SmallConstraintCount: 32,768</item>
        /// </list>
        /// </remarks>
        public static Capacity Default => new();

        /// <summary>
        /// Returns a string representation of the <see cref="Capacity"/>.
        /// </summary>
        /// <returns>A string that represents the current state of the <see cref="Capacity"/>.</returns>
        public readonly override string ToString()
        {
            return $"BodyCount: {BodyCount}, ContactCount: {ContactCount}, ConstraintCount: {ConstraintCount}, SmallConstraintCount: {SmallConstraintCount}";
        }

        /// <summary>
        /// Determines whether the specified <see cref="Capacity"/> is equal to the current <see cref="Capacity"/>.
        /// </summary>
        /// <param name="other">The <see cref="Capacity"/> to compare with the current <see cref="Capacity"/>.</param>
        /// <returns><c>true</c> if the specified <see cref="Capacity"/> is equal to the current <see cref="Capacity"/>; otherwise, <c>false</c>.</returns>
        public readonly bool Equals(Capacity other)
        {
            return BodyCount == other.BodyCount &&
                   ContactCount == other.ContactCount &&
                   ConstraintCount == other.ConstraintCount &&
                   SmallConstraintCount == other.SmallConstraintCount;
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current <see cref="Capacity"/>.
        /// </summary>
        /// <param name="obj">The object to compare with the current <see cref="Capacity"/>.</param>
        /// <returns><c>true</c> if the specified object is equal to the current <see cref="Capacity"/>; otherwise, <c>false</c>.</returns>
        public readonly override bool Equals(object? obj)
        {
            return obj is Capacity other && Equals(other);
        }

        /// <summary>
        /// Returns a hash code for the current <see cref="Capacity"/>.
        /// </summary>
        /// <returns>A hash code for the current <see cref="Capacity"/>.</returns>
        public readonly override int GetHashCode()
        {
            return HashCode.Combine(BodyCount, ContactCount, ConstraintCount, SmallConstraintCount);
        }

        /// <summary>
        /// Compares two <see cref="Capacity"/> instances for equality.
        /// </summary>
        /// <param name="left">The left instance to compare.</param>
        /// <param name="right">The right instance to compare.</param>
        /// <returns><c>true</c> if the two instances are equal; otherwise, <c>false</c>.</returns>
        public static bool operator ==(Capacity left, Capacity right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Compares two <see cref="Capacity"/> instances for inequality.
        /// </summary>
        /// <param name="left">The left instance to compare.</param>
        /// <param name="right">The right instance to compare.</param>
        /// <returns><c>true</c> if the two instances are not equal; otherwise, <c>false</c>.</returns>
        public static bool operator !=(Capacity left, Capacity right)
        {
            return !(left == right);
        }
    }
}