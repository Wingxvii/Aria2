using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class RTSHUD : MonoBehaviour
{
    [Header("Optimisation")]
    public int actionMenuPoolSize=8;

    [Header("Build Menu")]
    public ActionButton[] buildMenuButtons;

    [Serializable]
    public struct UIMenu {
        public EntityType selectable;
        public UIInfo info;
    }
    [Header("Selectable Menus")]
    public UIMenu[] menus;


    [Header("UI Object References")]
    public GameObject actionQueue;
    public GameObject actionMenu;
    public Multiselect multiSelect;
    public Image unitPortrait;
    public Text unitName;
    public Text unitHealth;

    [Header("Prefab References")]
    public GameObject hintPrefab = null;

    private UIInfo currentUIInfo=null;

    private List<ActionMenuButton> actionButtonPool;

    private void Start()
    {
        actionButtonPool = new List<ActionMenuButton>();
        for (int i = 0; i < actionMenuPoolSize; ++i) {
            GameObject go = new GameObject("ActionButton");
            go.transform.SetParent(actionMenu.transform);
            Image img = go.AddComponent<Image>();
            Button btn = go.AddComponent<Button>();
            btn.image = img;
            ActionMenuButton amb = go.AddComponent<ActionMenuButton>();
            amb.hintPanelPrefab = hintPrefab;
            amb.buttonInfo = null;
            actionButtonPool.Add(amb);
        }
    }

    private void Update()
    {
        //Obtain the UIInfo for the current selectable.
        EntityType entityType = EntityType.None;
        if (RTSInput.InputManager.Instance.PrimaryEntity)
            entityType = RTSInput.InputManager.Instance.PrimaryEntity.type;
        if (entityType==EntityType.None) {
            currentUIInfo = null;
        }
        foreach (UIMenu menu in menus) {
            if (entityType==menu.selectable) {
                currentUIInfo = menu.info;
                break;
            }
        }


        //Adjust UI based on provided info.
        if (currentUIInfo) {
            unitName.gameObject.SetActive(true);
            unitName.text = currentUIInfo.name;

            //Multiselect if required.
            if (RTSInput.InputManager.Instance.SelectedEntities.Count > 1) {
                unitHealth.gameObject.SetActive(false);
                multiSelect.gameObject.SetActive(true);
                multiSelect.UpdateSelection();
                actionQueue.gameObject.SetActive(false);
            }
            //Otherwise, do single select
            else if (RTSInput.InputManager.Instance.SelectedEntities.Count>0){
                //Display Unit Health
                unitHealth.gameObject.SetActive(true);
                unitHealth.text = string.Format("Health: {0}/{1}", RTSInput.InputManager.Instance.SelectedEntities[0].currentHealth, RTSInput.InputManager.Instance.SelectedEntities[0].maxHealth);
                multiSelect.gameObject.SetActive(false);

                //Display ActionQueue if required
                if (currentUIInfo.useActionQueue) actionQueue.gameObject.SetActive(true);
                else actionQueue.gameObject.SetActive(false);

            }

            //Set the portrait based on the primary selection.
            unitPortrait.gameObject.SetActive(true);
            unitPortrait.sprite = currentUIInfo.unitPortrait;
            for (int i = 0; i < actionButtonPool.Count; ++i) {
                actionButtonPool[i].buttonInfo = null;
            }
            for (int i = 0; i < actionButtonPool.Count && i < currentUIInfo.actions.Length; ++i) {
                actionButtonPool[i].buttonInfo = currentUIInfo.actions[i];
            }
        } 
        //If no info is provided: do the build menu
        else {
            unitPortrait.gameObject.SetActive(false);
            multiSelect.gameObject.SetActive(false);
            unitName.gameObject.SetActive(false);
            unitHealth.gameObject.SetActive(false);
            actionQueue.gameObject.SetActive(false);


            //TODO: Add build buttons
            for (int i = 0; i < actionButtonPool.Count; ++i) {
                actionButtonPool[i].buttonInfo = null;
            }
            for (int i = 0; i < actionButtonPool.Count && i < buildMenuButtons.Length; ++i) {
                actionButtonPool[i].buttonInfo = buildMenuButtons[i];
            }
        }
    }
}
