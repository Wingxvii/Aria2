﻿using UnityEngine.SceneManagement;
using UnityEngine;

public enum PlayerType { 
    Spectator,
    RTS,
    FPS,
}

public class GameController : MonoBehaviour
{

    #region SingletonCode
        private static GameController _instance;
        public static GameController Instance { get { return _instance; } }
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(this.gameObject);
            }
            else
            {
                _instance = this;
            }

            //loads start menu
            SceneManager.LoadSceneAsync(1, LoadSceneMode.Additive);
            gameStart = true;
        }
        //single pattern ends here
        #endregion

    public bool gameStart = false;
    public int loadedScene = 1;
    public string IP = "";
    public PlayerType type = PlayerType.Spectator;
    public int gameState = 0;

    //use to swap scene
    public void SwapScene(int scene)
    {
        UnloadScene(loadedScene);
        LoadScene(scene);
    }

    //use to load scene
    public void LoadScene(int scene)
    {
        loadedScene = scene;
        SceneManager.LoadSceneAsync(scene, LoadSceneMode.Additive);
    }

    //use to unload scene
    public void UnloadScene(int scene)
    {
        SceneManager.UnloadSceneAsync(scene);
    }

}

/*
 * Scenes Index:
 * 
 * 0 - Master manager
 * 1 - Start Menu
 * 2 - RTS
 * 3 - FPS
 * 4 - End scene
 * 
 */
