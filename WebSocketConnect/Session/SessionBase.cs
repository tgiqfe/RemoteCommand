using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.WebSockets;
using System.IO;
using System.Threading;
using WebSocketConnect.SessionInfo;
using Newtonsoft.Json;

namespace WebSocketConnect.Session
{
    public class SessionBase : ISessionBase
    {
        /// <summary>
        /// セッション名
        /// </summary>
        public virtual string Name { get { return this.GetType().Name; } }

        /// <summary>
        /// セッションステータス
        /// </summary>
        public SessionStatus Status { get; set; } = SessionStatus.None;

        /// <summary>
        /// ReceiverのInit時の事前チェックで使用。falseの場合は処理実行無し
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// エラーや各処理時時に出力されるメッセージ
        /// </summary>
        public string Remark { get; set; }

        /// <summary>
        /// WebSocketデータ通信用インスタンス
        /// </summary>
        public WebSocket WS { get; set; }

        #region ReceiverSide

        /// <summary>
        /// Initメッセージ受信 (セッション開始)
        /// </summary>
        /// <param name="info"></param>
        public virtual async Task Init(InitInfo info)
        {
            if (info.CheckVersion())
            {
                this.Enabled = true;
                info.Success = true;
            }
            else
            {
                this.Remark = string.Format(
                    "Error: Version mismatch. Sender->{0} Receiver->{1}", info.Version, WebSocketParam.Version);
                //"Error: Version mismatch. Sender->{0} Receiver->{1}", info.Version, Item.Version);
            }

            await WS.SendAsync(
                new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(info))),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None);
        }

        /// <summary>
        /// Receiver側。のパケット受信してからの処理
        /// </summary>
        /// <returns></returns>
        public async Task Receive()
        {
            this.Status = SessionStatus.Begin;
            this.BeginReceiveProcess();

            this.Status = SessionStatus.Main;
            while (WS.State == WebSocketState.Open)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    ArraySegment<byte> buff = new ArraySegment<byte>(new byte[1024]);
                    WebSocketReceiveResult ret = null;
                    do
                    {
                        ret = await WS.ReceiveAsync(buff, CancellationToken.None);
                        ms.Write(buff.Array, buff.Offset, ret.Count);
                    } while (!ret.EndOfMessage);
                    ms.Seek(0, SeekOrigin.Begin);

                    this.MainRceiveProcess(ms.ToArray());
                }
            }

            this.Status = SessionStatus.End;
            this.EndReceiveProcess();

            this.Status = SessionStatus.Finished;
        }

        /// <summary>
        /// Receive時、メイン処理前部分
        /// </summary>
        protected virtual void BeginReceiveProcess() { }

        /// <summary>
        /// Receive時、メイン処理
        /// </summary>
        /// <param name="msgBytes"></param>
        protected virtual void MainRceiveProcess(byte[] msgBytes) { }

        /// <summary>
        /// Receive時、メイン処理後部分
        /// </summary>
        protected virtual void EndReceiveProcess() { }

        /// <summary>
        /// Responseを返信 (ArraySegment&lt;byte&gt;型)
        /// </summary>
        /// <param name="message"></param>
        public virtual async Task Response(ArraySegment<byte> message)
        {
            if (WS != null && WS.State == WebSocketState.Open)
            {
                await WS.SendAsync(message, WebSocketMessageType.Text, true, CancellationToken.None);
            }
        }

        /// <summary>
        /// Responseを返信 (string型)
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public virtual async Task Response(string message)
        {
            await Response(new ArraySegment<byte>(Encoding.UTF8.GetBytes(message)));
        }

        #endregion
        #region SenderSide

        /// <summary>
        /// Sender側。Receiverへの接続開始。
        /// </summary>
        /// <param name="targetServer"></param>
        /// <returns></returns>
        public virtual async Task Connect(string targetServer)
        {
            this.WS = new ClientWebSocket();
            await ((ClientWebSocket)WS).ConnectAsync(new Uri(targetServer), CancellationToken.None);
        }

        /// <summary>
        /// Initメッセージ送信 (セッション開始)
        /// </summary>
        /// <returns></returns>
        public virtual async Task Init()
        {
            for (int i = 0; WS == null || WS.State != WebSocketState.Open && i < Item.TRY_OPENWAIT; i++)
            {
                Thread.Sleep(Item.TRY_INTERVAL);
            }
            
            InitInfo info = new InitInfo(Name, this.GetType());
            await WS.SendAsync(
                new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(info))),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None);

            if (WS.State == WebSocketState.Open)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    ArraySegment<byte> buff = new ArraySegment<byte>(new byte[1024]);
                    WebSocketReceiveResult ret = null;
                    do
                    {
                        ret = await WS.ReceiveAsync(buff, CancellationToken.None);
                        ms.Write(buff.Array, buff.Offset, ret.Count);
                    } while (!ret.EndOfMessage);
                    ms.Seek(0, SeekOrigin.Begin);

                    info = JsonConvert.DeserializeObject<InitInfo>(Encoding.UTF8.GetString(ms.ToArray()));
                    this.Enabled = info.Success;
                    if (!Enabled)
                    {
                        this.Remark = "Error: Server connect failed.";
                    }
                }
            }
        }

        /// <summary>
        /// Sender側。パケットを送信する処理。
        /// </summary>
        /// <returns></returns>
        public virtual async Task Send()
        {
            this.Status = SessionStatus.Begin;
            this.BeginSendProcess();

            this.Status = SessionStatus.Main;
            while (WS.State == WebSocketState.Open)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    ArraySegment<byte> buff = new ArraySegment<byte>(new byte[1024]);
                    WebSocketReceiveResult ret = null;
                    do
                    {
                        ret = await WS.ReceiveAsync(buff, CancellationToken.None);
                        ms.Write(buff.Array, buff.Offset, ret.Count);
                    } while (!ret.EndOfMessage);
                    ms.Seek(0, SeekOrigin.Begin);

                    this.MainSendProcess(ms.ToArray());
                }
            }

            this.Status = SessionStatus.End;
            this.EndSendProcess();

            this.Status = SessionStatus.Finished;
        }

        /// <summary>
        /// Send時。メイン処理前部分
        /// </summary>
        protected virtual void BeginSendProcess() { }

        /// <summary>
        /// Send時、メイン処理部
        /// </summary>
        /// <param name="msgBytes"></param>
        protected virtual void MainSendProcess(byte[] msgBytes) { }

        /// <summary>
        /// Send時。メイン処理後部分
        /// </summary>
        protected virtual void EndSendProcess() { }

        /// <summary>
        /// Requestを送信 (ArraySegment&lt;byte&gt;型)
        /// </summary>
        /// <param name="message">ArraySegmentに変換済みのメッセージ</param>
        /// <returns></returns>
        public virtual async Task Request(ArraySegment<byte> message)
        {
            if (WS != null && WS.State == WebSocketState.Open)
            {
                await WS.SendAsync(
                    message,
                    WebSocketMessageType.Text,
                    true,
                    CancellationToken.None);
            }
        }

        /// <summary>
        /// Requestを送信 (string型)
        /// </summary>
        /// <param name="message"文字列メッセージ></param>
        /// <returns></returns>
        public virtual async Task Request(string message)
        {
            await Request(new ArraySegment<byte>(Encoding.UTF8.GetBytes(message)));
        }

        /// <summary>
        /// InfoBaseインスタンスを指定してメッセージを送信
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        public virtual async Task Request(InfoBase info)
        {
            string message = JsonConvert.SerializeObject(info);
            await Request(new ArraySegment<byte>(Encoding.UTF8.GetBytes(message)));
        }

        /// <summary>
        /// Closeメッセージを送信してセッション終了
        /// </summary>
        /// <returns></returns>
        public virtual async Task Close()
        {
            await WS.CloseOutputAsync(WebSocketCloseStatus.Empty, "", CancellationToken.None);

            //  ↓この処理を記述するのを忘れていたのですが、Disposeしなくても大丈夫? したほうが良いと思うんですが。
            WS.Dispose();
        }

        #endregion
    }
}
