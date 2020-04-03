using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using RTSInput;
public class ResourceManager : MonoBehaviour
{
    #region SingletonCode
    private static ResourceManager _instance;
    public static ResourceManager Instance { get { return _instance; } }
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
    public static class ResourceConstants
    {
        public const int SUPPLY_MAX = 100;
        public const int COST_BARRACKS = 500;
        public const int COST_DROIDS = 200;
        public const int COST_TURRET = 400;
        public const int COST_WALL = 250;
        public const int COST_SCIENCE = 500;
        public const int TRICKLERATE = 4;
        public const int FRAMETICK = 1;
        public const int PLAYERKILLEDMONEY = 1000;

        public const int COST_RES1 = 1000;
        public const int COST_RES2 = 1000;
        public const int COST_RES3 = 1000;
        public const int COST_RES4 = 1000;
        public const int COST_RES5 = 1000;
        public const int COST_RES6 = 1000;


        public const float DROIDTRAINTIME = 5.0f;
        public const int SUPPLY_PER_BARRACKS = 20;

        public const bool CREDITS_OFF = false;
        public const bool UNKILLABLEPLAYER = true;

        public const bool RTSPLAYERDEBUGMODE = true;
    }

    public enum GameState
    {
        Preparing = 0,
        Running = 1,
        Win = 2,
        Loss = 3,
    }


    public GameState gameState = GameState.Preparing;

    public int credits = 0;
    public int totalSupply = 0;
    public int supplyCurrent = 0;

    public int numBarracksActive = 0;

    public Text creditText;
    public Text supplyText;
    public Text time;

    public bool doubleBounties = false;
    public bool buildingHealth = false;
    public bool resourceTrickle = false;
    public bool fasterTraining = false;
    public bool droidStronger = false;

    public float totalTimeRemaining;
    public float totalTimeElapsed;
    bool minutePassed = false;
    bool thirtyPassed = false;
    bool tenPassed = false;
    bool startTenPassed = false;
    bool startFivePassed = false;
    bool startOnePassed = false;

    //tick timestep
    int fixedTimeStep = 0;
    //tick delay
    int tickDelay = 50;

    // Start is called before the first frame update
    void Start()
    {
        credits = 1600;
        totalTimeElapsed = 0;
        if (Networking.NetworkManager.allUsers != null)
        {
            totalTimeRemaining = 180 + (Networking.NetworkManager.allUsers.Count * 30);
        }
        else {
            totalTimeRemaining = 180;
        }

        minutePassed = false;
        thirtyPassed = false;
        tenPassed = false;
        startTenPassed = false;
        startFivePassed = false;
        startOnePassed = false;
    }

    private void Update()
    {
        if (GameSceneController.Instance.gameStart) { gameState = GameState.Running; }
        if (gameState == GameState.Running)
        {
            totalTimeRemaining -= Time.deltaTime;
            totalTimeElapsed += Time.deltaTime;
            time.text = ((int)(totalTimeRemaining / 60.0f)).ToString("00") + ":" + ((int)(totalTimeRemaining % 60)).ToString("00");

            
            if (!startOnePassed) {
                NotificationManager.Instance.HitNotification(NotificationType.START_TEN);
                startOnePassed = true;
            }
            else if (totalTimeElapsed > 5 && !startFivePassed)
            {
                NotificationManager.Instance.HitNotification(NotificationType.START_FIVE);
                startFivePassed = true;
            }
            else if (totalTimeElapsed > 10 && !startTenPassed)
            {
                NotificationManager.Instance.HitNotification(NotificationType.START);
                startTenPassed = true;
            }
            

            if (!minutePassed && totalTimeRemaining < 60) {
                NotificationManager.Instance.HitNotification(NotificationType.MINUTE_MARK);
                minutePassed = true;
            }
            else if (!thirtyPassed && totalTimeRemaining < 30)
            {
                NotificationManager.Instance.HitNotification(NotificationType.THIRTY_MARK);
                thirtyPassed = true;
            }
            else if (!tenPassed && totalTimeRemaining < 10)
            {
                NotificationManager.Instance.HitNotification(NotificationType.TEN_MARK);
                tenPassed = true;
            }else if (totalTimeRemaining < 0)
            {
                //RTSGameManager.Instance.GameEndWin();
                Networking.NetworkManager.EndGame();
            }
        }
    }
    
    // Update is called once per frame
    void FixedUpdate()
    {
        if (gameState == GameState.Running)
        {
            //NetworkManager.SendGameData((int)gameState, timeElapsed);

            #region Fixed Tick
            ++fixedTimeStep;

            if (fixedTimeStep >= tickDelay)
            {
                TickUpdate();
                fixedTimeStep = 0;
            }
            #endregion



        }
        creditText.text = credits.ToString();
        supplyText.text = supplyCurrent.ToString() + "/" + totalSupply.ToString();

    }

    void TickUpdate()
    {

        if (ResourceConstants.CREDITS_OFF)
        {
            credits = 99999;
        }
        else
        {
            if (Networking.NetworkManager.allUsers != null)
            {
                credits += 50 + (Networking.NetworkManager.allUsers.Count * 20);
            }
            else
            {
                credits += 50;
            }

            if (resourceTrickle)
            {
                credits += 50;
            }
        }

    }


    public bool Purchase(EntityType type)
    {
        switch (type)
        {
            case EntityType.Barracks:
                if (credits >= ResourceConstants.COST_BARRACKS)
                {
                    credits -= ResourceConstants.COST_BARRACKS;
                    return true;
                }
                return false;
                break;
            case EntityType.Droid:
                if (credits >= ResourceConstants.COST_DROIDS)
                {
                    credits -= ResourceConstants.COST_DROIDS;
                    return true;
                }
                return false;
                break;
            case EntityType.Turret:
                if (credits >= ResourceConstants.COST_TURRET)
                {
                    credits -= ResourceConstants.COST_TURRET;
                    return true;
                }
                return false;
                break;
            case EntityType.Wall:
                if (credits >= ResourceConstants.COST_WALL)
                {
                    credits -= ResourceConstants.COST_WALL;
                    return true;
                }
                return false;
                break;
            case EntityType.Science:
                if (credits >= ResourceConstants.COST_SCIENCE)
                {
                    credits -= ResourceConstants.COST_SCIENCE;
                    return true;
                }
                return false;
                break;

            default:
                Debug.Log("PURCHACE ERROR");
                return false;
        }
    }

    public void Refund(EntityType type)
    {
        switch (type)
        {
            case EntityType.Barracks:
                credits += ResourceConstants.COST_BARRACKS;
                break;
            case EntityType.Droid:
                credits += ResourceConstants.COST_DROIDS;
                supplyCurrent--;
                break;
            case EntityType.Turret:
                credits += ResourceConstants.COST_TURRET;
                break;
            case EntityType.Wall:
                credits += ResourceConstants.COST_WALL;
                break;
            case EntityType.Science:
                credits += ResourceConstants.COST_SCIENCE;
                break;
            default:
                Debug.Log("REFUND ERROR");
                break;
        }
    }
    public void RefundHalf(EntityType type)
    {
        switch (type)
        {
            case EntityType.Barracks:
                credits += ResourceConstants.COST_BARRACKS/2;
                break;
            case EntityType.Droid:
                credits += ResourceConstants.COST_DROIDS/2;
                break;
            case EntityType.Turret:
                credits += ResourceConstants.COST_TURRET/2;
                break;
            case EntityType.Wall:
                credits += ResourceConstants.COST_WALL/2;
                break;
            case EntityType.Science:
                credits += ResourceConstants.COST_SCIENCE/2;
                break;
            default:
                Debug.Log("REFUND ERROR");
                break;
        }
    }

    //requests a drone to build, returns time to build
    public float RequestQueue(EntityType type)
    {
        switch (type)
        {
            case EntityType.Droid:
                ResourceManager.Instance.supplyCurrent++;
                if (fasterTraining)
                {
                    return 3f;
                }
                else {
                    return 5f;
                }
            default:
                Debug.Log("ERROR: DROID TYPE INVALID");
                return -1f;
        }
    }

    public bool RequestResearch(int num) {

        switch (num)
        {
            case 0:
                if (credits > ResourceConstants.COST_RES1)
                {
                    credits -= ResourceConstants.COST_RES1;
                    return true;
                }
                else {
                    NotificationManager.Instance.HitNotification(NotificationType.INSUFFICIENT_CREDITS);
                    return false;
                }
                break;
            case 1:
                if (credits > ResourceConstants.COST_RES2)
                {
                    credits -= ResourceConstants.COST_RES2;
                    return true;
                }
                else
                {
                    NotificationManager.Instance.HitNotification(NotificationType.INSUFFICIENT_CREDITS);
                    return false;
                }
                break;
            case 2:
                if (credits > ResourceConstants.COST_RES3)
                {
                    credits -= ResourceConstants.COST_RES3;
                    return true;
                }
                else
                {
                    NotificationManager.Instance.HitNotification(NotificationType.INSUFFICIENT_CREDITS);
                    return false;
                }
                break;
            case 3:
                if (credits > ResourceConstants.COST_RES4)
                {
                    credits -= ResourceConstants.COST_RES4;
                    return true;
                }
                else
                {
                    NotificationManager.Instance.HitNotification(NotificationType.INSUFFICIENT_CREDITS);
                    return false;
                }
                break;
            case 4:
                if (credits > ResourceConstants.COST_RES5)
                {
                    credits -= ResourceConstants.COST_RES5;
                    return true;
                }
                else
                {
                    NotificationManager.Instance.HitNotification(NotificationType.INSUFFICIENT_CREDITS);
                    return false;
                }
                break;
            case 5:
                Debug.Log("FOG NOT YET IMPLEMENTED");
                return false;

                break;
            default:
                Debug.LogWarning("Unrecognized Research");
                return false;
                break;
        }
    }


    //called when drone is requested to be built
    public void QueueFinished(Transform home, EntityType type)
    {
        if (home.gameObject.activeSelf)
        {
            switch (type)
            {
                case EntityType.Droid:
                    Droid temp = (Droid)EntityManager.Instance.GetNewEntity(EntityType.Droid);
                    temp.gameObject.transform.position = home.position;

                    Networking.NetworkManager.SendPacketBuild(
                        temp.id, (int)temp.type,
                        new Vector3(
                            temp.transform.position.x,
                            temp.transform.position.y,
                            temp.transform.position.z),
                        temp.deaths);

                    break;
                default:
                    Debug.Log("ERROR: DROID TYPE INVALID");
                    break;
            }
        }
    }


    //called when drone is requested to be built, with a rally
    public void QueueFinished(Transform home, EntityType type, Vector3 rally)
    {
        if (home.gameObject.activeSelf)
        {

            switch (type)
            {
                case EntityType.Droid:
                    Droid temp = (Droid)EntityManager.Instance.GetNewEntity(EntityType.Droid);
                    temp.gameObject.transform.position = home.position;
                    temp.IssueLocation(rally);
                    if (GameSceneController.Instance.type == PlayerType.RTS)
                    {
                        Networking.NetworkManager.SendPacketBuild(
                        temp.id, (int)temp.type,
                        new Vector3(
                            temp.transform.position.x,
                            temp.transform.position.y,
                            temp.transform.position.z),
                        temp.deaths);
                    }
                    break;
                default:
                    Debug.Log("ERROR: DROID TYPE INVALID");
                    break;
            }
        }
    }

    public void KilledPlayer() {
        //double player killed money
        if (doubleBounties) {
            credits += ResourceConstants.PLAYERKILLEDMONEY;
        }
        credits += ResourceConstants.PLAYERKILLEDMONEY;
    }

    public void UpdateSupply()
    {
        totalSupply = numBarracksActive * ResourceConstants.SUPPLY_PER_BARRACKS;
        if (totalSupply > ResourceConstants.SUPPLY_MAX)
        {
            totalSupply = ResourceConstants.SUPPLY_MAX;
        }
    }

}
