using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;

namespace RemoteCommand
{
    internal class Item
    {
        /// <summary>
        /// アプリケーション自体の名前
        /// </summary>
        public const string APPLICATION_NAME = "RemoteCommand";

        /// <summary>
        /// アプリケーションの作業フォルダー
        /// </summary>
        //public readonly static string WORK_DIRECTORY = Path.Combine(
        //    Environment.ExpandEnvironmentVariables("%ProgramData%"), APPLICATION_NAME);
        public readonly static string WORK_DIRECTORY = Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), APPLICATION_NAME);

        /// <summary>
        /// ログファイル出力先フォルダー
        /// </summary>
        public readonly static string LOG_DIRECTORY = Path.Combine(WORK_DIRECTORY, "Logs");

        /// <summary>
        /// CancelKeyPressイベント未割当/割当済
        /// </summary>
        public static bool UnassignedCancelEvent = true;

        /// <summary>
        /// 同コンソール上でCancelKeyPressを複数回実施されてしまうことを防止する為に、
        /// WebSocketServerを静的アイテムで管理し、どのCancelKeyPressイベントでも同じ
        /// 指定でStop()できるように
        /// </summary>
        public static WebSocketConnect.WebSocketServer WssHandle = null;

        public const int DEFAULT_PORT = 6080;
    }
}
