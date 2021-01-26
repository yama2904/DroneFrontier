using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ResultButtonsController : MonoBehaviour
{
    [SerializeField] Text NameText1st = null;   //一位の名前を表示するテキスト
    [SerializeField] Text NameText2st = null;   //二位の名前を表示するテキスト
    [SerializeField] Text NameText3st = null;   //三位の名前を表示するテキスト
    [SerializeField] Text NameText4st = null;   //四位の名前を表示するテキスト

    public enum Rank
    {
        RANK_1ST,
        RANK_2ST,
        RANK_3ST,
        RANK_4ST,

        NONE
    }
    static string[] ranks = new string[(int)Rank.NONE];

    void Start()
    {
        //バグ防止
        for (int i = 0; i < (int)Rank.NONE; i++)
        {
            if (ranks[i] == null)
            {
                ranks[i] = "";
            }
        }

        for (int i = 0; i < (int)Rank.NONE; i++)
        {
            switch (i)
            {
                case (int)Rank.RANK_1ST:
                    NameText1st.text = ranks[i];
                    break;

                case (int)Rank.RANK_2ST:
                    NameText2st.text = ranks[i];
                    break;

                case (int)Rank.RANK_3ST:
                    NameText3st.text = ranks[i];
                    break;

                case (int)Rank.RANK_4ST:
                    NameText4st.text = ranks[i];
                    break;
            }
        }

        //初期化
        ranks = new string[(int)Rank.NONE];
    }

    void Update()
    {

    }

    public void SelectOnemore()
    {

    }

    public void SelectEnd()
    {
        BaseScreenManager.SetScreen(BaseScreenManager.Screen.KURIBOCCHI);
    }

    public static void SetRank(string name, Rank rank)
    {
        ranks[(int)rank] = name;
    }
}
