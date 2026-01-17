namespace mooSQL.data.taos.Protocols.TDWebSocket
{

    public class TaosWSResult
    {
        public byte[] data { get; set; }
        public WSQueryRsp meta { get; set; }
        public int rows { get;  set; }
        public WSStmtExecRsp StmtExec { get;  set; }
    }


}