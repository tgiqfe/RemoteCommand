using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebSocketConnect.ScriptLanguage
{
    /// <summary>
    /// DefaultのLanguageの設定値を取得
    /// </summary>
    class DefaultLanguageSetting
    {
        public static List<Language> Create()
        {
            List<Language> list = new List<Language>();
            list.Add(new Language()
            {
                Name = "exe",
                Extensions = new string[] { ".exe" },
                Command = null,
            });
            list.Add(new Language()
            {
                Name = "cmd",
                Extensions = new string[] { ".bat", ".cmd" },
                Command = "cmd",
                ArgsPrefix = "/c \"",
                ArgsMidWithoutArgs = "\"",
                ArgsMidWithArgs = "\" ",
                ArgsSuffix = "",
            });
            list.Add(new Language()
            {
                Name = "PowerShell",
                Extensions = new string[] { ".ps1" },
                Command = @"C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe",
                ArgsPrefix = "-ExecutionPolicy Unrestricted -File \"",
                ArgsMidWithoutArgs = "\"",
                ArgsMidWithArgs = "\" ",
                ArgsSuffix = "",
            });
            list.Add(new Language()
            {
                Name = "WScript",
                Extensions = new string[] { ".vbs", ".vbe", ".js", ".jse", ".wsf", ".wsh" },
                Command = @"C:\Windows\System32\wscript.exe",
                ArgsPrefix = "//nologo \"",
                ArgsMidWithoutArgs = "\"",
                ArgsMidWithArgs = "\" ",
                ArgsSuffix = "",
            });
            list.Add(new Language()
            {
                Name = "Go",
                Extensions = new string[] { ".go" },
                Command = @"C:\Program Files\Go\bin\go.exe",
                ArgsPrefix = "\"",
                ArgsMidWithoutArgs = "\"",
                ArgsMidWithArgs = "\" ",
                ArgsSuffix = "",
            });
            list.Add(new Language()
            {
                Name = "Node.js",
                Extensions = new string[] { ".js" },
                Command = @"C:\Program Files\nodejs\node.exe",
                ArgsPrefix = "\"",
                ArgsMidWithoutArgs = "\"",
                ArgsMidWithArgs = "\" ",
                ArgsSuffix = "",
            });
            return list;
        }
    }
}
