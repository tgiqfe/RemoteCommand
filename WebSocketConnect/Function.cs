using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;
using NLog;
using NLog.Config;
using NLog.Targets;

namespace WebSocketConnect
{
    internal class Function
    {
        /// <summary>
        /// ログ出力設定
        /// </summary>
        /// <param name="preName"></param>
        /// <returns></returns>
        public static Logger SetLogger(string logDir, string preName, bool debugMode)
        {
            if (!Directory.Exists(logDir)) { Directory.CreateDirectory(logDir); }

            string logPath = System.IO.Path.Combine(
                logDir,
                string.Format("{0}_{1}.log", preName, DateTime.Now.ToString("yyyyMMdd")));

            //  ファイル出力先設定
            FileTarget file = new FileTarget("File");
            file.Encoding = Encoding.GetEncoding("Shift_JIS");
            file.Layout = "[${longdate}][${windows-identity}][${uppercase:${level}}] ${message}";
            //file.Layout = "[${longdate}][${uppercase:${level}}] ${message}";
            file.FileName = logPath;

            //  コンソール出力設定
            ConsoleTarget console = new ConsoleTarget("Console");
            //console.Layout = "[${longdate}][${windows-identity}][${uppercase:${level}}] ${message}";
            console.Layout = "[${longdate}][${uppercase:${level}}] ${message}";

            LoggingConfiguration conf = new LoggingConfiguration();
            conf.AddTarget(file);
            conf.AddTarget(console);
            if (debugMode)
            {
                conf.LoggingRules.Add(new LoggingRule("*", LogLevel.Debug, file));
                conf.LoggingRules.Add(new LoggingRule("*", LogLevel.Debug, console));
            }
            else
            {
                conf.LoggingRules.Add(new LoggingRule("*", LogLevel.Info, file));
            }
            LogManager.Configuration = conf;
            Logger logger = LogManager.GetCurrentClassLogger();

            return logger;
        }
    }
}
