using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using Sewer56.StructuredDiff.Interfaces;

[assembly:InternalsVisibleTo("Sewer56.StructuredDiff.Tests")]
namespace Sewer56.StructuredDiff;

/// <summary>
/// Encoder for Sewer's Diff Format.
/// </summary>
public unsafe class S56DiffEncoder
{
    /* See Readme for How Algorithm Works */

    /// <summary>
    /// Encodes a patch that converts source to target, without structural awareness (default resolver).
    /// </summary>
    /// <param name="source">The source data to create diff from.</param>
    /// <param name="target">The target data to diff into.</param>
    /// <param name="destination">Where the diff patch will be written out to.</param>
    /// <param name="sourceLength">Length of the source array.</param>
    /// <param name="targetLength">Length of the target array.</param>
    /// <returns>Number of bytes encoded.</returns>
    public static nuint Encode(byte* source, byte* target, byte* destination, nuint sourceLength, nuint targetLength)
    {
        return Encode(source, target, destination, sourceLength, targetLength, new UnstructuredResolver());
    }

    /// <summary>
    /// Encodes a patch that converts source to target.
    /// </summary>
    /// <param name="source">The source data to create diff from.</param>
    /// <param name="target">The target data to diff into.</param>
    /// <param name="destination">Where the diff patch will be written out to.</param>
    /// <param name="sourceLength">Length of the source array.</param>
    /// <param name="targetLength">Length of the target array.</param>
    /// <param name="resolver">Address resolver used for structural awareness.</param>
    /// <returns>Number of bytes encoded.</returns>
    public static nuint Encode<T>(byte* source, byte* target, byte* destination, nuint sourceLength, nuint targetLength, T resolver) where T : IEncoderFieldResolver
    {
        var sourcePtr = source;
        var targetPtr = target;
        var destinationPtr = destination;

        // Main Encode Loop
        nuint bytesToEncode = Math.Min(sourceLength, targetLength);
        nuint bytesLeft = bytesToEncode; // if target is smaller than source, file is not truncated. 
        while (bytesLeft > 0)
        {
            var match = FindLongestMatch(sourcePtr, targetPtr, bytesLeft);
            
            // There is no more bytes to decode.
            if (bytesLeft - match <= 0)
            {
                destinationPtr += EncodeSkip(match, destinationPtr);
                targetPtr += match;
                break;
            }

            if (match > 0)
            {
                if (resolver.Resolve((nuint)(targetPtr - target) + match, out int moveBy, out int length))
                {
                    match -= (nuint)moveBy;
                    
                    // Encode a skip then length for our mismatch.
                    if (match > 0)
                    {
                        destinationPtr += EncodeSkip(match, destinationPtr);
                        sourcePtr += match;
                        targetPtr += match;
                        bytesLeft -= match;
                    }
                    
                    destinationPtr += EncodeCopy(targetPtr, (nuint)length, destinationPtr);
                    sourcePtr += length;
                    targetPtr += length;
                    bytesLeft -= (nuint)length;
                }
                else
                {
                    // Unstructured
                    destinationPtr += EncodeSkip(match, destinationPtr);
                    sourcePtr += match;
                    targetPtr += match;
                    bytesLeft -= match;
                }
            }

            var misMatch = FindLongestMismatch(sourcePtr, targetPtr, bytesLeft);
            if (misMatch > 0)
            {
                if (resolver.Resolve((nuint)(targetPtr - target) + misMatch, out _, out int length))
                {
                    // Note: Ignoring MoveBy here is *not* a bug. Alignment with field is guaranteed from previous write
                    //       inside match > 0 above.
                    destinationPtr += EncodeCopy(targetPtr, (nuint)length, destinationPtr);
                    sourcePtr += length;
                    targetPtr += length;
                    bytesLeft -= (nuint)length;
                }
                else
                {
                    // Unstructured
                    destinationPtr += EncodeCopy(targetPtr, misMatch, destinationPtr);
                    sourcePtr += misMatch;
                    targetPtr += misMatch;
                    bytesLeft -= misMatch;
                }
            }
        }

        // Encode extra target bytes raw.
        nuint extraBytesWritten = 0;
        nuint extraTargetBytes = targetLength - sourceLength; // if target is longer than source, all bytes past end are copied verbatim.  
        if (extraTargetBytes > 0)
            extraBytesWritten += EncodeCopy(targetPtr, extraTargetBytes, destinationPtr);

        return (nuint)(destinationPtr - destination) + extraBytesWritten;
    }

    /// <summary>
    /// Encodes a skip operation into the destination array.
    /// </summary>
    /// <param name="skipLength">Length of the data to be skipped.</param>
    /// <param name="destination">Where the opcode should be written to.</param>
    /// <returns>Number of bytes written to destination.</returns>
    public static nuint EncodeSkip(nuint skipLength, byte* destination)
    {
        // Note: In this encoder we prefer encoding less tokens over
        //       so while it is smaller by 1 byte to encode 2x128 rather than 1x256, decoding 1x256 is faster.
        
        // Encode as 1 XXXXXXX
        if (skipLength <= (nuint)sbyte.MaxValue)
        {
            *destination = (byte)(skipLength - 1);
            return 1;
        }
        
        // Encode as 1 1111111 XXXXXXXX XXXXXXXX
        if (skipLength <= (ushort.MaxValue + sbyte.MaxValue))
        {
            destination[0] = 0b0111_1111;
            *(ushort*)(destination + 1) = (ushort)(skipLength - (nuint)sbyte.MaxValue - 1);
            return 3;
        }

        // Encode as 1 1111111 11111111 11111111 YYYYYYYY YYYYYYYY YYYYYYYY YYYYYYYY
        if ((long)skipLength <= ((long)uint.MaxValue + ushort.MaxValue + sbyte.MaxValue))
        {
            destination[0] = 0b0111_1111;
            *(ushort*)(destination + 1) = ushort.MaxValue;
            *(uint*)(destination + 3) = (uint)(skipLength - ushort.MaxValue - (nuint)sbyte.MaxValue - 1);
            return 7;
        }
        
        // Encode as 1 1111111 11111111 11111111 11111111 11111111 11111111 11111111
        destination[0] = 0b0111_1111;
        *(ulong*)(destination + 1) = ulong.MaxValue;
        *(ulong*)(destination + 7) = (ulong)skipLength - uint.MaxValue - ushort.MaxValue - (ulong)sbyte.MaxValue - 1;
        return 15;
    }
    
    /// <summary>
    /// Encodes a copy operation into the destination array.
    /// </summary>
    /// <param name="data">The data to be copied.</param>
    /// <param name="dataLength">Length of the data to be copied.</param>
    /// <param name="destination">Where the bytes should be written to.</param>
    /// <returns>Number of bytes written to destination.</returns>
    public static nuint EncodeCopy(byte* data, nuint dataLength, byte* destination)
    {
        // Note: In this encoder we prefer encoding less tokens over
        //       so while it is smaller by 1 byte to encode 2x128 rather than 1x256, decoding 1x256 is faster.
        
        if (dataLength <= (nuint)sbyte.MaxValue)
        {
            *destination = (byte)(1 << 7 | (byte)(dataLength - 1));
            Buffer.MemoryCopy(data, destination + 1, dataLength, dataLength);
            return dataLength + 1;
        }

        if (dataLength <= (ushort.MaxValue + sbyte.MaxValue))
        {
            destination[0] = 0b1111_1111;
            *(ushort*)(destination + 1) = (ushort)(dataLength - (nuint)sbyte.MaxValue - 1);
            Buffer.MemoryCopy(data, destination + 3, dataLength, dataLength);
            return dataLength + 3;
        }

        if ((long)dataLength <= (long)uint.MaxValue + ushort.MaxValue + sbyte.MaxValue)
        {
            *(uint*)(destination) = uint.MaxValue; // Fill with 1s
            *(uint*)(destination + 3) = (uint)((uint)dataLength - ushort.MaxValue - sbyte.MaxValue - 1);
            Buffer.MemoryCopy(data, destination + 7, dataLength, dataLength);
            return dataLength + 7;
        }
        
        *(ulong*)(destination) = ulong.MaxValue; // Fill with 1s
        *(ulong*)(destination + 7) = ((ulong)dataLength - uint.MaxValue - ushort.MaxValue - (ulong)sbyte.MaxValue - 1);
        Buffer.MemoryCopy(data, destination + 15, dataLength, dataLength);
        return dataLength + 15;
    }

    /// <summary/>
    /// <param name="targetLength">Length of the target to transform into.</param>
    /// <returns>Maximum possible length of patch.</returns>
    public static nint CalculateMaxDestinationLength(int targetLength)
    {
        // Longest possible encoding consists of write 1(s) followed by skip 1(s).
        // Meaning 2 bytes for every 1 byte skipped/written.
        return Math.Min(nint.MaxValue, targetLength * 2);
    }

    /// <summary>
    /// Finds longest sequence of matching bytes between two pointers.
    /// </summary>
    /// <param name="source">Source array.</param>
    /// <param name="destination">Destination array.</param>
    /// <param name="length">Length of smaller of the two arrays.</param>
    /// <returns>Number of bytes matching in arrays at this offset.</returns>
    public static nuint FindLongestMatch(byte* source, byte* destination, nuint length)
    {
        if (Avx2.IsSupported)
            return FindLongestMatchAvx2(source, destination, length);

        if (Sse2.IsSupported)
            return FindLongestMatchSse2(source, destination, length);

        // Non-vectorised fallback.
        return FindLongestMatchLong(source, destination, length);
    }

    internal static nuint FindLongestMatchAvx2(byte* source, byte* destination, nuint length)
    {
        const int avxRegisterLength = 32;
        var origSource = source;
        nuint GetMatchLength() => (nuint)(source - origSource);
        
        var lengthAligned = (length / avxRegisterLength * avxRegisterLength);
        var excessBytes   = length - lengthAligned;
        
        var vectorAllMatch = Vector256.Create(byte.MaxValue);
        var maxPtr = source + lengthAligned;
        while (source < maxPtr)
        {
            var srcVector = Avx.LoadVector256(source);
            var dstVector = Avx.LoadVector256(destination);
            var compare   = Avx2.CompareEqual(srcVector, dstVector); // 0xFF where match, 0 where not
            
            // All bytes match, advance to next.
            if (compare == vectorAllMatch)
            {
                source += avxRegisterLength;
                destination += avxRegisterLength;
                continue;
            }
            
            // Partial match, get from mask.
            int mask = ~Avx2.MoveMask(compare); // Compress to bits 0/1
            return (GetMatchLength() + (nuint)BitOperations.TrailingZeroCount((uint)mask)); // 32 bits in int
        }
        
        if (excessBytes > 0)
            return GetMatchLength() + FindLongestMatchByte(source, destination, excessBytes);
        
        return GetMatchLength();
    }
    
    internal static nuint FindLongestMatchSse2(byte* source, byte* destination, nuint length)
    {
        const int sseRegisterLength = 16;
        var origSource = source;
        nuint GetMatchLength() => (nuint)(source - origSource);
        
        var lengthAligned = (length / sseRegisterLength * sseRegisterLength);
        var excessBytes   = length - lengthAligned;
        
        var vectorAllMatch = Vector128.Create(byte.MaxValue);
        var maxPtr = source + lengthAligned;
        while (source < maxPtr)
        {
            var srcVector = Sse2.LoadVector128(source);
            var dstVector = Sse2.LoadVector128(destination);
            var compare   = Sse2.CompareEqual(srcVector, dstVector); // 0xFF where match, 0 where not
            
            // All bytes match, advance to next.
            if (compare == vectorAllMatch)
            {
                source += sseRegisterLength;
                destination += sseRegisterLength;
                continue;
            }
            
            // Partial match, get from mask.
            int mask = ~Sse2.MoveMask(compare); // Compress to bits 0/1
            return GetMatchLength() + (nuint)BitOperations.TrailingZeroCount((uint)mask); // 32 bits in int
        }
        
        if (excessBytes > 0)
            return GetMatchLength() + FindLongestMatchByte(source, destination, excessBytes);
        
        return GetMatchLength();
    }
    
    internal static nuint FindLongestMatchLong(byte* source, byte* destination, nuint length)
    {
        const int longLength = sizeof(long);
        var origSource = source;
        nuint GetMatchLength() => (nuint)(source - origSource);
        
        var lengthAligned = (length / longLength * longLength);
        var excessBytes   = length - lengthAligned;
        
        var maxPtr = source + lengthAligned;
        while (source < maxPtr)
        {
            // Note: This could probably be unrolled for better performance, it's just that
            // I'm not currently interested in pushing this to absolute limit.
            // Target is 64-bit applications where having SSE2 is a given, so this code path will be
            // virtually unused.
            var srcLong = *(ulong*)source;
            var dstLong = *(ulong*)destination;
            
            // All bytes match, advance to next.
            if (srcLong == dstLong)
            {
                source += longLength;
                destination += longLength;
                continue;
            }
            
            // Partial match, get from mask.
            ulong mask = srcLong ^ dstLong; // Where non-zero, is mismatch
            return GetMatchLength() + (nuint)(BitOperations.TrailingZeroCount(mask) / 8);
        }
        
        if (excessBytes > 0)
            return GetMatchLength() + FindLongestMatchByte(source, destination, excessBytes);
        
        return GetMatchLength();
    }

    private static nuint FindLongestMatchByte(byte* source, byte* destination, nuint length)
    {
        nuint result = 0;
        var maxPtr = source + length;
        while (source < maxPtr)
        {
            if (source[0] != destination[0])
                return result;

            result++;
            source++;
            destination++;
        }

        return result;
    }

    /// <summary>
    /// Finds longest sequence of mismatching bytes between two pointers.
    /// </summary>
    /// <param name="source">Source array.</param>
    /// <param name="destination">Destination array.</param>
    /// <param name="length">Length of smaller of the two arrays.</param>
    /// <returns>Number of bytes non-matching in arrays at this offset.</returns>
    public static nuint FindLongestMismatch(byte* source, byte* destination, nuint length)
    {
        if (Avx2.IsSupported)
            return FindLongestMismatchAvx2(source, destination, length);

        if (Sse2.IsSupported)
            return FindLongestMismatchSse2(source, destination, length);

        // Non-vectorised fallback.
        return FindLongestMismatchLong(source, destination, length);
    }

    internal static nuint FindLongestMismatchAvx2(byte* source, byte* destination, nuint length)
    {
        const int avxRegisterLength = 32;
        var origSource = source;
        nuint GetMatchLength() => (nuint)(source - origSource);
        
        var lengthAligned = (length / avxRegisterLength * avxRegisterLength);
        var excessBytes   = length - lengthAligned;
        
        var vectorAllMismatch = Vector256.Create((byte)0);
        var maxPtr = source + lengthAligned;
        while (source < maxPtr)
        {
            var srcVector = Avx.LoadVector256(source);
            var dstVector = Avx.LoadVector256(destination);
            var compare   = Avx2.CompareEqual(srcVector, dstVector); // 0xFF where match, 0 where not
            
            // All bytes match, advance to next.
            if (compare == vectorAllMismatch)
            {
                source += avxRegisterLength;
                destination += avxRegisterLength;
                continue;
            }
            
            // Partial match, get from mask.
            int mask = Avx2.MoveMask(compare); // Compress to bits 0/1, 1 where match, else 0.
            return GetMatchLength() + (nuint)BitOperations.TrailingZeroCount((uint)mask);
        }

        if (excessBytes > 0)
            return GetMatchLength() + FindLongestMismatchByte(source, destination, excessBytes);
        
        return GetMatchLength();
    }

    internal static nuint FindLongestMismatchSse2(byte* source, byte* destination, nuint length)
    {
        const int sseRegisterLength = 16;
        var origSource = source;
        nuint GetMatchLength() => (nuint)(source - origSource);
        
        var lengthAligned = (length / sseRegisterLength * sseRegisterLength);
        var excessBytes   = length - lengthAligned;
        
        var vectorAllMismatch = Vector128.Create((byte)0);
        var maxPtr = source + lengthAligned;
        while (source < maxPtr)
        {
            var srcVector = Sse2.LoadVector128(source);
            var dstVector = Sse2.LoadVector128(destination);
            var compare   = Sse2.CompareEqual(srcVector, dstVector); // 0xFF where match, 0 where not
            
            // All bytes match, advance to next.
            if (compare == vectorAllMismatch)
            {
                source += sseRegisterLength;
                destination += sseRegisterLength;
                continue;
            }
            
            // Partial match, get from mask.
            var mask = (ushort)Sse2.MoveMask(compare); // Compress to bits 0/1
            return GetMatchLength() + (nuint)BitOperations.TrailingZeroCount(mask);
        }
        
        if (excessBytes > 0)
            return GetMatchLength() + FindLongestMismatchByte(source, destination, excessBytes);
        
        return GetMatchLength();
    }
    
    internal static nuint FindLongestMismatchLong(byte* source, byte* destination, nuint length)
    {
        const int longLength = sizeof(long);
        var origSource = source;
        nuint GetMatchLength() => (nuint)(source - origSource);
        
        var lengthAligned = (length / longLength * longLength);
        var excessBytes = length - lengthAligned;
        
        var maxPtr = source + lengthAligned;
        while (source < maxPtr)
        {
            // Note: This could probably be better unrolled for performance, it's just that
            // I'm not currently interested in pushing this to absolute limit.
            // Target is 64-bit applications where having SSE2 is a given, so this code path will be
            // virtually unused.
            var srcLong = *(ulong*)source;
            var dstLong = *(ulong*)destination;
            
            // Where there is a match, result is 0.
            // 00 11 22 33 ^ 66 11 22 33 == 66 00 00 00 | XOR
            var xored = srcLong ^ dstLong;
            var xoredPtr = (byte*)&xored;

            if (xoredPtr[0] == 0x0) return GetMatchLength();
            if (xoredPtr[1] == 0x0) return GetMatchLength() + 1;
            if (xoredPtr[2] == 0x0) return GetMatchLength() + 2;
            if (xoredPtr[3] == 0x0) return GetMatchLength() + 3;
            if (xoredPtr[4] == 0x0) return GetMatchLength() + 4;
            if (xoredPtr[5] == 0x0) return GetMatchLength() + 5;
            if (xoredPtr[6] == 0x0) return GetMatchLength() + 6;
            if (xoredPtr[7] == 0x0) return GetMatchLength() + 7;

            source += longLength;
            destination += longLength;
        }
        
        if (excessBytes > 0)
            return GetMatchLength() + FindLongestMismatchByte(source, destination, excessBytes);
        
        return GetMatchLength();
    }
    
    internal static nuint FindLongestMismatchByte(byte* source, byte* destination, nuint length)
    {
        nuint result = 0;
        var maxPtr = source + length;
        while (source < maxPtr)
        {
            if (source[0] == destination[0])
                return result;

            result++;
            source++;
            destination++;
        }

        return result;
    }
    
    private struct UnstructuredResolver : IEncoderFieldResolver
    {
        public bool Resolve(nuint offset, out int moveBy, out int length)
        {
            moveBy = 0;
            length = -1;
            return false;
        }
    }
}