using Sewer56.StructuredDiff.Interfaces;
using Xunit.Abstractions;

namespace Sewer56.StructuredDiff.Tests.Diff;

public unsafe class EncoderTest
{
    private readonly ITestOutputHelper _testOutputHelper;

    public EncoderTest(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Fact]
    public void EncoderTestRun()
    {
        var before = File.ReadAllBytes(Assets.SkillBefore);
        var after = File.ReadAllBytes(Assets.SkillAfter);
        var decoded = new byte[after.Length];
        var destination = new byte[S56DiffEncoder.CalculateMaxDestinationLength(after.Length)];
        
        fixed (byte* beforePtr = &before[0])
        fixed (byte* afterPtr = &after[0])
        fixed (byte* decodedPtr = &decoded[0])
        fixed (byte* destinationPtr = &destination[0])
        {
            var numEncoded = S56DiffEncoder.Encode(beforePtr, afterPtr, destinationPtr, (nuint)before.Length, (nuint)after.Length);
            var numDecoded = S56DiffDecoder.Decode(beforePtr, destinationPtr, decodedPtr, numEncoded);
            _testOutputHelper.WriteLine($"Encoded: {numEncoded}");
            Assert.Equal(numEncoded, numDecoded);
            Assert.Equal(after, decoded);
        }
    }
    
    [Fact]
    public void EncoderTestRun_WithExtraDataAfter()
    {
        var before = File.ReadAllBytes(Assets.SkillBefore);
        var after = File.ReadAllBytes(Assets.SkillAfter).Concat(GenerateRandomByteArray(1024)).ToArray();
        var decoded = new byte[after.Length];
        var destination = new byte[S56DiffEncoder.CalculateMaxDestinationLength(after.Length)];
        
        fixed (byte* beforePtr = &before[0])
        fixed (byte* afterPtr = &after[0])
        fixed (byte* decodedPtr = &decoded[0])
        fixed (byte* destinationPtr = &destination[0])
        {
            var numEncoded = S56DiffEncoder.Encode(beforePtr, afterPtr, destinationPtr, (nuint)before.Length, (nuint)after.Length);
            var numDecoded = S56DiffDecoder.Decode(beforePtr, destinationPtr, decodedPtr, numEncoded);
            _testOutputHelper.WriteLine($"Encoded: {numEncoded}");
            Assert.Equal(numEncoded, numDecoded);
            Assert.Equal(after, decoded);
        }
    }
    
    [Fact]
    public void EncoderTestRun_With4ByteResolver()
    {
        var before = File.ReadAllBytes(Assets.SkillBefore);
        var after = File.ReadAllBytes(Assets.SkillAfter);
        var decoded = new byte[after.Length];
        var destination = new byte[S56DiffEncoder.CalculateMaxDestinationLength(after.Length)];
        
        fixed (byte* beforePtr = &before[0])
        fixed (byte* afterPtr = &after[0])
        fixed (byte* decodedPtr = &decoded[0])
        fixed (byte* destinationPtr = &destination[0])
        {
            var numEncoded = S56DiffEncoder.Encode(beforePtr, afterPtr, destinationPtr, (nuint)before.Length, (nuint)after.Length, new FourByteResolver());
            var numDecoded = S56DiffDecoder.Decode(beforePtr, destinationPtr, decodedPtr, numEncoded);
            _testOutputHelper.WriteLine($"Encoded: {numEncoded}");
            Assert.Equal(numEncoded, numDecoded);
            Assert.Equal(after, decoded);
        }
    }

    private struct FourByteResolver : IEncoderFieldResolver
    {
        public bool Resolve(nuint offset, out int moveBy, out int length)
        {
            var fourByteAligned = offset / 4 * 4;
            moveBy = (int)(offset - fourByteAligned);
            length = 4;
            return true;
        }
    }
    
    private byte[] GenerateRandomByteArray(int length)
    {
        var result = new byte[length];
        for (int x = 0; x < result.Length; x++)
            result[x] = (byte)Random.Shared.Next(0, int.MaxValue);
        
        return result;
    }
}