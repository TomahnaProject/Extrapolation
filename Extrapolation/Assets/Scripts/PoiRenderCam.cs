using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoiRenderCam : MonoBehaviour
{
    public Action OnCameraRender;

    Material _drawMaterial;

    void Start()
    {
        // Unity has a built-in shader that is useful for drawing simple colored things.
        Shader shader = Shader.Find("Hidden/Internal-Colored");
        _drawMaterial = new Material(shader)
        {
            hideFlags = HideFlags.HideAndDontSave
        };
        // Turn on alpha blending
        _drawMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        _drawMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        // Turn backface culling off
        _drawMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
        // Turn off depth writes
        _drawMaterial.SetInt("_ZWrite", 0);
    }

    void OnDestroy()
    {
        Destroy(_drawMaterial);
    }

    void OnPostRender()
    {
        if (_drawMaterial == null)
            return;

        // Apply the line material
        _drawMaterial.SetPass(0);

        // Let the NodeViewPanel handle rendering.
        OnCameraRender?.Invoke();
    }
}
