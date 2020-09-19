using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Management.Automation;
using WebSocketConnect.Session;
using WebSocketConnect.SessionInfo;
using System.IO;
using System.Threading;
using WebSocketConnect;

namespace RemoteCommand.Cmdlet
{
    [Cmdlet(VerbsLifecycle.Invoke, "RemoteCommand")]
    public class InvokeRemoteCommand : PSCmdlet
    {
        private NLog.Logger _logger = null;
        private const int _interval = 500;
        private string _currentDirectory = null;

        [Parameter(Mandatory = true, Position = 0)]
        public string Target { get; set; }
        [Parameter(Position = 1)]
        public string Command { get; set; }
        [Parameter]
        public string ScriptFile { get; set; }
        [Parameter, Alias("PS")]
        public SwitchParameter PowerShell { get; set; }
        [Parameter]
        public SwitchParameter DebugMode { get; set; }

        protected override void BeginProcessing()
        {
            //  カレントディレクトリカレントディレクトリの一時変更
            _currentDirectory = System.Environment.CurrentDirectory;
            System.Environment.CurrentDirectory = this.SessionState.Path.CurrentFileSystemLocation.Path;

            //  初期処理
            WebSocketParam.PrepareDebugMode(DebugMode);
            WebSocketParam.SetCurrentVersion();

            //  ログ開始
            _logger = Function.SetLogger(Item.LOG_DIRECTORY, "Sender", WebSocketParam.DebugMode);
            _logger.Info("Process Start.");
            _logger.Info("Version: {0}", WebSocketParam.Version);
        }

        protected override void ProcessRecord()
        {
            _logger.Info("Connect URI: {0}", Target);
            SessionBase session = new CmdSession();

            session.Connect(Target).ConfigureAwait(false);
            session.Init().Wait();
            
            if (session.Enabled)
            {
                try
                {
                    session.Send().ConfigureAwait(false);

                    WebSocketConnect.SessionInfo.CommandInfo info = new WebSocketConnect.SessionInfo.CommandInfo();
                    if (this.PowerShell)
                    {
                        info.Mode = WebSocketConnect.SessionInfo.CommandInfo.CommandMode.PowerShell;
                    }

                    if (!string.IsNullOrEmpty(Command))
                    {
                        //  リモートコマンド
                        info.Command = Command;
                        session.Request(info).ConfigureAwait(false);
                        while (session.Status < SessionStatus.Finished)
                        {
                            Thread.Sleep(_interval);
                        }
                        session.Close().ConfigureAwait(false);
                    }
                    else if (!string.IsNullOrEmpty(ScriptFile) && File.Exists(ScriptFile))
                    {
                        info.Mode = WebSocketConnect.SessionInfo.CommandInfo.CommandMode.Script;

                        //  リモートスクリプト
                        info.LoadScript(ScriptFile);
                        session.Request(info).ConfigureAwait(false);
                        while (session.Status < SessionStatus.Finished)
                        {
                            Thread.Sleep(_interval);
                        }
                        session.Close().ConfigureAwait(false);
                    }

                    if (!string.IsNullOrEmpty(session.Remark))
                    {
                        _logger.Info(session.Remark);
                    }
                }
                catch (Exception e)
                {
                    _logger.Error("Error: Error ccurred during WebSocket Connection.");
                    _logger.Error(e.ToString());
                }
            }

            if (!string.IsNullOrEmpty(session.Remark))
            {
                _logger.Info(session.Remark);
            }

            WebSocketParam.ClearConfig();
            _logger.Info("Process End.");
        }

        protected override void EndProcessing()
        {
            //  カレントディレクトリを戻す
            System.Environment.CurrentDirectory = _currentDirectory;
        }
    }
}
