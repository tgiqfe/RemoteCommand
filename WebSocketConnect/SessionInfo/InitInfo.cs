using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace WebSocketConnect.SessionInfo
{
    /// <summary>
    /// Senderから最初に実行するInitメソッドで使用するパラメータ
    /// </summary>
    public class InitInfo
    {
        /// <summary>
        /// これから開始するSessionの名前
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// これから開始するSessionのType
        /// </summary>
        public Type Type { get; set; }

        /// <summary>
        /// Sender側のアセンブリバージョン
        /// </summary>
        public Version Version { get; set; }

        /// <summary>
        /// 特記事項。セッション開始前の簡易的な判定に使用
        /// </summary>
        public string Remark { get; set; }

        /// <summary>
        /// Init処理の実行結果
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// その他拡張用パラメータ
        /// </summary>
        public virtual Dictionary<string, string> Extension { get; set; }

        public InitInfo() { }
        public InitInfo(string name, Type type)
        {
            this.Name = name;
            this.Type = type;
            //this.Version = Assembly.GetExecutingAssembly().GetName().Version;
            //WebSocketParam.Version = Version;
            this.Version = WebSocketParam.Version;
        }

        /// <summary>
        /// SenderのバージョンとReceiverのバージョンをチェック
        /// Revision不一致⇒○、Build不一致⇒×、Minor不一致⇒×、Major不一致⇒×
        /// Debug用のバージョン上書きにも対応
        /// </summary>
        public bool CheckVersion()
        {
            //  Version ⇒ Sender側バージョン
            //  Item.Version ⇒ Receiver側バージョン
            /*
            if(Item.Version == null)
            {
                Item.Version = Assembly.GetExecutingAssembly().GetName().Version;
            }
            */
            if(WebSocketParam.Version == null)
            {
                WebSocketParam.Version = Assembly.GetExecutingAssembly().GetName().Version;
            }
            /*
            return Version.Major == Item.Version.Major &&
                Version.Minor == Item.Version.Minor &&
                Version.Build == Item.Version.Build;
            */
            return Version.Major == WebSocketParam.Version.Major &&
                Version.Minor == WebSocketParam.Version.Minor &&
                Version.Build == WebSocketParam.Version.Build;
        }
    }
}
