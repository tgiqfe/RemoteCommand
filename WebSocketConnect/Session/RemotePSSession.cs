using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.WebSockets;
using System.Diagnostics;
using System.Threading;
using System.IO;
using Newtonsoft.Json;
using WebSocketConnect.SessionInfo;

namespace WebSocketConnect.Session
{
    public class RemotePSSession : SessionBase
    {
        private const string HEADER_STDIN = "STI:";     //  標準入力
        private const string HEADER_STDOUT = "STO:";    //  標準出力
        private const string HEADER_STDERR = "STE:";    //  標準エラー出力
        private const int HEADER_LENGTH = 4;

        private const string Ex_InitInfo_PSPath = "InitInfo_PSPath";

        private Process _process = null;

        private CancellationTokenSource _outputTokenSource = new CancellationTokenSource();
        private CancellationTokenSource _errorTokenSource = new CancellationTokenSource();
        private int _inputLength = 0;
        private int _inputCount = 0;
        private bool _isInit = true;
        private bool _isEnter = false;
        private int _bufferSize { get; set; } = 1024 * 1024;
        protected StringBuilder _outputBuffer = new StringBuilder("", 10 * 1024 * 1024);

        public string PSPath { get; set; }       //  PowerShell実行コマンドのパスを指定。powershell or pwsh

        #region ReceiverSide

        public override async Task Init(InitInfo info)
        {
            //  powrshell用コマンドパスを自動敵に探してDefaultPSCommandPathにセット。
            //  info.Remarkで指定されている場合はこちらを使用。
            //  もし指定されていな変えればDefaultPSCommandPathを使用。
            //  なぜかパスが見つからなかったという場合はエラー終了。
            string psPath = info.Extension.TryGetValue(Ex_InitInfo_PSPath, out string tempPSPath) ? tempPSPath : "";
            if (string.IsNullOrEmpty(psPath))
            {
                if (string.IsNullOrEmpty(Item.DefualtPSCommandPath))
                {
                    foreach (string psCommand in new string[] { "pwsh", "powershell" })
                    {
                        using (Process proc = new Process())
                        {
                            proc.StartInfo.FileName = "where.exe";
                            proc.StartInfo.Arguments = psCommand;
                            proc.StartInfo.CreateNoWindow = true;
                            proc.StartInfo.UseShellExecute = false;
                            proc.StartInfo.RedirectStandardOutput = true;
                            proc.Start();
                            string output_psCommandPath = proc.StandardOutput.ReadLine();     //最初の1行のみ取得
                            proc.WaitForExit();

                            if (proc.ExitCode == 0)
                            {
                                Item.DefualtPSCommandPath = output_psCommandPath.Trim();
                                break;
                            }
                        }
                    }
                }
                this.PSPath = Item.DefualtPSCommandPath;
            }
            else
            {
                using (Process proc = new Process())
                {
                    proc.StartInfo.FileName = "where.exe";
                    proc.StartInfo.Arguments = psPath;
                    proc.StartInfo.CreateNoWindow = true;
                    proc.StartInfo.UseShellExecute = false;
                    proc.StartInfo.RedirectStandardOutput = true;
                    proc.Start();
                    string output_psCommandPath = proc.StandardOutput.ReadLine();     //最初の1行のみ取得
                    proc.WaitForExit();

                    if (proc.ExitCode == 0)
                    {
                        this.PSPath = output_psCommandPath.Trim();
                    }
                }
            }
            if (string.IsNullOrEmpty(PSPath))
            {
                this.Remark = "Error: PowerShell command path is missing.";
                await WS.SendAsync(
                    new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(info))),
                    WebSocketMessageType.Text,
                    true,
                    CancellationToken.None);
                return;
            }

            //  バージョンチェック
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

        protected override void BeginReceiveProcess()
        {
            _process = new Process();
            //_process.StartInfo.FileName = "powershell.exe";
            _process.StartInfo.FileName = PSPath;
            _process.StartInfo.Arguments = "";
            _process.StartInfo.CreateNoWindow = true;
            _process.StartInfo.UseShellExecute = false;
            _process.StartInfo.RedirectStandardInput = true;
            _process.StartInfo.RedirectStandardOutput = true;
            _process.StartInfo.RedirectStandardError = true;
            _process.StartInfo.Environment["REMOTECONSOLE_SESSION"] = "1";
            _process.StartInfo.Environment["REMOTECONSOLE_TERMINAL"] = "PS";
            _process.Start();
            _process.EnableRaisingEvents = true;
            _process.Exited += new EventHandler(CloseEvent);

            RegisterOutputThread();
            RegisterErrorThread();
        }

        protected override void MainRceiveProcess(byte[] msgBytes)
        {
            if (msgBytes.Length >= HEADER_LENGTH)
            {
                string message = Encoding.UTF8.GetString(msgBytes);
                switch (message.Substring(0, HEADER_LENGTH))
                {
                    case HEADER_STDIN:
                        string command = message.Substring(HEADER_LENGTH);
                        _inputLength = command.Length;
                        _inputCount = 0;
                        _isInit = false;
                        _isEnter = true;
                        _process.StandardInput.WriteLine(command);
                        break;
                }
            }
        }

        protected override void EndReceiveProcess()
        {
            _process.StandardInput.WriteLine("exit");
            _process.Dispose();
        }

        /// <summary>
        /// 終了時イベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CloseEvent(object sender, EventArgs e)
        {
            _outputTokenSource.Cancel();
            _errorTokenSource.Cancel();
            Close().Wait();
        }

        /// <summary>
        /// 標準出力イベント
        /// </summary>
        private void RegisterOutputThread()
        {
            CancellationToken token = _outputTokenSource.Token;
            Task.Run(() =>
            {
                char[] buffer = new char[_bufferSize];
                try
                {
                    while (true)
                    {
                        if (token.IsCancellationRequested) { break; }
                        int count = _process.StandardOutput.Read(buffer, 0, buffer.Length);
                        lock (_outputBuffer)
                        {
                            _outputBuffer.Append(buffer, 0, count);
                            string output = new string(buffer, 0, count);
                            if (_isInit)
                            {
                                Response(HEADER_STDOUT + output).Wait();
                                continue;
                            }
                            if (_isEnter)
                            {
                                string tempOutput = output.Trim();
                                if (_inputLength == 0 && tempOutput == "")
                                {
                                    continue;
                                }
                                _inputCount += tempOutput.Length;
                                if (_inputCount <= _inputLength)
                                {
                                    continue;
                                }
                                else
                                {
                                    _isEnter = false;
                                }
                            }
                            Response(HEADER_STDOUT + output).Wait();
                        }
                    }
                }
                catch { }
            }, token);
        }

        /// <summary>
        /// 標準エラー出力イベント
        /// </summary>
        private void RegisterErrorThread()
        {
            CancellationToken token = _errorTokenSource.Token;
            Task.Run(() =>
            {
                char[] buffer = new char[_bufferSize];
                try
                {
                    while (true)
                    {
                        if (token.IsCancellationRequested) { break; }
                        int count = _process.StandardError.Read(buffer, 0, buffer.Length);
                        lock (_outputBuffer)
                        {
                            _outputBuffer.Append(buffer, 0, count);
                            string output = new string(buffer, 0, count);
                            Response(HEADER_STDERR + output).Wait();
                        }
                    }
                }
                catch { }
            }, token);
        }

        #endregion
        #region SenderSide

        protected override void MainSendProcess(byte[] msgBytes)
        {
            if (msgBytes.Length >= HEADER_LENGTH)
            {
                string message = Encoding.UTF8.GetString(msgBytes);
                switch (message.Substring(0, HEADER_LENGTH))
                {
                    case HEADER_STDOUT:
                        Console.Write(message.Substring(HEADER_LENGTH));
                        break;
                    case HEADER_STDERR:
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Write(message.Substring(HEADER_LENGTH));
                        Console.ResetColor();
                        break;
                }
            }
        }

        public override async Task Request(string message)
        {
            await Request(new ArraySegment<byte>(Encoding.UTF8.GetBytes(HEADER_STDIN + message)));
        }

        /// <summary>
        /// InitInfoにExtensionを埋め込む為のオーバーライド
        /// </summary>
        /// <returns></returns>
        public override async Task Init()
        {
            for (int i = 0; WS == null || WS.State != WebSocketState.Open && i < Item.TRY_OPENWAIT; i++)
            {
                Thread.Sleep(Item.TRY_INTERVAL);
            }

            InitInfo info = new InitInfo(Name, this.GetType());
            if(info.Extension == null)
            {
                info.Extension = new Dictionary<string, string>();
            }
            info.Extension[Ex_InitInfo_PSPath] = PSPath;

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

        #endregion
    }
}
