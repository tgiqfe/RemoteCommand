using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using WebSocketConnect.SessionInfo;
using System.Reflection;

namespace WebSocketConnect
{
    internal class Item
    {
        /// <summary>
        /// アプリケーション自体の名前
        /// </summary>
        public const string APPLICATION_NAME = "RemoteCommand";

        /// <summary>
        /// アプリケーション内の各プロジェクトごとの名前
        /// </summary>
        public const string PROJECT_NAME = "RemoteCommand";

        /// <summary>
        /// アプリケーションの作業フォルダー
        /// </summary>
        //public readonly static string WORK_DIRECTORY = Path.Combine(
        //  Environment.ExpandEnvironmentVariables("%ProgramData%"), APPLICATION_NAME);
        public readonly static string WORK_DIRECTORY = Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), APPLICATION_NAME);

        /// <summary>
        /// RemotePSセッション用のPowerShellのコマンドパス
        /// </summary>
        public static string DefualtPSCommandPath = null;

        /// <summary>
        /// Initメッセージ送信時のWebSocketセッション開始待ち
        /// </summary>
        public const int TRY_OPENWAIT = 50;

        /// <summary>
        /// Initメッセージ送信時のWebSocketセッション開始待ちのポーリングインターバル
        /// </summary>
        public const int TRY_INTERVAL = 100;

        /// <summary>
        /// アセンブリのバージョン。Receiver側で何度もチェック処理をしないように、静的パラメータに登録
        /// </summary>
        //public static Version Version = null;

        /// <summary>
        /// ログファイル出力先フォルダー
        /// </summary>
        public readonly static string LOG_DIRECTORY = Path.Combine(WORK_DIRECTORY, "Logs");

        /// <summary>
        /// Receiver側で実行するスクリプトファイル保存先
        /// </summary>
        public readonly static string SCRIPT_DIRECTORY = Path.Combine(WORK_DIRECTORY, "Scripts");
    }
}
