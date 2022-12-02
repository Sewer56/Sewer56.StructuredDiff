namespace Sewer56.StructuredDiff.Offsets;

/// <summary>
/// Defines a physical offset range with a minimum and maximum address.
/// </summary>
public struct OffsetRange
{
    /// <summary>
    /// Represents the first byte of the offset.
    /// </summary>
    public nuint Start;

    /// <summary>
    /// Represents the end of the offset. [Exclusive]
    /// </summary>
    public nuint End;

    /// <summary>
    /// The length of this offset range.
    /// </summary>
    public nuint Length => End - Start;

    /// <summary>
    /// Creates an offset range from a given start and end offset.
    /// </summary>
    /// <param name="start">The start offset.</param>
    /// <param name="end">The end offset.</param>
    public OffsetRange(nuint start, nuint end)
    {
        Start = start;
        End = end;
    }

    /// <summary>
    /// Creates an offset range from a given start and end offset.
    /// </summary>
    /// <param name="start">The start offset.</param>
    /// <param name="end">The end offset.</param>
    public static OffsetRange FromStartAndEnd(nuint start, nuint end) => new OffsetRange(start, end);

    /// <summary>
    /// Creates an offset range from a given start and length.
    /// </summary>
    /// <param name="start">The start offset.</param>
    /// <param name="length">Length of the offset range.</param>
    public static OffsetRange FromStartAndLength(nuint start, nuint length) => new OffsetRange(start, start + length);

    /// <summary>
    /// Returns true if a number "point", is between min and max of address range.
    /// </summary>
    /// <param name="range">The range to check.</param>
    /// <param name="point">The offset to check if between <see cref="Start"/> [inclusive] and <see cref="End"/> [inclusive].</param>
    public static bool PointInRange(ref OffsetRange range, nuint point)
    {
        if (point >= range.Start && point < range.End)
            return true;

        return false;
    }

    /// <summary>
    /// Gets the offset of the point inside the range.
    /// </summary>
    /// <returns>Offset of the point inside the range.</returns>
    public static int PointOffset(ref OffsetRange range, nuint point)
    {
        return (int)(point - range.Start);
    }

    /// <inheritdoc />
    public override string ToString() => $"{Start}-{End}";
}