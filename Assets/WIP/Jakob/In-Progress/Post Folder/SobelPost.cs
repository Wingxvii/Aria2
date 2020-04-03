using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using System;
using UnityEngine.Rendering;

[Serializable, VolumeComponentMenu("Post-processing/Custom/SobelSelect")]
public class SobelPost : CustomPostProcessVolumeComponent, IPostProcessComponent
{
    Material m_Material = null;
    public RenderTextureParameter tp = new RenderTextureParameter(null);
    //public MarchParamater marchManager = new MarchParamater(null);
    //public TextureParameter tp = new TextureParameter(null);
    //public TextureParameter TP;

    public override CustomPostProcessInjectionPoint injectionPoint => CustomPostProcessInjectionPoint.AfterPostProcess;

    public bool IsActive()
    {
        //return m_Material != null && mm != null;
        return m_Material != null && tp.value != null;
    }

    public override void Setup()
    {
        Shader sh = Shader.Find("Hidden/Custom/SobelEffect");
        if (sh != null)
        {
            Debug.Log("GOT IT");
            m_Material = new Material(sh);
        }
        else
        {
            Debug.Log("FUCKED UP");
        }
    }

    public override void Render(CommandBuffer cmd, HDCamera camera, RTHandle source, RTHandle destination)
    {
        if (m_Material == null)
        {
            Debug.Log("SHIIIT");
            return;
        }

        //if (tp.value.width != camera.actualWidth)
        //    tp.value.width = camera.actualWidth;
        //if (tp.value.height != camera.actualHeight)
        //    tp.value.height = camera.actualHeight;
        //
        //Texture t = tp.value;
        RenderTexture t = tp.value;

        m_Material.SetVector("_RT_SIZE", new Vector4(t.width, t.height, 0, 0));

        m_Material.SetTexture("_RT", t);

        m_Material.SetTexture("_InputTexture", source);
        HDUtils.DrawFullScreen(cmd, m_Material, destination);//
    }

    public override void Cleanup()
    {
        CoreUtils.Destroy(m_Material);
    }
}
