using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebSocketConnect.SessionInfo;

namespace WebSocketConnect.Session
{
    interface ISessionBase
    {
        /// <summary>
        /// セッション名
        /// </summary>
        string Name { get; }

        #region ReveiverSide

        /// <summary>
        /// Receiver側。パケット受信してからの処理
        /// </summary>
        /// <returns></returns>
        Task Receive();

        /// <summary>
        /// セッション開始時処理 (Receiver)
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        Task Init(InitInfo info);

        /// <summary>
        /// Responseを返信 (ArraySegment&lt;byte&gt;型)
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        Task Response(ArraySegment<byte> message);

        /// <summary>
        /// Responseを返信 (string型)
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        Task Response(string message);

        #endregion
        #region SenderSide

        /// <summary>
        /// Sender側。Receiverへの接続開始。
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        Task Connect(string uri);

        /// <summary>
        /// セッション開始時処理 (Sender)
        /// </summary>
        /// <returns></returns>
        Task Init();

        /// <summary>
        /// Sender側。パケットを送信する処理
        /// </summary>
        /// <returns></returns>
        Task Send();

        /// <summary>
        /// Requestを送信 (ArraySegment&lt;byte&gt;型)
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        Task Request(ArraySegment<byte> message);

        /// <summary>
        /// Requestを送信 (string型)
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        Task Request(string message);

        /// <summary>
        /// Requestを送信 (InfoBase型)
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        Task Request(InfoBase info);

        /// <summary>
        /// Closeメッセージを送信してセッション終了
        /// </summary>
        /// <returns></returns>
        Task Close();

        #endregion
    }
}
