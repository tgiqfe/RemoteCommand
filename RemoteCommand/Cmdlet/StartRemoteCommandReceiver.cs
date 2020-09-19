using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Management.Automation;
using WebSocketConnect;

namespace RemoteCommand.Cmdlet
{
    [Cmdlet(VerbsLifecycle.Start, "RemoteCommandReceiver")]
    public class StartRemoteCommandReceiver : PSCmdlet
    {
        private NLog.Logger _logger = null;

        [Parameter]
        public SwitchParameter DebugMode { get; set; }

        protected override void BeginProcessing()
        {
            //  初期処理
            WebSocketParam.PrepareDebugMode(DebugMode);
            WebSocketParam.SetCurrentVersion();

            //  ログ開始
            _logger = Function.SetLogger(Item.LOG_DIRECTORY, "Receiver", WebSocketParam.DebugMode);
            _logger.Info("Process Start.");
            _logger.Info("Version: {0}", WebSocketParam.Version);
        }

        protected override void ProcessRecord()
        {
            _logger.Info("ServerPrefiexes: {0}", string.Join(", ", WebSocketParam.ServerPrefixes));

            using (Item.WssHandle = new WebSocketServer())
            {
                _logger.Debug("Set cancel event.");
                Console.CancelKeyPress += (sender, e) =>
                {
                    e.Cancel = false;
                    if (Item.WssHandle != null)
                    {
                        Item.WssHandle.Stop();
                    }
                    _logger.Info("Cancel key entered.");
                };
                Item.UnassignedCancelEvent = false;

                //  ワーキングセットを定期的に縮小化
                _logger.Info("Start ShrinkWorkingSet Thread.");
                WorkingSet.Shrink().ConfigureAwait(false);

                //  WebSocket待ち受け開始
                _logger.Info("Start WebSocket Server.");
                Item.WssHandle.Start().Wait();
            }

            //  終了処理
            _logger.Info("Stop WebSocket Server.");
            _logger.Info("Process End.");
            WebSocketParam.ClearConfig();
            Item.WssHandle = null;
        }
    }
}
