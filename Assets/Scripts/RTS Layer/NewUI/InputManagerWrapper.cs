using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NewUI {

    public class InputManagerWrapper : MonoBehaviour {
        public static void OnBuildPrefabs(int prefab) {
            RTSInput.InputManager.Instance.OnBuildPrefabs(prefab);
        }
    }

}