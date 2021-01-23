using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class MatchingButtonsController : NetworkBehaviour
{
    void Start()
    {
        
    }
    
    void Update()
    {
        
    }

    public void SelectDecision()
    {
        
    }

    public void SelectBack()
    {
        if (isServer)
        {
            NetworkManager.singleton.StopHost();
        }
        NonGameManager.LoadNonGameScene(BaseScreenManager.Screen.KURIBOCCHI);
    }
}
