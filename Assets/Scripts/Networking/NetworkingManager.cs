using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using System.Text;
using System;
using UnityEngine.UI;

namespace Networking
{
    public enum PacketType 
    {
        //initialization connection
        INIT_CONNECTION = 0,
        //single string
        MESSAGE = 1,

        //FPS Managed Data

        //data of players
        PLAYER_DATA = 2,
        //player weapon switch
        WEAPON_DATA = 3,
        //environment damage
        ENVIRONMENT_DAMAGE = 4,

        //RTS Managed Data

        //data of all droids (up to 100)
        DROID_POSITION = 5,
        //entity built
        BUILD_ENTITY = 7,
        //entity killed
        KILL_ENTITY = 8,
        //game state
        GAME_STATE = 9,
        //player damaged
        PLAYER_DAMAGE = 10,
        //data of all turrets
        TURRET_DATA = 11,
    }


    public class NetworkingManager : MonoBehaviour
    {
        #region Netcode

        //networking import
        [DllImport("CNET.dll")]
        static extern IntPtr CreateClient();                            //Creates a client
        [DllImport("CNET.dll")]
        static extern void DeleteClient(IntPtr client);                 //Destroys a client
        [DllImport("CNET.dll")]
        static extern void Connect(string str, IntPtr client);          //Connects to c++ Server
        [DllImport("CNET.dll")]
        static extern void SendData(int type, string str, bool useTCP, IntPtr client);          //Sends Message to all other clients    
        [DllImport("CNET.dll")]
        static extern void StartUpdating(IntPtr client);                //Starts updating
        [DllImport("CNET.dll")]
        static extern void SetupPacketReception(Action<int, int, string> action); //recieve packets from server
        [DllImport("CNET.dll")]
        static extern int GetPlayerNumber(IntPtr client);

        public string ip;
        private static IntPtr Client;
        //index of the local user  (-1 = not assigned, 0 = RTS, 1-3 = FPS)
        private static int playerNumber = -1;
        #endregion

        private bool Connected = false;

        public void ConnectToServer()
        {
            if (ip != null) {
                Client = CreateClient();
                Connect(ip, Client);
                StartUpdating(Client);
                SetupPacketReception(PacketRecieved);
                playerNumber = GetPlayerNumber(Client);
                Connected = true;
            }
            Debug.Log("Player NNumber:" + playerNumber.ToString());
        }

        public static void PacketRecieved(int type, int sender, string data) {
            Debug.Log(type);
            Debug.Log(sender);
            Debug.Log(data);

        }

        public void SendPacket() {
            SendData((int)PacketType.MESSAGE, "Hello,", true, Client);
            Debug.Log("Data Sent");

        }

        public void OnDestroy()
        {
            DeleteClient(Client);
        }
    }

}