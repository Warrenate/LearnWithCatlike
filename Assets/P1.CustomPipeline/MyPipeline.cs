using System.Diagnostics;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

public class MyPipeline : RenderPipeline
{

    CullResults cull;
    CommandBuffer buffer = new CommandBuffer { name = "Render Camera" };

    Material errorMaterial;

    public override void Render(ScriptableRenderContext renderContext, Camera[] cameras)
    {
        //base.Render(renderContext, cameras);

        //renderContext.DrawSkybox(cameras[0]);
        //renderContext.Submit();

        foreach (var camera in cameras)
        {
            Render(renderContext, camera);
        }
    }

    void Render(ScriptableRenderContext context, Camera camera)
    {
        ScriptableCullingParameters cullingParameters;
        if (!CullResults.GetCullingParameters(camera, out cullingParameters))
            return;

#if UNITY_EDITOR
        if (camera.cameraType == CameraType.SceneView)
            ScriptableRenderContext.EmitWorldGeometryForSceneView(camera);
#endif

        CullResults.Cull(ref cullingParameters, context, ref cull);

        context.SetupCameraProperties(camera);

        //var buffer = new CommandBuffer { name = camera.name };
        //var buffer = new CommandBuffer { name = "Render Camera" };

        CameraClearFlags clearFlags = camera.clearFlags;
        buffer.BeginSample("Render Camera");
        buffer.ClearRenderTarget((clearFlags & CameraClearFlags.Depth) != 0, (clearFlags & CameraClearFlags.Color) != 0, camera.backgroundColor);
        //buffer.EndSample("Render Camera");
        context.ExecuteCommandBuffer(buffer);
        //buffer.Release();
        buffer.Clear();

        var drawSettings = new DrawRendererSettings(camera, new ShaderPassName("SRPDefaultUnlit"));
        drawSettings.sorting.flags = SortFlags.CommonOpaque;
        var filterSettings = new FilterRenderersSettings(true)
        {
            renderQueueRange = RenderQueueRange.opaque
        };
        context.DrawRenderers(cull.visibleRenderers, ref drawSettings, filterSettings);

        context.DrawSkybox(camera);

        drawSettings.sorting.flags = SortFlags.CommonTransparent;
        filterSettings.renderQueueRange = RenderQueueRange.transparent;
        context.DrawRenderers(cull.visibleRenderers, ref drawSettings, filterSettings);

        DrawDefaultPipeline(context, camera);

        buffer.EndSample("Render Camera");
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();

        context.Submit();
    }

    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    void DrawDefaultPipeline(ScriptableRenderContext context, Camera camera)
    {
        if (errorMaterial == null)
        {
            Shader errorShader = Shader.Find("Hidden/InternalErrorShader");
            errorMaterial = new Material(errorShader) { hideFlags = HideFlags.HideAndDontSave };
        }

        var drawSettings = new DrawRendererSettings(camera, new ShaderPassName("ForwardBase"));
        drawSettings.SetShaderPassName(1, new ShaderPassName("PrepassBase"));
        drawSettings.SetShaderPassName(2, new ShaderPassName("Always"));
        drawSettings.SetShaderPassName(3, new ShaderPassName("Vertex"));
        drawSettings.SetShaderPassName(4, new ShaderPassName("VertexLMRGBM"));
        drawSettings.SetShaderPassName(5, new ShaderPassName("VertexLM"));
        drawSettings.SetOverrideMaterial(errorMaterial, 0);
        var filterSettings = new FilterRenderersSettings(true);
        context.DrawRenderers(cull.visibleRenderers, ref drawSettings, filterSettings);
    }
}
