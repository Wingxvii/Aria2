﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    private void Start()
    {
        //create all lists
        AllEntities = new List<Entity>();
        ActiveEntitiesByType = new List<List<Entity>>();
        DeactivatedEntitiesByType = new List<Queue<Entity>>();


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
    }

    //returns an avaliable entity from pool or newly instantiated, if none are avaliable
    public Entity GetNewEntity(EntityType type) {
        if (DeactivatedEntitiesByType[(int)type].Count != 0)
        {
            Entity returnEntity = DeactivatedEntitiesByType[(int)type].Dequeue();
            returnEntity.OnActivate();
            return returnEntity;
        }
        else {
            return CreateEntity(type);
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
