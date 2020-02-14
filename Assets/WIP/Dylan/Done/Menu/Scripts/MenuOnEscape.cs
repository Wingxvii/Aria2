using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuOnEscape : MonoBehaviour
{
    public GameObject menu = null;
    void Update()
    {
        if (menu) {
            if (Input.GetKeyDown(KeyCode.Escape)) {
                menu.SetActive(!menu.activeInHierarchy);
            }
        } else {
            Debug.LogWarning("No menu specified!");
        }
    }
}
