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

    //managers
    public GameObject FPSManagers;
    public GameObject RTSManagers;
    public GameObject Debugger;
    private void Start()
    {
        GameSceneController.Instance.gameStart = true;
        //Debug.Log("HELLO");
        //add managers
        if (GameSceneController.Instance.type == PlayerType.FPS)
        {
            if (!Netcode.NetworkManager.isConnected)
                //Debug.Log("PROBLEM!");
                GameSceneController.Instance.playerNumber = 1;
            Destroy(RTSManagers);
        }
        else if (GameSceneController.Instance.type == PlayerType.RTS) {
            Destroy(FPSManagers); 

        }



        SpawnManager.Instance.Initialize();

        //create all lists
        AllEntities = new List<Entity>();
        ActiveEntitiesByType = new List<List<Entity>>();
        DeactivatedEntitiesByType = new List<Queue<Entity>>();

        if (AllEntities.Count > 0)
        {
            foreach (Entity entity in AllEntities)
            {
                if (entity.gameObject.activeSelf)
                {
                    ActiveEntitiesByType[(int)entity.type].Add(entity);
                }
            }
        }


        //create a list per type
        for (int counter = 0; counter < (int)EntityType.TOTAL; counter++) {
            ActiveEntitiesByType.Add(new List<Entity>());
            DeactivatedEntitiesByType.Add(new Queue<Entity>());
        }

        //create Layermasks
        staticsMask = LayerMask.GetMask("Background");
        staticsMask += LayerMask.GetMask("StaticsMask");

        entitysMask = LayerMask.GetMask("Wall");
        entitysMask += LayerMask.GetMask("Player");
        entitysMask += LayerMask.GetMask("Barracks");
        entitysMask += LayerMask.GetMask("Turret");
        entitysMask += LayerMask.GetMask("Droid");

        //spawn players for debugging
        EntityType[] spawnType = new EntityType[3];
        Entity temp;

        for (int i = 0; i < spawnType.Length; ++i)
        {
            spawnType[i] = (GameSceneController.Instance.playerNumber == i + 1) ? EntityType.Player : EntityType.Dummy;

            temp = GetNewEntity(spawnType[i]);

            //temp.transform.position = new Vector3(-10f, 0.5f, -10f);
            AllEntities.Add(temp);
            ActiveEntitiesByType[(int)EntityType.Player].Add(temp);
        }

        //Entity temp = GetNewEntity(EntityType.Player);
        //
        //temp.transform.position = new Vector3(-10f, 0.5f, -10f);
        //AllEntities.Add(temp);
        //ActiveEntitiesByType[(int)EntityType.Player].Add(temp);
        //
        //temp = GetNewEntity(EntityType.Player);
        //temp.transform.position = new Vector3(-15f, 0.5f, -10f);
        //AllEntities.Add(temp);
        //ActiveEntitiesByType[(int)EntityType.Player].Add(temp);
        //
        //temp = GetNewEntity(EntityType.Player);
        //temp.transform.position = new Vector3(-20f, 0.5f, -10f);
        //AllEntities.Add(temp);
        //ActiveEntitiesByType[(int)EntityType.Player].Add(temp);




    }

    //returns an avaliable entity from pool or newly instantiated, if none are avaliable
    public Entity GetNewEntity(EntityType type) {

        if (DeactivatedEntitiesByType[(int)type].Count != 0)
        {
            Entity returnEntity = DeactivatedEntitiesByType[(int)type].Dequeue();
            returnEntity.OnActivate();
            
            NetworkManager.SendBuildEntity(returnEntity);

            return returnEntity;
        }
        else {
            Entity returnEntity = CreateEntity(type);

            if (type != EntityType.Player && type != EntityType.Dummy)
                NetworkManager.SendBuildEntity(returnEntity);

            return returnEntity;
        }
    }
    //deactivates an entity: DO NOT USE
    //@Entity OnDeActivate()
    public void DeactivateEntity(EntityType type, Entity entity) {
        ActiveEntitiesByType[(int)type].Remove(entity);
        DeactivatedEntitiesByType[(int)type].Enqueue(entity);
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
        return ActiveEntitiesByType[(int)EntityType.Player];
    }


}
