namespace Sewer56.StructuredDiff.Tests.Diff;

public unsafe class DecodeTests
{
    // These tests assume encode works.
    
    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 1)]
    [InlineData(127, 1)]
    [InlineData(128, 3)]
    [InlineData(65662, 3)]
    [InlineData(65663, 7)]
    [InlineData(4295032957, 7)]
    [InlineData(4295032958, 15)]
    public void DecodeSkip(long skipLength, long expectedEncodedDecoded)
    {
        var bytes = new byte[expectedEncodedDecoded];
        fixed (byte* bytePtr = &bytes[0])
        {
            var numEncoded = S56DiffEncoder.EncodeSkip((nuint)skipLength, bytePtr);
            Assert.Equal((nuint)expectedEncodedDecoded, numEncoded);

            var numDecoded = S56DiffDecoder.DecodeSkip(bytePtr, out var numToSkip);
            Assert.Equal(numEncoded, numDecoded);
            Assert.Equal((nuint)skipLength, numToSkip);
        }
    }
    
    [Theory]
    [InlineData(2, new byte[] { 0x69 })]
    [InlineData(3, new byte[] { 0x69, 0x70 })]
    public void DecodeCopy(int expectedEncoded, byte[] data)
    {
        // Note to self: Maybe test the longer ones too. For now I know they work but not being lazy would be good.
        var bytes = new byte[expectedEncoded];
        fixed (byte* dataPtr = &data[0])
        fixed (byte* bytePtr = &bytes[0])
        {
            var numEncoded = S56DiffEncoder.EncodeCopy(dataPtr, (nuint)data.Length, bytePtr);
            Assert.Equal((nuint)expectedEncoded, numEncoded);

            var numDecoded = S56DiffDecoder.DecodeCopy(bytePtr, out _, out var numToCopy);
            Assert.Equal(numEncoded, numDecoded);
            Assert.Equal((nuint)data.Length, numToCopy);
        }
    }
}