namespace Sewer56.StructuredDiff.Tests.Diff;

public unsafe class EncoderTest
{
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
            Assert.Equal(numEncoded, numDecoded);
            Assert.Equal(after, decoded);
        }
    }
}