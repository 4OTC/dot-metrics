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
    private readonly Func<bool> _repositoryIsDisposed;

    public MappedMetricCounter(
        IAtomicBuffer buffer,
        IEpochMillisSupplier epochMillisSupplier,
        Func<bool> repositoryIsDisposed)
    {
        _epochMillisSupplier = epochMillisSupplier;
        _repositoryIsDisposed = repositoryIsDisposed;
        _buffer = buffer;
    }

    public void SetValue(double value)
    {
        if (_repositoryIsDisposed())
        {
            return;
        }

        _buffer.PutLongOrdered(BitUtil.SIZE_OF_DOUBLE, 0);
        MemoryMarshal.TryWrite(_valueBuffer, ref value);
        
        if (_repositoryIsDisposed())
        {
            return;
        }

        _buffer.PutBytes(0, _valueBuffer);
        
        if (_repositoryIsDisposed())
        {
            return;
        }

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