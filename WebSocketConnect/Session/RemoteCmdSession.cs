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

namespace WebSocketConnect.Session
{
    public class RemoteCmdSession : SessionBase
    {
        private const string HEADER_STDIN = "STI:";
        private const string HEADER_STDOUT = "STO:";
        private const string HEADER_STDERR = "STE:";
        private const int HEADER_LENGTH = 4;

        private Process _process = null;

        private CancellationTokenSource _outputTokenSource = new CancellationTokenSource();
        private CancellationTokenSource _errorTokenSource = new CancellationTokenSource();
        private int _inputLength = 0;
        private int _inputCount = 0;
        private bool _isInit = true;
        private bool _isEnter = false;
        private int _bufferSize { get; set; } = 1024 * 1024;
        protected StringBuilder _outputBuffer = new StringBuilder("", 10 * 1024 * 1024);

        #region ReceiverSide

        protected override void BeginReceiveProcess()
        {
            _process = new Process();
            _process.StartInfo.FileName = "cmd.exe";
            _process.StartInfo.Arguments = "";
            _process.StartInfo.CreateNoWindow = true;
            _process.StartInfo.UseShellExecute = false;
            _process.StartInfo.RedirectStandardInput = true;
            _process.StartInfo.RedirectStandardOutput = true;
            _process.StartInfo.RedirectStandardError = true;
            _process.StartInfo.Environment["REMOTECONSOLE_SESSION"] = "1";
            _process.StartInfo.Environment["REMOTECONSOLE_TERMINAL"] = "Cmd";
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

        #endregion
    }
}
