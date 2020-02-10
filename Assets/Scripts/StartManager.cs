using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Netcode;


public class StartManager : MonoBehaviour
{
    #region SingletonCode
    private static StartManager _instance;
    public static StartManager Instance { get { return _instance; } }
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            _instance = this;
        }
    }
    //single pattern ends here
    #endregion


    public InputField ipText;
    public InputField chatText;
    public InputField username;

    public Text connectionStatus;
    public Text chatLog;
    public Text slot1;
    public Text slot2;
    public Text slot3;
    public Text slot4;
    public Text readyStatus;

    public Button readyButton;
    public Button sendButton;
    public Button rtsButton;
    public Button fpsButton;

    public bool Ready = false;

    private PlayerType tempRole = PlayerType.Spectator;

    private void Start()
    {
        readyButton.interactable = false;
        sendButton.interactable = false;
        rtsButton.interactable = false;
        fpsButton.interactable = false;
        GameSceneController.Instance.type = PlayerType.Spectator;

    }

    //initial connection to server
    public void ConnectToServer()
    {
        if (ipText.text == "" || ipText.text == null)
        {
            NetworkManager.ConnectToServer("127.0.0.1");
        }
        else
        {
            NetworkManager.ConnectToServer(ipText.text);
        }
    }
    //recieve successful
    public void OnConnected(bool success)
    {
        if (success)
        {
            connectionStatus.text = "Connection Status: Connected";
            sendButton.interactable = true;
            rtsButton.interactable = true;
            fpsButton.interactable = true;
        }
        else {
            connectionStatus.text = "Connection Status: Unable to Connect";
        }
    }

    //try RTS join
    public void OnRTSButton()
    {
        NetworkManager.SelectRole(PlayerType.RTS);
    }
    //try FPS join
    public void OnFPSButton()
    {
        NetworkManager.SelectRole(PlayerType.FPS);
    }

    //check if role is avaliable
    public void OnRoleSelected(bool success) {
        if (success)
        {
            GameSceneController.Instance.type = this.tempRole;
            readyStatus.text = "Status: Role Selected";
            readyButton.interactable = true;
        }
        else {
            GameSceneController.Instance.type = PlayerType.Spectator;
            readyStatus.text = "Status: Role Unavaliable";
            readyButton.interactable = false;
            Ready = false;
        }
    }

    public void OnReadyButton() {
        if (Ready == false)
        {
            Ready = true;
        }
        else {
            Ready = false;
        }

        NetworkManager.OnReady(Ready);
    }

    public void LoadGame()
    {
        GameSceneController.Instance.SwapScene(2);
    }

    public void StartCount() { 
    
    }

    public void StopCount() { 
    
    }

    private void Update()
    {
        //update roles
    }

    public void recieveMessage(string messsage) { 
        
    }

    public void sendMessage() {
        NetworkManager.SendMessage(chatText.text);
        chatText.text = "";
    }
}
