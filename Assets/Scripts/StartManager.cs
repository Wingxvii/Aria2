using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Netcode;
using System.Text;

public class StartManager : MonoBehaviour
{
    #region SingletonCode
    private static StartManager _instance;
    public static StartManager Instance { get { return _instance; } }

    public Queue<string> queueMessages;
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
    public Text[] slots;
    public Text readyStatus;
    public Text readyButtonText;

    public Button readyButton;
    public Button sendButton;
    public Button rtsButton;
    public Button fpsButton;

    public bool Ready = false;

    private PlayerType tempRole = PlayerType.Spectator;
    public bool rolesUpdated = false;

    public float countdownSeconds = 5.0f;
    public bool countdown = false;
    public bool loadGame = false;
    public bool endGame = false;
    public bool connected = false;

    private void Start()
    {
        readyButton.interactable = false;
        sendButton.interactable = false;
        rtsButton.interactable = false;
        fpsButton.interactable = false;
        GameSceneController.Instance.type = PlayerType.Spectator;

        foreach (Text slot in slots) {
            slot.gameObject.SetActive(false);
        }

        queueMessages = new Queue<string>();

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

    public void OnRoleUpdate(bool b)
    {
        rolesUpdated = b;
    }
    //recieve successful
    public void OnConnected(bool success)
    {
        if (success)
        {
            connected = true;
            
        }
        else {
            connected = false;
        }
        OnRoleUpdate(true);
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
    public void OnRoleSelected(int type) {
        GameSceneController.Instance.type = (PlayerType)type;
        
        //readyStatus.text = "Status: Role " + GameSceneController.Instance.type.ToString();

        //if (GameSceneController.Instance.type == PlayerType.Spectator)
        //{
        //    readyButton.interactable = false;
        //    Ready = false;
        //}
        //else {
        //    readyButton.interactable = true;
        //}
        OnRoleUpdate(true);
    }

    public void OnReadyButton() {
        if (Ready == false)
        {
            Ready = true;
            readyButtonText.text = "Unready";
            readyStatus.text = "Status: Ready";

        }
        else {
            Ready = false;
            StopCount();
            readyButtonText.text = "Ready!";
            readyStatus.text = "Status: Not Ready";
        }
        OnRoleUpdate(true);
        NetworkManager.OnReady(Ready);
    }

    public void LoadGame()
    {
        loadGame = true;
    }

    public void EndGame()
    {
        endGame = true;
    }

    public void StartCount() {
        countdown = true;
        //rtsButton.interactable = false;
        //fpsButton.interactable = false;

    }

    public void StopCount() {
        countdown = false;
        countdownSeconds = 5.0f;
        //rtsButton.interactable = true;
        //fpsButton.interactable = true;
    }

    private void Update()
    {
        if(connected)
        {
            connectionStatus.text = "Connection Status: Connected";
            sendButton.interactable = true;
            rtsButton.interactable = true;
            fpsButton.interactable = true;
        }
        else
        {
            connectionStatus.text = "Connection Status: Unable to Connect";
            sendButton.interactable = false;
            rtsButton.interactable = false;
            fpsButton.interactable = false;
            readyButton.interactable = false;
        }
        if(loadGame)
        {
            loadGame = false;
            GameSceneController.Instance.SwapScene(2);
        }
        if(endGame)
        {
            endGame = false;
            GameSceneController.Instance.SwapScene(3);
        }
        if (countdown)
        {
            rtsButton.interactable = false;
            fpsButton.interactable = false;
        }
        else
        {
            rtsButton.interactable = true;
            fpsButton.interactable = true;
        }

        while (queueMessages.Count > 0)
        {
            chatLog.text += queueMessages.Dequeue();
        }

        readyStatus.text = "Status: Role " + GameSceneController.Instance.type.ToString();
        if (GameSceneController.Instance.type == PlayerType.Spectator)
        {
            readyButton.interactable = false;
            Ready = false;
        }
        else
        {
            readyButton.interactable = true;
        }

        //countdown
        if (countdown) {
            countdownSeconds -= Time.deltaTime;
            if (countdownSeconds < 0.0f) { countdownSeconds = 0.0f; }
            
            readyStatus.text = "Status: Game Starting in " + ((int)countdownSeconds).ToString() + " Seconds";
        }

        //update roles
        if (rolesUpdated) {
            for(int counter = 0; counter < NetworkManager.allUsers.Count; counter++) 
            {
                StringBuilder output = new StringBuilder();

                output.Append(NetworkManager.allUsers[counter].username);
                output.Append(" - ");
                if (NetworkManager.allUsers[counter].type == PlayerType.FPS) { output.Append("FPS - "); }
                else if (NetworkManager.allUsers[counter].type == PlayerType.RTS) { output.Append("RTS - "); }
                else { output.Append("Spectator - "); }

                if (NetworkManager.allUsers[counter].readyStatus)
                {
                    output.Append("Ready");
                }
                else {
                    output.Append("Not Ready");
                }

                //set as slot
                if (slots.Length > counter)
                {
                    slots[counter].text = output.ToString();
                    slots[counter].gameObject.SetActive(true);
                }
                else {
                    Debug.LogWarning("More than allowed slots of players tried to connect");
                }
            }
            OnRoleUpdate(false);
        }
    }

    public void recieveMessage(string messsage) {
        //chatLog.text += "\n";
        //chatLog.text += messsage;
        lock (queueMessages)
        {
            queueMessages.Enqueue("\n" + messsage);
        }
    }

    public void sendMessage() {
        NetworkManager.SendMessage(chatText.text);
        chatText.text = "";
    }
}
