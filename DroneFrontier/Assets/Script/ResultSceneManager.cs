using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ResultSceneManager : MonoBehaviour
{
    [SerializeField, Tooltip("一位の名前を表示するテキスト")] 
    private Text NameText1st = null;

    [SerializeField, Tooltip("二位の名前を表示するテキスト")]
    private Text NameText2st = null;

    [SerializeField, Tooltip("三位の名前を表示するテキスト")]
    private Text NameText3st = null;

    [SerializeField, Tooltip("四位の名前を表示するテキスト")]
    private Text NameText4st = null; 

    private static string[] _ranking = null;

    /// <summary>
    /// 順位が高い人から昇順に名前を指定してランキングを設定
    /// </summary>
    /// <param name="names">ランキングに表示する名前</param>
    public static void SetRank(params string[] names)
    {
        _ranking = new string[names.Length];
        _ranking = names;
    }

    public void SelectEnd()
    {
        // SE再生
        SoundManager.Play(SoundManager.SE.SELECT, SoundManager.SEVolume);

        // ホーム画面に戻る
        SceneManager.LoadScene("HomeScene");
    }

    private void Start()
    {
        // カーソル表示
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        for (int i = 0; i < _ranking.Length; i++)
        {
            switch (i)
            {
                // 一位
                case 0:
                    NameText1st.text = _ranking[i];
                    break;

                // 二位
                case 1:
                    NameText2st.text = _ranking[i];
                    break;

                // 三位
                case 2:
                    NameText3st.text = _ranking[i];
                    break;

                // 四位
                case 3:
                    NameText4st.text = _ranking[i];
                    break;
            }
        }

        // 初期化
        _ranking = null;
    }
}
