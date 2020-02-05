using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class AmmoStatsFPS
{
    public int maxBulletCount = 6;
    public int currentBullets = 6;
}

public class AmmoClipFPS : MonoBehaviour
{
    public BulletFPS bullet;
    public AmmoStatsFPS ammo;
}
