using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleSceneManager : MonoBehaviour
{
    void Start()
    {
        SoundManager.Play(SoundManager.BGM.Home, SoundManager.MasterBGMVolume * 0.8f);
    }

   void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            //SE再生
            SoundManager.Play(SoundManager.SE.Select);

            SceneManager.LoadScene("HomeScene");
        }
    }
}
