using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ResultSceneManager : MonoBehaviour
{
    [SerializeField] Text NameText1st = null;   //一位の名前を表示するテキスト
    [SerializeField] Text NameText2st = null;   //二位の名前を表示するテキスト
    [SerializeField] Text NameText3st = null;   //三位の名前を表示するテキスト
    [SerializeField] Text NameText4st = null;   //四位の名前を表示するテキスト

    static string[] ranking = null;

    void Start()
    {
        // カーソル表示
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // バグ防止
        if (ranking == null) return;

        for (int i = 0; i < ranking.Length; i++)
        {
            switch (i)
            {
                // 一位
                case 0:
                    NameText1st.text = ranking[i];
                    break;

                // 二位
                case 1:
                    NameText2st.text = ranking[i];
                    break;

                // 三位
                case 2:
                    NameText3st.text = ranking[i];
                    break;

                // 四位
                case 3:
                    NameText4st.text = ranking[i];
                    break;
            }
        }

        // 初期化
        ranking = null;
    }

    void Update() { }

    public void SelectEnd()
    {
        // SE再生
        SoundManager.Play(SoundManager.SE.SELECT, SoundManager.SEVolume);

        // ホーム画面に戻る
        SceneManager.LoadScene("HomeScene");
    }

    /// <summary>
    /// 順位が高い人から昇順に名前を指定してランキングを設定
    /// </summary>
    /// <param name="names">ランキングに表示する名前</param>
    public static void SetRank(params string[] names)
    {
        ranking = new string[names.Length];
        ranking = names;
    }
}
