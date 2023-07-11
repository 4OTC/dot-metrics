using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Adaptive.Agrona;
using Adaptive.Agrona.Concurrent;
using Adaptive.Agrona.Util;

namespace DotMetrics.Codec.Metrics;

public class MappedMetricRepository : IMetricRepository, IDisposable
{
    private readonly uint _maxRecordCount;
    private const byte SchemaVersion = 1;
    
    private const int VersionOffset = 0;
    private const int RecordCountOffset = VersionOffset + BitUtil.SIZE_OF_LONG;
    private const int SpinLockOffset = RecordCountOffset + BitUtil.SIZE_OF_LONG;
    private const int RecordOffset = BitUtil.CACHE_LINE_LENGTH * 4; 
    private const int RecordLength = 128;
    private const int LabelLength = RecordLength - (BitUtil.SIZE_OF_LONG + BitUtil.SIZE_OF_DOUBLE);
    private const int LabelValueByteLength = LabelLength - 1;
    private const int LabelValueOffset = 1;
    private const int ValueUnlocked = 0;
    private const int ValueLocked = 1;
    private const int MetricDataLength = 16;
    private const int RecordValueOffset = RecordLength - 16;
    private const int RecordTimestampOffset = RecordLength - 8;
    public const int MaxIdentifierLength = RecordLength - RecordValueOffset - 1;

    private readonly byte[] _tmpBuffer = new byte[RecordLength];
    private readonly byte[] _labelBuffer = new byte[RecordValueOffset];
    private readonly IndexCalculatorMetricValueReceiver _indexCalculator = new IndexCalculatorMetricValueReceiver();
    private readonly MappedByteBuffer _mappedByteBuffer;
    private readonly UnsafeBuffer _unsafeBuffer;

    public MappedMetricRepository(FileSystemInfo fileInfo, uint maxRecordCount)
    {
        _maxRecordCount = maxRecordCount;
        long fileLength = GetFileLength(maxRecordCount);
        if (!File.Exists(fileInfo.FullName))
        {
            string tmpFilePath = Path.GetTempFileName();
            FileInfo tmpFile = new FileInfo(Path.Combine(Path.GetTempPath(), tmpFilePath));
            using FileStream fileStream = File.Open(tmpFile.FullName, FileMode.Truncate);
            fileStream.SetLength(fileLength);
            fileStream.Write(new byte[fileLength]);
            try
            {
                File.Move(tmpFile.FullName, fileInfo.FullName);
            }
            catch (IOException)
            {
                File.Delete(tmpFile.FullName);
                // ignore, another process or thread already created the file
            }
        }

        MemoryMappedFile memoryMappedFile =
            MemoryMappedFile.CreateFromFile(fileInfo.FullName, FileMode.OpenOrCreate, null, fileLength);
        _mappedByteBuffer = new MappedByteBuffer(memoryMappedFile);
        _unsafeBuffer = new UnsafeBuffer(_mappedByteBuffer);
    }

    public IMetricCounter GetOrCreate(string identifier)
    {
        if (identifier == null || string.Empty.Equals(identifier))
        {
            throw new Exception("Label cannot be empty");
        }
        if (Encoding.UTF8.GetByteCount(identifier) > LabelValueByteLength)
        {
            throw new Exception($"Label is too long: {identifier}");
        }
        Encoding.UTF8.GetBytes(identifier, 0, identifier.Length, _labelBuffer, 0);
        _indexCalculator.Reset(new ReadOnlySpan<byte>(_labelBuffer, 0, identifier.Length));
        Read(_indexCalculator);
        int recordIndex;
        if (_indexCalculator.Found)
        {
            recordIndex = _indexCalculator.MatchedIndex;
        }
        else
        {
            AcquireSpinLock();
            try
            {
                int currentRecordCount = _unsafeBuffer.GetIntVolatile(RecordCountOffset);
                if (currentRecordCount == _maxRecordCount)
                {
                    throw new Exception("Repository is full");
                }
                
                _unsafeBuffer.PutIntVolatile(RecordCountOffset, currentRecordCount + 1);
                recordIndex = currentRecordCount;
            }
            finally
            {
                ReleaseSpinLock();
            }
        }

        _labelBuffer[0] = (byte)identifier.Length;
        Encoding.UTF8.GetBytes(identifier, 0, identifier.Length, _labelBuffer, LabelValueOffset);
        int recordOffset = RecordOffset + (recordIndex * RecordLength);
        _unsafeBuffer.PutBytes(recordOffset + LabelValueOffset, _labelBuffer, LabelValueOffset, _labelBuffer.Length - 1);
        Thread.MemoryBarrier();
        _unsafeBuffer.PutByte(recordOffset, _labelBuffer[0]);
        return new MappedMetricCounter(new UnsafeBuffer(_unsafeBuffer.BufferPointer, recordOffset + LabelLength, MetricDataLength));
    }

    public void Read(IMetricValueReceiver metricValueReceiver)
    {
        int offset = RecordOffset;
        int currentRecordCount = _unsafeBuffer.GetIntVolatile(RecordCountOffset);
        for (uint i = 0; i < currentRecordCount; i++)
        {
            _unsafeBuffer.GetBytes(offset, _tmpBuffer);
            if (_tmpBuffer[0] != 0)
            {
                int metricNameLength = _tmpBuffer[0];
                ReadOnlySpan<byte> metricName = new ReadOnlySpan<byte>(_tmpBuffer, LabelValueOffset, metricNameLength);
                MemoryMarshal.TryRead(new ReadOnlySpan<byte>(_tmpBuffer, RecordValueOffset, BitUtil.SIZE_OF_DOUBLE),
                    out double value);
                MemoryMarshal.TryRead(new ReadOnlySpan<byte>(_tmpBuffer, RecordTimestampOffset, BitUtil.SIZE_OF_LONG),
                    out long updateTimeEpochMs);
                metricValueReceiver.Receive(metricName, value, updateTimeEpochMs);
            }

            offset += RecordLength;
        }
    }

    public void Dispose()
    {
        _unsafeBuffer.Dispose();
        _mappedByteBuffer.Dispose();
    }

    private void ReleaseSpinLock()
    {
        _unsafeBuffer.PutIntVolatile(SpinLockOffset, ValueUnlocked);
    }

    private void AcquireSpinLock()
    {
        int retries = 10;
        while (--retries != 0)
        {
            if (_unsafeBuffer.CompareAndSetInt(SpinLockOffset, ValueUnlocked, ValueLocked))
            {
                return;
            }

            Thread.Sleep(1);
        }

        throw new Exception("Unable to acquire spin-lock");
    }

    private static long GetFileLength(uint recordCount)
    {
        return RecordOffset + (recordCount * RecordLength);
    }
}