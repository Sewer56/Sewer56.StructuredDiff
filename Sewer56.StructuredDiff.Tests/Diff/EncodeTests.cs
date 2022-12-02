namespace Sewer56.StructuredDiff.Tests.Diff;

public unsafe class EncodeTests
{
    [Theory]
    [InlineData(1, 1, new byte[] { 0x00 })]
    [InlineData(2, 1, new byte[] { 0x01 })]
    [InlineData(127, 1, new byte[] { 0b0111_1110 })]
    [InlineData(128, 3, new byte[] { 0b0111_1111, 0b0000_0000, 0b0000_0000 })]
    [InlineData(65662, 3, new byte[] { 0b0111_1111, 0b1111_1110, 0b1111_1111 })]
    [InlineData(65663, 7, new byte[] { 0b0111_1111, 0b1111_1111, 0b1111_1111, 0b0000_0000, 0b0000_0000, 0b0000_0000, 0b0000_0000 })]
    [InlineData(4295032957, 7, new byte[] { 0b0111_1111, 0b1111_1111, 0b1111_1111, 0b1111_1110, 0b1111_1111, 0b1111_1111, 0b1111_1111 })]
    [InlineData(4295032958, 15, new byte[] { 0b0111_1111, 0b1111_1111, 0b1111_1111, 0b1111_1111, 0b1111_1111, 0b1111_1111, 0b1111_1111, 0, 0, 0, 0, 0, 0, 0, 0 })]
    public void EncodeSkip(long skipLength, long expectedEncoded, byte[] expectedResult)
    {
        var bytes = new byte[expectedEncoded];
        fixed (byte* bytePtr = &bytes[0])
        {
            var numEncoded = S56DiffEncoder.EncodeSkip((nuint)skipLength, bytePtr);
            Assert.Equal((nuint)expectedEncoded, numEncoded);
            Assert.Equal(expectedResult, bytes);
        }
    }
    
    [Theory]
    [InlineData(1, 2, new byte[] { 0x69 }, new byte[] { 0b1000_0000, 0x69 })]
    [InlineData(2, 3, new byte[] { 0x69, 0x70 }, new byte[] { 0b1000_0001, 0x69, 0x70 })]
    public void EncodeCopy(long copyLength, int expectedEncoded, byte[] data, byte[] expectedResult)
    {
        // Note to self: Maybe test the longer ones too. For now I know they work but not being lazy would be good.
        var bytes = new byte[expectedEncoded];
        fixed (byte* dataPtr = &data[0])
        fixed (byte* bytePtr = &bytes[0])
        {
            var numEncoded = S56DiffEncoder.EncodeCopy(dataPtr, (nuint)copyLength, bytePtr);
            Assert.Equal((nuint)expectedEncoded, numEncoded);
            Assert.Equal(expectedResult, bytes);
        }
    }
}