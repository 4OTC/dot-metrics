using System;
using System.Runtime.InteropServices;
using System.Threading;
using Adaptive.Agrona;
using Adaptive.Agrona.Concurrent;

namespace DotMetrics.Codec.Metrics;

public class MappedMetricCounter : IMetricCounter
{
    private readonly byte[] _valueBuffer = new byte[BitUtil.SIZE_OF_DOUBLE];
    private readonly IAtomicBuffer _buffer;
    private readonly IEpochMillisSupplier _epochMillisSupplier;
    private bool _disposed;

    public MappedMetricCounter(
        IAtomicBuffer buffer,
        IEpochMillisSupplier epochMillisSupplier)
    {
        _epochMillisSupplier = epochMillisSupplier;
        _buffer = buffer;
    }

    public void SetValue(double value)
    {
        if (IsDisposed())
        {
            return;
        }

        _buffer.PutLongOrdered(BitUtil.SIZE_OF_DOUBLE, 0);
        MemoryMarshal.TryWrite(_valueBuffer, ref value);
        
        if (IsDisposed())
        {
            return;
        }

        _buffer.PutBytes(0, _valueBuffer);
        
        if (IsDisposed())
        {
            return;
        }

        _buffer.PutLongOrdered(BitUtil.SIZE_OF_DOUBLE, _epochMillisSupplier.EpochMs());
    }

    internal void Close()
    {
        Volatile.Write(ref _disposed, true);
        if (_buffer is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    private bool IsDisposed()
    {
        return Volatile.Read(ref _disposed);
    }
}