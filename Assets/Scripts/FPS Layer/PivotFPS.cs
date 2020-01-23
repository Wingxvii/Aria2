using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PivotFPS : MonoBehaviour
{
    public bool rotX = false;
    public bool rotY = false;
    public bool rotZ = false;

    public Vector3 sensitivity = Vector3.one;

    public void RotateSelf(Vector3 rotAngles)
    {
        transform.localRotation *= Quaternion.Euler(
            new Vector3(
                rotX ? rotAngles.x * sensitivity.x : 0, 
                rotY ? rotAngles.y * sensitivity.y : 0, 
                rotZ ? rotAngles.z * sensitivity.z : 0)
            );
        if (rotX)
        {
            Vector3 angles = transform.localRotation.eulerAngles;
            angles.x = Mathf.Clamp(angles.x, -90f, 90f);
            transform.localRotation = Quaternion.Euler(angles);
        }
    }
}
