using System.Diagnostics.CodeAnalysis;

namespace mooSQL.data
{
    /// <summary>
    /// 保留标记：声明该类型可能被 mooSQL 经反射读取构造/属性/字段/特性。
    /// Trim/AOT 下请实现本接口，并尽量走 Client 已注册的 SG Materializer/Accessor/EntityFactory。
    /// </summary>
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
    public interface IReflectionEntity
    {
    }
}
