using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using RTSInput;
using UnityEngine.EventSystems;
using UnityEngine.Events;

public class ActionMenuButton : MonoBehaviour
{
    public ActionButton buttonInfo;
    public GameObject hintPanelPrefab;
    private GameObject hintPanel;

    private Button button;


    private void Awake() {
        button = GetComponent<Button>();

        EventTrigger trigger = gameObject.AddComponent<EventTrigger>();
        EventTrigger.Entry onEnter = new EventTrigger.Entry();
        onEnter.eventID = EventTriggerType.PointerEnter;

        UnityAction<BaseEventData> beginHoverAction = new UnityAction<BaseEventData>(BeginHover);
        onEnter.callback.AddListener(beginHoverAction);

        EventTrigger.Entry onExit = new EventTrigger.Entry();
        onExit.eventID = EventTriggerType.PointerExit;

        UnityAction<BaseEventData> endHoverAction = new UnityAction<BaseEventData>(EndHover);
        onExit.callback.AddListener(endHoverAction);

        trigger.triggers.Add(onEnter);
        trigger.triggers.Add(onExit);
    }

    private void BeginHover(BaseEventData e) {
        if (buttonInfo && buttonInfo.hintInfo) {
            if (hintPanel==null) {
                hintPanel = Instantiate<GameObject>(hintPanelPrefab);
                hintPanel.transform.SetParent(transform);
            }
            hintPanel.SetActive(true);
        }
    }

    private void EndHover(BaseEventData e) {
        if (hintPanel)
            hintPanel.SetActive(false);
    }

    private void Update()
    {
        if (buttonInfo) {
            button.image.color = new Color(1, 1, 1, 1);
            button.image.sprite = buttonInfo.spriteToUse;
            button.image.raycastTarget = true;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnClick);

        } else {
            button.image.color = new Color(1, 1, 1, 0);
            button.image.raycastTarget = false;
            button.onClick.RemoveAllListeners();
        }
        if (hintPanel&&buttonInfo&&buttonInfo.hintInfo) {
            hintPanel.transform.localPosition = new Vector3(32, 32, 0);
            hintPanel.GetComponent<HoverHint>().m_hintInfo = buttonInfo.hintInfo;
        }
    }

    public void OnClick() {
        switch (buttonInfo.actionType) {
            case ActionButton.ActionType.BUILD:
                InputManager.Instance.OnBuildPrefabs(buttonInfo.argument);
                break;
            case ActionButton.ActionType.DO_TRAIN:
                InputManager.Instance.OnTrainBarracks();
                break;
            case ActionButton.ActionType.DO_ATTACK:
                InputManager.Instance.OnSelectAttack();
                break;
            case ActionButton.ActionType.DO_MOVE:
                InputManager.Instance.OnSelectMove();
                break;
            case ActionButton.ActionType.DO_RALLY:
                InputManager.Instance.OnRally();
                break;
            case ActionButton.ActionType.DO_RELOAD:
                InputManager.Instance.OnReload();
                break;
            case ActionButton.ActionType.DO_UPGRADE:
                ScienceManager.Instance.CallResearch(buttonInfo.argument);
                break;
        }
    }
}
