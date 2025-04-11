/// <summary>
/// 値の履歴を管理するクラス1
/// </summary>
public class ValueHistory<T>
{
    /// <summary>
    /// 現在値
    /// </summary>
    public T CurrentValue
    {
        get
        {
            return _currentValue;
        }
        set
        {
            if (AutoUpdate)
            {
                PreviousValue = _currentValue;
            }
            _currentValue = value;
        }
    }
    private T _currentValue = default;

    /// <summary>
    /// 前回値
    /// </summary>
    public T PreviousValue { get; set; } = default;

    /// <summary>
    /// 現在値を更新した際に自動的に前回値も更新するか
    /// </summary>
    public bool AutoUpdate { get; set; } = true;
}
