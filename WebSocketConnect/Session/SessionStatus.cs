using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.WebSockets;
using System.IO;
using System.Threading;

namespace WebSocketConnect.Session
{
    public enum SessionStatus
    {
        None = 0,       //  セッション開始前
        Begin = 1,      //  BeginReceiveProcess / BeginSendProcess
        Main = 2,       //  MainRceiveProcess / MainSendProcess
        End = 4,        //  EndReceiveProcess / EndSendProcess
        Finished = 8    //  セッション終了済み
    }
}
