using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.WebSockets;
using System.Threading;
using System.Reflection;
using WebSocketConnect.Session;
using WebSocketConnect.SessionInfo;
using Newtonsoft.Json;

namespace WebSocketConnect
{
    public class WebSocketServer : IDisposable
    {
        private HttpListener _listener = null;
        private NLog.Logger _logger = null;

        public WebSocketServer()
        {
            //  ログ開始
            _logger = Function.SetLogger(Item.LOG_DIRECTORY, "Receiver", WebSocketParam.DebugMode);

            WebSocketParam.ReceiverUpTime = DateTime.Now;
        }
        public WebSocketServer(NLog.Logger logger)
        {
            this._logger = logger;
            WebSocketParam.ReceiverUpTime = DateTime.Now;
        }


        /// <summary>
        /// 開始
        /// </summary>
        /// <returns></returns>
        public async Task Start()
        {
            _listener = new HttpListener();
            foreach (string prefix in WebSocketParam.ServerPrefixes)
            {
                _listener.Prefixes.Add(prefix);
            }
            _listener.Start();
            while (true)
            {
                HttpListenerContext listenerContext = await _listener.GetContextAsync();
                string remoteIP = listenerContext.Request.Headers["X-Real-IP"];
                //string remoteIP = listenerContext.Request.Headers["X-Forwarded-For"];
                string remoteHost = listenerContext.Request.Headers["Host"];

                _logger.Info("{0} RemoteEndpoint[{1}] HostName[{2}] RawURL[{3}]",
                    listenerContext.Request.HttpMethod,
                    remoteIP,
                    remoteHost,
                    listenerContext.Request.RawUrl);

                if (listenerContext.Request.IsWebSocketRequest)
                {
                    ProcessRequest(listenerContext);
                }
                else
                {
                    _logger.Warn("No WebSocket.");
                    listenerContext.Response.StatusCode = 400;
                    listenerContext.Response.Close();
                }
            }
        }

        /// <summary>
        /// リクエスト処理部分
        /// </summary>
        /// <param name="listenerContext"></param>
        private async void ProcessRequest(HttpListenerContext listenerContext)
        {
            using (WebSocket ws = (await listenerContext.AcceptWebSocketAsync(subProtocol: null)).WebSocket)
            {
                try
                {
                    ArraySegment<byte> initBuff = new ArraySegment<byte>(new byte[1024]);
                    WebSocketReceiveResult ret = await ws.ReceiveAsync(initBuff, CancellationToken.None);
                    if (ret.MessageType == WebSocketMessageType.Text)
                    {
                        string initMessage = Encoding.UTF8.GetString(initBuff.Take(ret.Count).ToArray());

                        /*
                         * 文字列からTypeに変換する処理のメモ。※今後このコードを使う可能性は低いけれど、メモとして残しておきます。
                         * Type type = Assembly.GetExecutingAssembly().GetTypes().
                         *    FirstOrDefault(x => x.BaseType == typeof(SessionBase) && x.Name == initMessage);
                         */

                        WebSocketParam.SessionCount++;
                        InitInfo info = JsonConvert.DeserializeObject<InitInfo>(Encoding.UTF8.GetString(initBuff.Take(ret.Count).ToArray()));
                        if (info.Type != null)
                        {
                            SessionBase session = Activator.CreateInstance(info.Type) as SessionBase;
                            session.WS = ws;
                            await session.Init(info);
                            if (session.Enabled)
                            {
                                await session.Receive();
                                _logger.Debug("Session End.");
                                if (!string.IsNullOrEmpty(session.Remark))
                                {
                                    _logger.Info(session.Remark);
                                }
                            }
                            else
                            {
                                _logger.Error("Skip: session disable.");
                                if (!string.IsNullOrEmpty(session.Remark))
                                {
                                    _logger.Info(session.Remark);
                                }
                            }
                        }
                        else
                        {
                            _logger.Error("Type Mismatch. [{0}]", initMessage);
                        }
                        WebSocketParam.SessionCount--;
                    }
                }
                catch (Exception e)
                {
                    _logger.Error(e.ToString());
                }
            }
        }

        /// <summary>
        /// 終了用メソッド
        /// </summary>
        public void Stop()
        {
            if (_listener.IsListening)
            {
                try
                {
                    _listener.Close();
                }
                catch (WebSocketException e)
                {
                    _logger.Error(e.ToString());
                }
            }
        }

        #region IDisposable Support

        /// <summary>
        /// Dispose用フラグ
        /// </summary>
        private bool disposedValue = false;

        /// <summary>
        /// Disposableメソッド
        /// </summary>
        public void Dispose()
        {
            if (!disposedValue)
            {
                this.Stop();
                disposedValue = true;
            }
        }
        #endregion
    }
}
