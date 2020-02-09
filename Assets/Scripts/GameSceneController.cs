using UnityEngine.SceneManagement;
using UnityEngine;

public enum PlayerType { 
    RTS = 0,
    FPS,
    Spectator
}

public class GameSceneController : MonoBehaviour
{

    #region SingletonCode
        private static GameSceneController _instance;
        public static GameSceneController Instance { get { return _instance; } }
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
            gameStart = true;

        }
    //single pattern ends here
    #endregion

    public int playerNumber = -1; 

    public bool gameStart = false;
    public int loadedScene = 1;
    public string IP = "";
    public PlayerType type;
    public int gameState = 0;

    public void Start()
    {
        if (SceneManager.GetActiveScene().buildIndex == 0)
        {
            LoadScene(1);
            loadedScene = 1;
        }
    }

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
        Debug.Log("Unloaded");
        SceneManager.UnloadSceneAsync(scene);
    }
    
}
