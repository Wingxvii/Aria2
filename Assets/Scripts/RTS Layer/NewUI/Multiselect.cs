using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

public class Multiselect : MonoBehaviour
{
    public GameObject template;
    public GameObject content;

    [Serializable]
    public struct UnitIcon {
        public EntityType type;
        public Sprite sprite;
    }
    public UnitIcon[] unitIcons;

    public List<MultiselectButton> buttons;

    private void Start()
    {
        
    }

    private void Update()
    {
        
    }

    public void UpdateSelection() {
        foreach (MultiselectButton btn in buttons) btn.gameObject.SetActive(false);
        for (int i = 0; i < RTSInput.InputManager.Instance.SelectedEntities.Count; ++i) {
            Entity entity = RTSInput.InputManager.Instance.SelectedEntities[i];
            foreach (UnitIcon icon in unitIcons) {
                if (entity.type == icon.type) {
                    SetButton(i, icon.sprite, entity);
                    break;
                }
            }
        }
    }

    public void SetButton(int i, Sprite _sprite, Entity linkedEntity) {
        while (i>=buttons.Count) {
            GameObject go = Instantiate<GameObject>(template);
            go.SetActive(true);
            MultiselectButton mb = go.GetComponent<MultiselectButton>();
            go.transform.parent = content.transform;
            buttons.Add(mb);
        }
        buttons[i].gameObject.SetActive(true);
        buttons[i].button.image.sprite = _sprite;
        buttons[i].linkedObject = linkedEntity;
    }
}
