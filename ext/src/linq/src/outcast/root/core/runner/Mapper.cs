using mooSQL.linq.Common;
using mooSQL.linq.Linq;
using mooSQL.linq.Tools;
using mooSQL.linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using mooSQL.linq.Expressions;
using mooSQL.data;


namespace mooSQL.linq.Data.mapper
{
    sealed class Mapper<T>
    {
        public Mapper(Expression<Func<IQueryRunner, DbDataReader, T>> mapperExpression)
        {
            _expression = mapperExpression;
        }

        readonly Expression<Func<IQueryRunner, DbDataReader, T>> _expression;
        readonly ConcurrentDictionary<Type, ReaderMapperInfo> _mappers = new();

        public sealed class ReaderMapperInfo
        {
            public Expression<Func<IQueryRunner, DbDataReader, T>> MapperExpression = null!;
            public Func<IQueryRunner, DbDataReader, T> Mapper = null!;
            public bool IsFaulted;
        }

        public T Map(DBInstance context, IQueryRunner queryRunner, DbDataReader dataReader, ref ReaderMapperInfo mapperInfo)
        {
            var a = mooSQL.linq.Common.Configuration.TraceMaterializationActivity ? ActivityService.Start(ActivityID.Materialization) : null;

            try
            {
                return mapperInfo.Mapper(queryRunner, dataReader);
            }
            // SqlNullValueException: MySqlData
            // OracleNullValueException: managed and native oracle providers
            catch (Exception ex) when (ex is FormatException or InvalidCastException  || ex.GetType().Name.Contains("NullValueException"))
            {
                // TODO: debug cases when our tests go into slow-mode (e.g. sqlite.ms)
                if (mapperInfo.IsFaulted)
                    throw;

                return ReMapOnException(context, queryRunner, dataReader, ref mapperInfo, ex);
            }
            finally
            {
                a?.Dispose();
            }
        }

        public T ReMapOnException(DBInstance context, IQueryRunner queryRunner, DbDataReader dataReader, ref ReaderMapperInfo mapperInfo, Exception ex)
        {

            queryRunner.MapperExpression = mapperInfo.MapperExpression;

            var dataReaderType = dataReader.GetType();
            var expression = TransformMapperExpression(context, dataReader, dataReaderType, true);
            var expr = mapperInfo.MapperExpression; // create new instance to avoid race conditions without locks

            mapperInfo = new ReaderMapperInfo()
            {
                MapperExpression = expr,
                Mapper = expression.CompileExpression(),
                IsFaulted = true
            };

            _mappers[dataReaderType] = mapperInfo;

            return mapperInfo.Mapper(queryRunner, dataReader);
        }

        public ReaderMapperInfo GetMapperInfo(DBInstance context, IQueryRunner queryRunner, DbDataReader dataReader)
        {
            var dataReaderType = dataReader.GetType();

            if (!_mappers.TryGetValue(dataReaderType, out var mapperInfo))
            {
                var mapperExpression = TransformMapperExpression(context, dataReader, dataReaderType, false);

                queryRunner.MapperExpression = mapperExpression;

                var mapper = mapperExpression.CompileExpression();

                mapperInfo = new() { MapperExpression = mapperExpression, Mapper = mapper };

                _mappers.TryAdd(dataReaderType, mapperInfo);
            }

            return mapperInfo;
        }
        public ReaderMapperInfo GetMapperInfo(DBInstance context,  DbDataReader dataReader)
        {
            var dataReaderType = dataReader.GetType();

            if (!_mappers.TryGetValue(dataReaderType, out var mapperInfo))
            {
                var mapperExpression = TransformMapperExpression(context, dataReader, dataReaderType, false);

                var mapper = mapperExpression.CompileExpression();

                mapperInfo = new() { MapperExpression = mapperExpression, Mapper = mapper };

                _mappers.TryAdd(dataReaderType, mapperInfo);
            }

            return mapperInfo;
        }

        // 转换提取到单独的方法，以避免在映射器缓存命中时分配闭包
        private Expression<Func<IQueryRunner, DbDataReader, T>> TransformMapperExpression(
            DBInstance context,
            DbDataReader dataReader,
            Type dataReaderType,
            bool slowMode)
        {
            var ctx = new TransformMapperExpressionContext(_expression, context, dataReader, dataReaderType);

            Expression expression;

            if (slowMode)
            {
                expression = _expression.Transform(
                    ctx,
                    static (context, e) =>
                    {
                        if (e is ConvertFromDataReaderExpression ex)
                            return new ConvertFromDataReaderExpression(ex.Type, ex.Index, ex.Converter, context.NewVariable!, context.Context).Reduce();

                        return ReplaceVariable(context, e);
                    });
            }
            else
            {
                expression = _expression.Transform(
                    ctx,
                    static (context, e) =>
                    {
                        if (e is ConvertFromDataReaderExpression ex)
                            return ex.Reduce(context.Context, context.DataReader, context.NewVariable!).Transform(context, ReplaceVariable);

                        return ReplaceVariable(context, e);
                    });
            }

            if (mooSQL.linq.Common.Configuration.OptimizeForSequentialAccess)
                expression = SequentialAccessHelper.OptimizeMappingExpressionForSequentialAccess(expression, dataReader.FieldCount, reduce: false);

            return (Expression<Func<IQueryRunner, DbDataReader, T>>)expression;
        }

        static Expression ReplaceVariable(TransformMapperExpressionContext context, Expression e)
        {
            if (e is ParameterExpression { Name: "ldr" } vex)
            {
                context.OldVariable = vex;
                return context.NewVariable ??= Expression.Variable(context.DataReader.GetType(), "ldr");
            }

            if (e is BinaryExpression { NodeType: ExpressionType.Assign } bex && bex.Left == context.OldVariable)
            {
                var dataReaderExpression = Expression.Convert(context.Expression.Parameters[1], context.DataReaderType);

                return Expression.Assign(context.NewVariable!, dataReaderExpression);
            }

            return e;
        }

        sealed class TransformMapperExpressionContext
        {
            public TransformMapperExpressionContext(Expression<Func<IQueryRunner, DbDataReader, T>> expression, DBInstance context, DbDataReader dataReader, Type dataReaderType)
            {
                Expression = expression;
                Context = context;
                DataReader = dataReader;
                DataReaderType = dataReaderType;
            }

            public Expression<Func<IQueryRunner, DbDataReader, T>> Expression;
            public readonly DBInstance Context;
            public readonly DbDataReader DataReader;
            public readonly Type DataReaderType;

            public ParameterExpression? OldVariable;
            public ParameterExpression? NewVariable;
        }
    }
}
