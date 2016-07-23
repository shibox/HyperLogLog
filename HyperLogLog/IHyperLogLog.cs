using System.IO;

namespace HyperLogLog
{
    public interface IHyperLogLog<in T>
    {
        /// <summary>
        /// 添加纪录
        /// </summary>
        /// <param name="value"></param>
        void Add(T value);
        /// <summary>
        ///  批量添加
        /// </summary>
        /// <param name="values"></param>
        /// <param name="offset"></param>
        /// <param name="size"></param>
        void BulkAdd(T[] values,int offset,int size);
        /// <summary>
        /// 将流中的数据以int添加
        /// </summary>
        /// <param name="value"></param>
        void AddAsInt(Stream value);
        /// <summary>
        /// 将流中的数据以uint添加
        /// </summary>
        /// <param name="value"></param>
        void AddAsUInt(Stream value);
        /// <summary>
        /// 将流中的数据以long添加
        /// </summary>
        /// <param name="value"></param>
        void AddAsLong(Stream value);
        /// <summary>
        /// 将流中的数据以ulong添加
        /// </summary>
        /// <param name="value"></param>
        void AddAsULong(Stream value);
        /// <summary>
        /// 将流中的数据以float添加
        /// </summary>
        /// <param name="value"></param>
        void AddAsFloat(Stream value);
        /// <summary>
        /// 将流中的数据以double添加
        /// </summary>
        /// <param name="value"></param>
        void AddAsDouble(Stream value);
        void AddAsInt(byte[] value,int offset,int size);
        void AddAsUInt(byte[] value, int offset, int size);
        void AddAsLong(byte[] value, int offset, int size);
        void AddAsULong(byte[] value, int offset, int size);
        void AddAsFloat(byte[] value, int offset, int size);
        void AddAsDouble(byte[] value, int offset, int size);
        /// <summary>
        /// 不重复记录数量
        /// </summary>
        /// <returns></returns>
        ulong Count();
    }
}
