using System;
using System.Runtime.InteropServices;
using Adaptive.Agrona;
using Adaptive.Agrona.Concurrent;

namespace DotMetrics.Codec.Metrics;

public class MappedMetricCounter : IMetricCounter, IDisposable
{
    private readonly byte[] _valueBuffer = new byte[BitUtil.SIZE_OF_DOUBLE];
    private readonly IAtomicBuffer _buffer;

    public MappedMetricCounter(IAtomicBuffer buffer)
    {
        _buffer = buffer;
    }

    public void SetValue(double value)
    {
        MemoryMarshal.TryWrite(_valueBuffer, ref value);
        _buffer.PutBytes(0, _valueBuffer);
    }

    public void Dispose()
    {
        if (_buffer is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}