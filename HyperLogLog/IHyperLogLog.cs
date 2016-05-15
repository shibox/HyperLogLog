using System.IO;

namespace HyperLogLog
{
    public interface IHyperLogLog<in T>
    {
        void Add(T value);
        void BulkAdd(T[] values,int offset,int size);
        void AddAsInt(Stream value);
        void AddAsUInt(Stream value);
        void AddAsLong(Stream value);
        void AddAsULong(Stream value);
        void AddAsFloat(Stream value);
        void AddAsDouble(Stream value);
        void AddAsInt(byte[] value,int offset,int size);
        void AddAsUInt(byte[] value, int offset, int size);
        void AddAsLong(byte[] value, int offset, int size);
        void AddAsULong(byte[] value, int offset, int size);
        void AddAsFloat(byte[] value, int offset, int size);
        void AddAsDouble(byte[] value, int offset, int size);
        ulong Count();
    }
}
