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
        PLAYER_DATA = 2,
        //int
        WEAPON_STATE = 3,
        //int
        DAMAGE_DEALT = 4,

        // fps, dmg, target
        //RTS DATA TYPES

        //vec4[0-100]
        ENTITY_DATA = 5,
        //2int + vec3
        BUILD = 6,
        //int
        KILL = 7,
        //int, float
        GAME_STATE = 8,

        PLAYER_DAMAGE = 9,
        // fps, dir, source, dmg

        TURRET_DATA = 10
    }

    struct packet_init
    {
        int playerID;
    };

    struct packet_msg
    {
        string message;
    };

    struct packet_entity
    {
        float posX;
        float posY;
        float posZ;
        float rotX;
        float rotY;
        float rotZ;
        int state;
    };

    struct packet_weapon
    {
        int weapon;
    };

    struct packet_damage
    {
        int playerID;
        bool dir;
        int entity;
        float damage;
    };

    struct packet_build
    {
        int id;
        int type;
        float posX;
        float posY;
        float posZ;
    };

    struct packet_kill
    {
        int id;
    };

    struct packet_state
    {
        int state;
    };


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

        const string DLL_NAME = "Network_Plugin";
        //net code
        [DllImport(DLL_NAME)]
        static extern IntPtr CreateClient();                                            //Creates a client
        [DllImport(DLL_NAME)]
        static extern void DeleteClient(IntPtr client);                                 //Destroys a client
        [DllImport(DLL_NAME)]
        static extern bool Connect(string str, IntPtr client);                          //Connects to c++ Server
        [DllImport(DLL_NAME)]
        static extern bool SendData(int type, string str, bool useTCP, IntPtr client);  //Sends Message to all other clients    
        [DllImport(DLL_NAME)]
        static extern void StartUpdating(IntPtr client);                                //Starts updating
        [DllImport(DLL_NAME)]
        static extern void SetupPacketReception(Action<int, int, string> action);       //recieve packets from server
        [DllImport(DLL_NAME)]
        static extern int GetPlayerNumber(IntPtr client);
        [DllImport(DLL_NAME)]
        static extern int GetError(IntPtr client);
        [DllImport(DLL_NAME)]
        static extern int GetErrorLoc(IntPtr client);

        public static string ip;
        private static IntPtr Client;
        private static int playerNumber = -1;

        static public bool isConnected = false;

        #endregion

        int fixedTimeStep;
        public static DataState dataState;
        public static FirearmHandler[] firearms = new FirearmHandler[3];

        void Awake()
        {
            //firearms = new FirearmHandler[3];
            fixedTimeStep = (int)(1f / Time.fixedDeltaTime);

            if (GameSceneController.Instance != null && GameSceneController.Instance.IP != "")
            {
                ip = GameSceneController.Instance.IP;
            }
            else
            {
                ip = "127.0.0.1";
            }
            dataState = new DataState();

        }

        public static void ConnectToServer(string ipAddr)
        {
            if (ipAddr != "")
                ip = ipAddr;
            //client Init  
            Client = CreateClient();
            if (!Connect(ip, Client))
            {
                Debug.Log("Error Loc: " + GetErrorLoc(Client).ToString() + " , Error: " + GetError(Client).ToString());
            }
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

            if (isConnected)
            {
                //update players
                if (dataState.p1.updated)
                {
                    dataState.p1.updated = false;

                    PlayerFPS player = (PlayerFPS)EntityManager.Instance.AllEntities[1];
                    if (player.type == EntityType.Dummy)
                    {
                        firearms[0].NetworkingUpdate(dataState.p1.weapon);
                        player.SendUpdate(dataState.p1.position, dataState.p1.rotation, dataState.p1.state, dataState.p1.weapon);
                    }
                }
                if (dataState.p2.updated)
                {
                    dataState.p2.updated = false;

                    PlayerFPS player = (PlayerFPS)EntityManager.Instance.AllEntities[2];
                    if (player.type == EntityType.Dummy)
                    {
                        firearms[1].NetworkingUpdate(dataState.p2.weapon);
                        player.SendUpdate(dataState.p2.position, dataState.p2.rotation, dataState.p2.state, dataState.p2.weapon);
                    }
                }
                if (dataState.p3.updated)
                {
                    dataState.p3.updated = false;

                    PlayerFPS player = (PlayerFPS)EntityManager.Instance.AllEntities[3];
                    if (player.type == EntityType.Dummy)
                    {
                        player.SendUpdate(dataState.p3.position, dataState.p3.rotation, dataState.p3.state, dataState.p3.weapon);
                    }
                }

                foreach (KeyValuePair<int, EntityData> kvp in dataState.entityUpdates)
                {
                    if (kvp.Value.updated)
                    {
                        Debug.Log("UPDATING POSITION FOR " + kvp.Key + "/" + EntityManager.Instance.AllEntities.Count);
                        kvp.Value.updated = false;
                        Entity temp = EntityManager.Instance.AllEntities[kvp.Key];
                        Debug.Log(temp.name);
                        //Debug.Log(kvp.Value.position + ", " + kvp.Value.rotation);
                        temp.UpdateEntityStats(kvp.Value);
                    }
                }

                //if (dataState.entityUpdates.Count > 0)
                //    dataState.entityUpdates.Clear();

                //update damage
                while (dataState.DamageDealt.Count > 0)
                {
                    //Debug.Log("WAITING...");
                    //rts damage calculation
                    if (GameSceneController.Instance.type == PlayerType.RTS)
                    {
                        //Debug.Log("NO!");
                        Tuple<int, int> damage = dataState.DamageDealt.Dequeue();

                        EntityManager.Instance.AllEntities[damage.Item2].OnDamage(damage.Item1);
                    }
                    else
                    {
                        //Debug.Log("YEAH!");
                        Tuple<int, int> damage = dataState.DamageDealt.Dequeue();
                        //Debug.Log("PLAYER NUMBER: " + GetPlayerNumber(Client));
                        //Debug.Log(EntityManager.Instance.AllEntities[GetPlayerNumber(Client)].name);
                        EntityManager.Instance.AllEntities[GetPlayerNumber(Client)].OnDamage(damage.Item1);
                    }
                }

                while (dataState.KilledEntity.Count > 0)
                {
                    if (GameSceneController.Instance.type == PlayerType.FPS)
                        EntityManager.Instance.AllEntities[dataState.KilledEntity.Dequeue()].OnDeActivate();
                }

                while (dataState.BuildEntity.Count > 0)
                {
                    if (GameSceneController.Instance.type == PlayerType.FPS)
                    {
                        Tuple<int, int, Vector3> tempTup = dataState.BuildEntity.Dequeue();

                        Entity temp = EntityManager.Instance.GetNewEntity((EntityType)tempTup.Item2);
                        temp.transform.position = tempTup.Item3;
                        temp.IssueBuild();

                    }
                    else
                    {
                        dataState.BuildEntity.Dequeue();
                    }
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

            //Debug.Log(data);

            //parse the data
            string[] parsedData = data.Split(',');

            switch ((PacketType)type)
            {
                case PacketType.INIT:
                    RecieveInit(sender, parsedData);
                    break;
                case PacketType.PLAYER_DATA:
                    if (sender == playerNumber)
                    {
                        break;
                    }
                    if (parsedData.Length == 7)
                    {
                        //Debug.Log("GOT DA DATA: " + sender);
                        //lock and update by sender
                        lock (dataState)
                        {
                            switch (sender)
                            {
                                case 1:
                                    dataState.p1.position.x = float.Parse(parsedData[0]);
                                    dataState.p1.position.y = float.Parse(parsedData[1]);
                                    dataState.p1.position.z = float.Parse(parsedData[2]);
                                    dataState.p1.rotation.x = float.Parse(parsedData[3]);
                                    dataState.p1.rotation.y = float.Parse(parsedData[4]);
                                    dataState.p1.rotation.z = float.Parse(parsedData[5]);
                                    dataState.p1.state = int.Parse(parsedData[6]);
                                    dataState.p1.updated = true;

                                    break;
                                case 2:
                                    dataState.p2.position.x = float.Parse(parsedData[0]);
                                    dataState.p2.position.y = float.Parse(parsedData[1]);
                                    dataState.p2.position.z = float.Parse(parsedData[2]);
                                    dataState.p2.rotation.x = float.Parse(parsedData[3]);
                                    dataState.p2.rotation.y = float.Parse(parsedData[4]);
                                    dataState.p2.rotation.z = float.Parse(parsedData[5]);
                                    dataState.p2.state = int.Parse(parsedData[6]);
                                    dataState.p2.updated = true;
                                    break;
                                case 3:
                                    dataState.p3.position.x = float.Parse(parsedData[0]);
                                    dataState.p3.position.y = float.Parse(parsedData[1]);
                                    dataState.p3.position.z = float.Parse(parsedData[2]);
                                    dataState.p3.rotation.x = float.Parse(parsedData[3]);
                                    dataState.p3.rotation.y = float.Parse(parsedData[4]);
                                    dataState.p3.rotation.z = float.Parse(parsedData[5]);
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

                case PacketType.WEAPON_STATE:
                    RecieveWeapon(sender, parsedData);
                    break;
                case PacketType.MESSAGE:
                    RecieveMessage(sender, parsedData);
                    break;
                case PacketType.DAMAGE_DEALT:
                    RecieveDamage(sender, parsedData);

                    break;
                case PacketType.ENTITY_DATA:
                    RecieveEntity(sender, parsedData);

                    break;
                case PacketType.BUILD:
                    RecieveBuild(sender, parsedData);

                    break;
                case PacketType.KILL:
                    RecieveKill(sender, parsedData);
                    break;

                case PacketType.GAME_STATE:
                    RecieveGamestate(sender, parsedData);
                    break;

                case PacketType.PLAYER_DAMAGE:

                    if (parsedData.Length == 3)
                    {
                        Tuple<int, int> temp = Tuple.Create(int.Parse(parsedData[1]), int.Parse(parsedData[2]));

                        lock (dataState)
                        {
                            dataState.DamageDealt.Enqueue(temp);
                        }

                        Debug.Log(parsedData[1] + ", " + parsedData[2]);
                    }
                    else
                    {
                        Debug.LogWarning("Error: Invalid DAMAGEDEALT Parsed Array Size");
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


        #region Reception
        //recieve data types
        public static void RecieveInit(int sender, string[] parsedData)
        {
            if (parsedData.Length == 2)
            {
                //init player gametype based on init data reception
                playerNumber = Convert.ToInt32(parsedData[0]);
                if (playerNumber != 0)
                {
                    GameSceneController.Instance.type = PlayerType.FPS;
                }
                else
                {
                    GameSceneController.Instance.type = PlayerType.RTS;
                }
                isConnected = true;
                GameSceneController.Instance.playerNumber = playerNumber;
            }
            else
            {
                Debug.LogWarning("Error: Invalid INIT Parsed Array Size");

            }

        }

        public static void RecieveMessage(int sender, string[] parsedData)
        {
            Debug.Log("Player " + sender.ToString() + ": " + parsedData[0]);
        }

        public static void RecieveEntity(int sender, string[] parsedData)
        {
            if (parsedData.Length >= 7)
            {

                if (GameSceneController.Instance.type == PlayerType.FPS)
                {
                    lock (dataState)
                    {
                        for (int counter = 0; counter < parsedData.Length / 7; counter++)
                        {
                            int offset = counter * 7;
                            if (!dataState.entityUpdates.ContainsKey(int.Parse(parsedData[0 + offset])))
                            {
                                Debug.Log("ONE: " + parsedData[0 + offset]);

                                //create entity data
                                EntityData tempEntity = new EntityData();
                                tempEntity.position = new Vector3(float.Parse(parsedData[1 + offset]), float.Parse(parsedData[2 + offset]), float.Parse(parsedData[3 + offset]));
                                tempEntity.rotation = new Vector3(float.Parse(parsedData[4 + offset]), float.Parse(parsedData[5 + offset]), float.Parse(parsedData[6 + offset]));
                                tempEntity.updated = true;

                                //add to map
                                dataState.entityUpdates.Add(int.Parse(parsedData[0 + offset]), tempEntity);
                            }
                            else
                            {
                                EntityData ed;
                                Debug.Log("SEVERAL: " + parsedData[0 + offset]);

                                if (!dataState.entityUpdates.TryGetValue(int.Parse(parsedData[0 + offset]), out ed))
                                    Debug.Break();

                                //updating all data on existing data
                                dataState.entityUpdates[int.Parse(parsedData[0 + offset])].position.x = float.Parse(parsedData[1 + offset]);
                                dataState.entityUpdates[int.Parse(parsedData[0 + offset])].position.y = float.Parse(parsedData[2 + offset]);
                                dataState.entityUpdates[int.Parse(parsedData[0 + offset])].position.z = float.Parse(parsedData[3 + offset]);
                                dataState.entityUpdates[int.Parse(parsedData[0 + offset])].rotation.x = float.Parse(parsedData[4 + offset]);
                                dataState.entityUpdates[int.Parse(parsedData[0 + offset])].rotation.y = float.Parse(parsedData[5 + offset]);
                                dataState.entityUpdates[int.Parse(parsedData[0 + offset])].rotation.z = float.Parse(parsedData[6 + offset]);

                                dataState.entityUpdates[int.Parse(parsedData[0 + offset])].updated = true;
                            }
                        }
                    }
                }
            }
            else
            {
                Debug.LogWarning("Error: Invalid ENTITYDATA Parsed Array Size");
            }

        }

        public static void RecieveWeapon(int sender, string[] parsedData)
        {
            //update state by sender type
            if (sender == playerNumber)
            {
                return;
            }
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
                Debug.LogWarning("Error: Invalid WEAPONSTATE Parsed Array Size:" + parsedData.Length);
            }


        }

        public static void RecieveDamage(int sender, string[] parsedData)
        {
            if (parsedData.Length == 3)
            {
                Tuple<int, int> temp = Tuple.Create(int.Parse(parsedData[1]), int.Parse(parsedData[2]));

                lock (dataState)
                {
                    dataState.DamageDealt.Enqueue(temp);
                }

                Debug.Log(parsedData[1] + ", " + parsedData[2]);
            }
            else
            {
                Debug.LogWarning("Error: Invalid DAMAGEDEALT Parsed Array Size");
            }
        }

        public static void RecieveBuild(int sender, string[] parsedData)
        {
            if (parsedData.Length == 5)
            {
                //Debug.Log("HIYA");
                Vector3 pos = new Vector3(float.Parse(parsedData[2]), float.Parse(parsedData[3]), float.Parse(parsedData[4]));

                Tuple<int, int, Vector3> temp = Tuple.Create(int.Parse(parsedData[0]), int.Parse(parsedData[1]), pos);

                lock (dataState)
                {
                    dataState.BuildEntity.Enqueue(temp);
                }

                Debug.Log("BUILT: " + parsedData[0]);
            }
            else
            {
                Debug.LogWarning("Error: Invalid BUILD Parsed Array Size");
            }
        }

        public static void RecieveGamestate(int sender, string[] parsedData)
        {
            if (parsedData.Length == 1)
            {
                lock (dataState)
                {
                    dataState.GameState = int.Parse(parsedData[0]);
                }
            }
            else
            {
                Debug.LogWarning("Error: Invalid GAMESTATE Parsed Array Size");
            }
        }

        public static void RecieveKill(int sender, string[] parsedData)
        {
            if (parsedData.Length == 1)
            {
                lock (dataState)
                {
                    dataState.KilledEntity.Enqueue(int.Parse(parsedData[0]));
                }
            }
            else
            {
                Debug.LogWarning("Error: Invalid KILL Parsed Array Size");
            }

        }
        #endregion


        public static void SendPlayerInfo(PlayerFPS playerFPS)
        {
            if (isConnected)
            {
                StringBuilder dataToSend = new StringBuilder();

                dataToSend.Append(playerFPS.transform.position.x);
                dataToSend.Append(",");
                dataToSend.Append(playerFPS.transform.position.y);
                dataToSend.Append(",");
                dataToSend.Append(playerFPS.transform.position.z);
                dataToSend.Append(",");
                Vector3 sumAng = Vector3.zero;
                for (int i = 0; i < playerFPS.pivots.Length; ++i)
                    sumAng += playerFPS.pivots[i].transform.localRotation.eulerAngles;
                dataToSend.Append(sumAng.x);
                dataToSend.Append(",");
                dataToSend.Append(sumAng.y);
                dataToSend.Append(",");
                dataToSend.Append(sumAng.z);
                dataToSend.Append(",");
                dataToSend.Append(playerFPS.stats.state);
                //dataToSend.Append(",");

                if (!SendData((int)PacketType.PLAYER_DATA, dataToSend.ToString(), false, Client))
                {
                    Debug.Log("Error Loc: " + GetErrorLoc(Client).ToString() + " , Error: " + GetError(Client).ToString());
                }
            }
        }

        public static void SendWeaponSwap(int weapon)
        {
            if (isConnected)
            {

                StringBuilder dataToSend = new StringBuilder();

                dataToSend.Append(weapon);

                if (!SendData((int)PacketType.WEAPON_STATE, dataToSend.ToString(), true, Client))
                {
                    Debug.Log("Error Loc: " + GetErrorLoc(Client).ToString() + " , Error: " + GetError(Client).ToString());
                }
            }
        }

        //this sends all droid positions
        public static void SendEntityPositions()
        {
            if (isConnected)
            {
                StringBuilder dataToSend = new StringBuilder();

                foreach (Entity droid in EntityManager.Instance.ActiveEntitiesByType[(int)EntityType.Droid])
                {
                    droid.GetEntityString(ref dataToSend);
                }
                foreach (Entity turret in EntityManager.Instance.ActiveEntitiesByType[(int)EntityType.Turret])
                {
                    turret.GetEntityString(ref dataToSend);
                }
                if (dataToSend.Length > 0)
                {
                    dataToSend.Remove(dataToSend.Length - 1, 1);
                }

                //Debug.Log(dataToSend);

                if (!SendData((int)PacketType.ENTITY_DATA, dataToSend.ToString(), false, Client))
                {
                    Debug.Log("Error Loc: " + GetErrorLoc(Client).ToString() + " , Error: " + GetError(Client).ToString());
                }
            }
        }

        public static void SendBuildEntity(Entity entity)
        {
            if (isConnected)
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
                if (!SendData((int)PacketType.BUILD, dataToSend.ToString(), true, Client))
                {
                    Debug.Log("Error Loc: " + GetErrorLoc(Client).ToString() + " , Error: " + GetError(Client).ToString());
                }

                Debug.Log("BUILT");
            }
        }

        public static void SendGameData(int state)
        {
            if (isConnected)
            {
                StringBuilder dataToSend = new StringBuilder();

                if (!SendData((int)PacketType.GAME_STATE, dataToSend.ToString(), true, Client))
                {
                    Debug.Log("Error Loc: " + GetErrorLoc(Client).ToString() + " , Error: " + GetError(Client).ToString());
                }
            }
        }

        public static void SendKilledEntity(Entity entity)
        {
            if (isConnected)
            {
                StringBuilder dataToSend = new StringBuilder();

                //add object id
                dataToSend.Append(entity.id);

                if (!SendData((int)PacketType.KILL, dataToSend.ToString(), true, Client))
                {
                    Debug.Log("Error Loc: " + GetErrorLoc(Client).ToString() + " , Error: " + GetError(Client).ToString());
                }
            }
        }

        //send damaged player
        public static void SendDamage(int damage, int damager, int victim)
        {
            if (isConnected)
            {
                StringBuilder dataToSend = new StringBuilder();

                dataToSend.Append(damager);
                dataToSend.Append(",");
                dataToSend.Append(damage);
                dataToSend.Append(",");
                dataToSend.Append(victim);

                if (!SendData((int)PacketType.DAMAGE_DEALT, dataToSend.ToString(), true, Client))
                {
                    Debug.Log("Error Loc: " + GetErrorLoc(Client).ToString() + " , Error: " + GetError(Client).ToString());
                }
            }
        }

        public static void SendEnvironmentalDamage(int damage, int victim, int damager)
        {
            if (isConnected)
            {
                StringBuilder dataToSend = new StringBuilder();

                dataToSend.Append(victim);
                dataToSend.Append(",");
                dataToSend.Append(damage);
                dataToSend.Append(",");
                dataToSend.Append(damager);

                if (!SendData((int)PacketType.PLAYER_DAMAGE, dataToSend.ToString(), true, Client))
                {
                    Debug.Log("Error Loc: " + GetErrorLoc(Client).ToString() + " , Error: " + GetError(Client).ToString());
                }
            }
        }
    }
}