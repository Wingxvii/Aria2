using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ActionQueue : MonoBehaviour
{
    public GameObject actionTemlpate;
    public Image progressFill;
    public Sprite emptySlotImage;
    public Sprite droidFilledImage;


    public Button currentActionSlot;

    public Button[] slots;
    private int maxActions = 6;

    private void Start()
    {
        //maxActions = Barracks.maxTrainingCap;
    }

    private void Update()
    {
        if (RTSInput.InputManager.Instance.SelectedEntities.Count > 0 && (RTSInput.InputManager.Instance.PrimaryEntity.type == EntityType.Barracks)) {
            Barracks barracks = ((Barracks)RTSInput.InputManager.Instance.PrimaryEntity);
            if (barracks.currentBuildTime > 0) progressFill.fillAmount = barracks.buildProcess.value;
            else progressFill.fillAmount = 0.0f;
            if (progressFill.fillAmount > 0) currentActionSlot.image.sprite = droidFilledImage;
            else currentActionSlot.image.sprite = emptySlotImage;
            for (int i = 0; i < maxActions; ++i) {
                slots[i].image.sprite = emptySlotImage;
            }
            for (int i = 0; i < barracks.buildTimes.Count; ++i) {
                slots[i].image.sprite = droidFilledImage;
            }
        }


    }
}
