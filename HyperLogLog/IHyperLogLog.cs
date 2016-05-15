using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HyperLogLog
{
    public interface IHyperLogLog<in T>
    {
        void Add(T element);
        void BulkAdd(T[] values);
        ulong Count();
    }
}
