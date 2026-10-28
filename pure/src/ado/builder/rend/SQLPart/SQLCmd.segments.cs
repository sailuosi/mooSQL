using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace mooSQL.data
{
    public partial class SQLCmd
    {
        /// <summary>始终非空；至少含 1 个 <see cref="SQLSeg"/>。</summary>
        List<SQLSeg> _segments;

        /// <summary>
        /// 物理执行段列表（始终 ≥1）。单段与多段同一形状，避免 sql/para 与 Segments 语义分叉。
        /// </summary>
        public IReadOnlyList<SQLSeg> Segments
        {
            get
            {
                EnsurePrimarySegment();
                return new ReadOnlyCollection<SQLSeg>(_segments);
            }
        }

        /// <summary>是否多段（Count &gt; 1）。单段时为 false，执行可走单命令路径。</summary>
        public bool HasSegments => SegmentCount > 1;

        /// <summary>段数（构造后至少为 1）。</summary>
        public int SegmentCount
        {
            get
            {
                EnsurePrimarySegment();
                return _segments.Count;
            }
        }

        /// <summary>首段（始终存在）。</summary>
        public SQLSeg PrimarySegment
        {
            get
            {
                EnsurePrimarySegment();
                return _segments[0];
            }
        }

        void EnsurePrimarySegment()
        {
            if (_segments != null && _segments.Count > 0)
                return;
            _segments = new List<SQLSeg>(1) { new SQLSeg() };
        }

        /// <summary>
        /// 用给定分段整体替换。空/null 时重置为单个空段。
        /// </summary>
        public SQLCmd UseSegments(IList<SQLSeg> segments)
        {
            if (segments == null || segments.Count == 0)
            {
                _segments = new List<SQLSeg>(1) { new SQLSeg() };
                return this;
            }

            var list = new List<SQLSeg>(segments.Count);
            for (var i = 0; i < segments.Count; i++)
            {
                var s = segments[i];
                if (s == null)
                    continue;
                if (s.para == null)
                    s.para = new Paras();
                list.Add(s);
            }

            if (list.Count == 0)
                list.Add(new SQLSeg());

            _segments = list;
            return this;
        }

        /// <summary>重置为单个空段。</summary>
        public void ClearSegments()
        {
            _segments = new List<SQLSeg>(1) { new SQLSeg() };
        }

        /// <summary>
        /// 重置为单个段并写入 sql/para（参数 Copy）。用于 set <see cref="sql"/>/<see cref="para"/> 与单段构造。
        /// </summary>
        public SQLCmd UseSingleSegment(string sqlText, Paras paras)
        {
            var seg = new SQLSeg();
            seg.sql = sqlText;
            if (paras != null)
                seg.para.Copy(paras);
            _segments = new List<SQLSeg>(1) { seg };
            return this;
        }

        /// <summary>
        /// 展开为可逐条 NonQuery 的单段命令。
        /// 仅 1 段时 yield 自身；多段时每段一个新 <see cref="SQLCmd"/>（继承元数据）。
        /// </summary>
        public IEnumerable<SQLCmd> ExpandSegments()
        {
            EnsurePrimarySegment();
            if (_segments.Count == 1)
            {
                yield return this;
                yield break;
            }

            for (var i = 0; i < _segments.Count; i++)
            {
                var seg = _segments[i];
                if (seg == null || seg.Empty)
                    continue;
                yield return CloneAsSingleSegment(seg);
            }
        }

        SQLCmd CloneAsSingleSegment(SQLSeg seg)
        {
            var cmd = new SQLCmd(seg.sql, seg.para);
            cmd.type = type;
            cmd.TargetTable = TargetTable;
            cmd.signal = signal;
            cmd.cmdType = cmdType;
            cmd.timeout = timeout;
            return cmd;
        }
    }
}
