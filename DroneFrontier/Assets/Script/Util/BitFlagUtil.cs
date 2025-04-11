/// <summary>
/// �r�b�g�t���O�Ɋւ��鏈����񋟂���N���X
/// </summary>
public class BitFlagUtil
{
    /// <summary>
    /// �r�b�g�t���O�������Ă��邩�`�F�b�N����
    /// </summary>
    /// <param name="flag">�`�F�b�N����r�b�g�t���O</param>
    /// <param name="digits">�`�F�b�N���錅�i0�n�܂�j</param>
    /// <returns></returns>
    public static bool CheckFlag(long flag, int digits)
    {
        return (flag & (1 << digits)) != 0;
    }

    /// <summary>
    /// �w�肵�����̃r�b�g�t���O���X�V����
    /// </summary>
    /// <param name="flag">�X�V����r�b�g�t���O</param>
    /// <param name="digits">�X�V���錅�i0�n�܂�j</param>
    /// <param name="value">�X�V�l</param>
    /// <returns>�X�V��̃r�b�g�t���O</returns>
    public static byte UpdateFlag(byte flag, int digits, bool value)
    {
        // digits���ڂ�0�ɃN���A���� + value��true�̏ꍇ��digits���ڂ�1�ɂ���
        return (byte)((flag & ~(1 << digits)) | (value ? 1 << digits : 0));
    }

    /// <summary>
    /// �w�肵�����̃r�b�g�t���O���X�V����
    /// </summary>
    /// <param name="flag">�X�V����r�b�g�t���O</param>
    /// <param name="digits">�X�V���錅�i0�n�܂�j</param>
    /// <param name="value">�X�V�l</param>
    /// <returns>�X�V��̃r�b�g�t���O</returns>
    public static int UpdateFlag(int flag, int digits, bool value)
    {
        // digits���ڂ�0�ɃN���A���� + value��true�̏ꍇ��digits���ڂ�1�ɂ���
        return (flag & ~(1 << digits)) | (value ? 1 << digits : 0);
    }

    /// <summary>
    /// �w�肵�����̃r�b�g�t���O���X�V����
    /// </summary>
    /// <param name="flag">�X�V����r�b�g�t���O</param>
    /// <param name="digits">�X�V���錅�i0�n�܂�j</param>
    /// <param name="value">�X�V�l</param>
    /// <returns>�X�V��̃r�b�g�t���O</returns>
    public static long UpdateFlag(long flag, int digits, bool value)
    {
        // digits���ڂ�0�ɃN���A���� + value��true�̏ꍇ��digits���ڂ�1�ɂ���
        return (flag & ~(1 << digits)) | (long)(value ? 1 << digits : 0);
    }
}
