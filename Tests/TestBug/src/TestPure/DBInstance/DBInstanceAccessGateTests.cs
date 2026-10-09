using System;
using System.Collections.Generic;
using FluentAssertions;
using mooSQL.config;
using mooSQL.data;
using Xunit;

namespace mooSQL.Pure.Tests
{
    /// <summary>
    /// 连接位 readable / writable 闸门：在 DBInstance 入口抛出 NotSupportedException，无需真实库。
    /// </summary>
    public class DBInstanceAccessGateTests
    {
        private static DBInstance CreateInstance(bool readable = true, bool writable = true)
        {
            return new DBInstance
            {
                config = new DataBase
                {
                    index = 1,
                    name = "biz",
                    readable = readable,
                    writable = writable,
                }
            };
        }

        [Fact]
        public void ExeQuery_WhenReadableFalse_ThrowsNotSupportedException()
        {
            var db = CreateInstance(readable: false);
            var act = () => db.ExeQuery(new SQLCmd("select 1"));
            act.Should().Throw<NotSupportedException>()
                .WithMessage("*readable=false*");
        }

        [Fact]
        public void ExeQueryGeneric_WhenReadableFalse_ThrowsNotSupportedException()
        {
            var db = CreateInstance(readable: false);
            var act = () => db.ExeQuery<int>(new SQLCmd("select 1"));
            act.Should().Throw<NotSupportedException>()
                .WithMessage("*readable=false*");
        }

        [Fact]
        public void ExeQueryScalar_WhenReadableFalse_ThrowsNotSupportedException()
        {
            var db = CreateInstance(readable: false);
            var act = () => db.ExeQueryScalar(new SQLCmd("select 1"));
            act.Should().Throw<NotSupportedException>()
                .WithMessage("*readable=false*");
        }

        [Fact]
        public void ExeNonQuery_WhenWritableFalse_ThrowsNotSupportedException()
        {
            var db = CreateInstance(writable: false);
            var act = () => db.ExeNonQuery(new SQLCmd("update t set a=1"));
            act.Should().Throw<NotSupportedException>()
                .WithMessage("*writable=false*");
        }

        [Fact]
        public void ExeNonQueryBatch_WhenWritableFalse_ThrowsNotSupportedException()
        {
            var db = CreateInstance(writable: false);
            var act = () => db.ExeNonQuery(new List<SQLCmd> { new SQLCmd("update t set a=1") });
            act.Should().Throw<NotSupportedException>()
                .WithMessage("*writable=false*");
        }

        [Fact]
        public void ExeNonQueryAsync_WhenWritableFalse_ThrowsNotSupportedException()
        {
            var db = CreateInstance(writable: false);
            Action act = () => { _ = db.ExeNonQueryAsync(new SQLCmd("update t set a=1")); };
            act.Should().Throw<NotSupportedException>()
                .WithMessage("*writable=false*");
        }

        [Fact]
        public void ExeQuery_WhenReadableTrue_DoesNotThrowNotSupported()
        {
            var db = CreateInstance(readable: true, writable: false);
            try
            {
                db.ExeQuery(new SQLCmd("select 1"));
            }
            catch (NotSupportedException)
            {
                throw; // gate must not fire for readable=true
            }
            catch
            {
                // expected: no dialect/cmd after gate
            }
        }

        [Fact]
        public void ExeNonQuery_WhenWritableTrue_DoesNotThrowNotSupported()
        {
            var db = CreateInstance(readable: false, writable: true);
            try
            {
                db.ExeNonQuery(new SQLCmd("update t set a=1"));
            }
            catch (NotSupportedException)
            {
                throw;
            }
            catch
            {
                // expected: no dialect/cmd after gate
            }
        }

        [Fact]
        public void AsDataBase_MapsReadableWritableFalse()
        {
            var pos = new DBPosition
            {
                Position = 2,
                Name = "ro",
                DbType = "MSSQL",
                ConnectString = "Server=.;Database=x;",
                Readable = false,
                Writable = false,
            };
            var cfg = pos.asDataBase();
            cfg.readable.Should().BeFalse();
            cfg.writable.Should().BeFalse();
            cfg.index.Should().Be(2);
            cfg.name.Should().Be("ro");
        }

        [Fact]
        public void AsDataBase_DefaultsReadableWritableTrue()
        {
            var pos = new DBPosition
            {
                Position = 1,
                Name = "rw",
                DbType = "MySQL",
                ConnectString = "Server=.;",
            };
            var cfg = pos.asDataBase();
            cfg.readable.Should().BeTrue();
            cfg.writable.Should().BeTrue();
        }

        [Fact]
        public void DataBase_SetReadableWritable_Chain()
        {
            var cfg = new DataBase()
                .setIndex(3)
                .setName("x")
                .setReadable(false)
                .setWritable(false);
            cfg.readable.Should().BeFalse();
            cfg.writable.Should().BeFalse();
        }
    }
}
