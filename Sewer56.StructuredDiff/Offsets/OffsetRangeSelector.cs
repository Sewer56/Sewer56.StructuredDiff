using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Sewer56.StructuredDiff.Offsets;

/// <summary>
/// Utility class for picking the index of the correct offset range
/// based on a given address to find.
/// </summary>
public struct OffsetRangeSelector
{
    /// <summary>
    /// The ranges assigned to this selector,
    /// sorted in ascending order.
    /// </summary>
    public OffsetRange[] Offsets { get; private set; }

    /// <summary/>
    /// <param name="offsets">
    ///     Ranges we will search within. Must be sorted in ascending order.
    /// </param>
    public OffsetRangeSelector(OffsetRange[] offsets)
    {
        Offsets = offsets;
    }

    /// <summary>
    /// Gets the index of the first <see cref="OffsetRange"/> that contains the given offset.
    /// </summary>
    /// <param name="offset">The offset to find in the offset range.</param>
    /// <returns>Index of the element which contains this offset. Otherwise -1 if not found.</returns>
    public int Select(nuint offset)
    {
        return Offsets.Length >= 13 ? SelectLoop(offset) : SelectBinarySearch(offset);
    }

    /// <summary>
    /// Gets the index of the first <see cref="OffsetRange"/> that contains the given offset, using a for loop.
    /// </summary>
    /// <param name="offset">The offset to check for.</param>
    /// <returns>Index of the element which contains this offset. Otherwise -1 if not found.</returns>
    public int SelectLoop(nuint offset)
    {
        for (int x = 0; x < Offsets.Length; x++)
        {
            if (OffsetRange.PointInRange(ref Offsets[x], offset))
                return x;
        }

        return -1;
    }

    /// <summary>
    /// Gets the index of the first <see cref="OffsetRange"/> that contains the given offset, using a binary search.
    /// </summary>
    /// <param name="offset">The offset to check for.</param>
    /// <returns>Index of the element which contains this offset. Otherwise -1 if not found.</returns>
    public int SelectBinarySearch(nuint offset) => BinarySearchOffset(Offsets, offset);
    
    private static int BinarySearchOffset(OffsetRange[] range, nuint item)
    {
        int minPtr = 0;
        int maxPtr = range.Length - 1;

        while (minPtr <= maxPtr)
        {
            int midPtr = (minPtr + maxPtr) / 2;
            ref var midItem = ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(range), midPtr);

            if (OffsetRange.PointInRange(ref midItem, item))
                return midPtr;
              
            if (item < midItem.Start)
                maxPtr = midPtr - 1;
            else
                minPtr = midPtr + 1;
        }

        return -1;
    }
}