using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#region Ingame Manager
//Calls functions from the BaseUpdater from Unity.
//Have one of these in the scene at all times, and only one.
public class UpdateManager : MonoBehaviour
{
    public bool DisplayEstimatedFPS = false;
    // FixedUpdate is called once per fixed time step
    private void FixedUpdate()
    {
        MANUAL_UPDATE.BaseUpdater.UpdateFixed();
    }

    // Update is called once per frame
    private void Update()
    {
        MANUAL_UPDATE.BaseUpdater.UpdateRegular();
        if (DisplayEstimatedFPS)
            Debug.Log(1f / Time.deltaTime);
    }

    // LateUpdate is called before rendering
    private void LateUpdate()
    {
        MANUAL_UPDATE.BaseUpdater.UpdateLate();
    }
}
#endregion

#region UpdateStages
// Defined stages of updates, each set will happen after the last and in parallel with each other
// More unique stages can be added later, in the order we want them to be executed
public enum UpdateStages
{
    FIXED_UPDATE_END,
    UPDATE_END,
    LATE_UPDATE_END,

    TOTAL
}
#endregion

namespace MANUAL_UPDATE
{
    #region Function for updating
    //Any update function to act on an object
    public delegate void UpdateFunction<T>(T toUpdate) where T : UpdateableObj;
    #endregion

    #region Object Lists
    //Object list base, allows CycleUpdates and RemoveFromSystem to be called on abstracted list
    public abstract class BaseObjectList
    {
        public abstract void RemoveFromSystem(UpdateableObj toRemove);

        public abstract void CycleUpdates();
    }

    //Specific lists, containing a function and a list of entities on which to act.
    public class GenericObjectList<T> : BaseObjectList where T : UpdateableObj
    {
        //Contains a static list with references to all of itself, to remember if a list already exists when adding an entity
        private static GenericObjectList<T>[] _gol;

        //Using AddToSystem, entities can add themselves to the list only to be removed upon destruction
        //Lists are never destroyed, this could be optimized in the future
        //Due to the fact that a limited number of lists can ever exist, it's not a problem currently
        public static void AddToSystem(T toUpdate, UpdateFunction<T> func, UpdateStages stage)
        {
            if (_gol == null)
            {
                _gol = new GenericObjectList<T>[(int)UpdateStages.TOTAL];
            }

            if (_gol[(int)stage] == null)
            {
                _gol[(int)stage] = new GenericObjectList<T>(func, stage);
            }

            _gol[(int)stage].objectsToUpdate.Add(toUpdate);

            //Object knows that this list is using it's reference
            toUpdate.functions.Add(_gol[(int)stage]);
        }

        //Objects call this to remove themselves from the system
        public override void RemoveFromSystem(UpdateableObj toRemove)
        {
            objectsToUpdate.Remove((T)toRemove);
        }

        //Private constructor to avoid accidental creation - only created by itself
        private GenericObjectList(UpdateFunction<T> _update_function, UpdateStages stage)
        {
            BaseUpdater.ToUpdate[(int)stage].Add(this);

            objectsToUpdate = new List<T>();
            updateFunction = _update_function;
        }

        //Cycle through all entities and update them. Called by BaseUpdater.
        public override void CycleUpdates()
        {
            for (int i = 0; i < objectsToUpdate.Count; ++i)
            {
                if (objectsToUpdate[i].enabled)
                    updateFunction(objectsToUpdate[i]);
            }
        }

        //List of objects to act on, and the function that acts on them
        public List<T> objectsToUpdate { get; private set; }
        public UpdateFunction<T> updateFunction { get; private set; }
    }
    #endregion

    #region Update All Object Lists
    //Notifies all ObjectLists to update the objects
    public abstract class BaseUpdater
    {
        #region UpdateSingleton
        private static List<BaseObjectList>[] toUpdate;

        public static List<BaseObjectList>[] ToUpdate
        {
            get
            {
                if (toUpdate == null)
                {
                    toUpdate = new List<BaseObjectList>[(int)UpdateStages.TOTAL];
                    for (int i = 0; i < (int)UpdateStages.TOTAL; ++i)
                        toUpdate[i] = new List<BaseObjectList>();
                }
                return toUpdate;
            }
        }
        #endregion

        #region UpdateCode
        //Iterates through everything until FIXED_UPDATE_END, assumes that it all applies on FixedUpdate()
        public static void UpdateFixed()
        {
            for (int i = 0; i <= (int)UpdateStages.FIXED_UPDATE_END; ++i)
            {
                UpdateList(ToUpdate[i]);
            }
        }

        //Iterates from everything between FIXED_UPDATE_END and UPDATE_END, assumes all functions are called on Update()
        public static void UpdateRegular()
        {
            for (int i = (int)UpdateStages.FIXED_UPDATE_END + 1; i <= (int)UpdateStages.UPDATE_END; ++i)
            {
                UpdateList(ToUpdate[i]);
            }
        }

        //Calls remaining functions on LateUpdate();
        public static void UpdateLate()
        {
            for (int i = (int)UpdateStages.UPDATE_END + 1; i <= (int)UpdateStages.LATE_UPDATE_END; ++i)
            {
                UpdateList(ToUpdate[i]);
            }
        }

        //Updates a single list of object lists, which all apply on the same update step
        static void UpdateList(List<BaseObjectList> objList)
        {
            for (int i = 0; i < objList.Count; ++i)
            {
                objList[i].CycleUpdates();
            }
        }
        #endregion
    }
    #endregion
}