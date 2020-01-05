using UnityEngine;

public class AmmoClip : MonoBehaviour
{
    public int TotalAmmo = 100;
    public int MaxBulletCount = 6;

    public float reloadTime = 2f;
    public float remainingReload { get; private set; } = 0f;

    public Bullet ammunition;

    [HideInInspector]
    public int CurrentBulletCount = 0;

    private void Awake()
    {
        CurrentBulletCount = MaxBulletCount;
    }

    public void Reload()
    {
        if (remainingReload <= 0)
        {
            remainingReload = reloadTime;
            int ammoToSpend = TotalAmmo;
            TotalAmmo = Mathf.Max(0, TotalAmmo - (MaxBulletCount - CurrentBulletCount));
            CurrentBulletCount = Mathf.Min(MaxBulletCount, ammoToSpend);
        }
    }

    private void Update()
    {
        if (remainingReload > 0)
        remainingReload -= Time.deltaTime;
    }
}
