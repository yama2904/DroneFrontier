/// <summary>
/// ビットフラグに関する処理を提供するクラス
/// </summary>
public class BitFlagUtil
{
    /// <summary>
    /// ビットフラグが立っているかチェックする
    /// </summary>
    /// <param name="flag">チェックするビットフラグ</param>
    /// <param name="digits">チェックする桁（0始まり）</param>
    /// <returns></returns>
    public static bool CheckFlag(int flag, int digits)
    {
        return (flag & (1 << digits)) != 0;
    }

    /// <summary>
    /// 指定した桁のビットフラグを更新する
    /// </summary>
    /// <param name="flag">更新するビットフラグ</param>
    /// <param name="digits">更新する桁（0始まり）</param>
    /// <param name="value">更新値</param>
    /// <returns>更新後のビットフラグ</returns>
    public static int UpdateFlag(int flag, int digits, bool value)
    {
        // digits桁目を0にクリアする + valueがtrueの場合にdigits桁目を1にする
        return (flag & ~(1 << digits)) | (value ? 1 : 0 << digits);
    }
}
