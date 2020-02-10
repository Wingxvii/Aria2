using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using System;
using System.Text;

namespace Netcode
{
    enum PacketType
    {
        // initialization connection
        INIT = 0,
        // Join the Game
        JOIN,
        // single string
        MESSAGE,
        // game state
        STATE,

        // Entity Data
        ENTITY,
        // Damage dealt (int ID, bool Dir, int source, float damage)
        DAMAGE,

        // FPS weapon switch
        WEAPON,

        //RTS Managed Data
        // entity built
        BUILD,
        // entity killed
        KILL
    };

    struct packet_init
    {
        public int index;
    };

    struct packet_join
    {
        public PlayerType type;		// 0 = rts, 0 = fps
        public int playerID;
    };

    struct packet_msg
    {
        public string message;
    };

    struct entity
    {
        public float posX;
        public float posY;
        public float posZ;
        public float rotX;
        public float rotY;
        public float rotZ;
        public int state;
    };


    struct packet_entity
    {
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 500)]
        public entity[] entities;
    }

    struct packet_weapon
    {
        public int weapon;
    };

    struct packet_damage
    {
        public int playerID;
        public bool dir;
        public int entity;
        public float damage;
    };

    struct packet_build
    {
        public int id;
        public int type;
        public float posX;
        public float posY;
        public float posZ;
    };

    struct packet_kill
    {
        public int id;
    };

    struct packet_state
    {
        public int state;
    };

    //container for user data storage
    public class UsersData {
        public string username = "Nameless";
        public bool readyStatus = false;
        public PlayerType type = PlayerType.Spectator;
    }


    public class EntityData
    {
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
        static extern bool SendDataInit(packet_init pkt, bool useTCP, IntPtr client);  //Sends Message to all other clients    
        [DllImport(DLL_NAME)]
        static extern bool SendDataJoin(packet_join pkt, bool useTCP, IntPtr client);
        [DllImport(DLL_NAME)]
        static extern bool SendDataMsg(packet_msg pkt, bool useTCP, IntPtr client);
        [DllImport(DLL_NAME)]
        static extern bool SendDataState(packet_state pkt, bool useTCP, IntPtr client);
        [DllImport(DLL_NAME)]
        static extern bool SendDataEntity(packet_entity pkt, bool useTCP, IntPtr client);
        [DllImport(DLL_NAME)]
        static extern bool SendDataDamage(packet_damage pkt, bool useTCP, IntPtr client);
        [DllImport(DLL_NAME)]
        static extern bool SendDataWeapon(packet_weapon pkt, bool useTCP, IntPtr client);
        [DllImport(DLL_NAME)]
        static extern bool SendDataBuild(packet_build pkt, bool useTCP, IntPtr client);
        [DllImport(DLL_NAME)]
        static extern bool SendDataKill(packet_kill pkt, bool useTCP, IntPtr client);
        [DllImport(DLL_NAME)]
        static extern void StartUpdating(IntPtr client);                                //Starts updating
        [DllImport(DLL_NAME)]
        static extern void SetupPacketReception(Action<int, string> action);       //recieve packets from server
        [DllImport(DLL_NAME)]
        static extern void SetupPacketReceptionInit(Action<int, packet_init> action);
        [DllImport(DLL_NAME)]
        static extern void SetupPacketReceptionJoin(Action<int, packet_join> action);
        [DllImport(DLL_NAME)]
        static extern void SetupPacketReceptionMsg(Action<int, packet_msg> action);
        [DllImport(DLL_NAME)]
        static extern void SetupPacketReceptionState(Action<int, packet_state> action);
        [DllImport(DLL_NAME)]
        static extern void SetupPacketReceptionEntity(Action<int, packet_entity> action);
        [DllImport(DLL_NAME)]
        static extern void SetupPacketReceptionDamage(Action<int, packet_damage> action);
        [DllImport(DLL_NAME)]
        static extern void SetupPacketReceptionWeapon(Action<int, packet_weapon> action);
        [DllImport(DLL_NAME)]
        static extern void SetupPacketReceptionBuild(Action<int, packet_build> action);
        [DllImport(DLL_NAME)]
        static extern void SetupPacketReceptionKill(Action<int, packet_kill> action);
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
        public static List<UsersData> allUsers;  
        void Awake()
        {
            //firearms = new FirearmHandler[3];
            fixedTimeStep = (int)(1f / Time.fixedDeltaTime);

            dataState = new DataState();
            allUsers = new List<UsersData>();
        }

        /*
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
            SetupPacketReceptionInit(PacketReceivedInit);
            SetupPacketReceptionJoin(PacketReceivedJoin);
            SetupPacketReceptionMsg(PacketReceivedMsg);
            SetupPacketReceptionState(PacketReceivedState);
            SetupPacketReceptionEntity(PacketReceivedEntity);
            SetupPacketReceptionDamage(PacketReceivedDamage);
            SetupPacketReceptionWeapon(PacketReceivedWeapon);
            SetupPacketReceptionBuild(PacketReceivedBuild);
            SetupPacketReceptionKill(PacketReceivedKill);
        }
        */
        /*
        // NEW JOIN GAME FUNCTION! @JOHN
        public static void JoinGame(int id)
        {
            if (isConnected)
            {
                packet_join join = new packet_join();
                join.playerID = id;
                if (GameSceneController.Instance.type == PlayerType.RTS)
                {
                    join.type = PlayerType.RTS;
                }
                else if (GameSceneController.Instance.type == PlayerType.FPS)
                {
                    join.type = PlayerType.FPS;
                }


                if (!SendDataJoin(join, true, Client))
                {
                    Debug.Log("Error Loc: " + GetErrorLoc(Client).ToString() + " , Error: " + GetError(Client).ToString());
                }
            }
        }

            */

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

                while (dataState.KilledEntity.Count > 0)
                {
                    if (GameSceneController.Instance.type == PlayerType.FPS)
                    {
                        EntityManager.Instance.AllEntities[dataState.KilledEntity.Dequeue()].OnDeActivate();
                    }
                    else
                    {
                        if (EntityManager.Instance.AllEntities[dataState.KilledEntity.Dequeue()].type == EntityType.Dummy)
                        {
                            ResourceManager.Instance.KilledPlayer();
                        }
                    }
                }

                while (dataState.BuildEntity.Count > 0)
                {
                    if (GameSceneController.Instance.type == PlayerType.FPS)
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
        }
            void TickUpdate()
            {

            }
            //called on data recieve action, then process
            //static void PacketRecieved(int type, int sender, string data)
            //{
            //    data.TrimEnd();

            //    //Debug.Log(data);

            //    //parse the data
            //    string[] parsedData = data.Split(',');

            //    switch ((PacketType)type)
            //    {
            //        case PacketType.INIT:
            //            RecieveInit(sender, parsedData);
            //            break;
            //        case PacketType.PLAYER_DATA:
            //            if (sender == playerNumber)
            //            {
            //                break;
            //            }
            //            ReceivePlayer(sender);
            //            break;

            //        case PacketType.WEAPON_STATE:
            //            RecieveWeapon(sender, parsedData);
            //            break;
            //        case PacketType.MESSAGE:
            //            RecieveMessage(sender, parsedData);
            //            break;
            //        case PacketType.DAMAGE_DEALT:
            //            RecieveDamage(sender, parsedData);

            //            break;
            //        case PacketType.ENTITY_DATA:
            //            RecieveEntity(sender, parsedData);

            //            break;
            //        case PacketType.BUILD:
            //            RecieveBuild(sender, parsedData);

            //            break;
            //        case PacketType.KILL:
            //            RecieveKill(sender, parsedData);
            //            break;

            //        case PacketType.GAME_STATE:
            //            RecieveGamestate(sender, parsedData);
            //            break;

            //        case PacketType.PLAYER_DAMAGE:

            //            if (parsedData.Length == 3)
            //            {
            //                Tuple<int, int> temp = Tuple.Create(int.Parse(parsedData[1]), int.Parse(parsedData[2]));

            //                lock (dataState)
            //                {
            //                    dataState.DamageDealt.Enqueue(temp);
            //                }

            //                Debug.Log(parsedData[1] + ", " + parsedData[2]);
            //            }
            //            else
            //            {
            //                Debug.LogWarning("Error: Invalid DAMAGEDEALT Parsed Array Size");
            //            }
            //            break;

            //        default:
            //            Debug.LogWarning("Error: Invalid Datatype recieved:" + type.ToString());

            //            break;
            //    }
            //}

            static void PacketReceivedInit(int sender, packet_init packet)
            {
                int index = packet.index;

                isConnected = true;
                GameSceneController.Instance.playerNumber = index;
            }


            
        /*
            // Received Joined Game from Server @JOHN
            static void PacketReceivedJoin(int sender, packet_join packet)
            {
                //PlayerType type = packet.type;
                int ID = packet.playerID;

                GameSceneController.Instance.playerNumber = ID;
                if (ID == 0 && packet.type == PlayerType.RTS)
                {
                    GameSceneController.Instance.type = packet.type;

                }
                else if (ID != 0 && packet.type == PlayerType.FPS)
                {
                    GameSceneController.Instance.type = packet.type;
                }
                else
                {
                    Debug.Log("Join Error: " + packet.type + " , " + packet.playerID);
                }
            }
            */


            static void PacketReceivedMsg(int sender, packet_msg packet)
            {
                string msg = packet.message;
                Debug.Log("Player " + sender.ToString() + ": " + msg);
            }

            static void PacketReceivedState(int sender, packet_state packet)
            {
                lock (dataState)
                {
                    dataState.GameState = packet.state;
                }
            }

            // NEEDS UPDATE @PROGRAMMERS
            static void PacketReceivedEntity(int sender, packet_entity packet)
            {
                entity e = packet.entities[0];
                float posX = e.posX;
                float posY = e.posY;
                float posZ = e.posZ;
                float rotX = e.rotX;
                float rotY = e.rotY;
                float rotZ = e.rotZ;
                int state = e.state;
            }

            // NEEDS UPDATE @PROGRAMMERS
            static void PacketReceivedDamage(int sender, packet_damage packet)
            {
                int ID = packet.playerID;
                bool direction = packet.dir; // 0 = Damage to RTS Entity , 1 = Damage to FPS playerID
                int entity = packet.entity;
                float damage = packet.damage;
            }

            // Example FROM:VICTOR
            static void PacketReceivedWeapon(int sender, packet_weapon packet)
            {
                int state = packet.weapon;

                //update state by sender type
                if (sender == playerNumber)
                {
                    return;
                }
                lock (dataState)
                {
                    switch (sender)
                    {
                        case 1:
                            dataState.p1.weapon = state;
                            dataState.p1.updated = true;
                            break;
                        case 2:
                            dataState.p2.weapon = state;
                            dataState.p2.updated = true;
                            break;
                        case 3:
                            dataState.p2.weapon = state;
                            dataState.p3.updated = true;
                            break;
                        default:
                            Debug.Log("Error: WEAPONSTATE Sender Invalid");
                            break;
                    }
                }
            }

            static void PacketReceivedBuild(int sender, packet_build packet)
            {
                int ID = packet.id;
                int type = packet.type;
                float posX = packet.posX;
                float posY = packet.posY;
                float posZ = packet.posZ;

                lock (dataState)
                {
                    Tuple<int, int, Vector3> temp = Tuple.Create(ID, type, new Vector3(posX, posY, posZ));
                    dataState.BuildEntity.Enqueue(temp);
                }
            }

            static void PacketReceivedKill(int sender, packet_kill packet)
            {
                lock (dataState)
                {
                    dataState.KilledEntity.Enqueue(packet.id);
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
            /*public static void RecieveInit(int sender, string[] parsedData)
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
            */

            //public static void RecieveMessage(int sender, string[] parsedData)
            //{
            //    Debug.Log("Player " + sender.ToString() + ": " + parsedData[0]);
            //}

            public static void ReceivePlayer(int sender, string[] parsedData)
            {

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

            //public static void RecieveWeapon(int sender, string[] parsedData)
            //{
            //    //update state by sender type
            //    if (sender == playerNumber)
            //    {
            //        return;
            //    }
            //    if (parsedData.Length == 1)
            //    {
            //        lock (dataState)
            //        {
            //            switch (sender)
            //            {
            //                case 1:
            //                    dataState.p1.weapon = int.Parse(parsedData[0]);
            //                    dataState.p1.updated = true;
            //                    break;
            //                case 2:
            //                    dataState.p2.weapon = int.Parse(parsedData[0]);
            //                    dataState.p2.updated = true;
            //                    break;
            //                case 3:
            //                    dataState.p2.weapon = int.Parse(parsedData[0]);
            //                    dataState.p3.updated = true;
            //                    break;
            //                default:
            //                    Debug.Log("Error: WEAPONSTATE Sender Invalid");
            //                    break;
            //            }
            //        }
            //    }
            //    else
            //    {
            //        Debug.LogWarning("Error: Invalid WEAPONSTATE Parsed Array Size:" + parsedData.Length);
            //    }


            //}

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

            /*public static void RecieveBuild(int sender, string[] parsedData)
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
            }*/

            /*
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
            */

            /*
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

            }*/
            #endregion

            // Old
            public static void SendPlayerInfo(PlayerFPS playerFPS)
            {
                //    if (isConnected)
                //    {
                //        StringBuilder dataToSend = new StringBuilder();

                //        dataToSend.Append(playerFPS.transform.position.x);
                //        dataToSend.Append(",");
                //        dataToSend.Append(playerFPS.transform.position.y);
                //        dataToSend.Append(",");
                //        dataToSend.Append(playerFPS.transform.position.z);
                //        dataToSend.Append(",");
                //        Vector3 sumAng = Vector3.zero;
                //        for (int i = 0; i < playerFPS.pivots.Length; ++i)
                //            sumAng += playerFPS.pivots[i].transform.localRotation.eulerAngles;
                //        dataToSend.Append(sumAng.x);
                //        dataToSend.Append(",");
                //        dataToSend.Append(sumAng.y);
                //        dataToSend.Append(",");
                //        dataToSend.Append(sumAng.z);
                //        dataToSend.Append(",");
                //        dataToSend.Append(playerFPS.stats.state);
                //        //dataToSend.Append(",");

                //        if (!SendData((int)PacketType.PLAYER_DATA, dataToSend.ToString(), false, Client))
                //        {
                //            Debug.Log("Error Loc: " + GetErrorLoc(Client).ToString() + " , Error: " + GetError(Client).ToString());
                //        }
                //    }
            }

            public static void SendWeaponSwap(int w)
            {
                if (isConnected)
                {
                    packet_weapon weapon = new packet_weapon
                    {
                        weapon = w
                    };

                    if (!SendDataWeapon(weapon, true, Client))
                    {
                        Debug.Log("Error Loc: " + GetErrorLoc(Client).ToString() + " , Error: " + GetError(Client).ToString());
                    }
                }
            }

            //this sends all entity data
            public static void SendEntities()
            {
                if (isConnected)
                {
                    packet_entity entitiesP = new packet_entity();
                    // Example
                    entitiesP.entities[0] = new entity
                    {
                        posX = 0.0f,
                        posY = 0.0f,
                        posZ = 0.0f,
                        rotX = 0.0f,
                        rotY = 0.0f,
                        rotZ = 0.0f,
                        state = 0
                    };

                    // NEED UPDATE @PROGRAMMERS
                    foreach (Entity entity in EntityManager.Instance.AllEntities)
                    {
                        entitiesP.entities[entity.id] = new entity
                        {
                            // Need link
                            posX = 0.0f,
                            posY = 0.0f,
                            posZ = 0.0f,
                            rotX = 0.0f,
                            rotY = 0.0f,
                            rotZ = 0.0f,
                            state = 0
                        };
                    }

                    //StringBuilder dataToSend = new StringBuilder();

                    //foreach (Entity droid in EntityManager.Instance.ActiveEntitiesByType[(int)EntityType.Droid])
                    //{
                    //    droid.GetEntityString(ref dataToSend);
                    //}
                    //foreach (Entity turret in EntityManager.Instance.ActiveEntitiesByType[(int)EntityType.Turret])
                    //{
                    //    turret.GetEntityString(ref dataToSend);
                    //}
                    //if (dataToSend.Length > 0)
                    //{
                    //    dataToSend.Remove(dataToSend.Length - 1, 1);
                    //}

                    //Debug.Log(dataToSend);

                    if (!SendDataEntity(entitiesP, false, Client))
                    {
                        Debug.Log("Error Loc: " + GetErrorLoc(Client).ToString() + " , Error: " + GetError(Client).ToString());
                    }
                }
            }

            public static void SendBuildEntity(Entity entity)
            {
                if (isConnected)
                {
                    packet_build build = new packet_build
                    {
                        id = entity.id,
                        type = (int)entity.type,
                        posX = entity.transform.position.x,
                        posY = entity.transform.position.y,
                        posZ = entity.transform.position.z
                    };
                    if (!SendDataBuild(build, true, Client))
                    {
                        Debug.Log("Error Loc: " + GetErrorLoc(Client).ToString() + " , Error: " + GetError(Client).ToString());
                    }

                    Debug.Log("BUILT");
                }
            }

            public static void SendGameData(int s)
            {
                if (isConnected)
                {
                    packet_state state = new packet_state
                    {
                        state = s
                    };

                    if (!SendDataState(state, true, Client))
                    {
                        Debug.Log("Error Loc: " + GetErrorLoc(Client).ToString() + " , Error: " + GetError(Client).ToString());
                    }
                }
            }

            public static void SendKilledEntity(Entity entity)
            {
                if (isConnected)
                {
                    packet_kill kill = new packet_kill
                    {
                        id = entity.id
                    };

                    if (!SendDataKill(kill, true, Client))
                    {
                        Debug.Log("Error Loc: " + GetErrorLoc(Client).ToString() + " , Error: " + GetError(Client).ToString());
                    }
                }
            }

            // Send Damage
            public static void SendDamage(int fpsPlayer, bool direction, int entity, float dmg)
            {
                if (isConnected)
                {
                    packet_damage damage = new packet_damage
                    {
                        playerID = fpsPlayer,
                        dir = direction,
                        entity = entity,
                        damage = dmg
                    };

                    if (!SendDataDamage(damage, true, Client))
                    {
                        Debug.Log("Error Loc: " + GetErrorLoc(Client).ToString() + " , Error: " + GetError(Client).ToString());
                    }
                }
            }

            // Old NEED UPDATE TO USE THE FUNCTION ABOVE ^ @PROGRAMMERS
            public static void SendEnvironmentalDamage(int damage, int victim, int damager)
            {
                //    if (isConnected)
                //    {
                //        StringBuilder dataToSend = new StringBuilder();

                //        dataToSend.Append(victim);
                //        dataToSend.Append(",");
                //        dataToSend.Append(damage);
                //        dataToSend.Append(",");
                //        dataToSend.Append(damager);

                //        if (!SendData((int)PacketType.PLAYER_DAMAGE, dataToSend.ToString(), true, Client))
                //        {
                //            Debug.Log("Error Loc: " + GetErrorLoc(Client).ToString() + " , Error: " + GetError(Client).ToString());
                //        }
                //    }
            }



        #region LobbyFunctions

        /*
         * ConnectToServer
         * @desc 
         *  Connects to the TCP server. 
         * @param
         *  string: ip of the server
         * 
         */
        public static void ConnectToServer(string ip) {}

        /*
        * ConnectToServer
        * @desc 
        *  Connects to the TCP server. 
        * @param
        *  string: ip of the server
        *  string: username of the player connecting
        * 
        */
        public static void ConnectToServer(string ip, string username){}


        /*
         * OnConencted
         * @desc
         *  Action returning if connection was successful
         * @param
         *  bool: success or failure
         * 
         */
        public static void OnConnected(bool success)
        {
            StartManager.Instance.OnConnected(success);
        }

        /*
         * SelectRole
         * @desc
         *  Sends role request to server
         * @param
         *  PlayerType: Selected role
         */
        public static void SelectRole(PlayerType type) { }

        /*
         * OnRoleSelected
         * @desc
         *  Action that recieves if role is avaliable
         * @param
         *  bool: if role request was successful
         */
        public static void OnRoleSelected(bool success) {
            StartManager.Instance.OnRoleSelected(success);
        }

        /*
         * RoleUpdate
         * @desc
         *  Action that updates the connected player data in the lobby (ONLY CALLED BEFORE GAME STARTS)
         * @param
         *  int: slot index of the player(1-4)
         *  bool: wether player is ready
         *  int: type of player's gamemode
         *  string: username of player
         */
        public static void RoleUpdate(int slotNum, bool readyStatus, int type, string userName) {
            if (!GameSceneController.Instance.gameStart)
            {
                while (slotNum > allUsers.Count)
                {
                    allUsers.Add(new UsersData());
                }

                //update ready status
                if (allUsers[slotNum - 1].readyStatus != readyStatus) {
                    allUsers[slotNum - 1].readyStatus = readyStatus;

                    if (readyStatus)
                    {
                        RecieveMessage("User " + userName + " is Ready.");
                    }
                    else {
                        RecieveMessage("User " + userName + " Unreadied.");
                    }
                }

                //update type status
                if (allUsers[slotNum - 1].type != (PlayerType)type)
                {
                    allUsers[slotNum - 1].type = (PlayerType)type;

                    if ((PlayerType)type == PlayerType.FPS)
                    {
                        RecieveMessage("User " + userName + " is now FPS.");
                    }
                    else if ((PlayerType)type == PlayerType.RTS)
                    {
                        RecieveMessage("User " + userName + " is now RTS.");
                    }
                    else {
                        RecieveMessage("User " + userName + " is now SPECTATOR.");
                    }
                }


                allUsers[slotNum - 1].username = userName;
            }
            else {
                Debug.LogWarning("Warning: Attempted role update while in game.");
            }
        }
        public static void RoleUpdate(int slotNum, bool readyStatus, int type)
        {
            RoleUpdate(slotNum, readyStatus, type, "Nameless");
        }

        /*
         * OnReady
         * @desc
         *  Call the server to update the readyness of the player. Once all connected players are ready, the game starts, up to a maximum of 4. If game countdown is 
         * @param
         *  bool: the ready status of the player
         * 
         */
        public static void OnReady(bool ready) { }


        /*
         * StartCountdown
         * @desc
         *  Action that starts the countdown state. When all users are loaded, countdown begins for player 
         * 
         */
        public static void StartCountdown() { 
            
        }
        /*
         * StopCountdown
         * @desc
         *  Action that stops, and resets the countdown state.
         * 
         */
        public static void StopCountdown() { 
            
        }

        /*
         * OnLoaded
         * @desc
         *  Tells the server that this player has finished loading the game scene, allowing the next player to load.
         * 
         */
        public static void OnLoaded() { }


        /*
         * GameReady
         * @desc
         *  Action that is called when all users have finished loading
         * 
         */ 
        public static void GameReady() {
            GameSceneController.Instance.gameStart = true;
        }

        /*
        * SendMessage
        * @desc
        *   Sends a message to every connected client
        * @param
        *   string: string of message to send
        *   
        */
        public static void SendMessage(string message)
        {
     

        }

        /*
         * RecieveMessage
         * @desc
         *  Action that relays recieved message to startManager
         * @param
         *  string: message contents
         * 
         */
        public static void RecieveMessage(string message) {
            StartManager.Instance.recieveMessage(message);
        }


        #endregion
    }
}