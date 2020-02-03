using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Netcode;
using System;
using System.Text;

public enum EntityType
{
    None,
    Wall,
    Barracks,
    Droid,
    Turret,
    Player,
    Dummy,
    
    
    TOTAL,
}

public abstract class Entity : MonoBehaviour
{

    //ID
    public int id;
    private static int idtracker = 0;
    private static List<Entity> indexedList = new List<Entity>();

    //attributes
    public EntityType type;
    public int currentHealth = 1;
    public int maxHealth = 1;
    public bool destructable = false;

    //RTS BEHAVIOURS
    #region RTS
    private bool isRTS = false;

    //canvas
    private Slider healthBar;
    private RectTransform canvasTransform;
    //selection
    private Behaviour selectedHalo;
    protected bool selected = false;
    #endregion

    private void Start()
    {
        

        if (GameSceneController.Instance.type == PlayerType.RTS)
        {
            //clean up unwanted items
            isRTS = true;

            //init all ui elements
            selectedHalo = (Behaviour)this.GetComponent("Halo");
            selectedHalo.enabled = false;
            canvasTransform = this.transform.Find("Canvas").GetComponent<RectTransform>();
            healthBar = canvasTransform.transform.Find("Health").GetComponent<Slider>();
        }
        else if (GameSceneController.Instance.type == PlayerType.FPS) {
            //clean up unwanted items

        }
        //set id
        id = ++idtracker;
        indexedList.Add(this);
        BaseStart();
    }

    //updates
    private void Update()
    {
        if (isRTS)
            healthBar.value = (float)currentHealth / (float)maxHealth;
        BaseUpdate();
    }
    private void FixedUpdate()
    {
        BaseFixedUpdate();
    }

    private void LateUpdate()
    {
        if (isRTS)
        {
            canvasTransform.eulerAngles = new Vector3(90, 0, 0);
        }
        BaseLateUpdate();
    }




    //selection by RTS player
    public void OnSelect()
    {
        if (isRTS)
        {
            selected = true;
            selectedHalo.enabled = true;
        }
        else {
            Debug.LogError("Selection is disabled");
        }
        BaseSelected();
    }
    //selection by FPS player
    public void OnDeselect()
    {
        BaseDeselected();
        if (isRTS)
        {

            selected = false;
            selectedHalo.enabled = false;
        }
        else {
            Debug.LogError("Selection is disabled");
        }
    }

    //do not use this outside of entity manager
    public virtual void OnActivate()
    {
        this.gameObject.SetActive(true);
        BaseActivation();
    }
    //managed method for deactivation
    public virtual void OnDeActivate()
    {
        BaseDeactivation();
        EntityManager.Instance.DeactivateEntity(type, this);
        ResetValues();
        OnDeselect();
        this.gameObject.SetActive(false);
    }


    //deals damage to entity
    public virtual void OnDamage(int num)
    {
        if (destructable)
        {
            currentHealth -= num;
        }
        if (currentHealth <= 0)
        {
            OnDeath();
        }
    }
    public virtual void OnDamage(int num, Entity culprit)
    {
        if (destructable)
        {
            currentHealth -= num;
        }
        if (currentHealth <= 0)
        {
            OnDeath();
        }
    }


    //resets all values
    public virtual void ResetValues()
    {
        if (isRTS)
        {
            OnDeselect();
        }

        this.gameObject.transform.position = Vector3.zero;
        this.gameObject.transform.rotation = Quaternion.identity;
        this.currentHealth = maxHealth;
    }
    //death of unit
    public virtual void OnDeath()
    {
        //deactivate
        NetworkManager.SendKilledEntity(this);
        OnDeActivate();
    }
    
    protected virtual void BaseAwake() {}
    protected virtual void BaseStart() {}
    protected virtual void BaseEnable() {}
    protected virtual void BaseUpdate() {}
    protected virtual void BaseLateUpdate() {}
    protected virtual void BaseFixedUpdate() {}
    protected virtual void BaseOnDestory() {}

    public virtual void IssueLocation(Vector3 location) { Debug.LogWarning("BASE FUNCTION USED ON ENTITY:" + id.ToString()); }
    public virtual void IssueAttack(Vector3 location) { Debug.LogWarning("BASE FUNCTION USED ON ENTITY:" + id.ToString()); }
    public virtual void IssueAttack(Entity attackee) { Debug.LogWarning("BASE FUNCTION USED ON ENTITY:" + id.ToString()); }
    public virtual void CallAction(int action) { Debug.LogWarning("BASE FUNCTION USED ON ENTITY:" + id.ToString()); }

    public virtual void BaseActivation() {}
    public virtual void BaseDeactivation() { }

    public virtual void BaseSelected() {}
    public virtual void BaseDeselected() { }


    public virtual void GetEntityString(ref StringBuilder dataToSend) {  }

    public virtual void UpdateEntityStats(EntityData ed) { }
}