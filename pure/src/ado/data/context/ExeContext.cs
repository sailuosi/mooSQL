


namespace mooSQL.data.context
{
    /// <summary>
    /// ִ��������  ������SQL����sqlCommand��ִ���������ݿⷽ�� ��
    /// </summary>
    public class ExeContext {
        /// <summary>
        /// ����ִ��������
        /// </summary>
        public ExeContext() { 
        
        
        }
        /// <summary>
        /// 字段 cmd（CmdBuilder）。
        /// </summary>
        public CmdBuilder cmd = null;

        /// <summary>
        /// ���ݿ�Ự
        /// </summary>
        public ExeSession session;
        /// <summary>
        /// ���ݿ�ķ��� 
        /// </summary>
        public Dialect dialect;
        /// <summary>
        /// ��ǰ�����ݿ�ʵ��
        /// </summary>
        public DBInstance DBLive { get; set; }
    } 

}