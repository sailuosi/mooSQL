using mooSQL.data.call;



namespace mooSQL.linq.translator;



internal partial class ClauseMethodVisitor

{

    public override MethodCall VisitAlias(AliasCall method) => DispatchPassThrough(method);

}

