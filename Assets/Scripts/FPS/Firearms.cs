using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Firearm", menuName = "Firearms")]
public class Firearms : ScriptableObject
{
	public GunStats gunStats;
}
[System.Serializable]
public struct GunStats
{
	public float accuracy;
	public int accReduc;
	public float dmg;
	public float RoF;
	public int clip;
	public int projNum;

	public AudioClip gunShot;

	public Transform barrelPos1;

	public ParticleSystem muzzleFlash;
	public ParticleSystem tracer;

	public Mesh gunMesh;

	public Vector3 barrelPosition;
	public Vector3 gunPosition;
}
