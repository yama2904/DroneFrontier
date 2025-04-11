using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ドローンが使用可能なアイテムを実装するインターフェース
/// </summary>
public interface IDroneItem
{
    /// <summary>
    /// アイテムの所持アイコン生成
    /// </summary>
    /// <returns>生成したアイコン</returns>
    Image InstantiateIcon();

    /// <summary>
    /// アイテム使用
    /// </summary>
    /// <param name="drone">アイテムを使用するドローン</param>
    /// <returns>true:成功, false:失敗</returns>
    bool UseItem(GameObject drone);
}
