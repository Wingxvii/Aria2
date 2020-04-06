using System.Collections;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine;
using System;

[RequireComponent(typeof(Camera))]
public class AlternateCamera : MonoBehaviour
{
    public static List<GameObject> objectsToOutline = new List<GameObject>();
    public static List<Renderer> meshes = new List<Renderer>();
    //public static List<SkinnedMeshRenderer> skinnedMeshes = new List<SkinnedMeshRenderer>();
    public static List<int> previousLayers = new List<int>();
    int sobelLayer = -1;
    Camera parentCam;
    Camera thisCam;

    static int AddFuncs = 0;

    private void Awake()
    {
        objectsToOutline.Clear();

        parentCam = transform.parent.GetComponentInParent<Camera>();
        thisCam = GetComponent<Camera>();

        sobelLayer = LayerMask.NameToLayer("SobelOutline");
        Debug.Log("SOBEL: " + sobelLayer);

        if (AddFuncs == 0)
        {
            RenderPipelineManager.beginFrameRendering += OnBeforeFrame;
            RenderPipelineManager.endFrameRendering += OnAfterFrame;

            RenderPipelineManager.beginCameraRendering += OnBeforeRender;
        }

        ++AddFuncs;
    }

    public static bool AddObjectToList(GameObject obj)
    {
        if (objectsToOutline.Contains(obj))
            return false;
        objectsToOutline.Add(obj);
        return true;
    }

    public static bool RemoveObjectFromList(GameObject obj)
    {
        return objectsToOutline.Remove(obj);
    }

    private void OnBeforeRender(ScriptableRenderContext src, Camera c)
    {
        if (thisCam != null && c == thisCam)
        {
            thisCam.fieldOfView = parentCam.fieldOfView;
            thisCam.focalLength = parentCam.focalLength;
        }
    }

    void OnBeforeFrame(ScriptableRenderContext src, Camera[] c)
    {
        if (meshes.Count > 0)
        {
            meshes.Clear();
            previousLayers.Clear();
        }

        for (int i = 0; i < objectsToOutline.Count; ++i)
        {
            if (objectsToOutline[i] == null)
                continue;

            Renderer[] allMeshes = objectsToOutline[i].GetComponentsInChildren<Renderer>();

            for (int j = 0; j < allMeshes.Length; ++j)
            {
                meshes.Add(allMeshes[j]);
            }
        }

        for (int i = 0; i < meshes.Count; ++i)
        {
            previousLayers.Add(meshes[i].gameObject.layer);
            meshes[i].gameObject.layer = sobelLayer;
        }
    }

    void OnAfterFrame(ScriptableRenderContext src, Camera[] c)
    {
        for (int i = meshes.Count - 1; i >= 0; --i)
        {
            meshes[i].gameObject.layer = previousLayers[i];
        }
    }

    private void OnDestroy()
    {

        --AddFuncs;

        if (AddFuncs == 0)
        {
            RenderPipelineManager.beginFrameRendering -= OnBeforeFrame;
            RenderPipelineManager.endFrameRendering -= OnAfterFrame;

            RenderPipelineManager.beginCameraRendering -= OnBeforeRender;
        }
    }
}
