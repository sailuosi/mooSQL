using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.utils
{
    public class StatusResult
    {

        public bool Status { get; set; }
        public string Message { get; set; }

        public StatusResult(bool status, string message)
        {
            Status = status;
            Message = message;
        }
    }
}
