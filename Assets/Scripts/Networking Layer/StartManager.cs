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
        while (!NetworkManager.isConnected)
        {

        }

        GameSceneController.Instance.SwapScene(2);
    }
}
