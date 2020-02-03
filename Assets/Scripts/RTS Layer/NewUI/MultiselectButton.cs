using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class MultiselectButton : MonoBehaviour {

    public Button button;
    public Entity linkedObject = null;

    private void Start()
    {
        button = GetComponent<Button>();
    }

    public void ChangeButton(Sprite _sprite, Entity _linkedObject) {
        button.image.sprite = _sprite;
        linkedObject = _linkedObject;
    }

    public void OnClick() {
        if (!linkedObject) {
            Debug.LogWarning("No linked object for multiselect button!");
            return;
        }
        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.LeftControl)) {
            RTSInput.InputManager.Instance.DeselectItem(linkedObject);
        } else {
            RTSInput.InputManager.Instance.OnFocusSelected(linkedObject);
        }
    }
}
