using UnityEngine;

/// <summary>
/// ゲーム中で使用可能なアイテムを実装するインターフェース
/// </summary>
public interface IGameItem
{
    /// <summary>
    /// アイテム使用
    /// </summary>
    /// <param name="drone">アイテムを使用するドローン</param>
    /// <returns>true:成功, false:失敗</returns>
    bool UseItem(GameObject drone);
}
