using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Netcode;
public class EntityManager : MonoBehaviour
{
    #region SingletonCode
    private static EntityManager _instance;
    public static EntityManager Instance { get { return _instance; } }
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

    //list of all entities;
    public List<Entity> AllEntities;

    //unsorted entities; Managed interally;
    public List<List<Entity>> ActiveEntitiesByType;
    public List<Queue<Entity>> DeactivatedEntitiesByType;

    //layermasks
    public LayerMask staticsMask;
    public LayerMask entitysMask;

    //prefabs
    public GameObject droidPrefab;
    public GameObject playerPrefab;
    public GameObject wallPrefab;
    public GameObject barracksPrefab;
    public GameObject turretPrefab;
    public GameObject controllablePlayerPrefab;
    public GameObject sciencePrefab;

    //managers
    public GameObject FPSManagers;
    public GameObject RTSManagers;
    public GameObject Debugger;

    private void Start()
    {
        //add managers
        if (GameSceneController.Instance.type == PlayerType.FPS)
        {
            if (!Netcode.NetworkManager.isConnected)
            {
                GameSceneController.Instance.playerNumber = 1;
            }
            Destroy(RTSManagers);
        }
        else if (GameSceneController.Instance.type == PlayerType.RTS) {
            if (!Netcode.NetworkManager.isConnected)
            {
                GameSceneController.Instance.playerNumber = 0;
            }
            Destroy(FPSManagers); 

        }


        //create all lists
        ActiveEntitiesByType = new List<List<Entity>>();
        DeactivatedEntitiesByType = new List<Queue<Entity>>();

        //create a list per type
        for (int counter = 0; counter < (int)EntityType.TOTAL; counter++) {
            ActiveEntitiesByType.Add(new List<Entity>());
            DeactivatedEntitiesByType.Add(new Queue<Entity>());
        }

        if (AllEntities.Count > 0)
        {
            foreach (Entity entity in AllEntities)
            {
                if (entity&&entity.gameObject.activeSelf)
                {
                    ActiveEntitiesByType[(int)entity.type].Add(entity);
                }
            }
        }


        //create Layermasks
        staticsMask = LayerMask.GetMask("Buildable");
        staticsMask += LayerMask.GetMask("StaticsMask");
        staticsMask += LayerMask.GetMask("Ground");

        entitysMask = LayerMask.GetMask("Wall");
        entitysMask += LayerMask.GetMask("Player");
        entitysMask += LayerMask.GetMask("Barracks");
        entitysMask += LayerMask.GetMask("Turret");
        entitysMask += LayerMask.GetMask("Droid");
        entitysMask += LayerMask.GetMask("Science");

        //spawn players for debugging
        EntityType[] spawnType = new EntityType[3];
        Entity temp;
        SpawnManager.Instance.Initialize();

        for (int i = 0; i < spawnType.Length; ++i)
        {
            spawnType[i] = (GameSceneController.Instance.playerNumber == i + 1) ? EntityType.Player : EntityType.Dummy;
            temp = GetNewEntity(spawnType[i]);

            //set spawn
            int spawnPointNum = SpawnManager.Instance.freeSpawnPoints.Dequeue();
            temp.transform.position = SpawnManager.Instance.FPSspawnpoints[spawnPointNum].position;
            temp.transform.rotation = Quaternion.identity;



            //temp.transform.position = new Vector3(-10f, 0.5f, -10f);
            //AllEntities.Add(temp);
            //ActiveEntitiesByType[(int)EntityType.Player].Add(temp);
        }

        //tell network that user is done loading
        NetworkManager.OnLoaded();
    }

    private void FixedUpdate()
    {
        if (ActiveEntitiesByType[(int)EntityType.Turret].Count + ActiveEntitiesByType[(int)EntityType.Droid].Count > 0
            && GameSceneController.Instance.type == PlayerType.RTS)
            NetworkManager.SendPacketEntities();
    }

    //returns an avaliable entity from pool or newly instantiated, if none are avaliable
    public Entity GetNewEntity(EntityType type) {
        if (DeactivatedEntitiesByType[(int)type].Count != 0)
        {
            Entity returnEntity = DeactivatedEntitiesByType[(int)type].Dequeue();
            returnEntity.OnActivate();

            return returnEntity;
        }
        else
        {
            Entity returnEntity = CreateEntity(type);

            return returnEntity;
        }
    }

    public Entity EntitySwitch(EntityType type)
    {
        Entity returnValue;

        switch (type)
        {
            case EntityType.Barracks:
                returnValue = Instantiate(barracksPrefab).GetComponent<Entity>();

                break;
            case EntityType.Droid:
                returnValue = Instantiate(droidPrefab).GetComponent<Entity>();

                break;
            case EntityType.Player:
                returnValue = Instantiate(controllablePlayerPrefab).GetComponent<Entity>();

                break;
            case EntityType.Dummy:
                returnValue = Instantiate(playerPrefab).GetComponent<Entity>();

                break;
            case EntityType.Turret:
                returnValue = Instantiate(turretPrefab).GetComponent<Entity>();

                break;
            case EntityType.Wall:
                returnValue = Instantiate(wallPrefab).GetComponent<Entity>();

                break;
            case EntityType.Science:
                returnValue = Instantiate(sciencePrefab).GetComponent<Entity>();

                break;
            default:
                returnValue = Instantiate(wallPrefab).GetComponent<Entity>();
                Debug.LogWarning("Tried to create invalid entity type, creating wall entity instead");

                break;
        }

        return returnValue;
    }

    public Entity GetEntityAt(EntityType type, int entityID)
    {
        Entity returnEntity;
        if (entityID < AllEntities.Count)
        {
            if (AllEntities[entityID] == null)
            {
                AllEntities[entityID] = EntitySwitch(type);
            }

            returnEntity = AllEntities[entityID];
            returnEntity.OnActivate();
            return returnEntity;
        }
        else
        {
            returnEntity = EntitySwitch(type);

            while (AllEntities.Count <= entityID)
            {
                AllEntities.Add(null);
            }

            AllEntities.Add(returnEntity);
        }

        returnEntity.id = entityID;
        return returnEntity;
    }

    //deactivates an entity: DO NOT USE
    //@Entity OnDeActivate()
    public void DeactivateEntity(EntityType type, Entity entity) {
        if (GameSceneController.Instance.type == PlayerType.RTS)
        {
            ActiveEntitiesByType[(int)type].Remove(entity);
            DeactivatedEntitiesByType[(int)type].Enqueue(entity);
        }
    }

    //initalize new entity with overloads
    public Entity InitNewEntity(EntityType type, Vector3 position) 
    {
        return InitNewEntity(type, position, Quaternion.identity);
    }
    public Entity InitNewEntity(EntityType type, Vector3 position, Vector3 rotation)
    {
        return InitNewEntity(type, position, Quaternion.Euler(rotation.x, rotation.y, rotation.z)); ;
    }
    public Entity InitNewEntity(EntityType type, Vector3 position, Quaternion rotation)
    {
        Entity returnEntity = GetNewEntity(type);
        returnEntity.transform.position = position;
        returnEntity.transform.rotation = rotation;

        return returnEntity;
    }

    //factory for entity creation
    private Entity CreateEntity(EntityType type) {
        Entity returnValue;

        switch (type) {
            case EntityType.Barracks:
                returnValue = Instantiate(barracksPrefab).GetComponent<Entity>();

                break;
            case EntityType.Droid:
                returnValue = Instantiate(droidPrefab).GetComponent<Entity>();

                break;
            case EntityType.Player:
                returnValue = Instantiate(controllablePlayerPrefab).GetComponent<Entity>();

                break;
            case EntityType.Dummy:
                returnValue = Instantiate(playerPrefab).GetComponent<Entity>();

                break;
            case EntityType.Turret:
                returnValue = Instantiate(turretPrefab).GetComponent<Entity>();

                break;
            case EntityType.Wall:
                returnValue = Instantiate(wallPrefab).GetComponent<Entity>();

                break;
            case EntityType.Science:
                returnValue = Instantiate(sciencePrefab).GetComponent<Entity>();

                break;
            default:
                returnValue = Instantiate(wallPrefab).GetComponent<Entity>();
                Debug.LogWarning("Tried to create invalid entity type, creating wall entity instead");

                break;
        }

        AllEntities.Add(returnValue);
        ActiveEntitiesByType[(int)type].Add(returnValue);

        return returnValue;
    }

    //simple getters
    public List<Entity> ActivePlayers() {
        return ActiveEntitiesByType[(int)EntityType.Dummy];
    }
}
