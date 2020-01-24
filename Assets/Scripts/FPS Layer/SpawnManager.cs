using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    #region SingletonCode
    private static SpawnManager _instance;
    public static SpawnManager Instance { get { return _instance; } }
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
    }
    //single pattern ends here
    #endregion


    public Transform[] FPSspawnpoints;
    public Queue<int> freeSpawnPoints;

    bool initialized = false;

    // Start is called before the first frame update
    void Start()
    {
        Initialize();

    }

    public void Initialize()
    {
        if (!initialized)
        {
            initialized = true;
        }

        freeSpawnPoints = new Queue<int>();
        for (int i = 0; i < freeSpawnPoints.Count; ++i)
        {
            freeSpawnPoints.Enqueue(i);
        }
    }


    // Update is called once per frame
    void Update()
    {
        
    }
}
