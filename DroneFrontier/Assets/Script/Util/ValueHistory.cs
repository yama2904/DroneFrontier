/// <summary>
/// �l�̗������Ǘ�����N���X1
/// </summary>
public class ValueHistory<T>
{
    /// <summary>
    /// ���ݒl
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
    /// �O��l
    /// </summary>
    public T PreviousValue { get; set; } = default;

    /// <summary>
    /// ���ݒl���X�V�����ۂɎ����I�ɑO��l���X�V���邩
    /// </summary>
    public bool AutoUpdate { get; set; } = true;
}
