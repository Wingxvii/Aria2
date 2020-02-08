using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using RTSInput;

public class ActionMenuButton : MonoBehaviour
{
    public ActionButton buttonInfo;

    private Button button;

    private void Start() {
        button = GetComponent<Button>();
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
