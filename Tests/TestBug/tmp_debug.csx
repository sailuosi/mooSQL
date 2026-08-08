using System;
using System.Linq;
using System.Linq.Expressions;
using mooSQL.data;
using mooSQL.linq;
using mooSQL.linq.translator;
using mooSQL.Pure.Tests.TestHelpers;

var fixture = new LinqSqliteTestFixture();
fixture.EnsureInitialized();
var db = fixture.Db;
var expr = db.useQueryable<SQLiteTestUser>().Where(u => u.Age == null).Expression;
var expr2 = db.useQueryable<SQLiteTestUser>().Where(u => u.Age > 18).Expression;
Console.WriteLine("Age==null SQL: " + LinqStatementCompiler.GetSqlText(db, expr));
Console.WriteLine("Age>18 SQL: " + LinqStatementCompiler.GetSqlText(db, expr2));
var bagExpr = expr;
var bag = mooSQL.linq.Linq.QueryMate.GetQuery<SQLiteTestUser>(db, ref bagExpr, out _);
var sq = bag.Sentences[0].Statement.SelectQuery;
Console.WriteLine("HasWhere: " + (sq?.Where?.SearchCondition?.Predicates?.Count ?? 0));
