# Sewer56.StructuredDiff

A diff algorithm for creating diffs of binary data with known structure.  

![Nuget](https://img.shields.io/nuget/v/Sewer56.StructuredDiff)

## The Problem

Suppose one field in a struct is an uint (u32), and you want to create a set of patches that will turn one version of the data into a slightly modified version of the same data.  
For example, changing value of an object property...  

Original data: `12 00 00 00`  
Mod A: `B3 15 00 00`  
Mod B: `44 00 44 44`  

The first two bytes changed in Mod A, so after patching original, result is `B3 15 00 00`.  
However when you apply the diff of Mod B, which changes bytes 0, 2 & 3, you get `44 15 44 44`.  

This preserves the modified byte from Mod A, which may sometimes be undesirable.  
As such, this diff algorithm avoids the problem by replacing *the entire u32* provided the size of the field is known.  

## Usage

Example taken from Unit Tests:  

```csharp
var before      = File.ReadAllBytes(Assets.SkillBefore);
var after       = File.ReadAllBytes(Assets.SkillAfter);
var decoded     = new byte[after.Length];
var destination = new byte[S56DiffEncoder.CalculateMaxDestinationLength(after.Length)];
        
fixed (byte* beforePtr = &before[0])
fixed (byte* afterPtr = &after[0])
fixed (byte* decodedPtr = &decoded[0])
fixed (byte* destinationPtr = &destination[0])
{
    var numEncoded = S56DiffEncoder.Encode(beforePtr, afterPtr, destinationPtr, (nuint)before.Length, (nuint)after.Length, new FourByteResolver());
    var numDecoded = S56DiffDecoder.Decode(beforePtr, destinationPtr, decodedPtr, numEncoded);
    Assert.Equal(numEncoded, numDecoded);
    Assert.Equal(after, decoded);
}

// Example Resolver, for an array of u32s.
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
```

For big structures, might be helpful to use `OffsetRangeSelector`:  

```csharp
private struct StructResolver : IEncoderFieldResolver
{
    private static OffsetRangeSelector Selector = new OffsetRangeSelector(new OffsetRange[]
    {
        new (0, 3),   // int
        new (4, 5),   // short
        new (6, 7),   // short
        new (8, 8),   // byte
        new (9, 9),   // byte
        new (10, 10), // byte
        new (11, 11), // byte
        // ... some more 
    });
    
    public bool Resolve(nuint offset, out int moveBy, out int length)
    {
        // OffsetRangeSelector can perform binary search, as long as input ranges are valid (sorted, joined, no overlaps).
        var range = selector.Offsets[selector.SelectBinarySearch(6)]; // Search for range that fits offset 6
        moveBy = offset - range.Start;
        length = range.Length;
        return true;
    }
}
```

## Encoding Scheme

```
Sewer's Diff Format for Non-Shiftable Data.  

Encoding:

    All opcodes+operands are byte aligned to enable for faster access of data (at expense of size).

    1 = Copy Direct | XXXXXXX YYYYYYYY YYYYYYYY WWW... | Copies X bytes from the bytestream, up to 2GB.  
    0 = Skip Data   | XXXXXXX YYYYYYYY YYYYYYYY WWW... | Copies X amount of items, up to 2GB.  
                                           
Values are stored -1 from actual length, i.e. value of 0 means length 1.

    Operands are variable length, in order:
    - 7 bits (127 max)
    - 16 bits (65K max)
    - 32 bits (4GB max)
    - 64 bits (like 8000 petabytes max)
             
    These lengths are additive, i.e. to encode 128 you encode a length with 7 bits and add a length represented in 16 bits.
            
    For example, to encode a 1 byte copy, you would do:
    - 1 0000000
            
    To encode a 127 byte copy you would do:  
    - 1 1111110
            
    To encode a >= 128 byte copy, you will need to use 3 bytes:  
    - 1 1111111 XXXXXXXX XXXXXXXX
                ----------------- [16 bits extension]
            
    To encode a >= 65K copy, you will need to use 7 bytes:  
    - 1 1111111 11111111 11111111 YYYYYYYY YYYYYYYY YYYYYYYY YYYYYYYY 
                                  ----------------------------------- [32 bits extension]
        
Notes on Size:  
    If target is longer than source, all bytes past end are copied verbatim.  
    In cases target is longer than source, library user is responsible for ensuring there is enough memory, e.g. by prepending another header with length outside of library.
    
    If target is smaller than source, file is not truncated.  
    
Notes on Perf:  
    Format prioritises less tokens over space for faster decode.  
    As such it encodes 1x512 rather than 2x256 etc. even if latter saves 1 byte.  
    
    Reference implementation uses native unsigned integers. i.e. Max data size is limited to 4GB on 32-bit machines.
    Assumes Little Endian, no support for big.
```