using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleSceneManager : MonoBehaviour
{
    void Start()
    {
        SoundManager.Play(SoundManager.BGM.DRONE_UP, SoundManager.BGMVolume * 0.8f);
    }

   void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            //SE再生
            SoundManager.Play(SoundManager.SE.SELECT);

            SceneManager.LoadScene("HomeScene");
        }
    }
}
