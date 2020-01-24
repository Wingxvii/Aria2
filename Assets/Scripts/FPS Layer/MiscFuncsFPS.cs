using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MiscFuncsFPS
{
    public static Vector3 ApplyFriction(Vector3 velocity, in PlayerStats stats)
    {
        Vector3 stableVelocity = velocity;
        if (stats.colliding)
            stableVelocity = Quaternion.Inverse(stats.groundAngle) * velocity;
        Vector2 normal = new Vector2(stableVelocity.x, stableVelocity.z);
        float mag = normal.magnitude;
        normal /= mag;

        float frict;
        if (stats.colliding)
            frict = stats.friction * Time.fixedDeltaTime;
        else
            frict = stats.aerialDrag * Time.fixedDeltaTime;

        if (mag <= frict)
        {
            stableVelocity.x = 0;
            stableVelocity.z = 0;
        }
        else
        {
            stableVelocity.x -= normal.x * frict;
            stableVelocity.z -= normal.y * frict;
        }

        if (stats.colliding)
            return stats.groundAngle * stableVelocity;
        return stableVelocity;
    }

    public static Vector3 ClampVelocity(Vector3 currentVelocity, Vector3 addVelocity, in PlayerStats stats)
    {
        Vector3 stableVelocity = currentVelocity;
        if (stats.colliding)
            stableVelocity = Quaternion.Inverse(stats.groundAngle) * stableVelocity;
        float maxMag = Mathf.Max(new Vector2(stableVelocity.x, stableVelocity.z).magnitude, stats.maxSpeed);
        stableVelocity += addVelocity;
        float realMag = new Vector2(stableVelocity.x, stableVelocity.z).magnitude;

        if (realMag > maxMag)
        {
            stableVelocity.x *= (maxMag / realMag);
            stableVelocity.z *= (maxMag / realMag);
        }

        if (stats.colliding)
        return stats.groundAngle * stableVelocity;
            return stableVelocity;
    }
}
