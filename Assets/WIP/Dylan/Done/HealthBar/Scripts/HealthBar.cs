using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    public Entity observedEntity = null;

    public Image fill=null;

    private void Update()
    {
        if (fill) {
            if (observedEntity)
                fill.fillAmount = ((float)observedEntity.currentHealth) / ((float)observedEntity.maxHealth);
        } else {
            Debug.LogWarning("[HealthBar] Fill object not specified!");
        }
    }
}
