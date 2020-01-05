using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class PlayerFPS : MonoBehaviour
{
    Rigidbody rb;
    Collider[] colliders;

    public List<Gun> guns;

    public ModuleDirectional movement;
                 
    public ModuleDirectional turning;

    public AngleClamp3D turnLimit;

    public ModuleButton jumping;

    public ModuleAxis cycleWeapons;
    public float threshold = 0.7f;
    public float swapCooldown = 0.2f;
    float remainingCooldown = 0f;

    public ModuleButton shooting;

    public ModuleButton reloading;

    public Clamp3D speedLimitAxes;

    Vector3 move = Vector3.zero;
    Vector3 turn = Vector3.zero;
    bool jump = false;
    int weaponActive = 0;
    bool shoot = false;

    public bool tilt = true;
    bool cooldown = false;
    public Quaternion groundAngle { get; private set; } = Quaternion.identity;

    public float jumpForce = 0f;
    public float globalSpeedLimit = 0f;
    public Vector3 axisSpeedLimits = Vector3.zero;

    public float acceleration = 0f;
    public float airborneAcceleration = 0f;

    public bool Grounded { get; private set; } = false;
    bool backCheckGrounded = false;
    public float maxAngle = 0f;

    public float frictionStrength = 10f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        colliders = GetComponentsInChildren<Collider>();
    }

    private void FixedUpdate()
    {
        JumpPlayer();

        MovePlayer();
    }

    void JumpPlayer()
    {
        if (jump && Grounded && cooldown)
        {
            //SpecificActor.Container.GetObj(0).pState |= (uint)PlayerState.Jumping;

            Quaternion groundtransform = transform.rotation;
            if (tilt)
                groundtransform = groundAngle * groundtransform;

            Vector3 v = Quaternion.Inverse(groundtransform) * rb.velocity;
            v.y = 0f;
            rb.velocity = groundtransform * v;

            rb.AddForce(groundtransform *
                ForceAdjustment(Vector3.up * jumpForce, tilt, globalSpeedLimit, axisSpeedLimits.x, axisSpeedLimits.y, axisSpeedLimits.z));
            cooldown = false;
        }
        else if (!cooldown)
        {
            //SpecificActor.Container.GetObj(0).pState &= ~(uint)PlayerState.Jumping;
            cooldown = true;
        }
    }

    void MovePlayer()
    {
        Quaternion groundtransform = transform.rotation;
        if (tilt)
            groundtransform = groundAngle * groundtransform;

        if (Grounded)
        {
            Slow();
        }

        rb.AddForce(groundtransform *
            ForceAdjustment(move * (Grounded ? acceleration : airborneAcceleration), tilt, globalSpeedLimit, axisSpeedLimits.x, axisSpeedLimits.y, axisSpeedLimits.z));
    }

    public void Slow()
    {
        if (rb.velocity.sqrMagnitude > frictionStrength * frictionStrength)
        {
            rb.velocity -= rb.velocity.normalized * frictionStrength;
        }
        else
        {
            rb.velocity = Vector3.zero;
        }
    }

    private void Update()
    {
        if (remainingCooldown > 0f)
        {
            remainingCooldown -= Time.deltaTime;
        }

        move = movement.direction;
        turn = turning.direction;

        jump = jumping.pressed;

        if (cycleWeapons.value > threshold && remainingCooldown <= 0f)
        {
            weaponActive += 1;
            if (weaponActive >= guns.Count)
            {
                weaponActive = 0;
            }
            remainingCooldown = swapCooldown;
        }
        else if (cycleWeapons.value < -threshold && remainingCooldown <= 0f)
        {
            weaponActive -= 1;
            if (weaponActive < 0)
            {
                weaponActive = guns.Count - 1;
            }
            remainingCooldown = swapCooldown;
        }

        shoot = shooting.pressed;

        if(reloading.pressed)
        {
            guns[weaponActive].ammo.Reload();
        }
    }

    private void LateUpdate()
    {
        RotatePlayer();

        if (shoot)
        {
            guns[weaponActive].Shoot(colliders);
        }
    }

    void RotatePlayer()
    {
        Vector3 eulers = transform.localRotation.eulerAngles;

        AddSingular(ref eulers.x, turnLimit.xConstraint, 0);
        AddSingular(ref eulers.y, turnLimit.yConstraint, 1);
        AddSingular(ref eulers.z, turnLimit.zConstraint, 2);

        transform.localRotation = Quaternion.Euler(eulers);
    }

    void AddSingular(ref float newRot, AngleClamp axis, int axisNum)
    {
        if (axis == AngleClamp.Immobile)
            return;
        newRot += turn[axisNum];
        while (newRot > 180f)
            newRot -= 360f;
        while (newRot <= -180f)
            newRot += 360f;
        if (axis == AngleClamp.NinetyNinety)
            newRot = Mathf.Clamp(newRot, -90f, 90f);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (backCheckGrounded)
        {
            backCheckGrounded = false;
            Grounded = false;
            groundAngle = Quaternion.identity;
        }

        if (CullSingle(collision))
        {
            //Ent.GetObj(0).Container.GetObj(0).pState &= ~(uint)PlayerState.Jumping;
            Grounded = true;
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        if (backCheckGrounded)
        {
            backCheckGrounded = false;
            Grounded = false;
            groundAngle = Quaternion.identity;
        }

        if (CullSingle(collision))
        {

            Grounded = true;
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (backCheckGrounded)
        {
            backCheckGrounded = false;
            Grounded = false;
            groundAngle = Quaternion.identity;
        }
    }

    bool CullSingle(Collision collision)
    {
        Vector3 norm = Vector3.zero;

        for (int j = collision.contactCount - 1; j >= 0; j--)
            norm += collision.GetContact(j).normal;

        if (norm.sqrMagnitude > 0)
        {
            Vector3.Normalize(norm);
            float DotAngle = Vector3.Dot(norm, transform.up);
            if (DotAngle > Mathf.Cos(maxAngle * Mathf.Deg2Rad))
            {
                if (tilt && (!Grounded || DotAngle > Vector3.Dot(groundAngle * transform.up, transform.up)))
                {
                    groundAngle = Quaternion.FromToRotation(transform.up, norm);
                }
                return true;
            }
        }
        return false;
    }

    float SquareLengthCalculation(Vector3 vel)
    {
        return
            (speedLimitAxes.x == ClampType.Global ? vel.x * vel.x : 0) +
            (speedLimitAxes.y == ClampType.Global ? vel.y * vel.y : 0) +
            (speedLimitAxes.z == ClampType.Global ? vel.z * vel.z : 0);
    }

    public Vector3 ForceAdjustment(Vector3 force, bool tilt, float globalLimit = 0, float individualX = 0, float individualY = 0, float individualZ = 0)
    {
        Vector3 combined = new Vector3(individualX, individualY, individualZ);

        Vector3 vel = rb.velocity;
        if (tilt)
            vel = Quaternion.Inverse(groundAngle) * vel;
        vel = Quaternion.Inverse(rb.transform.rotation) * vel;

        Vector3 afterVel = vel + force / rb.mass * Time.fixedDeltaTime;

        float globalLength = Mathf.Sqrt(SquareLengthCalculation(afterVel));
        float preLength = Mathf.Sqrt(SquareLengthCalculation(vel));

        afterVel.x = GetClamped(speedLimitAxes.x, afterVel.x, globalLength, Mathf.Max(globalLimit, preLength), Mathf.Max(individualX, Mathf.Abs(vel.x)));
        afterVel.y = GetClamped(speedLimitAxes.y, afterVel.y, globalLength, Mathf.Max(globalLimit, preLength), Mathf.Max(individualY, Mathf.Abs(vel.y)));
        afterVel.z = GetClamped(speedLimitAxes.z, afterVel.z, globalLength, Mathf.Max(globalLimit, preLength), Mathf.Max(individualZ, Mathf.Abs(vel.z)));

        return (afterVel - vel) * rb.mass / Time.fixedDeltaTime;
    }

    float GetClamped(ClampType axis, float velocityAxis, float globalLength, float globalLimit, float individualLimit)
    {
        if (axis == ClampType.Global && globalLength > globalLimit)
            return velocityAxis / globalLength * globalLimit;
        else if (axis == ClampType.Singular)
            return Mathf.Clamp(velocityAxis, -individualLimit, individualLimit);
        return velocityAxis;
    }
}

[Serializable]
public class Clamp3D
{
    public ClampType x = ClampType.None;
    public ClampType y = ClampType.None;
    public ClampType z = ClampType.None;
}

public enum ClampType
{
    None,
    Global,
    Singular
}

[Serializable]
public class AngleClamp3D
{
    public AngleClamp xConstraint = AngleClamp.Immobile;
    public AngleClamp yConstraint = AngleClamp.Immobile;
    public AngleClamp zConstraint = AngleClamp.Immobile;
}

public enum AngleClamp
{
    None,
    Immobile,
    NinetyNinety
}