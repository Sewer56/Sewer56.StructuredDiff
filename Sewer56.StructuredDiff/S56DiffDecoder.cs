namespace Sewer56.StructuredDiff;

/// <summary>
/// Decoder for Sewer's diff format.
/// </summary>
public unsafe class S56DiffDecoder
{
    /// <summary>
    /// Encodes a patch that converts source to target.
    /// </summary>
    /// <param name="source">The source data to create diff from.</param>
    /// <param name="patch">The target data to diff into.</param>
    /// <param name="destination">Where the new file will be written out to.</param>
    /// <param name="patchLength">Length of the patch data.</param>
    /// <returns>Number of bytes decoded.</returns>
    public static nuint Decode(byte* source, byte* patch, byte* destination, nuint patchLength)
    {
        var destinationPtr = destination;
        var sourcePtr      = source;
        var patchPtr       = patch;
        var patchEndPtr    = patchPtr + patchLength;
        
        while (patchPtr < patchEndPtr)
        {
            var operation = *patchPtr;
            if ((operation & 0b1000_0000) == 0b1000_0000)
            {
                var numRead = DecodeCopy(patchPtr, out var dataPtr, out var numToCopy);
                Buffer.MemoryCopy(dataPtr, destinationPtr, numToCopy, numToCopy);
                patchPtr += numRead;
                sourcePtr += numToCopy;
                destinationPtr += numToCopy;
            }
            else
            {
                var numRead = DecodeSkip(patchPtr, out var numToSkip);
                Buffer.MemoryCopy(sourcePtr, destinationPtr, numToSkip, numToSkip);
                patchPtr += numRead;
                sourcePtr += numToSkip;
                destinationPtr += numToSkip;
            }
        }

        return (nuint)(patchPtr - patch);
    }

    /// <summary>
    /// Decodes a skip operation.
    /// </summary>
    /// <param name="data">Pointer where the opcode should be read from.</param>
    /// <param name="numToSkip">Number of bytes to skip.</param>
    /// <returns>Number of bytes read from data.</returns>
    public static nuint DecodeSkip(byte* data, out nuint numToSkip)
    {
        // Encoded as 0 XXXXXXX
        numToSkip = *data;
        bool isSingleByte = numToSkip != (nuint)sbyte.MaxValue;
        numToSkip += 1; // we encode as length - 1
        if (isSingleByte)
            return 1;

        var nextSkipUShort = *(ushort*)(data + 1);
        numToSkip += nextSkipUShort;
        if (nextSkipUShort != ushort.MaxValue)
            return 3;
        
        var nextSkipUInt = *(uint*)(data + 3);
        numToSkip += nextSkipUInt;
        if (nextSkipUInt != uint.MaxValue)
            return 7;
        
        var nextSkipUlong = *(ulong*)(data + 7);
        numToSkip += (nuint)nextSkipUlong;
        return 15;
    }

    /// <summary>
    /// Decodes a copy operation into the destination array.
    /// </summary>
    /// <param name="data">Pointer where the opcode should be read from.</param>
    /// <param name="dataPtr">Pointer to where the data starts.</param>
    /// <param name="numToCopy">Number of bytes to copy from <see cref="dataPtr"/>.</param>
    /// <returns>Number of bytes read from data.</returns>
    public static nuint DecodeCopy(byte* data, out byte* dataPtr, out nuint numToCopy)
    {
        // Encoded as 1 XXXXXXX
        numToCopy = (nuint)(*data & 0b01111111);
        bool isSingleByte = numToCopy != (nuint)sbyte.MaxValue;
        numToCopy += 1; // we encode as length - 1
        if (isSingleByte)
        {
            dataPtr = data + 1;
            return numToCopy + 1;
        }

        var nextCopyUShort = *(ushort*)(data + 1);
        numToCopy += nextCopyUShort;
        if (nextCopyUShort != ushort.MaxValue)
        {
            dataPtr = data + 3;
            return numToCopy + 3;
        }
        
        var nextCopyUInt = *(uint*)(data + 3);
        numToCopy += nextCopyUInt;
        if (nextCopyUInt != uint.MaxValue)
        {
            dataPtr = data + 7;
            return numToCopy + 7;
        }
        
        var nextCopyUlong = *(ulong*)(data + 7);
        numToCopy += (nuint)nextCopyUlong;
        dataPtr = data + 15;
        return numToCopy + 15;
    }
}