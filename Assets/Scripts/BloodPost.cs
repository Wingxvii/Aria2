using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

[Serializable, VolumeComponentMenu("Post-processing/Custom/Blood")]
public sealed class BloodSettings : CustomPostProcessVolumeComponent, IPostProcessComponent
{
    //public Vector3Parameter angleIntensity1 = new Vector3Parameter(new Vector3(0, 0, 0));
    //public Vector3Parameter angleIntensity2 = new Vector3Parameter(new Vector3(0, 0, 0));
    //public Vector3Parameter angleIntensity3 = new Vector3Parameter(new Vector3(0, 0, 0));
    //public Vector3Parameter angleIntensity4 = new Vector3Parameter(new Vector3(0, 0, 0));
    //public Vector3Parameter angleIntensity5 = new Vector3Parameter(new Vector3(0, 0, 0));

    public static int currentBlood = 0;
    public static Vector3[] v3 = new Vector3[5];

    Material m_Material;

    public bool IsActive() => (m_Material != null);

    public override CustomPostProcessInjectionPoint injectionPoint => CustomPostProcessInjectionPoint.AfterPostProcess;

    public override void Setup()

    {

        if (Shader.Find("Hidden/Shader/BloodEffect") != null)

            m_Material = new Material(Shader.Find("Hidden/Shader/BloodEffect"));

    }

    public override void Render(CommandBuffer cmd, HDCamera camera, RTHandle source, RTHandle destination)

    {
        if (m_Material == null)

            return;

        m_Material.SetVector("_AngleIntensity1", v3[0]);
        m_Material.SetVector("_AngleIntensity2", v3[1]);
        m_Material.SetVector("_AngleIntensity3", v3[2]);
        m_Material.SetVector("_AngleIntensity4", v3[3]);
        m_Material.SetVector("_AngleIntensity5", v3[4]);

        for (int i = 0; i < 5; ++i)
        {
            v3[i].z = Mathf.Max(v3[i].z - Time.deltaTime, 0f);
        }

        m_Material.SetTexture("_InputTexture", source);

        HDUtils.DrawFullScreen(cmd, m_Material, destination);

    }

    public override void Cleanup() => CoreUtils.Destroy(m_Material);

}