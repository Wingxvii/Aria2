using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class HoverHint : MonoBehaviour
{
    public HoverHintInfo m_hintInfo = null;

    public Text m_title = null;
    public Text m_description = null;

    public Image m_trigger = null;

    private void Awake() {
        if (m_trigger) {
            EventTrigger trigger = m_trigger.gameObject.AddComponent<EventTrigger>();
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
    }

    void BeginHover(BaseEventData e) {
        
    }

    void EndHover(BaseEventData e) {
        
    }

    private void Update() {
        if (m_hintInfo==null) {
            Debug.LogWarning("Hover-Hint info is undefined!");
            return;
        }
        if (m_title==null) {
            Debug.LogWarning("Hover-Hint title is undefined!");
            return;
        }
        if (m_description==null) {
            Debug.LogWarning("Hover-Hint description is undefined!");
            return;
        }

        m_title.text = m_hintInfo.title;
        m_title.color = m_hintInfo.titleColor;
        m_description.supportRichText = true;
        m_description.text = m_hintInfo.title;
    }
}
