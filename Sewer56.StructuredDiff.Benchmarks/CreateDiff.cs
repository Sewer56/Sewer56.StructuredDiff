using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using Sewer56.StructuredDiff.Interfaces;
using Sewer56.StructuredDiff.Tests;

namespace Sewer56.StructuredDiff.Benchmarks;

[MemoryDiagnoser]
public class CreateDiff
{
    private byte[] _origArr = null!;
    private byte[] _tgtArr = null!;
    private GCHandle _orig;
    private GCHandle _tgt;
    private GCHandle _dst;

    [GlobalSetup]
    public void Setup()
    {
        _origArr = File.ReadAllBytes(Assets.SkillBefore);
        _tgtArr  = File.ReadAllBytes(Assets.SkillAfter);
        _orig    = GCHandle.Alloc(_origArr, GCHandleType.Pinned);
        _tgt     = GCHandle.Alloc(_tgtArr, GCHandleType.Pinned);
        _dst     = GCHandle.Alloc(new byte[_tgtArr.Length], GCHandleType.Pinned);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _orig.Free();
        _tgt.Free();
        _dst.Free();
    }

    [Benchmark]
    public unsafe nuint Create()
    {
        return S56DiffEncoder.Encode((byte*)_orig.AddrOfPinnedObject(), (byte*)_tgt.AddrOfPinnedObject(), (byte*)_dst.AddrOfPinnedObject(), (nuint)_origArr.Length, (nuint)_tgtArr.Length);
    }
    
    [Benchmark]
    public unsafe nuint Create4ByteStructured()
    {
        return S56DiffEncoder.Encode((byte*)_orig.AddrOfPinnedObject(), (byte*)_tgt.AddrOfPinnedObject(), (byte*)_dst.AddrOfPinnedObject(), (nuint)_origArr.Length, (nuint)_tgtArr.Length, new FourByteResolver());
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
}