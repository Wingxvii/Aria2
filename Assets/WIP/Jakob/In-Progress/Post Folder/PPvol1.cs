using UnityEngine;

using UnityEngine.Rendering;

using UnityEngine.Rendering.HighDefinition;

using System;

[Serializable, VolumeComponentMenu("Post-processing/Custom/PPvol1")] // Optimize later

public sealed class PPvol1 : CustomPostProcessVolumeComponent, IPostProcessComponent

{

    [Tooltip("Controls the intensity of the effect.")]

    public ClampedFloatParameter intensity = new ClampedFloatParameter(0f, 0f, 1f);

    Material m_Material;

    public bool IsActive() => m_Material != null && intensity.value > 0f;

    //USE TO ORDER STUFF VVVVVVVVVVVVVVVVVVVVVVVVVVVV
    public override CustomPostProcessInjectionPoint injectionPoint => CustomPostProcessInjectionPoint.BeforePostProcess;

    public override void Setup()

    {
        string shaderString = "Shader Graphs/PPunlit1"; //"Hidden/Shader/PPshad1"; // 
        //Shader[] allObj = Resources.FindObjectsOfTypeAll<Shader>();
        //Debug.Log(allObj.Length);
        //foreach(Shader sh in allObj)
        //{
        //    string[] splits = sh.name.Split('/');
        //    if (splits[0] == "Shader Graphs")
        //    {
        //        Debug.Log(sh.name);
        //        Debug.Log(Shader.Find(sh.name));
        //    }
        //
        //}
        Shader myShad = Shader.Find(shaderString);
        //Debug.Log(myShad == null);

        if (myShad != null) // Optimize later
        {
            //Debug.Log("GOT IT");
            m_Material = new Material(myShad);
        }
        //Debug.Log("ERR_NULL_SHADER");
    }

    public override void Render(CommandBuffer cmd, HDCamera camera, RTHandle source, RTHandle destination)

    {
        if (m_Material == null)
        {
            return;
        }

        m_Material.SetFloat("_Intensity", intensity.value);

        m_Material.SetTexture("_InputTexture", source);

        HDUtils.DrawFullScreen(cmd, m_Material, destination);

    }

    public override void Cleanup() => CoreUtils.Destroy(m_Material);

}