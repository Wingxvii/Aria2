using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TrainingProgress : MonoBehaviour
{
    public Barracks linkedBarracks = null;

    public Image fill;

    private void Update() {
        if (fill) {
            if (linkedBarracks) {
                fill.fillAmount = linkedBarracks.buildProcess.value;
            }
        } else {
            Debug.LogWarning("[TrainingProgress] Fill object not specified!");
        }
    }
}
