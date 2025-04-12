/// <summary>
/// �l�̗������Ǘ�����N���X1
/// </summary>
public class ValueHistory<T>
{
    /// <summary>
    /// ���ݒl
    /// </summary>
    public T CurrentValue { get; set; } = default;

    /// <summary>
    /// �O��l
    /// </summary>
    public T PreviousValue { get; set; } = default;

    /// <summary>
    /// �O��l���X�V���Č��ݒl��ݒ肷��
    /// </summary>
    public void UpdateCurrentValue(T value)
    {
        UpdatePreviousValue();
        CurrentValue = value;
    }

    /// <summary>
    /// �O��l�����ݒl�ōX�V
    /// </summary>
    public void UpdatePreviousValue()
    {
        PreviousValue = CurrentValue;
    }
}
