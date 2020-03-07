﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RTSInput
{

    public enum NotificationType
    {
        NONE,
        INSUFFICIENT_CREDITS,
        INVALID_PLACEMENT,
        GATE_OPENED,
        SUPPLY_BLOCKED,
        QUEUE_FULL,
        MINUTE_MARK,
        THIRTY_MARK,
        TEN_MARK,
        START_TEN,
        START_FIVE,
        START,
    }

    public class NotificationManager : MonoBehaviour
    {
        #region SingletonCode
        private static NotificationManager _instance;
        public static NotificationManager Instance { get { return _instance; } }
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

        public GameObject notification;
        public Transform notifCanvas;

        public Stack<GameObject> notifPool;


        private void Start()
        {
            notifPool = new Stack<GameObject>();
        }

        //add notification to list
        public void HitNotification(NotificationType type)
        {
            switch (type)
            {
                case NotificationType.INSUFFICIENT_CREDITS:
                    StartCoroutine(NotifPlay("Insufficient Credits"));
                    break;
                case NotificationType.INVALID_PLACEMENT:
                    StartCoroutine(NotifPlay("Invalid Placements"));
                    break;
                case NotificationType.GATE_OPENED:
                    StartCoroutine(NotifPlay("A Gate was Opened"));
                    break;
                case NotificationType.MINUTE_MARK:
                    StartCoroutine(NotifPlay("One Minute Remaining"));
                    break;
                case NotificationType.SUPPLY_BLOCKED:
                    StartCoroutine(NotifPlay("You are out of supply"));
                    break;
                case NotificationType.QUEUE_FULL:
                    StartCoroutine(NotifPlay("Your Queue is Full"));
                    break;
                case NotificationType.THIRTY_MARK:
                    StartCoroutine(NotifPlay("Thirty Seconds Remaining"));
                    break;
                case NotificationType.TEN_MARK:
                    StartCoroutine(NotifPlay("Ten Seconds Remaining"));
                    break;
                case NotificationType.START_TEN:
                    StartCoroutine(NotifPlay("You have Ten Seconds to Build..."));
                    break;
                case NotificationType.START_FIVE:
                    StartCoroutine(NotifPlay("You have Five Seconds to Build..."));
                    break;
                case NotificationType.START:
                    StartCoroutine(NotifPlay("The game has Started!!"));
                    break;

            }
        }


        IEnumerator NotifPlay(string newText)
        {
            GameObject notifObj;

            //pool this
            if (notifPool.Count > 0)
            {
                notifObj = notifPool.Pop();
                notifObj.SetActive(true);
            }
            else
            {
                notifObj = Instantiate(notification, new Vector3(960, 365, 0), Quaternion.identity);
            }

            notifObj.transform.parent = notifCanvas;
            notifObj.GetComponent<Text>().text = newText;

            Animation anim = notifObj.GetComponent<Animation>();

            //play build animation
            anim.Play();

            while (anim.isPlaying)
            {
                yield return 0;
            }

            notifPool.Push(notifObj);
            notifObj.SetActive(false);

        }

    }
}