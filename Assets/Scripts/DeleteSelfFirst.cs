using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-201)]
public class DeleteSelfFirst : MonoBehaviour
{
    // Start is called before the first frame update
    void Awake()
    {
        Debug.Log("First");
        if (GameSceneController.Instance.type == PlayerType.RTS) { this.gameObject.SetActive(false); }
    }

}
