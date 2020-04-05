﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using System;
using System.Text;

namespace Networking
{
    enum PacketType
    {
        // initialization connection
        INIT = 0,
        USER,
        // Ready
        TYPE,
        READY,
        // LOADED, // Game State = game loaded
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
        DEATH,

        TERMINAL,

        FIRING,

        TURRETFIRE,

        PINGSELF = 10000,

        RETURNPING = 10001,
    };

    enum PlayerMask
    {
        SERVER = 1 << 0,
        CLIENT1 = 1 << 1,
        CLIENT2 = 1 << 2,
        CLIENT3 = 1 << 3,
        CLIENT4 = 1 << 4,
    }

    public enum UpdateDataMask
    {
        ID = 1 << 0,
        STATE = 1 << 1,
        POSX = 1 << 2,
        POSY = 1 << 3,
        POSZ = 1 << 4,
        ROTX = 1 << 5,
        ROTY = 1 << 6,
        ROTZ = 1 << 7,
    }

    public enum GameState
    {
        LOBBY = 0,
        TIMER,
        LOAD,
        GAME,
        ENDGAME
    }

    //struct packet_init
    //{
    //    public int index;
    //};

    //struct packet_join
    //{
    //    public PlayerType type;		// 0 = rts, 0 = fps
    //    public int playerID;
    //};

    //struct packet_msg
    //{
    //    public string message;
    //};

    //struct entity
    //{
    //    public float posX;
    //    public float posY;
    //    public float posZ;
    //    public float rotX;
    //    public float rotY;
    //    public float rotZ;
    //    public int state;
    //};


    //struct packet_entity
    //{
    //    [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 500)]
    //    public entity[] entities;
    //}

    //struct packet_weapon
    //{
    //    public int weapon;
    //};

    //struct packet_damage
    //{
    //    public int playerID;
    //    public bool dir;
    //    public int entity;
    //    public float damage;
    //};

    //struct packet_build
    //{
    //    public int id;
    //    public int type;
    //    public float posX;
    //    public float posY;
    //    public float posZ;
    //};

    //struct packet_kill
    //{
    //    public int id;
    //};

    //struct packet_state
    //{
    //    public int state;
    //};

    //container for user data storage

    public class UsersData
    {
        public string username = "Nameless";
        public bool readyStatus = false;
        public PlayerType type = PlayerType.Spectator;
    }

    public class EntityData
    {
        public byte changedMask = 0;
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
        //public EntityData p1 = new EntityData();
        //public EntityData p2 = new EntityData();
        //public EntityData p3 = new EntityData();
        public int[] playerWeapons = new int[3];

        //droid and turret data
        public Dictionary<int, EntityData> entityUpdates = new Dictionary<int, EntityData>();

        //instanced data
        public Queue<Tuple<int, int, Vector3, int>> BuildEntity = new Queue<Tuple<int, int, Vector3, int>>();
        public Queue<int> KilledEntity = new Queue<int>();
        //for fps: damage, culprit; for fps: damage, hit id
        public Queue<Tuple<int, float, int, int, int>> DamageDealt = new Queue<Tuple<int, float, int, int, int>>();

        public Queue<Tuple<int>> TerminalsOpened = new Queue<Tuple<int>>();

        public Queue<Tuple<int, Vector3, Vector3>> bullets = new Queue<Tuple<int, Vector3, Vector3>>();

        public Queue<int> turretFires = new Queue<int>();

        //game state
        public int GameState = -1;
    }

    public class PingPacket
    {
        public PingPacket(uint id)
        {
            ID = id;
        }

        public float totalTime = 0f;
        public uint ID = 0;
        public int playerNumber = 0;
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
        static extern bool Connect(string ipAddr, IntPtr client);                          //Connects to c++ Server
        [DllImport(DLL_NAME)]
        static extern void SetupPacketReception(Action<IntPtr, int, bool> action);
        [DllImport(DLL_NAME)]
        static extern void StartUpdating(IntPtr client);                                //Starts updating
        [DllImport(DLL_NAME)]
        static extern bool SendDataPacket(IntPtr ptr, int length, bool TCP, IntPtr client);
        [DllImport(DLL_NAME)]
        static extern bool SendDebugOutput(string data);
        [DllImport(DLL_NAME)]
        static extern void ShowConsole(IntPtr client, bool open);
        [DllImport(DLL_NAME)]
        static extern int GetError(IntPtr client);
        [DllImport(DLL_NAME)]
        static extern int GetErrorLoc(IntPtr client);

        public static string ip = "127.0.0.1";
        private static IntPtr Client;
        private static int playerNumber = -1;
        private static bool endGame = false;
        //public static GameState gameState = GameState.LOBBY;
        static public bool isConnected = false;

        #endregion

        int fixedTimeStep;

        public static int updateInterval = 1;
        public static int updateCounter = 0;

        public static float PING_INTERVAL = 0.4f;
        static float PING_TIME = 0f;
        public static float PING_TIMEOUT = 10f;
        public static float[] ESTIMATED_PING = { 0f, 0f, 0f, 0f };
        public static List<PingPacket>[] timePings = new List<PingPacket>[4];
        public static List<uint>[] toRemove = new List<uint>[4];
        public static uint pingIterator = 0;

        public static DataState dataState;
        public static FirearmHandler[] firearms = new FirearmHandler[3];
        public static List<Terminal> gates = new List<Terminal>();

        static byte[] sendByteArray = new byte[5000];
        static byte[] tcpByteArray = new byte[5000];
        static byte[] udpByteArray = new byte[5000];

        public static List<UsersData> allUsers;
        void Awake()
        {
            gates = new List<Terminal>();

            for (int i = 0; i < timePings.Length; ++i)
            {
                timePings[i] = new List<PingPacket>();
                toRemove[i] = new List<uint>();
            }
            //firearms = new FirearmHandler[3];
            fixedTimeStep = (int)(1f / Time.fixedDeltaTime);

            dataState = new DataState();
            allUsers = new List<UsersData>();

            // Client Init  
            Client = CreateClient();
            SetupPacketReception(receivePacket);
            StartUpdating(Client);
            ShowConsole(Client, true);
        }

        #region packingData
        public static void PackData(ref byte[] bytes, ref int loc, bool data)
        {
            BitConverter.GetBytes(data).CopyTo(bytes, loc);
            loc += Marshal.SizeOf(data);
        }

        public static void PackData(ref byte[] bytes, ref int loc, byte data)
        {
            BitConverter.GetBytes(data).CopyTo(bytes, loc);
            loc += Marshal.SizeOf(data);
        }

        public static void PackData(ref byte[] bytes, ref int loc, int data)
        {
            BitConverter.GetBytes(data).CopyTo(bytes, loc);
            loc += Marshal.SizeOf(data);
        }

        public static void PackData(ref byte[] bytes, ref int loc, uint data)
        {
            BitConverter.GetBytes(data).CopyTo(bytes, loc);
            loc += Marshal.SizeOf(data);
        }

        public static void PackData(ref byte[] bytes, ref int loc, float data)
        {
            BitConverter.GetBytes(data).CopyTo(bytes, loc);
            loc += Marshal.SizeOf(data);
        }
        public static void PackData(ref byte[] bytes, ref int loc, char data)
        {
            //BitConverter.GetBytes(data).CopyTo(bytes, loc);
            bytes[loc] = (byte)data;
            loc += Marshal.SizeOf(data);
        }

        public static void PackData(ref byte[] bytes, ref int loc, string data)
        {
            PackData(ref bytes, ref loc, data.Length);

            for (int i = 0; i < data.Length; ++i)
            {
                PackData(ref bytes, ref loc, data[i]);
            }
        }

        static int InitialOffset = 20;
        static int PACKET_SENDER = 16;
        static int PACKET_TYPE = 12;
        static int PACKET_RECEIVERS = 8;
        static int PACKET_LENGTH = 4;
        static int PACKET_STAMP = 0;
        static int OK_STAMP = 123456789;

        static bool SendIntPtr(ref byte[] bytes, int length, bool TCP, int receiver, int packetType)
        {
            bool returnVal = false;
            if (isConnected)
            {
                int playerID = GameSceneController.Instance.playerNumber;

                BitConverter.GetBytes(OK_STAMP).CopyTo(bytes, 0);
                BitConverter.GetBytes(length).CopyTo(bytes, 4);
                BitConverter.GetBytes(receiver).CopyTo(bytes, 8);
                BitConverter.GetBytes(packetType).CopyTo(bytes, 12);
                BitConverter.GetBytes(playerID).CopyTo(bytes, 16);

                //SendDebugOutput("ID: " + playerID.ToString() + ", Type: " + packetType.ToString() + ", LENGTH: " + length.ToString());

                IntPtr ptr = Marshal.AllocCoTaskMem(length);

                Marshal.Copy(bytes, 0, ptr, length);

                //SendDataFunc

                //SendDebugOutput("C#: SENDING PACKET");
                returnVal = SendDataPacket(ptr, length, TCP, Client);

                Marshal.FreeCoTaskMem(ptr);

            }
            return returnVal;
        }
        #endregion

        #region ByteSender


        public static void SendPacketInitUDP(int index)
        {
            int loc = InitialOffset;
            int Receiver = 0;

            Receiver = (int)PlayerMask.SERVER;

            PackData(ref sendByteArray, ref loc, index);

            //SendDebugOutput("SENDING UDP PACKET...");
            SendIntPtr(ref sendByteArray, loc, false, Receiver, (int)PacketType.INIT);
        }

        public static void SendPacketUser(int index, string username)
        {
            if (username == "")
            {
                username = "Nameless";
            }
            int loc = InitialOffset;
            int Receiver = 0;

            Receiver = ~(1 << (index + 1));

            PackData(ref sendByteArray, ref loc, index);
            PackData(ref sendByteArray, ref loc, username);

            //SendDebugOutput("C#: SENDING INTPTR");
            SendIntPtr(ref sendByteArray, loc, true, Receiver, (int)PacketType.USER);

            StartManager.Instance.OnRoleUpdate(true);
        }

        public static void SendPacketMsg(string message)
        {
            if (message != "")
            {
                int loc = InitialOffset;
                int Receiver = 0;

                Receiver = (~(int)PlayerMask.SERVER);

                PackData(ref sendByteArray, ref loc, message);

                SendIntPtr(ref sendByteArray, loc, true, Receiver, (int)PacketType.MESSAGE);
            }
        }

        public static void SendPacketType(PlayerType type)
        {
            if (type > 0)
            {
                int loc = InitialOffset;
                int Receiver = (int)PlayerMask.SERVER;

                PackData(ref sendByteArray, ref loc, (int)type);

                SendIntPtr(ref sendByteArray, loc, true, Receiver, (int)PacketType.TYPE);

                StartManager.Instance.OnRoleUpdate(true);
            }
        }

        public static void SendPacketReady(bool ready)
        {
            int loc = InitialOffset;
            int Receiver = (~0);

            PackData(ref sendByteArray, ref loc, ready);

            SendIntPtr(ref sendByteArray, loc, true, Receiver, (int)PacketType.READY);

            StartManager.Instance.OnRoleUpdate(true);
        }

        public static void SendPacketState(int state)
        {
            if (state >= 0 && state <= 10)
            {
                int loc = InitialOffset;
                int Receiver = 0;

                Receiver = ((int)PlayerMask.SERVER);

                PackData(ref sendByteArray, ref loc, state);

                SendIntPtr(ref sendByteArray, loc, true, Receiver, (int)PacketType.STATE);
            }
        }

        public static void SendPacketEntities()
        {
            if (updateCounter > 100)
            {
                int loc = InitialOffset;
                int Receiver = 0;

                Receiver = ~(1 << (GameSceneController.Instance.playerNumber + 1));

                if (GameSceneController.Instance.type == PlayerType.RTS)
                {
                    //This is not being called on respawned entities - broken
                    //Debug.Log("EVERYTHING SENT! ");
                    foreach (Droid droid in EntityManager.Instance.ActiveEntitiesByType[(int)EntityType.Droid])
                    {
                        //PackData(ref sendByteArray, ref loc, droid.changed);
                        PackData(ref sendByteArray, ref loc, droid.id);
                        PackData(ref sendByteArray, ref loc, (int)droid.state);

                        //if ((droid.changed | (byte)UpdateDataMask.POSX) > 0)
                        PackData(ref sendByteArray, ref loc, droid.transform.position.x);

                        //if ((droid.changed | (byte)UpdateDataMask.POSY) > 0)
                        PackData(ref sendByteArray, ref loc, droid.transform.position.y);

                        //if ((droid.changed | (byte)UpdateDataMask.POSZ) > 0)
                        PackData(ref sendByteArray, ref loc, droid.transform.position.z);

                        // if ((droid.changed | (byte)UpdateDataMask.ROTX) > 0)
                        PackData(ref sendByteArray, ref loc, droid.transform.rotation.eulerAngles.x);

                        //if ((droid.changed | (byte)UpdateDataMask.ROTY) > 0)
                        PackData(ref sendByteArray, ref loc, droid.transform.rotation.eulerAngles.y);

                        //if ((droid.changed | (byte)UpdateDataMask.ROTZ) > 0)
                        PackData(ref sendByteArray, ref loc, droid.transform.rotation.eulerAngles.z);
                    }

                    foreach (Turret turret in EntityManager.Instance.ActiveEntitiesByType[(int)EntityType.Turret])
                    {
                        //PackData(ref sendByteArray, ref loc, turret.changed);
                        PackData(ref sendByteArray, ref loc, turret.id);
                        PackData(ref sendByteArray, ref loc, (int)turret.state);

                        //if ((turret.changed | (byte)UpdateDataMask.POSX) > 0)
                        PackData(ref sendByteArray, ref loc, turret.transform.position.x);

                        //if ((turret.changed | (byte)UpdateDataMask.POSY) > 0)
                        PackData(ref sendByteArray, ref loc, turret.transform.position.y);

                        //if ((turret.changed | (byte)UpdateDataMask.POSZ) > 0)
                        PackData(ref sendByteArray, ref loc, turret.transform.position.z);

                        //if ((turret.changed | (byte)UpdateDataMask.ROTX) > 0)
                        PackData(ref sendByteArray, ref loc, turret.faceingPoint.x);

                        //if ((turret.changed | (byte)UpdateDataMask.ROTY) > 0)
                        PackData(ref sendByteArray, ref loc, turret.faceingPoint.y);

                        //if ((turret.changed | (byte)UpdateDataMask.ROTZ) > 0)
                        PackData(ref sendByteArray, ref loc, turret.faceingPoint.z);
                    }
                }
                else if (GameSceneController.Instance.type == PlayerType.FPS)
                {
                    FPSPlayer.Player player = (FPSPlayer.Player)EntityManager.Instance.AllEntities[playerNumber];

                    //PackData(ref sendByteArray, ref loc, player.changed);
                    PackData(ref sendByteArray, ref loc, player.id);
                    PackData(ref sendByteArray, ref loc, (int)player.firearmHandler.currState);

                    //if ((player.changed | (byte)UpdateDataMask.POSX) > 0)
                    PackData(ref sendByteArray, ref loc, player.transform.position.x);

                    //if ((player.changed | (byte)UpdateDataMask.POSY) > 0)
                    PackData(ref sendByteArray, ref loc, player.transform.position.y);

                    //if ((player.changed | (byte)UpdateDataMask.POSZ) > 0)
                    PackData(ref sendByteArray, ref loc, player.transform.position.z);

                    //if ((player.changed | (byte)UpdateDataMask.ROTX) > 0)
                    PackData(ref sendByteArray, ref loc, player.m_pitch);

                    //if ((player.changed | (byte)UpdateDataMask.ROTY) > 0)
                    PackData(ref sendByteArray, ref loc, player.m_yaw);

                    //if ((player.changed | (byte)UpdateDataMask.ROTZ) > 0)
                    PackData(ref sendByteArray, ref loc, player.transform.rotation.eulerAngles.z);
                }
                else
                {
                    return;
                }

                SendIntPtr(ref sendByteArray, loc, false, Receiver, (int)PacketType.ENTITY);
                updateCounter = 0;
                Debug.Log("Sent UDP Update Packet");
            }
        }


        public static void SendPacketDamage(int senderID, int receiverID, float damage, int entityLife, int importantReceivers)
        {
            if (damage >= 0)
            {
                int loc = InitialOffset;
                int Receiver = 0;

                Receiver = (~0);

                PackData(ref sendByteArray, ref loc, senderID);
                PackData(ref sendByteArray, ref loc, receiverID);
                PackData(ref sendByteArray, ref loc, damage);
                PackData(ref sendByteArray, ref loc, entityLife);
                PackData(ref sendByteArray, ref loc, importantReceivers);

                SendIntPtr(ref sendByteArray, loc, true, Receiver, (int)PacketType.DAMAGE);
            }
        }

        public static void SendPacketWeapon(int weapon)
        {
            if (weapon >= 0)
            {
                int loc = InitialOffset;
                int Receiver = 0;

                Receiver = ~((1 << (GameSceneController.Instance.playerNumber + 1)) & (int)PlayerMask.SERVER);

                PackData(ref sendByteArray, ref loc, weapon);

                SendIntPtr(ref sendByteArray, loc, true, Receiver, (int)PacketType.WEAPON);
            }
        }

        public static void SendPacketBuild(int ID, int type, Vector3 pos, int entityLife)
        {
            if (ID >= 0 && type >= 0)
            {
                Debug.Log("BUILDING TYPE: " + (EntityType)type);
                int loc = InitialOffset;
                int Receiver = 0;

                //Receiver = ~(((int)PlayerMask.SERVER) + (1 << (GameSceneController.Instance.playerNumber + 1)));
                Receiver = ~(1 << (GameSceneController.Instance.playerNumber + 1));

                PackData(ref sendByteArray, ref loc, ID);
                PackData(ref sendByteArray, ref loc, type);
                PackData(ref sendByteArray, ref loc, pos.x);
                PackData(ref sendByteArray, ref loc, pos.y);
                PackData(ref sendByteArray, ref loc, pos.z);
                PackData(ref sendByteArray, ref loc, entityLife);

                SendIntPtr(ref sendByteArray, loc, true, Receiver, (int)PacketType.BUILD);
            }
        }

        public static void SendPacketDeath(int ID, int killerID)
        {
            if (ID >= 0)
            {
                int loc = InitialOffset;
                int Receiver = 0;

                //Receiver = ~(((int)PlayerMask.SERVER) + (1 << (GameSceneController.Instance.playerNumber + 1)));
                Receiver = ~(1 << (GameSceneController.Instance.playerNumber + 1));

                PackData(ref sendByteArray, ref loc, ID);
                PackData(ref sendByteArray, ref loc, killerID);

                SendIntPtr(ref sendByteArray, loc, true, Receiver, (int)PacketType.DEATH);
            }
        }

        public static void SendPacketGateOpen(int gateNum)
        {
            int loc = InitialOffset;
            int Receiver = ~(((int)PlayerMask.SERVER) + (1 << (GameSceneController.Instance.playerNumber + 1)));

            PackData(ref sendByteArray, ref loc, gateNum);

            SendIntPtr(ref sendByteArray, loc, true, Receiver, (int)PacketType.TERMINAL);
        }

        public static void SendPacketFiring(int id, Vector3 hitPoint, Vector3 hitNormal)
        {
            int loc = InitialOffset;
            int Receiver = 0;

            Receiver = ~(((int)PlayerMask.SERVER) + (1 << (GameSceneController.Instance.playerNumber + 1)));

            PackData(ref sendByteArray, ref loc, id);
            PackData(ref sendByteArray, ref loc, hitPoint.x);
            PackData(ref sendByteArray, ref loc, hitPoint.y);
            PackData(ref sendByteArray, ref loc, hitPoint.z);
            PackData(ref sendByteArray, ref loc, hitNormal.x);
            PackData(ref sendByteArray, ref loc, hitNormal.y);
            PackData(ref sendByteArray, ref loc, hitNormal.z);

            SendIntPtr(ref sendByteArray, loc, false, Receiver, (int)PacketType.FIRING);
        }

        public static void SendPacketTurretFire(int id)
        {
            int loc = InitialOffset;
            int Receiver = 0;

            Receiver = ~(((int)PlayerMask.SERVER) + (1 << (GameSceneController.Instance.playerNumber + 1)));

            PackData(ref sendByteArray, ref loc, id);

            SendIntPtr(ref sendByteArray, loc, false, Receiver, (int)PacketType.TURRETFIRE);
        }

        public static void SendPacketPing()
        {
            for (int i = 0; i < timePings.Length; ++i)
            {
                if (i != GameSceneController.Instance.playerNumber)
                {
                    uint pingNum = ++pingIterator;

                    timePings[i].Add(new PingPacket(pingNum));

                    int loc = InitialOffset;
                    int Receiver = (1 << (i + 1));

                    PackData(ref sendByteArray, ref loc, pingNum);

                    SendIntPtr(ref sendByteArray, loc, false, Receiver, (int)PacketType.PINGSELF);
                }
            }
        }

        public static void SendPacketReturnPing(int returnTo, uint pingID)
        {
            int loc = InitialOffset;
            int Receiver = (1 << (returnTo + 1));

            PackData(ref udpByteArray, ref loc, pingID);

            SendIntPtr(ref udpByteArray, loc, false, Receiver, (int)PacketType.RETURNPING);
        }
        #endregion

        #region ReceivingPackets
        public static void UnpackBool(ref byte[] byteArray, ref int loc, ref bool output)
        {
            output = BitConverter.ToBoolean(byteArray, loc);
            loc += Marshal.SizeOf(output);
        }

        public static void UnpackByte(ref byte[] byteArray, ref int loc, ref byte output)
        {
            output = byteArray[loc];
            loc += Marshal.SizeOf(output);
        }

        public static void UnpackInt(ref byte[] byteArray, ref int loc, ref int output)
        {
            output = BitConverter.ToInt32(byteArray, loc);
            loc += Marshal.SizeOf(output);
        }

        public static void UnpackUint(ref byte[] byteArray, ref int loc, ref uint output)
        {
            output = BitConverter.ToUInt32(byteArray, loc);
            loc += Marshal.SizeOf(output);
        }

        public static void UnpackFloat(ref byte[] byteArray, ref int loc, ref float output)
        {
            output = BitConverter.ToSingle(byteArray, loc);
            loc += Marshal.SizeOf(output);
        }
        public static void UnpackChar(ref byte[] byteArray, ref int loc, ref char output)
        {
            output = (char)byteArray[loc];
            loc += Marshal.SizeOf(output);
        }

        public static void UnpackString(ref byte[] byteArray, ref int loc, ref string output)
        {
            int strLen = 0;
            UnpackInt(ref byteArray, ref loc, ref strLen);
            strLen += loc;

            while (loc < strLen)
            {
                char c = '0';
                UnpackChar(ref byteArray, ref loc, ref c);
                output += c;
            }
        }

        static void receivePacket(IntPtr ptr, int length, bool TCP)
        {
            if (length < 5000 && length >= InitialOffset)
            {

                //SendDebugOutput("C# RECEIVED PACKET");
                if (TCP)
                {
                    Marshal.Copy(ptr, tcpByteArray, 0, length);
                    //SendDebugOutput("C# DECODED TCP PACKET");
                    deconstructPacket(ref tcpByteArray, length);
                }
                else
                {
                    Marshal.Copy(ptr, udpByteArray, 0, length);
                    //SendDebugOutput("C# DECODED UDP PACKET");
                    deconstructPacket(ref udpByteArray, length);
                }
            }
            else
            {
                SendDebugOutput("Invalid Packet Received");
            }
        }

        static void deconstructPacket(ref byte[] bytes, int length)
        {
            int type = BitConverter.ToInt32(bytes, PACKET_TYPE);
            int sender = BitConverter.ToInt32(bytes, PACKET_SENDER);
            int loc = InitialOffset;

            //SendDebugOutput("Type: " + type.ToString() + " , Sender: " + sender.ToString());
            switch (type)
            {
                case (int)PacketType.INIT:
                    {
                        if (sender == -1)
                        {
                            //SendDebugOutput("C# GOT INIT FROM SERVER");
                            int index = 0;
                            UnpackInt(ref bytes, ref loc, ref index);
                            PacketReceivedInit(sender, index);
                        }
                    }
                    break;
                case (int)PacketType.USER:
                    {
                        if (sender == -1)
                        {
                            SendDebugOutput("C# GOT USERS FROM SERVER");
                            PacketReceivedAllUser(ref bytes, length, loc);
                        }
                        else
                        {
                            SendDebugOutput("C# GOT USER FROM OTHER PLAYER");
                            int index = 0;
                            string user = "";
                            UnpackInt(ref bytes, ref loc, ref index);
                            UnpackString(ref bytes, ref loc, ref user);
                            PacketReceivedUser(index, user);
                        }
                    }
                    break;
                case (int)PacketType.TYPE:
                    int playerType = 0;
                    UnpackInt(ref bytes, ref loc, ref playerType);
                    if (sender == GameSceneController.Instance.playerNumber)
                    {
                        allUsers[playerNumber].type = (PlayerType)playerType;
                        PacketReceivedType(playerType);
                    }
                    else
                    {
                        PacketReceivedType(sender, playerType);
                    }
                    break;
                case (int)PacketType.READY:
                    bool ready = false;
                    UnpackBool(ref bytes, ref loc, ref ready);
                    PacketReceivedReady(sender, ready);
                    break;
                case (int)PacketType.MESSAGE:
                    string message = "";
                    UnpackString(ref bytes, ref loc, ref message);
                    PacketReceivedMsg(sender, message);
                    break;
                case (int)PacketType.STATE:
                    int state = 0;
                    UnpackInt(ref bytes, ref loc, ref state);
                    PacketReceivedState(sender, state);
                    break;
                case (int)PacketType.ENTITY:
                    //SendDebugOutput("Update");
                    if (sender != playerNumber)
                    {
                        PacketReceivedEntity(ref bytes, ref loc, length, sender);
                        //SendDebugOutput("Other Updates");
                    }
                    break;
                case (int)PacketType.DAMAGE:
                    int senderID = 0;
                    int receiverID = 0;
                    float damage = 0.0f;
                    int entityLife = -1;
                    int importantReceivers = 0;
                    UnpackInt(ref bytes, ref loc, ref senderID);
                    UnpackInt(ref bytes, ref loc, ref receiverID);
                    UnpackFloat(ref bytes, ref loc, ref damage);
                    UnpackInt(ref bytes, ref loc, ref entityLife);
                    UnpackInt(ref bytes, ref loc, ref importantReceivers);
                    PacketReceivedDamage(senderID, receiverID, damage, entityLife, importantReceivers);
                    break;
                case (int)PacketType.WEAPON:
                    int weapon = 0;
                    UnpackInt(ref bytes, ref loc, ref weapon);
                    PacketReceivedWeapon(sender, weapon);
                    break;
                case (int)PacketType.BUILD:
                    PacketReceivedBuild(ref bytes, ref loc);
                    break;
                case (int)PacketType.DEATH:
                    int id = 0;
                    int killerID = 0;
                    UnpackInt(ref bytes, ref loc, ref id);
                    UnpackInt(ref bytes, ref loc, ref killerID);
                    PacketReceivedDeath(id, killerID);
                    break;
                case (int)PacketType.TERMINAL:
                    int toDisarm = -1;
                    UnpackInt(ref bytes, ref loc, ref toDisarm);
                    if (toDisarm >= 0 && gates[toDisarm] != null)
                    {
                        dataState.TerminalsOpened.Enqueue(new Tuple<int>(toDisarm));
                        //gates[toDisarm].openGate(gates[toDisarm].gate);
                    }
                    break;
                case (int)PacketType.FIRING:
                    PacketReceivedFiring(ref bytes, ref loc);
                    break;
                default:
                    SendDebugOutput("Error Packet Type");
                    break;
                case (int)PacketType.TURRETFIRE:
                    int TID = 0;
                    UnpackInt(ref bytes, ref loc, ref TID);
                    dataState.turretFires.Enqueue(TID);
                    break;
                case (int)PacketType.RETURNPING:
                    uint pID = 0;
                    UnpackUint(ref bytes, ref loc, ref pID);
                    lock (toRemove)
                    {
                        toRemove[sender].Add(pID);
                    }
                    break;
                case (int)PacketType.PINGSELF:
                    uint pingID = 0;
                    UnpackUint(ref bytes, ref loc, ref pingID);
                    SendPacketReturnPing(sender, pingID);
                    break;
            }

        }

        #endregion


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
            //SetupPacketReceptionJoin(PacketReceivedJoin);
            SetupPacketReceptionMsg(PacketReceivedMsg);
            SetupPacketReceptionState(PacketReceivedState);
            SetupPacketReceptionEntity(PacketReceivedEntity);
            SetupPacketReceptionDamage(PacketReceivedDamage);
            SetupPacketReceptionWeapon(PacketReceivedWeapon);
            SetupPacketReceptionBuild(PacketReceivedBuild);
            SetupPacketReceptionKill(PacketReceivedKill);

            //SetupTextReception(TextReceived);
        }


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

        private void Update()
        {
            for (int p = 0; p < timePings.Length; ++p)
            {
                for (int i = toRemove[p].Count - 1; i >= 0; --i)
                {
                    for (int j = timePings[p].Count - 1; j >= 0; --j)
                    {
                        if (timePings[p][j].ID == toRemove[p][i])
                        {
                            ESTIMATED_PING[p] = Mathf.Lerp(ESTIMATED_PING[p], timePings[p][j].totalTime * 0.5f, 0.1f);
                            timePings[p].RemoveAt(j);
                            toRemove[p].RemoveAt(i);
                            break;
                        }
                    }
                }

                for (int j = timePings[p].Count - 1; j >= 0; --j)
                {
                    timePings[p][j].totalTime += Time.deltaTime;

                    if (timePings[p][j].totalTime > PING_TIMEOUT)
                    {
                        timePings[p].RemoveAt(j);
                    }
                }

                //Debug.Log("ESTIMATED PING FOR PLAYER " + p + ": " + ESTIMATED_PING[p]);
            }


            PING_TIME += Time.deltaTime;
            if (PING_TIME >= PING_INTERVAL)
            {
                PING_TIME = 0f;

                SendPacketPing();
            }
        }

        // Update is called once per frame
        void FixedUpdate()
        {
            updateCounter = updateCounter + updateInterval;
            //Debug.Log("Update Counter Increment: " + updateCounter);

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
                if (Input.GetKeyDown(KeyCode.P) && dataState.GameState == (int)GameState.GAME)
                {
                    endGame = true;
                }
                if (endGame)
                {
                    GameSceneController.Instance.SwapScene(4);
                    endGame = false;
                }
                if (Input.GetKeyDown(KeyCode.U) && dataState.GameState == (int)GameState.GAME)
                {
                    updateInterval++;
                    Debug.Log("Update Interval: " + updateInterval);
                }
                if (Input.GetKeyDown(KeyCode.I) && dataState.GameState == (int)GameState.GAME)
                {
                    if (updateInterval > 0)
                    {
                        updateInterval--;
                    }
                    else
                    {
                        updateInterval = 1;
                    }
                    Debug.Log("Update Interval: " + updateInterval);
                }
                //update players
                //if (dataState.p1.updated)
                //{
                //    dataState.p1.updated = false;
                //
                //    PlayerFPS player = (PlayerFPS)EntityManager.Instance.AllEntities[1];
                //    if (player.type == EntityType.Dummy)
                //    {
                //        firearms[0].NetworkingUpdate(dataState.p1.weapon);
                //        player.SendUpdate(dataState.p1.position, dataState.p1.rotation, dataState.p1.state, dataState.p1.weapon);
                //    }
                //}
                //if (dataState.p2.updated)
                //{
                //    dataState.p2.updated = false;
                //
                //    PlayerFPS player = (PlayerFPS)EntityManager.Instance.AllEntities[2];
                //    if (player.type == EntityType.Dummy)
                //    {
                //        firearms[1].NetworkingUpdate(dataState.p2.weapon);
                //        player.SendUpdate(dataState.p2.position, dataState.p2.rotation, dataState.p2.state, dataState.p2.weapon);
                //    }
                //}
                //if (dataState.p3.updated)
                //{
                //    dataState.p3.updated = false;
                //
                //    PlayerFPS player = (PlayerFPS)EntityManager.Instance.AllEntities[3];
                //    if (player.type == EntityType.Dummy)
                //    {
                //        player.SendUpdate(dataState.p3.position, dataState.p3.rotation, dataState.p3.state, dataState.p3.weapon);
                //    }
                //}
                //SendDebugOutput("Game State: " + dataState.GameState.ToString());

                if (dataState.GameState == (int)GameState.GAME)
                {

                    //SendDebugOutput("Game Update");
                    //foreach (FPSPlayer.Player pfps in EntityManager.Instance.ActivePlayers())
                    //{
                    //
                    //    pfps. = pfps.playerGun.slots[dataState.playerWeapons[pfps.id - 1]];
                    //}
                    //
                    //foreach (FirearmHandler firearm in firearms)
                    //{
                    //   
                    //}

                    for (int i = 0; i < 3; i++)
                    {
                        firearms[i].NetworkingUpdate(dataState.playerWeapons[i]);
                    }

                    lock (dataState.entityUpdates)
                    {
                        //Debug.Log(dataState)
                        foreach (var keyVal in dataState.entityUpdates.Keys)
                        {

                            EntityData ed = dataState.entityUpdates[keyVal];
                            //SendDebugOutput("Updating Entities...");
                            if (ed.updated)
                            {
                                //SendDebugOutput("Updating Entities now....");
                                //Debug.Log("UPDATING POSITION FOR " + kvp.Key + "/" + EntityManager.Instance.AllEntities.Count);
                                ed.updated = false;
                                if (EntityManager.Instance.AllEntities.Count > keyVal && EntityManager.Instance.AllEntities[keyVal].isActiveAndEnabled)
                                {
                                    Entity temp = EntityManager.Instance.AllEntities[keyVal];
                                    temp.UpdateEntityStats(ed);
                                }
                                //Debug.Log(temp.name);
                                //Debug.Log(kvp.Value.position + ", " + kvp.Value.rotation);

                            }
                        }
                    }
                }

                while (dataState.turretFires.Count > 0)
                {
                    int TID = dataState.turretFires.Dequeue();
                    if (TID >= EntityManager.Instance.AllEntities.Count)
                        continue;
                    Turret t = (Turret)EntityManager.Instance.AllEntities[TID];
                    if (t != null)
                        t.muzzle.Play();
                }

                while (dataState.bullets.Count > 0)
                {
                    Tuple<int, Vector3, Vector3> singleBullet = dataState.bullets.Dequeue();

                    if (EntityManager.Instance.AllEntities[singleBullet.Item1] != null && EntityManager.Instance.AllEntities[singleBullet.Item1].isActiveAndEnabled)
                    {
                        if (EntityManager.Instance.AllEntities[singleBullet.Item1].type == EntityType.Dummy)
                        {
                            ((FPSPlayer.Player)EntityManager.Instance.AllEntities[singleBullet.Item1]).firearmHandler.PlayShootEffect(singleBullet.Item2, singleBullet.Item3);
                        }
                    }
                }

                while (dataState.TerminalsOpened.Count > 0)
                {
                    Tuple<int> toOpen = dataState.TerminalsOpened.Dequeue();

                    gates[toOpen.Item1].openGate(gates[toOpen.Item1].gate);
                }

                //if (dataState.entityUpdates.Count > 0)
                //    dataState.entityUpdates.Clear();

                //update damage
                while (dataState.DamageDealt.Count > 0)
                {

                    Tuple<int, float, int, int, int> damage = dataState.DamageDealt.Dequeue();
                    //Debug.Log(damage.Item1 + ", " + damage.Item2 + ", " + damage.Item3);
                    //Debug.Log("PLAYER NUMBER: " + GameSceneController.Instance.playerNumber);
                    //Debug.Log(EntityManager.Instance.AllEntities[GameSceneController.Instance.playerNumber].name);
                    if (EntityManager.Instance.AllEntities.Count > damage.Item1 && EntityManager.Instance.AllEntities[damage.Item1].isActiveAndEnabled)
                    {
                        if ((damage.Item5 & (1 << (GameSceneController.Instance.playerNumber + 1))) > 0)
                        {
                            EntityManager.Instance.AllEntities[damage.Item1].OnDamage(damage.Item2, damage.Item3, damage.Item4);
                        }
                        else
                        {
                            EntityManager.Instance.AllEntities[damage.Item1].OnOtherDamage(damage.Item2, damage.Item3, damage.Item4);
                        }

                        //Debug.Log("DAMAGED");
                    }
                }

                while (dataState.KilledEntity.Count > 0)
                {
                    //if (GameSceneController.Instance.type == PlayerType.FPS)
                    //{
                    EntityManager.Instance.AllEntities[dataState.KilledEntity.Dequeue()].OnDeath(false);
                    //}
                    //else
                    //{
                    //    dataState.KilledEntity.Dequeue();
                    //}
                }

                while (dataState.BuildEntity.Count > 0)
                {
                    if (GameSceneController.Instance.type == PlayerType.FPS)
                    {
                        Tuple<int, int, Vector3, int> tempTup = dataState.BuildEntity.Dequeue();

                        Debug.Log(tempTup.Item1 + ", " + tempTup.Item2 + ", " + tempTup.Item3 + ", " + tempTup.Item4);

                        Entity temp = EntityManager.Instance.GetEntityAt((EntityType)tempTup.Item2, tempTup.Item1);
                        temp.transform.position = tempTup.Item3;
                        temp.deaths = tempTup.Item4;
                        temp.IssueBuild();

                    }
                    else
                    {
                        dataState.BuildEntity.Dequeue();
                    }
                }
            }
        }
        void TickUpdate() { }

        #region PacketReception
        static void PacketReceivedInit(int sender, int index)
        {
            GameSceneController.Instance.playerNumber = index;
            playerNumber = index;
            if (!isConnected)
            {
                SendDebugOutput("INIT PACKET");
                isConnected = true;
                OnConnected(true);
                allUsers.Add(new UsersData());
                SendDebugOutput("Sending UDP INIT");
                SendPacketInitUDP(GameSceneController.Instance.playerNumber);
                SendDebugOutput("Sending TCP for username");
                SendPacketUser(GameSceneController.Instance.playerNumber, StartManager.Instance.username.text);
            }
        }

        static void PacketReceivedUser(int index, string user)
        {
            while (index >= allUsers.Count)
            {
                allUsers.Add(new UsersData());
                SendDebugOutput("User Added! Total: " + allUsers.Count.ToString());
            }
            allUsers[index].username = user;
            RecieveMessage(user + " has joined the server!");
            StartManager.Instance.OnRoleUpdate(true);
        }

        static void PacketReceivedAllUser(ref byte[] data, int length, int loc)
        {
            int index = 0;
            string user = "";

            while (loc < length)
            {
                //SendDebugOutput("Loc:" + loc.ToString() + " , Len: " + length.ToString());
                UnpackInt(ref data, ref loc, ref index);
                UnpackString(ref data, ref loc, ref user);
                //SendDebugOutput("Index: " + index.ToString());

                while (allUsers.Count <= index)
                {
                    allUsers.Add(new UsersData());
                    SendDebugOutput("User Added! Total: " + allUsers.Count.ToString());
                }

                allUsers[index].username = user;
                user = "";
            }
            StartManager.Instance.OnRoleUpdate(true);
        }

        static void PacketReceivedType(int type)
        {
            OnRoleSelected(type);
        }

        static void PacketReceivedType(int sender, int type)
        {
            if (allUsers[sender].type != (PlayerType)type)
            {
                allUsers[sender].type = (PlayerType)type;

                if ((PlayerType)type == PlayerType.FPS)
                {
                    RecieveMessage("User " + allUsers[sender].username + " is now FPS.");
                }
                else if ((PlayerType)type == PlayerType.RTS)
                {
                    RecieveMessage("User " + allUsers[sender].username + " is now RTS.");
                }
                else
                {
                    RecieveMessage("User " + allUsers[sender].username + " is now SPECTATOR.");
                }
            }
            StartManager.Instance.OnRoleUpdate(true);
        }

        static void PacketReceivedReady(int sender, bool ready)
        {
            //SendDebugOutput("Sender: " + sender.ToString() + ", Count: " + allUsers.Count.ToString());
            if (allUsers[sender].readyStatus != ready)
            {
                allUsers[sender].readyStatus = ready;

                if (ready)
                {
                    RecieveMessage("User " + allUsers[sender].username + " is Ready.");
                }
                else
                {
                    RecieveMessage("User " + allUsers[sender].username + " Unreadied.");
                }
            }
        }

        static void PacketReceivedMsg(int sender, string msg)
        {
            SendDebugOutput("Player " + sender.ToString() + ": " + msg);
            RecieveMessage(allUsers[sender].username + ": " + msg);
        }

        static void PacketReceivedState(int sender, int state)
        {
            lock (dataState)
            {
                switch (state)
                {
                    case (int)GameState.LOBBY:
                        if (dataState.GameState == (int)(GameState.TIMER))
                        {
                            StopCountdown();
                        }
                        break;
                    case (int)GameState.TIMER:
                        StartCountdown();
                        break;
                    case (int)GameState.LOAD:
                        Debug.Log("Load Game!");
                        LoadGame();
                        break;
                    case (int)GameState.GAME:
                        Debug.Log("Game Start!");
                        GameReady();
                        break;
                    case (int)GameState.ENDGAME:
                        Debug.Log("End Game!");
                        GameEnded();
                        break;
                }
                dataState.GameState = state;
            }
        }

        static void PacketReceivedEntity(ref byte[] bytes, ref int loc, int length, int sender)
        {
            while (loc < length)
            {

                EntityData ed = new EntityData();
                int id = 0;

                //UnpackByte(ref bytes, ref loc, ref ed.changedMask);

                UnpackInt(ref bytes, ref loc, ref id);

                //SendDebugOutput("C#: ENTITY PROCESSED: " + id.ToString());

                UnpackInt(ref bytes, ref loc, ref ed.state);

                //if ((ed.changedMask | (byte)UpdateDataMask.POSX) > 0)
                UnpackFloat(ref bytes, ref loc, ref ed.position.x);

                //if ((ed.changedMask | (byte)UpdateDataMask.POSY) > 0)
                UnpackFloat(ref bytes, ref loc, ref ed.position.y);

                //if ((ed.changedMask | (byte)UpdateDataMask.POSZ) > 0)
                UnpackFloat(ref bytes, ref loc, ref ed.position.z);

                // if ((ed.changedMask | (byte)UpdateDataMask.ROTX) > 0)
                UnpackFloat(ref bytes, ref loc, ref ed.rotation.x);

                //if ((ed.changedMask | (byte)UpdateDataMask.ROTY) > 0)
                UnpackFloat(ref bytes, ref loc, ref ed.rotation.y);

                //if ((ed.changedMask | (byte)UpdateDataMask.ROTZ) > 0)
                UnpackFloat(ref bytes, ref loc, ref ed.rotation.z);
                ed.updated = true;

                lock (dataState.entityUpdates)
                {
                    //SendDebugOutput("Entity Updates");
                    dataState.entityUpdates[id] = ed;
                    //SendDebugOutput("E-UPDATES SIZE: " + dataState.entityUpdates.Count.ToString());
                }

                // EntityManager.Instance.AllEntities[id];
            }

            //SendDebugOutput("C#: ENTITY PROCESSING FINISHED!");
        }

        // NEEDS UPDATE @PROGRAMMERS
        static void PacketReceivedDamage(int senderID, int receiverID, float damage, int entityLife, int importantReceivers)
        {
            Tuple<int, float, int, int, int> temp = Tuple.Create(receiverID, damage, senderID, entityLife, importantReceivers);

            lock (dataState.DamageDealt)
            {
                dataState.DamageDealt.Enqueue(temp);
            }
        }

        static void PacketReceivedWeapon(int sender, int weapon)
        {
            //update state by sender type
            if (sender == playerNumber)
            {
                return;
            }
            lock (dataState.playerWeapons)
            {
                dataState.playerWeapons[sender - 1] = weapon;
                //switch (sender)
                //{
                //    case 1:
                //        dataState.p1.weapon = weapon;
                //        dataState.p1.updated = true;
                //        break;
                //    case 2:
                //        dataState.p2.weapon = weapon;
                //        dataState.p2.updated = true;
                //        break;
                //    case 3:
                //        dataState.p2.weapon = weapon;
                //        dataState.p3.updated = true;
                //        break;
                //    default:
                //        Debug.Log("Error: WEAPONSTATE Sender Invalid");
                //        break;
                //}
            }
        }

        static void PacketReceivedBuild(ref byte[] bytes, ref int loc)
        {
            int ID = 0;
            int type = 0;
            float posX = 0;
            float posY = 0;
            float posZ = 0;
            int entityLife = -1;
            UnpackInt(ref bytes, ref loc, ref ID);
            UnpackInt(ref bytes, ref loc, ref type);
            UnpackFloat(ref bytes, ref loc, ref posX);
            UnpackFloat(ref bytes, ref loc, ref posY);
            UnpackFloat(ref bytes, ref loc, ref posZ);
            UnpackInt(ref bytes, ref loc, ref entityLife);

            Debug.Log("Building: " + (EntityType)type);
            lock (dataState.BuildEntity)
            {
                Tuple<int, int, Vector3, int> temp = Tuple.Create(ID, type, new Vector3(posX, posY, posZ), entityLife);
                dataState.BuildEntity.Enqueue(temp);
            }
        }

        static void PacketReceivedDeath(int id, int killerID)
        {
            lock (dataState.KilledEntity)
            {
                dataState.KilledEntity.Enqueue(id);
            }
        }

        static void PacketReceivedFiring(ref byte[] bytes, ref int loc)
        {
            int ID = 0;
            float pointX = 0;
            float pointY = 0;
            float pointZ = 0;
            float normalX = 0;
            float normalY = 0;
            float normalZ = 0;

            UnpackInt(ref bytes, ref loc, ref ID);
            UnpackFloat(ref bytes, ref loc, ref pointX);
            UnpackFloat(ref bytes, ref loc, ref pointY);
            UnpackFloat(ref bytes, ref loc, ref pointZ);
            UnpackFloat(ref bytes, ref loc, ref normalX);
            UnpackFloat(ref bytes, ref loc, ref normalY);
            UnpackFloat(ref bytes, ref loc, ref normalZ);

            lock (dataState.bullets)
            {
                dataState.bullets.Enqueue(new Tuple<int, Vector3, Vector3>(ID, new Vector3(pointX, pointY, pointZ), new Vector3(normalX, normalY, normalZ)));
            }
        }

        //call c++ cleanup
        private void OnDestroy()
        {
            //clean up client
            DeleteClient(Client);
        }
        #endregion

        // Old Reception
        #region OldReception
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
        /*
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
        */

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

        #region OldSendData
        /*
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

            */
        #endregion

        #region LobbyFunctions

        /*
         * ConnectToServer
         * @desc 
         *  Connects to the TCP server. 
         * @param
         *  string: ip of the server
         * 
         */
        //public static void ConnectToServer(string ipAddr)
        //{
        //    if (ipAddr != "")
        //    {
        //        ip = ipAddr;
        //    }
        //    // Client Connections
        //    if (!Connect(ip, Client))
        //    {
        //        Debug.Log("Error Loc: " + GetErrorLoc(Client).ToString() + " , Error: " + GetError(Client).ToString());
        //        OnConnected(false);
        //    }
        //    else
        //    {
        //        OnConnected(true);
        //    }
        //    SendPacketInit(GameSceneController.Instance.playerNumber, "Nameless");
        //}

        /*
        * ConnectToServer
        * @desc 
        *  Connects to the TCP server. 
        * @param
        *  string: ip of the server
        *  string: username of the player connecting
        * 
        */
        public static void ConnectToServer(string ipAddr, string username = "Nameless")
        {
            if (ipAddr != "")
            {
                ip = ipAddr;
            }
            // Client Connections
            if (!Connect(ip, Client))
            {
                Debug.Log("Error Loc: " + GetErrorLoc(Client).ToString() + " , Error: " + GetError(Client).ToString());
                OnConnected(false);
            }
        }


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
        public static void SelectRole(PlayerType type)
        {
            SendPacketType(type);
        }

        /*
         * OnRoleSelected
         * @desc
         *  Action that recieves if role is avaliable
         * @param
         *  bool: if role request was successful
         */
        public static void OnRoleSelected(int role)
        {
            StartManager.Instance.OnRoleSelected(role);
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
        public static void RoleUpdate(int slotNum, bool readyStatus, int type, string userName)
        {
            if (!GameSceneController.Instance.gameStart)
            {
                if (slotNum - 1 == playerNumber)
                {

                }
                StartManager.Instance.rolesUpdated = true;

                //while (slotNum > allUsers.Count)
                //{
                //allUsers.Add(new UsersData());
                //}

                //update ready status
                if (allUsers[slotNum - 1].readyStatus != readyStatus)
                {
                    allUsers[slotNum - 1].readyStatus = readyStatus;

                    if (readyStatus)
                    {
                        RecieveMessage("User " + userName + " is Ready.");
                    }
                    else
                    {
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
                    else
                    {
                        RecieveMessage("User " + userName + " is now SPECTATOR.");
                    }
                }


                allUsers[slotNum - 1].username = userName;
            }
            else
            {
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
        public static void OnReady(bool ready)
        {
            SendPacketReady(ready);
        }


        /*
         * StartCountdown
         * @desc
         *  Action that starts the countdown state. When all users are loaded, countdown begins for player 
         * 
         */
        public static void StartCountdown()
        {
            Debug.Log("Start CountDown");
            StartManager.Instance.StartCount();
        }
        /*
         * StopCountdown
         * @desc
         *  Action that stops, and resets the countdown state.
         * 
         */
        public static void StopCountdown()
        {
            Debug.Log("Stop CountDown");
            StartManager.Instance.StopCount();
        }

        /*
        * LoadGame
        * @desc
        *  Action call to let client know when they should start loading
        * 
        */

        public static void LoadGame()
        {
            StartManager.Instance.LoadGame();
        }

        /*
         * OnLoaded
         * @desc
         *  Tells the server that this player has finished loading the game scene, allowing the next player to load.
         * 
         */
        public static void OnLoaded()
        {
            SendPacketState((int)GameState.LOAD);
        }


        /*
         * GameReady
         * @desc
         *  Action that is called when all users have finished loading
         * 
         */
        public static void GameReady()
        {
            GameSceneController.Instance.gameStart = true;
        }

        /*
        * SendMessage
        * @desc
        *   Sends a message to every connected client, including self.
        *   Format needs to be: "[Username]: [message]"
        * @param
        *   string: string of message to send
        *   
        */
        public static void SendMessage(string message)
        {
            SendPacketMsg(message);
        }

        /*
         * RecieveMessage
         * @desc
         *  Action that relays recieved message to startManager
         *  Format needs to be: "[Username]: [message]"
         * @param
         *  string: message contents
         * 
         */
        public static void RecieveMessage(string message)
        {
            StartManager.Instance.recieveMessage(message);
        }
        #endregion


        public static void EndGame()
        {
            Debug.Log("ENDED GAME");
            SendPacketState((int)GameState.ENDGAME);
        }

        // Client receive ENDGAME state from server, Call Scene Switch Here
        public static void GameEnded()
        {
            // Insert SceneSwap to End Game Scene
            endGame = true;
        }
    }
}