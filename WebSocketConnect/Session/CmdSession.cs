using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Threading;
using System.IO;
using WebSocketConnect.SessionInfo;
using Newtonsoft.Json;
using System.Net.WebSockets;
using WebSocketConnect.ScriptLanguage;

namespace WebSocketConnect.Session
{
    public class CmdSession : SessionBase
    {
        private static List<Language> Languages { get; set; }

        #region ReceiverSide

        protected override void MainRceiveProcess(byte[] msgBytes)
        {
            CommandInfo info = JsonConvert.DeserializeObject<CommandInfo>(Encoding.UTF8.GetString(msgBytes));
            try
            {
                switch (info.Mode)
                {
                    case CommandInfo.CommandMode.Cmd:
                        CommandProcess(info);
                        break;
                    case CommandInfo.CommandMode.PowerShell:
                        PowerShellProcess(info);
                        break;
                    case CommandInfo.CommandMode.Script:
                        ScriptOrocess(info);
                        break;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            Response(JsonConvert.SerializeObject(info)).Wait();
            WS.CloseAsync(WebSocketCloseStatus.Empty, "", CancellationToken.None).Wait();
        }

        /// <summary>
        /// cmdでコマンド実行
        /// </summary>
        /// <param name="info"></param>
        private void CommandProcess(CommandInfo info)
        {
            using (Process proc = new Process())
            {
                proc.StartInfo.FileName = "cmd.exe";
                proc.StartInfo.Arguments = "/c " + info.Command;
                proc.StartInfo.CreateNoWindow = true;
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.RedirectStandardOutput = true;
                proc.StartInfo.RedirectStandardError = true;

                proc.OutputDataReceived += (sender, e) =>
                {
                    info.AddOutputLine(e.Data);
                };
                proc.ErrorDataReceived += (sender, e) =>
                {
                    info.AddOutputLine(e.Data, error: true);
                };

                proc.Start();

                proc.BeginOutputReadLine();
                proc.BeginErrorReadLine();

                proc.WaitForExit();
                info.ReturnCode = proc.ExitCode;
            }
        }

        /// <summary>
        /// PowerShellでコマンド実行
        /// </summary>
        /// <param name="info"></param>
        private void PowerShellProcess(CommandInfo info)
        {
            if (string.IsNullOrEmpty(Item.DefualtPSCommandPath))
            {
                foreach (string psCommand in new string[] { "pwsha", "powershella" })
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
            if (string.IsNullOrEmpty(Item.DefualtPSCommandPath))
            {
                this.Remark = "Error: PowerShell command path is missing.";
                info.Remark = "Error: PowerShell command path is missing.";
                /*
                await WS.SendAsync(
                    new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(info))),
                    WebSocketMessageType.Text,
                    true,
                    CancellationToken.None);
                */
                return;
            }

            using (Process proc = new Process())
            {
                proc.StartInfo.FileName = Item.DefualtPSCommandPath;
                proc.StartInfo.Arguments = "-ExecutionPolicy Unrestricted -Command " + info.Command;
                proc.StartInfo.CreateNoWindow = true;
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.RedirectStandardOutput = true;
                proc.StartInfo.RedirectStandardError = true;

                proc.OutputDataReceived += (sender, e) =>
                {
                    info.AddOutputLine(e.Data);
                };
                proc.ErrorDataReceived += (sender, e) =>
                {
                    info.AddOutputLine(e.Data, error: true);
                };

                proc.Start();

                proc.BeginOutputReadLine();
                proc.BeginErrorReadLine();

                proc.WaitForExit();
                info.ReturnCode = proc.ExitCode;
            }
        }

        /// <summary>
        /// スクリプト実行
        /// </summary>
        /// <param name="info"></param>
        private void ScriptOrocess(CommandInfo info)
        {
            if (Languages == null)
            {
                //  事前登録ScriptLanguage設定のみ使用。今回はカスタマイズさせない方向で
                Languages = DefaultLanguageSetting.Create();
            }

            string scriptFile = Path.Combine(Item.SCRIPT_DIRECTORY, info.ScriptName);
            info.SaveScript(scriptFile);
            string extension = Path.GetExtension(scriptFile);
            Language lang = Languages.FirstOrDefault(x =>
               x.Extensions.Any(y =>
                   y.Equals(extension, StringComparison.OrdinalIgnoreCase)));

            using (Process proc = lang.GetProcess(scriptFile, ""))
            {
                proc.StartInfo.CreateNoWindow = true;
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.RedirectStandardOutput = true;
                proc.StartInfo.RedirectStandardError = true;

                proc.OutputDataReceived += (sender, e) =>
                {
                    info.AddOutputLine(e.Data);
                };
                proc.ErrorDataReceived += (sender, e) =>
                {
                    info.AddOutputLine(e.Data, error: true);
                };

                proc.Start();

                proc.BeginOutputReadLine();
                proc.BeginErrorReadLine();

                proc.WaitForExit();
                info.ReturnCode = proc.ExitCode;
            }
        }

        #endregion
        #region Sender Side

        protected override void MainSendProcess(byte[] msgBytes)
        {
            CommandInfo info = JsonConvert.DeserializeObject<CommandInfo>(Encoding.UTF8.GetString(msgBytes));
            info.OutputList.ForEach(x => x.ConsoleWrite());

            WS.CloseAsync(WebSocketCloseStatus.Empty, "", CancellationToken.None).Wait();
        }

        #endregion
    }
}