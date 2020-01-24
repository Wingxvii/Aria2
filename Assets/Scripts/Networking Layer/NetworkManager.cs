using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using System;
using System.Text;

namespace Netcode
{
    public enum PacketType
    {
        //initialization connection
        INIT = 0,

        //single string
        MESSAGE = 1,

        //FPS DATA TYPES

        //2vec3 + int
        PLAYERDATA = 2,
        //int
        WEAPONSTATE = 3,
        //int
        DAMAGEDEALT = 4,

        //RTS DATA TYPES

        //vec4[0-100]
        ENTITYDATA = 5,
        //2int + vec3
        BUILD = 6,
        //int
        KILL = 7,
        //int, float
        GAMESTATE = 8,
    }

    public class EntityData {
        public Vector3 position = new Vector3();
        public Vector3 rotation = new Vector3();
        public int state = 0;
        public int weapon = 0;
        public bool updated = false;

    }

    //this is used for both RTS and FPS
    public class DataState
    {
        //players data(ignore self)
        public EntityData p1 = new EntityData();
        public EntityData p2 = new EntityData();
        public EntityData p3 = new EntityData();

        //droid and turret data
        public Dictionary<int, EntityData> entityUpdates = new Dictionary<int, EntityData>();

        //instanced data
        public Queue<Tuple<int, int, Vector3>> BuildEntity = new Queue<Tuple<int, int, Vector3>>();
        public Queue<int> KilledEntity = new Queue<int>();
        //for fps: damage, culprit; for fps: damage, hit id
        public Queue<Tuple<int, int>> DamageDealt = new Queue<Tuple<int, int>>();

        //game state
        public int GameState = -1;
    }


    public class NetworkManager : MonoBehaviour
    {
        #region Netcode

        //net code
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

        public static string ip;
        private static IntPtr Client;
        private static int playerNumber = -1;

        static public bool isConnected = false;

        #endregion

        int fixedTimeStep;
        public static DataState dataState;
        bool isConnected = false;

        void Awake()
        {
            fixedTimeStep = (int)(1f / Time.fixedDeltaTime);

            if (GameSceneController.Instance != null && GameSceneController.Instance.IP != "")
            {
                ip = GameSceneController.Instance.IP;
            }
            else {
                ip = "127.0.0.1";
            }
            dataState = new DataState();

        }

        public static void ConnectToServer()
        {
            //client Init  
            Client = CreateClient();            
            Connect(ip, Client);
            StartUpdating(Client);
            SetupPacketReception(PacketRecieved);
        }

        // Update is called once per frame
        void FixedUpdate()
        {
            #region Fixed Tick
            //count down
            --fixedTimeStep;

            //tick is called 10 times per 50 updates
            if (fixedTimeStep % 5 == 0)
            {
                TickUpdate();
            }

            //reset the clock
            if (fixedTimeStep <= 0)
            {
                //updates 50Hz
                fixedTimeStep = (int)(1f / Time.fixedDeltaTime);
            }
            #endregion


            //update players
            if (dataState.p1.updated) {
                dataState.p1.updated = false;

                PlayerFPS player = (PlayerFPS)EntityManager.Instance.AllEntities[1];
                player.SendUpdate(dataState.p1.position, dataState.p1.rotation, dataState.p1.state);
            }
            if (dataState.p2.updated)
            {
                dataState.p2.updated = false;

                PlayerFPS player = (PlayerFPS)EntityManager.Instance.AllEntities[2];
                player.SendUpdate(dataState.p2.position, dataState.p2.rotation, dataState.p2.state);
            }
            if (dataState.p3.updated)
            {
                dataState.p3.updated = false;

                PlayerFPS player = (PlayerFPS)EntityManager.Instance.AllEntities[3];
                player.SendUpdate(dataState.p3.position, dataState.p3.rotation, dataState.p3.state);
            }

            //update damage
            while (dataState.DamageDealt.Count > 0) {

                //rts damage calculation
                if (playerNumber == 0)
                {
                    Tuple<int, int> damage = dataState.DamageDealt.Dequeue();

                    EntityManager.Instance.AllEntities[damage.Item2].OnDamage(damage.Item1);
                }

            }
        }

        void TickUpdate()
        {

        }

        private void Update()
        {
             
        }

        //called on data recieve action, then process
        static void PacketRecieved(int type, int sender, string data)
        {
            data.TrimEnd();

            Debug.Log(data);

            //parse the data
            string[] parsedData = data.Split(',');

            switch ((PacketType)type)
            {
                case PacketType.INIT:
                    if (parsedData.Length == 2)
                    {
                        //init player gametype based on init data reception
                        playerNumber = Convert.ToInt32(parsedData[0]);
                        if (playerNumber != 0)
                        {
                            GameSceneController.Instance.type = PlayerType.FPS;
                        }
                        else {
                            GameSceneController.Instance.type = PlayerType.RTS;
                        }
                        isConnected = true;
                        GameSceneController.Instance.playerNumber = playerNumber;
                    }
                    else
                    {
                        Debug.LogWarning("Error: Invalid INIT Parsed Array Size");

                    }
                    break;
                case PacketType.PLAYERDATA:
                    if (parsedData.Length == 7)
                    {
                        //lock and update by sender
                        lock (dataState)
                        {
                            switch (sender)
                                {
                                case 1:
                                    dataState.p1.position.x = float.Parse(parsedData[0]);
                                    dataState.p1.position.y = float.Parse(parsedData[1]);
                                    dataState.p1.position.z = float.Parse(parsedData[2]);
                                    dataState.p1.position.x = float.Parse(parsedData[3]);
                                    dataState.p1.position.y = float.Parse(parsedData[4]);
                                    dataState.p1.position.z = float.Parse(parsedData[5]);
                                    dataState.p1.state = int.Parse(parsedData[6]);
                                    dataState.p1.updated = true;

                                    break;
                                case 2:
                                    dataState.p2.position.x = float.Parse(parsedData[0]);
                                    dataState.p2.position.y = float.Parse(parsedData[1]);
                                    dataState.p2.position.z = float.Parse(parsedData[2]);
                                    dataState.p2.position.x = float.Parse(parsedData[3]);
                                    dataState.p2.position.y = float.Parse(parsedData[4]);
                                    dataState.p2.position.z = float.Parse(parsedData[5]);
                                    dataState.p2.state = int.Parse(parsedData[6]);
                                    dataState.p2.updated = true;
                                    break;
                                case 3:
                                    dataState.p3.position.x = float.Parse(parsedData[0]);
                                    dataState.p3.position.y = float.Parse(parsedData[1]);
                                    dataState.p3.position.z = float.Parse(parsedData[2]);
                                    dataState.p3.position.x = float.Parse(parsedData[3]);
                                    dataState.p3.position.y = float.Parse(parsedData[4]);
                                    dataState.p3.position.z = float.Parse(parsedData[5]);
                                    dataState.p3.state = int.Parse(parsedData[6]);
                                    dataState.p3.updated = true;
                                    break;
                                default:
                                    Debug.Log("Error: PLAYERDATA Sender Invalid");

                                break;
                                }
                        }
                    }
                    else
                    {
                        Debug.LogWarning("Error: Invalid PLAYERDATA Parsed Array Size");
                    }
                    break;

                case PacketType.WEAPONSTATE:
                    //update state by sender type
                    if (parsedData.Length == 1)
                    {
                        lock (dataState)
                        {
                            switch (sender)
                            {
                                case 1:
                                    dataState.p1.weapon = int.Parse(parsedData[0]);
                                    dataState.p1.updated = true;
                                    break;
                                case 2:
                                    dataState.p2.weapon = int.Parse(parsedData[0]);
                                    dataState.p2.updated = true;
                                    break;
                                case 3:
                                    dataState.p2.weapon = int.Parse(parsedData[0]);
                                    dataState.p3.updated = true;
                                    break;
                                default:
                                    Debug.Log("Error: WEAPONSTATE Sender Invalid");
                                    break;
                            }
                        }
                    }
                    else
                    {
                        Debug.LogWarning(parsedData.Length);
                        Debug.LogWarning("Error: Invalid WEAPONSTATE Parsed Array Size:" + data);
                    }

                    break;
                case PacketType.DAMAGEDEALT:

                    if (parsedData.Length == 3)
                    {
                        Tuple<int, int> temp = Tuple.Create(int.Parse(parsedData[1]), int.Parse(parsedData[2]));

                        lock (dataState)
                        {
                            dataState.DamageDealt.Enqueue(temp);
                        }
                    }
                    else {
                        Debug.LogWarning("Error: Invalid DAMAGEDEALT Parsed Array Size");
                    }
                    break;
                case PacketType.ENTITYDATA:
                    if (parsedData.Length >= 7) {

                        for (int counter = 0; counter < parsedData.Length / 7; counter++)
                        {
                            int offset = counter * 7;
                            if (!dataState.entityUpdates.ContainsKey(int.Parse(parsedData[0 + offset])))
                            {

                                //create entity data
                                EntityData tempEntity = new EntityData();
                                tempEntity.position = new Vector3(float.Parse(parsedData[1+ offset]), float.Parse(parsedData[2+ offset]), float.Parse(parsedData[3+ offset]));
                                tempEntity.rotation = new Vector3(float.Parse(parsedData[4+ offset]), float.Parse(parsedData[5+ offset]), float.Parse(parsedData[6+ offset]));
                                tempEntity.updated = true;

                                //add to map
                                dataState.entityUpdates.Add(int.Parse(parsedData[0+ offset]), tempEntity);
                            }
                            else
                            {
                                //updating all data on existing data
                                dataState.entityUpdates[int.Parse(parsedData[0+ offset])+ offset].position.x = float.Parse(parsedData[1+ offset]);
                                dataState.entityUpdates[int.Parse(parsedData[0+ offset])+ offset].position.y = float.Parse(parsedData[2+ offset]);
                                dataState.entityUpdates[int.Parse(parsedData[0+ offset])+ offset].position.z = float.Parse(parsedData[3+ offset]);
                                dataState.entityUpdates[int.Parse(parsedData[0+ offset])+ offset].rotation.x = float.Parse(parsedData[4+ offset]);
                                dataState.entityUpdates[int.Parse(parsedData[0+ offset])+ offset].rotation.y = float.Parse(parsedData[5+ offset]);
                                dataState.entityUpdates[int.Parse(parsedData[0+ offset])+ offset].rotation.z = float.Parse(parsedData[6+ offset]);

                                dataState.entityUpdates[int.Parse(parsedData[0])].updated = true;
                            }
                        }
                    }
                    else
                    {
                        Debug.LogWarning("Error: Invalid ENTITYDATA Parsed Array Size");
                    }


                    break;
                case PacketType.BUILD:
                    if (parsedData.Length == 5) {
                        Vector3 pos = new Vector3(float.Parse(parsedData[2]), float.Parse(parsedData[3]), float.Parse(parsedData[4]));

                        Tuple<int, int, Vector3> temp = Tuple.Create(int.Parse(parsedData[0]), int.Parse(parsedData[1]), pos);
                        dataState.BuildEntity.Enqueue(temp);
                    }
                    else
                    {
                        Debug.LogWarning("Error: Invalid BUILD Parsed Array Size");
                    }


                    break;
                case PacketType.KILL:

                    if (parsedData.Length == 1) {
                        dataState.KilledEntity.Enqueue(int.Parse(parsedData[0]));
                    }
                    else
                    {
                        Debug.LogWarning("Error: Invalid KILL Parsed Array Size");
                    }
                    break;

                case PacketType.GAMESTATE:

                    if (parsedData.Length == 1)
                    {
                        dataState.GameState = int.Parse(parsedData[0]);
                    }
                    else
                    {
                        Debug.LogWarning("Error: Invalid GAMESTATE Parsed Array Size");
                    }
                    break;

                default:
                    Debug.LogWarning("Error: Invalid Datatype recieved:" + type.ToString());

                    break;
            }
        }

        //call c++ cleanup
        private void OnDestroy()
        {
            //clean up client
            DeleteClient(Client);
        }



        //this sends all droid positions
        public static void SendEntityPositions()
        {
            StringBuilder dataToSend = new StringBuilder();

            foreach (Entity droid in EntityManager.Instance.ActiveEntitiesByType[(int)EntityType.Droid])
            {

                //send object id
                dataToSend.Append(droid.id);
                dataToSend.Append(",");

                //send object positions
                dataToSend.Append(droid.transform.position.x);
                dataToSend.Append(",");
                dataToSend.Append(droid.transform.position.y);
                dataToSend.Append(",");
                dataToSend.Append(droid.transform.position.z);
                dataToSend.Append(",");
                dataToSend.Append(droid.transform.rotation.eulerAngles.x);
                dataToSend.Append(",");
                dataToSend.Append(droid.transform.rotation.eulerAngles.y);
                dataToSend.Append(",");
                dataToSend.Append(droid.transform.rotation.eulerAngles.z);

            }
            foreach (Entity turret in EntityManager.Instance.ActiveEntitiesByType[(int)EntityType.Turret])
            {

                //send object id
                dataToSend.Append(turret.id);
                dataToSend.Append(",");

                //send object positions
                dataToSend.Append(turret.transform.position.x);
                dataToSend.Append(",");
                dataToSend.Append(turret.transform.position.y);
                dataToSend.Append(",");
                dataToSend.Append(turret.transform.position.z);
                dataToSend.Append(",");
                dataToSend.Append(turret.transform.rotation.eulerAngles.x);
                dataToSend.Append(",");
                dataToSend.Append(turret.transform.rotation.eulerAngles.y);
                dataToSend.Append(",");
                dataToSend.Append(turret.transform.rotation.eulerAngles.z);

            }


            SendData((int)PacketType.ENTITYDATA, dataToSend.ToString(), false, Client);

        }

        public static void SendBuildEntity(Entity entity)
        {
            StringBuilder dataToSend = new StringBuilder();
            //add object id
            dataToSend.Append(entity.id);
            dataToSend.Append(",");

            //add object type
            dataToSend.Append(((int)entity.type));
            dataToSend.Append(",");

            //add object position x
            dataToSend.Append(entity.transform.position.x);
            dataToSend.Append(",");

            //add object position y
            dataToSend.Append(entity.transform.position.y);
            dataToSend.Append(",");

            //add object position z
            dataToSend.Append(entity.transform.position.z);
            SendData((int)PacketType.BUILD, dataToSend.ToString(), true, Client);
        }

        public static void SendGameData(int state)
        {
            StringBuilder dataToSend = new StringBuilder();

            dataToSend.Append(state);

            SendData((int)PacketType.GAMESTATE, dataToSend.ToString(), true, Client);
        }

        public static void SendKilledEntity(Entity entity)
        {

            StringBuilder dataToSend = new StringBuilder();

            //add object id
            dataToSend.Append(entity.id);

            SendData((int)PacketType.KILL, dataToSend.ToString(), true, Client);
        }

        //send damaged player
        public static void SendDamagePlayer(int damage, int player, int culprit)
        {
            StringBuilder dataToSend = new StringBuilder();

            dataToSend.Append(player);
            dataToSend.Append(",");
            dataToSend.Append(damage);
            dataToSend.Append(",");
            dataToSend.Append(culprit);

            SendData((int)PacketType.DAMAGEDEALT, dataToSend.ToString(), true, Client);

        }

    }
}