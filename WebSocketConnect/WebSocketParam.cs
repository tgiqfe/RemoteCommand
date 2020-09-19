using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace WebSocketConnect
{
    public class WebSocketParam
    {
        public static string[] ServerPrefixes = new string[] { "http://127.0.0.1:3000/" };
        public static int SessionCount = 0;
        public static DateTime ReceiverUpTime;
        public static bool DebugMode = false;
        public static Version Version = null;

        public static void SetCurrentVersion()
        {
            Version = Assembly.GetExecutingAssembly().GetName().Version;
        }

        /// <summary>
        /// デバッグモードtrue/falseの設定を準備
        /// </summary>
        /// <param name="debugMode"></param>
        public static void PrepareDebugMode(bool debugMode)
        {
#if DEBUG
            debugMode = true;
#endif
            DebugMode = debugMode;
        }

        /// <summary>
        /// プロセス終了時用処理
        /// </summary>
        public static void ClearConfig()
        {
        }
    }
}
