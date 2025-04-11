using System;

namespace Network.Udp
{
    [Flags]
    public enum UdpHeader
    {
        None,

        /// <summary>
        /// エラー
        /// </summary>
        Error = 1 << 0,

        /// <summary>
        /// 同期
        /// </summary>
        SimpleSync = 1 << 1,

        /// <summary>
        /// プレイヤー探索
        /// </summary>
        Discover = 1 << 2,

        /// <summary>
        /// プレイヤー探索応答
        /// </summary>
        DiscoverResponse = 1 << 3,

        /// <summary>
        /// 実行メソッド送信
        /// </summary>
        SendMethod = 1 << 4,

        /// <summary>
        /// 入力情報
        /// </summary>
        Input = 1 << 5,

        /// <summary>
        /// フレームレート調整
        /// </summary>
        FrameRate = 1 << 6,

        /// <summary>
        /// オブジェクト生成
        /// </summary>
        Spawn = 1 << 7,

        /// <summary>
        /// オブジェクト削除
        /// </summary>
        Destroy = 1 << 8,

        /// <summary>
        /// 座標同期
        /// </summary>
        Position = 1 << 9,

        /// <summary>
        /// フレーム同期
        /// </summary>
        FrameSync = 1 << 10,

        /// <summary>
        /// ドローンアクション
        /// </summary>
        DroneAction = 1 << 11,

        /// <summary>
        /// アイテム使用
        /// </summary>
        GetItem = 1 << 12,
    }
}