using mooSQL.linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace mooSQL.data
{
    public class DbContextReadyBook<T>
    {

        private ConcurrentDictionary<T, DbContext> _readyPairs = new ConcurrentDictionary<T, DbContext>();


        public DbContext Get(T index)
        {
            if (_readyPairs.TryGetValue(index, out var val))
            {
                return val;
            }
            return null;
        }

        public void Add(T index, DbContext val)
        {
            _readyPairs.TryAdd(index, val);
        }
    }
    public class LinqReadyBook:DbContextReadyBook<int>
    {
    }
}
