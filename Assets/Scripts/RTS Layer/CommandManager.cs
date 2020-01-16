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
        public void Move(Entity source, Vector3 position){

        }

        //tells unit to attack sepific target
        public void AttackTarget(Entity source, Entity target ) { 
        
        }

        //issues a position for a rally point
        public void IssueRally(Entity source, Vector3 position) { 
            
        }

        #endregion
    }
}