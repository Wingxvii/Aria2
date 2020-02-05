using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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
                RTSInput.InputManager.Instance.OnBuildPrefabs(buttonInfo.argument);
                break;
            case ActionButton.ActionType.DO_TRAIN:
                RTSInput.InputManager.Instance.OnTrainBarracks();
                break;
            case ActionButton.ActionType.DO_ATTACK:
                RTSInput.InputManager.Instance.OnSelectAttack();
                break;
            case ActionButton.ActionType.DO_MOVE:
                RTSInput.InputManager.Instance.OnSelectMove();
                break;
            case ActionButton.ActionType.DO_RALLY:
                RTSInput.InputManager.Instance.OnRally();
                break;
            case ActionButton.ActionType.DO_RELOAD:
                RTSInput.InputManager.Instance.OnReload();
                break;
        }
    }
}
