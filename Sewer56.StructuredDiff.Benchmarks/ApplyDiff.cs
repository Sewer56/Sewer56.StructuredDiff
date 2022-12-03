using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using Sewer56.StructuredDiff.Tests;

namespace Sewer56.StructuredDiff.Benchmarks;

[MemoryDiagnoser]
public class ApplyDiff
{
    private byte[] _origArr = null!;
    private byte[] _tgtArr = null!;
    private byte[] _patchArr = null!;
    private GCHandle _orig;
    private GCHandle _tgt;
    private GCHandle _ptch;
    private nuint _ptchNumBytes;

    [GlobalSetup]
    public unsafe void Setup()
    {
        _origArr = File.ReadAllBytes(Assets.SkillBefore);
        _tgtArr  = File.ReadAllBytes(Assets.SkillAfter);
        _orig    = GCHandle.Alloc(_origArr, GCHandleType.Pinned);
        _tgt     = GCHandle.Alloc(_tgtArr, GCHandleType.Pinned);

        _patchArr = new byte[_tgtArr.Length];
        _ptch     = GCHandle.Alloc(_patchArr, GCHandleType.Pinned);
        _ptchNumBytes = S56DiffEncoder.Encode((byte*)_orig.AddrOfPinnedObject(), (byte*)_tgt.AddrOfPinnedObject(), (byte*)_ptch.AddrOfPinnedObject(), (nuint)_origArr.Length, (nuint)_tgtArr.Length);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _orig.Free();
        _tgt.Free();
        _ptch.Free();
    }

    [Benchmark]
    public unsafe nuint Apply()
    {
        return S56DiffDecoder.Decode((byte*)_orig.AddrOfPinnedObject(), (byte*)_ptch.AddrOfPinnedObject(), (byte*)_tgt.AddrOfPinnedObject(), _ptchNumBytes, out _);
    }
}