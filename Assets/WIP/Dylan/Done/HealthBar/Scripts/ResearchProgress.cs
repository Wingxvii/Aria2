using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ResearchProgress : MonoBehaviour
{
    public Image fill;

    public void SetValue(float val) {
        if (fill) {
            fill.fillAmount = val;
        } else {
            Debug.LogWarning("[TrainingProgress] Fill object not specified!");
        }
    }

    public void SetImage(Sprite image) {
        this.GetComponent<Image>().sprite = image;
    }
}
