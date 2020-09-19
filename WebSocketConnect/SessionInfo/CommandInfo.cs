using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hnx8.ReadJEnc;
using System.IO;

namespace WebSocketConnect.SessionInfo
{
    public class CommandInfo : InfoBase
    {
        #region Public Parameter

        public string Command { get; set; }

        public string ScriptName { get; set; }
        public string ScriptBody { get; set; }
        public string Encoding { get; set; }
        public bool WithBOM { get; set; }

        public class StandardOutput
        {
            public bool Error { get; set; }
            public string Output { get; set; }

            public void ConsoleWrite()
            {
                if (Error)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(Output);
                    Console.ResetColor();
                }
                else
                {
                    Console.WriteLine(Output);
                }
            }
        }
        public List<StandardOutput> OutputList { get; set; }

        public enum CommandMode
        {
            Cmd,
            PowerShell,
            Script,
        };
        public CommandMode Mode { get; set; } = CommandMode.Cmd;

        #endregion

        public CommandInfo()
        {
            this.OutputList = new List<StandardOutput>();
        }

        /// <summary>
        /// 標準出力結果を格納
        /// </summary>
        /// <param name="outputLine"></param>
        public void AddOutputLine(string outputLine)
        {
            this.OutputList.Add(new StandardOutput() { Output = outputLine });
        }

        /// <summary>
        /// 標準エラー出力を格納
        /// </summary>
        /// <param name="outputLine"></param>
        /// <param name="error"></param>
        public void AddOutputLine(string outputLine, bool error)
        {
            this.OutputList.Add(new StandardOutput() { Output = outputLine, Error = error });
        }

        /// <summary>
        /// BOM有りUTF文字コード名
        /// </summary>
        private static CharCode[] UTFCodes = new CharCode[]
        {
            CharCode.UTF8, CharCode.UTF32, CharCode.UTF32B, CharCode.UTF16, CharCode.UTF16B
        };

        /// <summary>
        /// 文字コードを取得
        /// </summary>
        /// <returns></returns>
        public Encoding GetEncoding()
        {
            switch (Encoding)
            {
                case "utf-8":
                    return WithBOM ? new UTF8Encoding(true) : new UTF8Encoding(false);
                case "utf-16":
                    return WithBOM ? new UnicodeEncoding(false, true) : new UnicodeEncoding(false, false);
                case "utf-16BE":
                    return WithBOM ? new UnicodeEncoding(true, true) : new UnicodeEncoding(true, false);
                case "utf-32":
                    return WithBOM ? new UTF32Encoding(false, true) : new UTF32Encoding(false, false);
                case "utf-32BE":
                    return WithBOM ? new UTF32Encoding(true, true) : new UTF32Encoding(true, false);
                default:
                    return System.Text.Encoding.GetEncoding(Encoding);
            }
        }

        /// <summary>
        /// Sender側。スクリプトファイルを読み込んで格納する
        /// </summary>
        /// <param name="scriptFile"></param>
        public void LoadScript(string scriptFile)
        {
            this.ScriptName = Path.GetFileName(scriptFile);

            FileInfo fi = new FileInfo(scriptFile);
            if (fi.Length > 0)
            {
                using (var fr = new FileReader(fi))
                {
                    CharCode code = fr.Read(fi);
                    this.Encoding = code.GetEncoding().WebName;
                    this.WithBOM = UTFCodes.Contains(code);
                    this.ScriptBody = fr.Text;
                }
            }
            else
            {
                this.Encoding = System.Text.Encoding.UTF8.WebName;
                this.WithBOM = true;
            }
        }

        /// <summary>
        /// Receiver側。スクリプトファイルを書き込む
        /// </summary>
        public void SaveScript(string scriptFile)
        {
            if (!Directory.Exists(Item.SCRIPT_DIRECTORY))
            {
                Directory.CreateDirectory(Item.SCRIPT_DIRECTORY);
            }
            
            using(var sw = new StreamWriter(scriptFile, false, this.GetEncoding()))
            {
                sw.Write(this.ScriptBody);
            }
        }
    }
}
