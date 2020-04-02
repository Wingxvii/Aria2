using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Networking;
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
    Science,
    
    TOTAL,
}

public abstract class Entity : MonoBehaviour
{
    public static float SnapThreshold = 1f;
    //public byte changed { get; set; } = 0;
    public static float posThreshold = 0.05f;
    public static float rotThreshold = 0.01f;
    public Vector3 previousPosition { get; set; }
    public Vector3 previousRotation { get; set; }

    public Vector3 lerpTarg { get; set; }

    //ID
    public int deaths = 0;
    public int id;
    public int killerID;
    private static int idtracker = 0;
    private static List<Entity> indexedList = new List<Entity>();

    //attributes
    public EntityType type;
    public float currentHealth = 1f;
    public int maxHealth = 1;
    public bool destructable = false;
    public bool ready = true;

    EntityData lastNetworked;

    //canvas
    private Slider healthBar;
    private RectTransform canvasTransform;

    //RTS BEHAVIOURS
    #region RTS
    private bool isRTS = false;

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
        }

        canvasTransform = this.transform.Find("Canvas").GetComponent<RectTransform>();
        healthBar = canvasTransform.transform.Find("Health").GetComponent<Slider>();

        //set id
        //id = idtracker++;
        //indexedList.Add(this);
        BaseStart();
    }

    //updates
    private void Update()
    {
        if (ready)
        {
            healthBar.value = (float)currentHealth / (float)maxHealth;
            BaseUpdate();
        }
    }
    private void FixedUpdate()
    {
        if (ready)
        {
            BaseFixedUpdate();
        }
    }

    private void LateUpdate()
    {
        if (ready)
        {
            if (isRTS)
            {
                canvasTransform.eulerAngles = new Vector3(90, 90, 0);
            }
            else {
                canvasTransform.eulerAngles = new Vector3(0, 0, 0);
            }
            BaseLateUpdate();
        }
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
    public virtual void OnDamage(float num, int kID, int entityLife)
    {
        if (deaths == entityLife)
        {
            Debug.Log(type);
            if (destructable)
            {
                currentHealth -= num;
            }
            if (currentHealth <= 0)
            {
                killerID = kID;
                OnDeath(true);
            }//
        }
    }

    public virtual void OnOtherDamage(float num, int kID, int entityLife)
    {
        currentHealth -= num;
        currentHealth = Mathf.Max(currentHealth, num);
    }

    public virtual void OnDamage(int num, Entity culprit)
    {
        //if (destructable)
        //{
        //    currentHealth -= num;
        //}
        //if (currentHealth <= 0 && GameSceneController.Instance.type == PlayerType.RTS)
        //{
        //    killerID = culprit.id;
        //    OnDeath();
        //}
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
    public virtual void OnDeath(bool networkData)
    {
		if (networkData)
			++deaths;
        //deactivate
        int killerID = 0; // NEED UPDATE @PROGRAMMER
        if (networkData)
            NetworkManager.SendPacketDeath(this.id, killerID);
        OnDeActivate();
    }

    private void Awake()
    {
        BaseAwake();
    }

    protected virtual void BaseAwake() {
        id = idtracker++;
        indexedList.Add(this);
    }
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
    public virtual void IssueBuild() { Debug.LogWarning("BASE FUNCTION USED ON ENTITY:" + id.ToString()); }
    public virtual void IncreaseBuildingHealth() { Debug.LogWarning("BASE FUNCTION USED ON ENTITY:" + id.ToString()); }
    
    public virtual void BaseActivation() {}
    public virtual void BaseDeactivation() { }

    public virtual void BaseSelected() {}
    public virtual void BaseDeselected() { }


    public virtual void GetEntityString(ref StringBuilder dataToSend) {  }

    public virtual void UpdateEntityStats(EntityData ed) {
        Debug.LogWarning("BASE FUNCTION USED ON ENTITY:" + id.ToString());
    }
}