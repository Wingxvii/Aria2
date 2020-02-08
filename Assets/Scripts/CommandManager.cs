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
            if (type == EntityType.Barracks || type == EntityType.Turret || type == EntityType.Wall) {
                Entity newEntity = EntityManager.Instance.GetNewEntity(type);

                newEntity.transform.position = position;
                newEntity.IssueBuild();
                if (GameSceneController.Instance.type == PlayerType.RTS)
                    Netcode.NetworkManager.SendBuildEntity(newEntity);
            }
        }

        //gives unit a movement command
        public void IssueLocation(Entity source, Vector3 position){
            if (source.type == EntityType.Droid || source.type == EntityType.Barracks)
            {
                source.IssueLocation(position);
            }
        }

        //tells unit to attack sepific target
        public void IssueAttack(Entity source, Entity target)
        {
            if (source.type == EntityType.Droid || source.type == EntityType.Turret)
            {
                source.IssueAttack(target);
            }
        }

        //tells unit to attack sepific target
        public void IssueAttack(Entity source, Vector3 position)
        {
            if (source.type == EntityType.Droid)
            {
                source.IssueAttack(position);
            }
        }


        public void CallAction(Entity source, int action) {
            if (source.type == EntityType.Barracks || source.type == EntityType.Turret)
            {
                source.CallAction(action);
            }
        }

        #endregion
    }
}