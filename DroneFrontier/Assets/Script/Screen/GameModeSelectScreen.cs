using UnityEngine;

public class GameModeSelectScreen : MonoBehaviour, IScreen
{
    //ゲームモード
    public enum GameMode
    {
        BATTLE,   //バトルモード
        RACE,     //レースモード

        NONE
    }
    public static GameMode Mode { get; set; } = GameMode.NONE;  //選んだゲームモード

    public void Initialize() { }
}
