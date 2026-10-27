using System;
using System.Collections.Generic;

namespace dbTest
{
    /// <summary>
    /// 写操作基准共用实体工厂（对齐 LiteOrm.Benchmark 的 NewBenchmarkUser 思路，落在本工程 TestEntity 字段上）。
    /// </summary>
    public static class TestEntityFactory
    {
        public static TestEntity New(string tag, int i = 0)
        {
            return new TestEntity
            {
                F_Bool = true,
                F_Byte = 1,
                F_DateTime = DateTime.Now,
                F_Decimal = 100.23M + i,
                F_Double = 23.22,
                F_Float = 1.22F,
                F_Int16 = 22,
                F_Int32 = 333 + i,
                F_Int64 = 333,
                F_String = tag + i
            };
        }

        public static List<TestEntity> NewList(string tag, int count)
        {
            var list = new List<TestEntity>(count);
            for (int i = 0; i < count; i++)
                list.Add(New(tag, i));
            return list;
        }

        public static void Mutate(TestEntity e, string tag, Random random)
        {
            e.F_String = tag + Guid.NewGuid().ToString("N").Substring(0, 8);
            e.F_Int32 = random.Next(20, 60);
            e.F_Decimal = random.Next(1, 999);
        }
    }
}
