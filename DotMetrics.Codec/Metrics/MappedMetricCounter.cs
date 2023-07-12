using System;
using System.Runtime.InteropServices;
using Adaptive.Agrona;
using Adaptive.Agrona.Concurrent;

namespace DotMetrics.Codec.Metrics;

public class MappedMetricCounter : IMetricCounter, IDisposable
{
    private readonly byte[] _valueBuffer = new byte[BitUtil.SIZE_OF_DOUBLE];
    private readonly IAtomicBuffer _buffer;
    private readonly IEpochMillisSupplier _epochMillisSupplier;

    public MappedMetricCounter(
        IAtomicBuffer buffer, 
        IEpochMillisSupplier epochMillisSupplier)
    {
        _epochMillisSupplier = epochMillisSupplier;
        _buffer = buffer;
    }

    public void SetValue(double value)
    {
        _buffer.PutLongOrdered(BitUtil.SIZE_OF_DOUBLE, 0);
        MemoryMarshal.TryWrite(_valueBuffer, ref value);
        _buffer.PutBytes(0, _valueBuffer);
        _buffer.PutLongOrdered(BitUtil.SIZE_OF_DOUBLE, _epochMillisSupplier.EpochMs());
    }

    public void Dispose()
    {
        if (_buffer is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}