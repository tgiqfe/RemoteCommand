using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebSocketConnect.SessionInfo
{
    public class InfoBase : IInfoBase
    {
        public virtual string Name { get { return this.GetType().Name; } }

        public virtual string Description { get; set; }
        public virtual Dictionary<string, string> Extension { get; set; }
        public virtual int ReturnCode { get; set; }
        public virtual string Remark { get; set; }
    }
}
