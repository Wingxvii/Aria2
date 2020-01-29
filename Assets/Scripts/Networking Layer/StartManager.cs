using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Netcode;

public class StartManager : MonoBehaviour
{
    public InputField ipText;

    public void RTSStart()
    {
        GameSceneController.Instance.IP = ipText.text;
        GameSceneController.Instance.SwapScene(2);
        NetworkManager.ConnectToServer();

    }
    public void FPSStart()
    {

        //Debug.Break();
        GameSceneController.Instance.IP = ipText.text;
        NetworkManager.ConnectToServer();
        //while (!NetworkManager.isConnected)
        //{
        //
        //}
        if (GameSceneController.Instance.playerNumber <= 0)
        {
            GameSceneController.Instance.playerNumber = 1;
            GameSceneController.Instance.type = PlayerType.FPS;
        }

        GameSceneController.Instance.SwapScene(2);
    }
}
