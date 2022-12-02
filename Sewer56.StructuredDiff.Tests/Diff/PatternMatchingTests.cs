namespace Sewer56.StructuredDiff.Tests.Diff;

public class PatternMatchingTests
{
    [Theory]
    [InlineData(7, TestMethod.Long)] // Under register length (Normal)
    [InlineData(8, TestMethod.Long)] // On register length (Normal)
    [InlineData(9, TestMethod.Long)] // Over register length (Normal)
    [InlineData(10, TestMethod.Long)] // Over register length (Normal)
    [InlineData(15, TestMethod.Sse2)] // Under register length (SSE)
    [InlineData(16, TestMethod.Sse2)] // On register length (SSE)
    [InlineData(17, TestMethod.Sse2)] // Over register length (SSE)
    [InlineData(18, TestMethod.Sse2)] // Over register length (SSE)
    [InlineData(31, TestMethod.Avx2)] // Under register length (AVX)
    [InlineData(32, TestMethod.Avx2)] // On register length (AVX)
    [InlineData(33, TestMethod.Avx2)] // Over register length (AVX)
    [InlineData(34, TestMethod.Avx2)] // Over register length (AVX)
    public void FindLongestMatch(int numBytes, TestMethod method)
    {
        var extraPadding = method switch
        {
            TestMethod.Long => 8,
            TestMethod.Avx2 => 32,
            TestMethod.Sse2 => 16,
            _ => throw new ArgumentOutOfRangeException(nameof(method), method, null)
        };

        FindLongestMatchCore(numBytes, method, extraPadding);
    }
    
    [Theory]
    [InlineData(7, TestMethod.Long)] // Under register length (Normal)
    [InlineData(8, TestMethod.Long)] // On register length (Normal)
    [InlineData(9, TestMethod.Long)] // Over register length (Normal)
    [InlineData(10, TestMethod.Long)] // Over register length (Normal)
    [InlineData(15, TestMethod.Sse2)] // Under register length (SSE)
    [InlineData(16, TestMethod.Sse2)] // On register length (SSE)
    [InlineData(17, TestMethod.Sse2)] // Over register length (SSE)
    [InlineData(18, TestMethod.Sse2)] // Over register length (SSE)
    [InlineData(31, TestMethod.Avx2)] // Under register length (AVX)
    [InlineData(32, TestMethod.Avx2)] // On register length (AVX)
    [InlineData(33, TestMethod.Avx2)] // Over register length (AVX)
    [InlineData(34, TestMethod.Avx2)] // Over register length (AVX)
    public void FindLongestMatch_WithoutPadding(int numBytes, TestMethod method)
    {
        FindLongestMatchCore(numBytes, method, 0);
    }

    private unsafe void FindLongestMatchCore(int numBytes, TestMethod method, int extraPadding)
    {
        var src = GenerateByteArrayForLongestMatch(numBytes, extraPadding);
        var dst = GenerateByteArrayForLongestMatch(numBytes, extraPadding);

        fixed (byte* srcPtr = &src[0])
        fixed (byte* dstPtr = &dst[0])
        {
            var result = method switch
            {
                TestMethod.Long => S56DiffEncoder.FindLongestMatchLong(srcPtr, dstPtr, (nuint)(numBytes + extraPadding)),
                TestMethod.Avx2 => S56DiffEncoder.FindLongestMatchAvx2(srcPtr, dstPtr, (nuint)(numBytes + extraPadding)),
                TestMethod.Sse2 => S56DiffEncoder.FindLongestMatchSse2(srcPtr, dstPtr, (nuint)(numBytes + extraPadding)),
                _ => throw new ArgumentOutOfRangeException(nameof(method), method, null)
            };

            Assert.Equal((nuint)numBytes, result);
        }
    }

    [Theory]
    [InlineData(7, TestMethod.Long)] // Under register length (Normal)
    [InlineData(8, TestMethod.Long)] // On register length (Normal)
    [InlineData(9, TestMethod.Long)] // Over register length (Normal)
    [InlineData(10, TestMethod.Long)] // Over register length (Normal)
    [InlineData(15, TestMethod.Sse2)]   // Under register length (SSE)
    [InlineData(16, TestMethod.Sse2)]   // On register length (SSE)
    [InlineData(17, TestMethod.Sse2)]   // Over register length (SSE)
    [InlineData(18, TestMethod.Sse2)]   // Over register length (SSE)
    [InlineData(31, TestMethod.Avx2)]   // Under register length (AVX)
    [InlineData(32, TestMethod.Avx2)]   // On register length (AVX)
    [InlineData(33, TestMethod.Avx2)]   // Over register length (AVX)
    [InlineData(34, TestMethod.Avx2)]   // Over register length (AVX)
    public void FindLongestMismatch(int numBytes, TestMethod method)
    {
        var extraPadding = method switch
        {
            TestMethod.Long => 8,
            TestMethod.Avx2 => 32,
            TestMethod.Sse2 => 16,
            _ => throw new ArgumentOutOfRangeException(nameof(method), method, null)
        };

        FindLongestMismatchCore(numBytes, method, extraPadding);
    }
    
    [Theory]
    [InlineData(7, TestMethod.Long)]  // Under register length (Normal)
    [InlineData(8, TestMethod.Long)]  // On register length (Normal)
    [InlineData(9, TestMethod.Long)]  // Over register length (Normal)
    [InlineData(10, TestMethod.Long)]  // Over register length (Normal)
    [InlineData(15, TestMethod.Sse2)] // Under register length (SSE)
    [InlineData(16, TestMethod.Sse2)] // On register length (SSE)
    [InlineData(17, TestMethod.Sse2)] // Over register length (SSE)
    [InlineData(18, TestMethod.Sse2)] // Over register length (SSE)
    [InlineData(31, TestMethod.Avx2)] // Under register length (AVX)
    [InlineData(32, TestMethod.Avx2)] // On register length (AVX)
    [InlineData(33, TestMethod.Avx2)] // Over register length (AVX)
    [InlineData(34, TestMethod.Avx2)] // Over register length (AVX)
    public void FindLongestMismatch_WithoutPadding(int numBytes, TestMethod method)
    {
        FindLongestMismatchCore(numBytes, method, 0);
    }

    private unsafe void FindLongestMismatchCore(int numBytes, TestMethod method, int extraPadding)
    {
        GenerateByteArraysForLongestMismatch(numBytes, extraPadding, out var src, out var dst);
        fixed (byte* srcPtr = &src[0])
        fixed (byte* dstPtr = &dst[0])
        {
            var result = method switch
            {
                TestMethod.Long => S56DiffEncoder.FindLongestMismatchLong(srcPtr, dstPtr, (nuint)(numBytes + extraPadding)),
                TestMethod.Avx2 => S56DiffEncoder.FindLongestMismatchAvx2(srcPtr, dstPtr, (nuint)(numBytes + extraPadding)),
                TestMethod.Sse2 => S56DiffEncoder.FindLongestMismatchSse2(srcPtr, dstPtr, (nuint)(numBytes + extraPadding)),
                _ => throw new ArgumentOutOfRangeException(nameof(method), method, null)
            };

            Assert.Equal((nuint)numBytes, result);
        }
    }

    public enum TestMethod
    {
        Long,
        Avx2,
        Sse2
    }

    private byte[] GenerateByteArrayForLongestMatch(int length, int extraRandomPadding)
    {
        var result = new byte[length + extraRandomPadding];
        for (int x = 0; x < result.Length; x++)
            result[x] = (byte)x;

        for (int x = 0; x < extraRandomPadding; x++)
        {
            // Introduce some randomness of matching and mismatching
            if (x > 1 && x % 2 == 0)
                result[x + length] = (byte)x;
            else
                result[x + length] = (byte)Random.Shared.Next(0, int.MaxValue);
        }

        return result;
    }
    
    private void GenerateByteArraysForLongestMismatch(int length, int extraMatchingPadding, out byte[] first, out byte[] second)
    {
        first  = new byte[length + extraMatchingPadding];
        second = new byte[length + extraMatchingPadding];
        for (int x = 0; x < first.Length; x++)
        {
            first[x] = (byte)x;
            second[x] = (byte)(x + 1);
        }

        for (int x = 0; x < extraMatchingPadding; x++)
        {
            // Introduce some randomness of matching and mismatching
            if (x > 2 && x % 2 == 0)
            {
                first[x + length] = (byte)x;
                second[x + length] = (byte)((byte)x + 1);   
            }
            else
            {
                first[x + length] = (byte)x;
                second[x + length] = (byte)x;
            }
        }
    }
}