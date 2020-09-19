using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebSocketConnect.SessionInfo
{
    interface IInfoBase
    {
        string Name { get; }                                //  各SessionInfoの名前
        string Description { get; set; }                    //  説明
        Dictionary<string, string> Extension { get; set; }  //  将来拡張用パラメータ
        int ReturnCode { get; set; }                        //  Requestに対する処理の結果を書き込む。数字でのリターンコード
        string Remark { get; set; }                         //  Requestに対する処理時に発生したメッセージ、備考、特記事項を書き込む。
    }
}
