using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MiscFuncsFPS
{
    public static Vector3 ApplyFriction(Vector3 velocity, in PlayerStats stats)
    {
        Vector3 stableVelocity = Quaternion.Inverse(stats.groundAngle) * velocity;
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

        return stats.groundAngle * stableVelocity;
    }

    public static Vector3 ClampVelocity(Vector3 currentVelocity, Vector3 addVelocity, in PlayerStats stats)
    {
        Vector3 stableVelocity = Quaternion.Inverse(stats.groundAngle) * currentVelocity;
        float maxMag = Mathf.Max(new Vector2(stableVelocity.x, stableVelocity.z).magnitude, stats.maxSpeed);
        float realMag = new Vector2(stableVelocity.x + addVelocity.x, stableVelocity.z + addVelocity.z).magnitude;

        if (realMag > maxMag)
        {
            stableVelocity.x *= (maxMag / realMag);
            stableVelocity.z *= (maxMag / realMag);
        }

        return stats.groundAngle * stableVelocity;
    }
}
