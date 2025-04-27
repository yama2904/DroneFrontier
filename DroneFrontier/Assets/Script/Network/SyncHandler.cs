using Cysharp.Threading.Tasks;
using Network.Udp;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Network
{
    /// <summary>
    /// 同期に関する処理を行うクラス
    /// </summary>
    public class SyncHandler
    {
        /// <summary>
        /// 同期パケットを受信した各プレイヤー名
        /// </summary>
        private List<string> _receivedPlayers = new List<string>();

        /// <summary>
        /// 同期した値
        /// </summary>
        private object _syncValue = null;

        /// <summary>
        /// 全てのプレイヤーが同期を行うまで非同期で待機する
        /// </summary>
        /// <param name="timeout">タイムアウト（秒）</param>
        /// <returns>同期が完了した場合はtrue</returns>
        /// <exception cref="TimeoutException">タイムアウト</exception>
        public async UniTask WaitAsync(int timeout = 0)
        {
            await SyncValueAsync(null, timeout);
        }

        /// <summary>
        /// ホストが指定した値を全てのプレイヤーが取得して同期を行うまで非同期で待機する
        /// </summary>
        /// <param name="value">同期する値</param>
        /// <param name="timeout">タイムアウト（秒）</param>
        /// <returns>同期が完了した場合はtrue</returns>
        /// <exception cref="TimeoutException">タイムアウト</exception>
        public async UniTask<object> SyncValueAsync(object value, int timeout = 0)
        {
            _receivedPlayers.Clear();
            _receivedPlayers.Add(NetworkManager.Singleton.MyPlayerName);

            // 同期パケット受信イベント設定
            NetworkManager.Singleton.OnUdpReceive += OnUdpReceiveOfSync;

            // 受信前に同期パケット送信
            UdpPacket packet = new SimpleSyncPacket();
            if (NetworkManager.Singleton.IsHost)
            {
                if (value != null)
                {
                    packet = new SimpleSyncPacket(value);
                    _syncValue = value;
                }
                NetworkManager.Singleton.SendToAll(packet);
            }

            // タイムアウト計測用ストップウォッチ開始
            Stopwatch timeoutStopwatch = Stopwatch.StartNew();

            // ホスト側再送計測用ストップウォッチ開始
            Stopwatch retryStopwatch = new Stopwatch();
            if (NetworkManager.Singleton.IsHost)
            {
                retryStopwatch.Start();
            }

            // 同期完了まで待機
            bool success = false;
            while (true)
            {
                // 全てのプレイヤーから受信した場合は終了
                if (_receivedPlayers.Count == NetworkManager.Singleton.PlayerCount)
                {
                    success = true;
                    break;
                }

                // タイムアウト検知
                if (timeout > 0)
                {
                    if (timeoutStopwatch.Elapsed.Seconds > timeout) break;
                }

                // 1秒ごとにリトライ
                if (retryStopwatch.Elapsed.Seconds >= 1)
                {
                    NetworkManager.Singleton.SendToAll(packet);
                    retryStopwatch.Restart();
                }

                // 100ミリ秒ごとにチェック
                await UniTask.Delay(100);
            }

            // 同期パケット受信イベント削除
            NetworkManager.Singleton.OnUdpReceive -= OnUdpReceiveOfSync;

            if (!success)
            {
                throw new TimeoutException("タイムアウトに達したため同期をキャンセルしました。");
            }

            return _syncValue;
        }

        /// <summary>
        /// 同期パケット受信イベント
        /// </summary>
        /// <param name="name">プレイヤー名</param>
        /// <param name="header">受信したUDPパケットのヘッダ</param>
        /// <param name="packet">受信したUDPパケット</param>
        private void OnUdpReceiveOfSync(string name, UdpHeader header, UdpPacket packet)
        {
            // 同期パケット以外は無視
            if (header != UdpHeader.SimpleSync) return;

            if (!_receivedPlayers.Contains(name))
            {
                _receivedPlayers.Add(name);
            }

            if (_syncValue == null)
            {
                _syncValue = (packet as SimpleSyncPacket).Value;
            }

            // 同期パケットを返す
            NetworkManager.Singleton.SendToAll(packet);
        }
    }
}