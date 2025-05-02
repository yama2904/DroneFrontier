using Common;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleSceneManager : MonoBehaviour
{
    private void Start()
    {
        ConfigManager.ReadConfig();
        SoundManager.Play(SoundManager.BGM.Home, 0.8f);
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            //SE再生
            SoundManager.Play(SoundManager.SE.Select);

            SceneManager.LoadScene("HomeScene");
        }
    }
}
