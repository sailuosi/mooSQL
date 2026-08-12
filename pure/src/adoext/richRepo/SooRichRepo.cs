using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using mooSQL.data.clip;
using mooSQL.data.model;
using mooSQL.data.richRepo.schema;
using mooSQL.data.richRepo.tracking;

namespace mooSQL.data.richRepo
{
    /// <summary>
    /// 富仓储（独立类型，不继承 <see cref="SooRepository{T}"/>）。
    /// 内部组合薄仓转发 CRUD；厚能力（Tracking / EntityCache / Schema / Upsert）仅挂本类。
    /// </summary>
    public class SooRichRepo<T> where T : class, new()
    {
        readonly DBInstance _db;
        readonly SooRepository<T> _thin;
        readonly EntityTranslator _translator;
        TrackingOptions _trackingOptions = new TrackingOptions();
        bool _autoTrackOnQuery;
        int _entityCacheSeconds = 300;
        EntityCacheStore<T> _cacheStore;
        Action<string> _onPrint;
        DBExecutor _executor;
        EntityInfo _en;

        /// <summary>构造富仓储。</summary>
        public SooRichRepo(DBInstance db)
            : this(db, null)
        {
        }

        /// <summary>带自定义翻译器。</summary>
        public SooRichRepo(DBInstance db, EntityTranslator translator)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _translator = translator ?? db.client.ClientFactory.getEntityTranslator();
            _thin = new SooRepository<T>(db, _translator);
        }

        /// <summary>内部薄仓（同 DB / Translator / 事务绑定）。</summary>
        public SooRepository<T> Thin => _thin;

        /// <summary>数据库实例。</summary>
        public DBInstance DB => _db;

        /// <summary>实体翻译器。</summary>
        public EntityTranslator Translator => _translator;

        /// <summary>实体元数据。</summary>
        public EntityInfo En
        {
            get
            {
                if (_en != null) return _en;
                _en = _db.client.EntityCash.getEntityInfo(typeof(T));
                return _en;
            }
        }

        /// <summary>当前事务执行器（若已绑定）。</summary>
        public DBExecutor Executor => _executor;

        /// <summary>打印 SQL（返回富仓储以便继续链式配置）。</summary>
        public SooRichRepo<T> print(Action<string> onPrint)
        {
            _onPrint = onPrint;
            _thin.print(onPrint);
            return this;
        }

        /// <summary>绑定事务执行器。</summary>
        public SooRichRepo<T> useTransaction(DBExecutor executor)
        {
            _executor = executor;
            _thin.useTransaction(executor);
            return this;
        }

        /// <summary>实体字典缓存 TTL（秒），默认 300。</summary>
        public int EntityCacheSeconds
        {
            get => _entityCacheSeconds;
            set
            {
                _entityCacheSeconds = value > 0 ? value : 300;
                _cacheStore = null;
            }
        }

        SQLBuilder getKit()
        {
            var kit = _db.useSQL();
            if (_onPrint != null) kit.print(_onPrint);
            if (_executor != null) kit.useTransaction(_executor);
            return kit;
        }

        void AfterWriteClear(bool ok)
        {
            if (ok) ClearCache();
        }

        void AfterWriteClear(int res)
        {
            if (res > 0) ClearCache();
        }

        #region Tracking 配置

        /// <summary>配置脏字段追踪选项。</summary>
        public SooRichRepo<T> useTracking(TrackingOptions options)
        {
            _trackingOptions = options ?? new TrackingOptions();
            return this;
        }

        /// <summary>
        /// 查询物化后自动 Begin 快照。
        /// 覆盖：GetById / GetByIds / GetList / GetPageList / GetFirst / GetChildList / GetTreeList。
        /// </summary>
        public SooRichRepo<T> autoTrackOnQuery(bool enabled = true)
        {
            _autoTrackOnQuery = enabled;
            return this;
        }

        /// <summary>开始快照追踪。</summary>
        public T Track(T entity)
        {
            if (entity == null) return null;
            EntityTracking.Begin(entity, En, _trackingOptions);
            return entity;
        }

        /// <summary>批量追踪。</summary>
        public IEnumerable<T> Track(IEnumerable<T> list)
        {
            if (list == null) return Enumerable.Empty<T>();
            var arr = list as IList<T> ?? list.ToList();
            EntityTracking.BeginRange(arr, En, _trackingOptions);
            return arr;
        }

        #endregion

        #region 查询（可选自动追踪）

        /// <inheritdoc cref="SooRepository{T}.GetById{K}"/>
        public T GetById<K>(K id) => AfterQueryTrack(_thin.GetById(id));

        /// <inheritdoc cref="SooRepository{T}.GetByIds{K}(List{K})"/>
        public List<T> GetByIds<K>(List<K> ids) => AfterQueryTrack(_thin.GetByIds(ids));

        /// <inheritdoc cref="SooRepository{T}.GetByIds(IEnumerable)"/>
        public List<T> GetByIds(IEnumerable ids) => AfterQueryTrack(_thin.GetByIds(ids));

        /// <inheritdoc cref="SooRepository{T}.GetByIds{K}(K[])"/>
        public List<T> GetByIds<K>(params K[] ids) => AfterQueryTrack(_thin.GetByIds(ids));

        /// <inheritdoc cref="SooRepository{T}.GetList()"/>
        public List<T> GetList() => AfterQueryTrack(_thin.GetList());

        /// <inheritdoc cref="SooRepository{T}.GetList(int)"/>
        public List<T> GetList(int top) => AfterQueryTrack(_thin.GetList(top));

        /// <inheritdoc cref="SooRepository{T}.GetList(Action{SQLBuilder})"/>
        public List<T> GetList(Action<SQLBuilder> onBuildSQL) => AfterQueryTrack(_thin.GetList(onBuildSQL));

        /// <inheritdoc cref="SooRepository{T}.GetList(Action{SQLClip, T})"/>
        public List<T> GetList(Action<SQLClip, T> filterClip) => AfterQueryTrack(_thin.GetList(filterClip));

        /// <inheritdoc cref="SooRepository{T}.GetList(QueryPara)"/>
        public List<T> GetList(QueryPara para) => AfterQueryTrack(_thin.GetList(para));

        /// <inheritdoc cref="SooRepository{T}.GetList(Expression{Func{T, bool}})"/>
        public List<T> GetList(Expression<Func<T, bool>> whereExpression)
            => AfterQueryTrack(_thin.GetList(whereExpression));

        /// <inheritdoc cref="SooRepository{T}.GetPageList(QueryPara)"/>
        public PageOutput<T> GetPageList(QueryPara para) => AfterQueryTrack(_thin.GetPageList(para));

        /// <inheritdoc cref="SooRepository{T}.GetPageList(int, int, Action{SQLClip, T})"/>
        public PageOutput<T> GetPageList(int pageSize, int pageNum, Action<SQLClip, T> filterClip = null)
            => AfterQueryTrack(_thin.GetPageList(pageSize, pageNum, filterClip));

        /// <inheritdoc cref="SooRepository{T}.GetPageList(Action{SQLBuilder})"/>
        public PageOutput<T> GetPageList(Action<SQLBuilder> onBuildSQL)
            => AfterQueryTrack(_thin.GetPageList(onBuildSQL));

        /// <inheritdoc cref="SooRepository{T}.GetPageList(QueryPara, Action{SQLBuilder, EntityInfo})"/>
        public PageOutput<T> GetPageList(QueryPara para, Action<SQLBuilder, EntityInfo> onBuildSQL)
            => AfterQueryTrack(_thin.GetPageList(para, onBuildSQL));

        /// <inheritdoc cref="SooRepository{T}.GetFirst(Expression{Func{T, bool}})"/>
        public T GetFirst(Expression<Func<T, bool>> whereExpression)
            => AfterQueryTrack(_thin.GetFirst(whereExpression));

        /// <inheritdoc cref="SooRepository{T}.GetFirst(Action{SQLClip, T})"/>
        public T GetFirst(Action<SQLClip, T> filterClip)
            => AfterQueryTrack(_thin.GetFirst(filterClip));

        /// <inheritdoc cref="SooRepository{T}.GetFirst{R}(Expression{Func{T, R}}, R)"/>
        public T GetFirst<R>(Expression<Func<T, R>> filterClip, R value)
            => AfterQueryTrack(_thin.GetFirst(filterClip, value));

        /// <inheritdoc cref="SooRepository{T}.GetChildList{R}"/>
        public List<T> GetChildList<R>(Expression<Func<T, R>> keySelector, R parentVal, Action<SQLClip, T> filterMore = null)
            => AfterQueryTrack(_thin.GetChildList(keySelector, parentVal, filterMore));

        /// <inheritdoc cref="SooRepository{T}.GetTreeList{R}"/>
        public TreeListOutput<T> GetTreeList<R>(Expression<Func<T, R>> keySelector, R parentVal, Action<SQLClip, T> filterMore = null)
        {
            var tree = _thin.GetTreeList(keySelector, parentVal, filterMore);
            if (_autoTrackOnQuery && tree?.Nodes != null)
            {
                var list = new List<T>();
                CollectTreeRecords(tree.Nodes, list);
                if (list.Count > 0) Track(list);
            }
            return tree;
        }

        /// <inheritdoc cref="SooRepository{T}.Count"/>
        public int Count(Expression<Func<T, bool>> whereExpression) => _thin.Count(whereExpression);

        /// <inheritdoc cref="SooRepository{T}.IsAny"/>
        public bool IsAny(Expression<Func<T, bool>> whereExpression) => _thin.IsAny(whereExpression);

        static void CollectTreeRecords(List<TreeNodeOutput<T>> nodes, List<T> into)
        {
            if (nodes == null || into == null) return;
            foreach (var n in nodes)
            {
                if (n?.Record != null) into.Add(n.Record);
                CollectTreeRecords(n?.Children, into);
            }
        }

        T AfterQueryTrack(T entity)
        {
            if (_autoTrackOnQuery && entity != null) Track(entity);
            return entity;
        }

        List<T> AfterQueryTrack(List<T> list)
        {
            if (_autoTrackOnQuery && list != null && list.Count > 0) Track(list);
            return list;
        }

        PageOutput<T> AfterQueryTrack(PageOutput<T> page)
        {
            if (_autoTrackOnQuery && page?.Items != null)
            {
                var list = page.Items as IList<T> ?? page.Items.ToList();
                Track(list);
                page.Items = list;
            }
            return page;
        }

        #endregion

        #region 写入（转发薄仓 + 清缓存；Update 走脏路径）

        /// <inheritdoc cref="SooRepository{T}.Insert"/>
        public bool Insert(T insertObj)
        {
            var ok = _thin.Insert(insertObj);
            AfterWriteClear(ok);
            return ok;
        }

        /// <inheritdoc cref="SooRepository{T}.InsertRange(IEnumerable{T})"/>
        public int InsertRange(IEnumerable<T> insertObjs)
        {
            var n = _thin.InsertRange(insertObjs);
            AfterWriteClear(n);
            return n;
        }

        /// <summary>
        /// 已追踪则脏更新；未追踪则按 <see cref="TrackingOptions.UntrackedUpdateAllColumns"/>（默认全列兼容）。
        /// </summary>
        public bool Update(T updateObj)
        {
            if (updateObj == null) return false;
            if (EntityTracking.HasSnapshot(updateObj) || EntityTracking.IsTracked(updateObj))
                return UpdateDirty(updateObj);

            var opt = _trackingOptions ?? new TrackingOptions();
            if (opt.UntrackedUpdateAllColumns)
            {
                var ok = _thin.Update(updateObj);
                AfterWriteClear(ok);
                return ok;
            }
            return ApplyEmpty(updateObj, opt, "实体未追踪") > 0;
        }

        /// <summary>仅脏字段更新。</summary>
        public bool UpdateDirty(T updateObj)
        {
            return UpdateDirtyInner(updateObj) > 0;
        }

        /// <summary>显式全列更新（走薄仓）。</summary>
        public bool UpdateAllColumns(T updateObj)
        {
            var ok = _thin.Update(updateObj);
            AfterWriteClear(ok);
            return ok;
        }

        /// <inheritdoc cref="SooRepository{T}.UpdateRange(IEnumerable{T})"/>
        public int UpdateRange(IEnumerable<T> updateObjs)
        {
            var n = _thin.UpdateRange(updateObjs);
            AfterWriteClear(n);
            return n;
        }

        /// <inheritdoc cref="SooRepository{T}.Update(Expression{Func{T, T}}, Expression{Func{T, bool}})"/>
        public bool Update(Expression<Func<T, T>> columns, Expression<Func<T, bool>> whereExpression)
        {
            var ok = _thin.Update(columns, whereExpression);
            AfterWriteClear(ok);
            return ok;
        }

        /// <inheritdoc cref="SooRepository{T}.Save"/>
        public bool Save(T entity)
        {
            var ok = _thin.Save(entity);
            AfterWriteClear(ok);
            return ok;
        }

        /// <inheritdoc cref="SooRepository{T}.SaveRange"/>
        public int SaveRange(IEnumerable<T> entities)
        {
            var n = _thin.SaveRange(entities);
            AfterWriteClear(n);
            return n;
        }

        /// <inheritdoc cref="SooRepository{T}.Delete(T)"/>
        public bool Delete(T deleteObj)
        {
            var ok = _thin.Delete(deleteObj);
            AfterWriteClear(ok);
            return ok;
        }

        /// <inheritdoc cref="SooRepository{T}.Delete(IEnumerable{T})"/>
        public int Delete(IEnumerable<T> deleteObjs)
        {
            var n = _thin.Delete(deleteObjs);
            AfterWriteClear(n);
            return n;
        }

        /// <inheritdoc cref="SooRepository{T}.Delete(Expression{Func{T, bool}})"/>
        public int Delete(Expression<Func<T, bool>> whereExpression)
        {
            var n = _thin.Delete(whereExpression);
            AfterWriteClear(n);
            return n;
        }

        /// <inheritdoc cref="SooRepository{T}.DeleteById{K}"/>
        public bool DeleteById<K>(K id)
        {
            var ok = _thin.DeleteById(id);
            AfterWriteClear(ok);
            return ok;
        }

        /// <inheritdoc cref="SooRepository{T}.DeleteByIds{K}"/>
        public int DeleteByIds<K>(IEnumerable<K> ids)
        {
            var n = _thin.DeleteByIds(ids);
            AfterWriteClear(n);
            return n;
        }

        int UpdateDirtyInner(T entity)
        {
            if (entity == null) return 0;
            var opt = _trackingOptions ?? new TrackingOptions();

            if (!EntityTracking.HasSnapshot(entity) && !EntityTracking.IsTracked(entity))
                return ApplyEmpty(entity, opt, "实体未追踪");

            var dirty = EntityTracking.GetDirtyMembers(entity);
            if (dirty == null || dirty.Count == 0)
                return ApplyEmpty(entity, opt, "无脏字段");

            var kit = getKit();
            var prep = Translator.prepareUpdateMembers(kit, entity, typeof(T), dirty, En, null);
            if (!prep.Status)
            {
                kit.Client.Loggor.LogError(prep.Message);
                return -1;
            }
            var cc = kit.doUpdate();
            AfterWriteClear(cc);
            if (cc > 0) EntityTracking.AcceptChanges(entity);
            return cc;
        }

        int ApplyEmpty(T entity, TrackingOptions opt, string reason)
        {
            switch (opt.EmptyBehavior)
            {
                case DirtyEmptyBehavior.Throw:
                    throw new InvalidOperationException("UpdateDirty 中止：" + reason);
                case DirtyEmptyBehavior.FallBackAllColumns:
                    return UpdateAllColumns(entity) ? 1 : 0;
                default:
                    return 0;
            }
        }

        #endregion

        #region Upsert

        /// <summary>
        /// 插入或更新。优先方言原生 upsert（MySQL ON DUPLICATE / MERGE），否则先查后写。
        /// </summary>
        public int InsertOrUpdate(T entity, UpsertOptions options = null)
        {
            if (entity == null) return 0;
            options = options ?? new UpsertOptions();

            if (TryNativeDuplicateKeyUpsert(entity, options, out var n1))
            {
                AfterWriteClear(n1);
                return n1;
            }

            if (TryNativeMergeUpsert(entity, options, out var n2))
            {
                AfterWriteClear(n2);
                return n2;
            }

            return InsertOrUpdateBySelectWrite(entity, options);
        }

        /// <summary>批量 Upsert（逐条；BatchSize 仅为切片大小，非多行 SQL）。</summary>
        public int InsertOrUpdate(IEnumerable<T> entities, UpsertOptions options = null)
        {
            if (entities == null) return 0;
            options = options ?? new UpsertOptions();
            var list = entities as IList<T> ?? entities.ToList();
            if (list.Count == 0) return 0;

            int batch = options.BatchSize > 0 ? options.BatchSize : list.Count;
            int total = 0;
            for (int i = 0; i < list.Count; i += batch)
            {
                foreach (var e in list.Skip(i).Take(batch))
                {
                    if (InsertOrUpdate(e, options) > 0) total++;
                }
            }
            return total;
        }

        int InsertOrUpdateBySelectWrite(T entity, UpsertOptions options)
        {
            if (ExistsByConstraint(entity, options))
            {
                if (options.IfExistsSkipUpdate)
                {
                    options.SqlOut = "select-write:exists-skip";
                    return 0;
                }
                var n = UpdateByUpsertOptions(entity, options);
                options.SqlOut = options.SqlOut ?? "select-write:update";
                return n;
            }

            var ok = Insert(entity);
            options.SqlOut = "select-write:insert";
            return ok ? 1 : 0;
        }

        bool TryNativeDuplicateKeyUpsert(T entity, UpsertOptions options, out int affected)
        {
            affected = 0;
            var flags = _db?.dialect?.Option?.ProviderFlags;
            if (flags == null || !flags.IsInsertOrUpdateSupported)
                return false;

            var kit = getKit();
            var table = Translator.GetResolvedTableName(En, entity, null);
            kit.setTable(table);

            var insertCols = CollectInsertColumns(entity);
            var updateCols = CollectUpdateColumns(entity, options);

            foreach (var kv in insertCols)
                kit.setI(kv.Key, kv.Value);

            if (!options.IfExistsSkipUpdate)
            {
                foreach (var kv in updateCols)
                    kit.setU(kv.Key, kv.Value);
            }

            var suffix = ResolveDuplicateKeySuffix();
            var cmd = kit.toInsertWithDuplicateUpdate(suffix);
            options.SqlOut = cmd?.sql ?? cmd?.toRawSQL();
            affected = kit.exeNonQuery(cmd);
            return true;
        }

        bool TryNativeMergeUpsert(T entity, UpsertOptions options, out int affected)
        {
            affected = 0;
            if (!SupportsMergeDialect(_db?.config?.dbType ?? DataBaseType.None))
                return false;

            var kit = getKit();
            var table = Translator.GetResolvedTableName(En, entity, null);
            var constraints = ResolveConstraintColumns(options);
            if (constraints.Count == 0)
                return false;

            var insertCols = CollectInsertColumns(entity);
            var updateCols = CollectUpdateColumns(entity, options);
            var sourceCols = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            foreach (var c in constraints)
            {
                var col = FindColumn(c);
                if (col == null) continue;
                sourceCols[col.DbColumnName] = col.PropertyInfo.GetValue(entity);
            }
            foreach (var kv in insertCols)
                sourceCols[kv.Key] = kv.Value;
            foreach (var kv in updateCols)
                sourceCols[kv.Key] = kv.Value;

            if (sourceCols.Count == 0)
                return false;

            var merge = kit.mergeInto(table, "t");
            merge.from("s", src =>
            {
                var parts = new List<string>();
                foreach (var kv in sourceCols)
                {
                    var pname = src.addPara("u_" + kv.Key, kv.Value);
                    parts.Add(pname + " AS " + kv.Key);
                }
                src.select(string.Join(", ", parts));
            });

            var on = new StringBuilder();
            for (int i = 0; i < constraints.Count; i++)
            {
                var col = FindColumn(constraints[i]);
                if (col == null) continue;
                if (on.Length > 0) on.Append(" AND ");
                on.Append("t.").Append(col.DbColumnName).Append("=s.").Append(col.DbColumnName);
            }
            if (on.Length == 0) return false;
            merge.on(on.ToString());

            if (!options.IfExistsSkipUpdate && updateCols.Count > 0)
            {
                merge.whenMatchThenUpdate(u =>
                {
                    foreach (var kv in updateCols)
                        u.set(kv.Key, "s." + kv.Key, false);
                });
            }

            merge.whenNotMatchThenInsert(ins =>
            {
                foreach (var kv in insertCols)
                    ins.set(kv.Key, "s." + kv.Key, false);
            });

            var cmd = merge.toMergeInto();
            options.SqlOut = cmd?.sql ?? cmd?.toRawSQL();
            affected = kit.exeNonQuery(cmd);
            return true;
        }

        static bool SupportsMergeDialect(DataBaseType dbType)
        {
            switch (dbType)
            {
                case DataBaseType.MSSQL:
                case DataBaseType.Oracle:
                case DataBaseType.PostgreSQL:
                case DataBaseType.Oscar:
                case DataBaseType.DM:
                    return true;
                default:
                    return false;
            }
        }

        static string ResolveDuplicateKeySuffix() => "ON DUPLICATE KEY UPDATE";

        List<string> ResolveConstraintColumns(UpsertOptions options)
        {
            var list = new List<string>();
            if (options.ConstraintMembers != null && options.ConstraintMembers.Count > 0)
            {
                list.AddRange(options.ConstraintMembers);
                return list;
            }
            var pks = En.GetPK();
            if (pks != null)
            {
                foreach (var pk in pks)
                    list.Add(pk.PropertyName);
            }
            return list;
        }

        Dictionary<string, object> CollectInsertColumns(T entity)
        {
            var dict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            foreach (var col in En.Columns)
            {
                if (col.IsIgnore || col.IsOnlyIgnoreInsert || col.PropertyInfo == null) continue;
                if ((col.Kind == FieldKind.Base || col.Kind == FieldKind.None) == false) continue;
                dict[col.DbColumnName] = col.PropertyInfo.GetValue(entity);
            }
            return dict;
        }

        Dictionary<string, object> CollectUpdateColumns(T entity, UpsertOptions options)
        {
            var dict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            var constraints = new HashSet<string>(ResolveConstraintColumns(options), StringComparer.OrdinalIgnoreCase);

            if (options.UpdateMembers != null && options.UpdateMembers.Count > 0)
            {
                foreach (var name in options.UpdateMembers)
                {
                    var col = FindColumn(name);
                    if (col == null || col.IsPrimarykey) continue;
                    if (constraints.Contains(col.PropertyName) || constraints.Contains(col.DbColumnName))
                        continue;
                    dict[col.DbColumnName] = col.PropertyInfo.GetValue(entity);
                }
                return dict;
            }

            foreach (var col in En.Columns)
            {
                if (col.IsIgnore || col.IsPrimarykey || col.PropertyInfo == null) continue;
                if ((col.Kind == FieldKind.Base || col.Kind == FieldKind.None) == false) continue;
                if (constraints.Contains(col.PropertyName) || constraints.Contains(col.DbColumnName))
                    continue;
                dict[col.DbColumnName] = col.PropertyInfo.GetValue(entity);
            }
            return dict;
        }

        bool ExistsByConstraint(T entity, UpsertOptions options)
        {
            var ck = getKit();
            ck.from(Translator.GetResolvedTableName(En, entity, null));
            var members = options.ConstraintMembers;
            if (members != null && members.Count > 0)
            {
                foreach (var name in members)
                {
                    var col = FindColumn(name);
                    if (col == null) continue;
                    ck.where(col.DbColumnName, col.PropertyInfo.GetValue(entity));
                }
            }
            else
            {
                Translator.setPKWhere(ck, entity, En);
            }
            return ck.count() > 0;
        }

        int UpdateByUpsertOptions(T entity, UpsertOptions options)
        {
            var members = options.UpdateMembers;
            if (members == null || members.Count == 0)
                return UpdateAllColumns(entity) ? 1 : 0;

            var dict = new Dictionary<string, object>(StringComparer.Ordinal);
            foreach (var name in members)
            {
                var col = FindColumn(name);
                if (col == null || col.IsPrimarykey) continue;
                dict[col.PropertyName] = col.PropertyInfo.GetValue(entity);
            }
            if (dict.Count == 0) return 0;

            if (options.ConstraintMembers == null || options.ConstraintMembers.Count == 0)
            {
                var kit = getKit();
                var prep = Translator.prepareUpdateMembers(kit, entity, typeof(T), dict, En, null);
                if (!prep.Status)
                {
                    kit.Client.Loggor.LogError(prep.Message);
                    return -1;
                }
                var cc = kit.doUpdate();
                AfterWriteClear(cc);
                return cc;
            }

            var upd = getKit();
            upd.setTable(Translator.GetResolvedTableName(En, entity, null));
            foreach (var name in options.ConstraintMembers)
            {
                var col = FindColumn(name);
                if (col == null) continue;
                upd.where(col.DbColumnName, col.PropertyInfo.GetValue(entity));
            }
            foreach (var kv in dict)
            {
                var col = FindColumn(kv.Key);
                if (col == null) continue;
                upd.set(col.DbColumnName, kv.Value);
            }
            var n = upd.doUpdate();
            AfterWriteClear(n);
            return n;
        }

        EntityColumn FindColumn(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName)) return null;
            foreach (var c in En.Columns)
            {
                if (string.Equals(c.PropertyName, propertyName, StringComparison.Ordinal)
                    || string.Equals(c.DbColumnName, propertyName, StringComparison.OrdinalIgnoreCase))
                    return c;
            }
            return null;
        }

        #endregion

        #region EntityCache

        EntityCacheStore<T> CacheStore
        {
            get
            {
                if (_cacheStore == null)
                {
                    var dbName = _db?.config != null ? _db.config.index.ToString() : "";
                    _cacheStore = new EntityCacheStore<T>(dbName, typeof(T), _entityCacheSeconds);
                }
                return _cacheStore;
            }
        }

        /// <summary>缓存预热查询范围；默认全表。</summary>
        protected virtual SQLBuilder CacheQuery()
        {
            var kit = getKit();
            Translator.BuildSelectFrom(kit, En);
            return kit;
        }

        /// <summary>对象字典缓存全部项。</summary>
        public IEnumerable<T> AllCache => GetCacheMap().Values;

        /// <summary>清除本实体字典缓存。</summary>
        public void ClearCache() => CacheStore.Clear();

        /// <summary>从字典缓存按条件过滤（内存）。</summary>
        public List<T> QueryFromCache(Expression<Func<T, bool>> pred)
        {
            if (pred == null) return AllCache.ToList();
            var compiled = pred.Compile();
            return GetCacheMap().Values.Where(compiled).ToList();
        }

        /// <summary>按主键从缓存取一项（复合主键用 "|" 拼接各 PK 值）。</summary>
        public T QueryItemFromCache(object pk)
        {
            if (pk == null) return null;
            GetCacheMap().TryGetValue(FormatCacheKey(pk), out var item);
            return item;
        }

        /// <summary>按复合主键各列值从缓存取一项。</summary>
        public T QueryItemFromCache(params object[] pkParts)
        {
            if (pkParts == null || pkParts.Length == 0) return null;
            GetCacheMap().TryGetValue(FormatCacheKeyParts(pkParts), out var item);
            return item;
        }

        /// <summary>从缓存取第一项匹配。</summary>
        public T QueryItemFromCache(Expression<Func<T, bool>> pred)
        {
            return QueryFromCache(pred).FirstOrDefault();
        }

        Dictionary<string, T> GetCacheMap()
        {
            return CacheStore.GetOrWarm(() =>
            {
                var list = CacheQuery().query<T>()?.ToList() ?? new List<T>();
                var map = new Dictionary<string, T>(StringComparer.Ordinal);
                foreach (var row in list)
                {
                    var key = BuildEntityCacheKey(row);
                    if (!string.IsNullOrEmpty(key))
                        map[key] = row;
                }
                return map;
            });
        }

        string BuildEntityCacheKey(T row)
        {
            if (row == null) return null;
            var pks = En.GetPK();
            if (pks == null || pks.Count == 0)
                return row.GetHashCode().ToString();
            if (pks.Count == 1)
                return Convert.ToString(pks[0].PropertyInfo.GetValue(row));
            var parts = new string[pks.Count];
            for (int i = 0; i < pks.Count; i++)
                parts[i] = Convert.ToString(pks[i].PropertyInfo.GetValue(row)) ?? "";
            return string.Join("|", parts);
        }

        static string FormatCacheKey(object pk)
        {
            if (pk is object[] arr) return FormatCacheKeyParts(arr);
            return Convert.ToString(pk);
        }

        static string FormatCacheKeyParts(object[] parts)
        {
            if (parts == null || parts.Length == 0) return null;
            if (parts.Length == 1) return Convert.ToString(parts[0]);
            var s = new string[parts.Length];
            for (int i = 0; i < parts.Length; i++)
                s[i] = Convert.ToString(parts[i]) ?? "";
            return string.Join("|", s);
        }

        #endregion

        #region Schema

        /// <summary>对齐表结构（默认只增不删）。</summary>
        public SchemaEnsureResult EnsureSchema(SyncMode mode = SyncMode.AddMissingColumns)
            => SchemaEnsure.Ensure<T>(_db, new SchemaEnsureOptions { Mode = mode });

        /// <summary>对齐表结构（完整选项）。</summary>
        public SchemaEnsureResult EnsureSchema(SchemaEnsureOptions options)
            => SchemaEnsure.Ensure<T>(_db, options);

        /// <summary>预览结构同步 SQL。</summary>
        public IReadOnlyList<string> PreviewSchema(SyncMode mode = SyncMode.AddMissingColumns)
            => SchemaEnsure.Preview<T>(_db, mode);

        /// <summary>EnsureSchema 别名。</summary>
        public void SyncFields(SyncMode mode = SyncMode.AddMissingColumns)
            => EnsureSchema(mode);

        /// <summary>同步注释（Mode=SyncCaptions）。</summary>
        public void SyncCaptions()
            => EnsureSchema(SyncMode.SyncCaptions);

        #endregion

        #region Include / 分表转发

        /// <summary>对已物化主列表加载一对多导航（二次 IN，复用 includeNav）。</summary>
        public NavQueryGuide<T, Child> Include<Child>(
            IEnumerable<T> list,
            Expression<Func<T, ICollection<Child>>> nav,
            Action<SQLBuilder> childFilter = null)
            where Child : class, new()
        {
            return getKit().includeNav(list, nav, childFilter);
        }

        /// <summary>指定物理表名（转发薄仓）。</summary>
        public SooRichRepo<T> UseTable(string tableName)
        {
            _thin.UseTable(tableName);
            return this;
        }

        /// <summary>按时间点解析分表（转发薄仓）。</summary>
        public SooRichRepo<T> ForShard(DateTime pointTime)
        {
            _thin.ForShard(pointTime);
            return this;
        }

        #endregion
    }
}
