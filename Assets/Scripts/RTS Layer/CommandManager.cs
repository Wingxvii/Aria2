using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace RTSInput
{
    public class CommandManager : MonoBehaviour
    {
        #region SingletonCode
        private static CommandManager _instance;
        public static CommandManager Instance { get { return _instance; } }
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


        #region commandFunctions

        //builds a entity from a blueprint
        public void Build(Vector3 position, EntityType type) { 
            
        }

        //gives unit a movement command
        public void Move(Entity target, Vector3 position)
        {

        }



        #endregion
    }
}