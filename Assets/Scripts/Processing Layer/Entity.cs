using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum EntityType
{
    None,
    Wall,
    Barracks,
    Droid,
    Turret,
    Player,
    
    
    
    TOTAL,
}

public class Entity : MonoBehaviour
{

    //ID
    public int id;
    private static int idtracker = 0;
    private static List<Entity> indexedList = new List<Entity>();

    //attributes
    public EntityType type;
    public int currentHealth = 1;
    protected int maxHealth = 1;
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

    private void Awake()
    {
        //init all ui elements
        selectedHalo = (Behaviour)this.GetComponent("Halo");
        selectedHalo.enabled = false;
        canvasTransform = this.transform.Find("Canvas").GetComponent<RectTransform>();

        if (GameController.Instance.type == PlayerType.RTS)
        {
            //clean up unwanted items

        }
        else if (GameController.Instance.type == PlayerType.FPS) {
            //clean up unwanted items

            Destroy(selectedHalo);
            Destroy(canvasTransform);


        }
        //set id
        id = ++idtracker;
        indexedList.Add(this);
        BaseAwake();
    }

    //called on activate
    private void Start()
    {
        //ensure values are reset on start
        ResetValues();
        BaseStart();
    }
    //updates
    private void Update()
    {
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

        this.gameObject.SetActive(false);
    }
    //death of unit
    public virtual void OnDeath()
    {
        //deactivate
        OnDeActivate();
    }

    protected virtual void BaseAwake() { }
    protected virtual void BaseStart() { }
    protected virtual void BaseEnable() { }
    protected virtual void BaseUpdate() { }
    protected virtual void BaseLateUpdate() { }
    protected virtual void BaseFixedUpdate() { }
    protected virtual void BaseOnDestory() { }

    public virtual void IssueLocation(Vector3 location) { }
    public virtual void BaseActivation() { }
    public virtual void BaseDeactivation() { }

    public virtual void BaseSelected() { }
    public virtual void BaseDeselected() { }


}