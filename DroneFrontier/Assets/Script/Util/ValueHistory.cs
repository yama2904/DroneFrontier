/// <summary>
/// 値の履歴を管理するクラス1
/// </summary>
public class ValueHistory<T>
{
    /// <summary>
    /// 現在値
    /// </summary>
    public T CurrentValue { get; set; } = default;

    /// <summary>
    /// 前回値
    /// </summary>
    public T PreviousValue { get; set; } = default;

    /// <summary>
    /// 前回値を更新して現在値を設定する
    /// </summary>
    public void UpdateCurrentValue(T value)
    {
        UpdatePreviousValue();
        CurrentValue = value;
    }

    /// <summary>
    /// 前回値を現在値で更新
    /// </summary>
    public void UpdatePreviousValue()
    {
        PreviousValue = CurrentValue;
    }
}
