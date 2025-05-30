using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// This renderer feature lets you create single-pass full screen post processing effects without needing to write code.
/// </summary>
[URPHelpURL("renderer-features/renderer-feature-full-screen-pass")]
public partial class FullScreenPassRendererFeature : ScriptableRendererFeature
{
    /// <summary>
    /// An injection point for the full screen pass. This is similar to the RenderPassEvent enum but limited to only supported events.
    /// </summary>
    public enum InjectionPoint
    {
        /// <summary>
        /// Inject a full screen pass before transparents are rendered.
        /// </summary>
        BeforeRenderingTransparents = RenderPassEvent.BeforeRenderingTransparents,

        /// <summary>
        /// Inject a full screen pass before post processing is rendered.
        /// </summary>
        BeforeRenderingPostProcessing = RenderPassEvent.BeforeRenderingPostProcessing,

        /// <summary>
        /// Inject a full screen pass after post processing is rendered.
        /// </summary>
        AfterRenderingPostProcessing = RenderPassEvent.AfterRenderingPostProcessing
    }

    /// <summary>
    /// Specifies at which injection point the pass will be rendered.
    /// </summary>
    public InjectionPoint injectionPoint = InjectionPoint.AfterRenderingPostProcessing;

    /// <summary>
    /// Specifies whether the assigned material will need to use the current screen contents as an input texture.
    /// Disable this to optimize away an extra color copy pass when you know that the assigned material will only need
    /// to write on top of or hardware blend with the contents of the active color target.
    /// </summary>
    public bool fetchColorBuffer = true;

    /// <summary>
    /// A mask of URP textures that the assigned material will need access to. Requesting unused requirements can degrade
    /// performance unnecessarily as URP might need to run additional rendering passes to generate them.
    /// </summary>
    public ScriptableRenderPassInput requirements = ScriptableRenderPassInput.None;

    /// <summary>
    /// The material used to render the full screen pass (typically based on the Fullscreen Shader Graph target).
    /// </summary>
    public Material passMaterial;

    internal bool showAdditionalProperties = false;

    /// <summary>
    /// The shader pass index that should be used when rendering the assigned material.
    /// </summary>
    public int passIndex = 0;

    /// <summary>
    /// Specifies if the active camera's depth-stencil buffer should be bound when rendering the full screen pass.
    /// Disabling this will ensure that the material's depth and stencil commands will have no effect (this could also have a slight performance benefit).
    /// </summary>
    public bool bindDepthStencilAttachment = false;

    private FullScreenRenderPass m_FullScreenPass;

    /// <inheritdoc/>
    public override void Create()
    {
        m_FullScreenPass = new FullScreenRenderPass(name);
    }

    internal override bool RequireRenderingLayers(bool isDeferred, bool needsGBufferAccurateNormals, out RenderingLayerUtils.Event atEvent, out RenderingLayerUtils.MaskSize maskSize)
    {
        atEvent = RenderingLayerUtils.Event.Opaque;
        maskSize = RenderingLayerUtils.MaskSize.Bits8;
        return false;
    }

    /// <inheritdoc/>
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (UniversalRenderer.IsOffscreenDepthTexture(in renderingData.cameraData) || renderingData.cameraData.cameraType == CameraType.Preview || renderingData.cameraData.cameraType == CameraType.Reflection)
            return;

        if (passMaterial == null)
        {
            Debug.LogWarningFormat("The full screen feature \"{0}\" will not execute - no material is assigned. Please make sure a material is assigned for this feature on the renderer asset.", name);
            return;
        }

        if (passIndex < 0 || passIndex >= passMaterial.passCount)
        {
            Debug.LogWarningFormat("The full screen feature \"{0}\" will not execute - the pass index is out of bounds for the material.", name);
            return;
        }

        m_FullScreenPass.renderPassEvent = (RenderPassEvent)injectionPoint;
        m_FullScreenPass.ConfigureInput(requirements);
        m_FullScreenPass.SetupMembers(passMaterial, passIndex, fetchColorBuffer, bindDepthStencilAttachment);

        renderer.EnqueuePass(m_FullScreenPass);
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        m_FullScreenPass.Dispose();
    }

    internal class FullScreenRenderPass : ScriptableRenderPass
    {
        private Material m_Material;
        private int m_PassIndex;
        private bool m_CopyActiveColor;
        private bool m_BindDepthStencilAttachment;
        private RTHandle m_CopiedColor;

        private static MaterialPropertyBlock s_SharedPropertyBlock = new MaterialPropertyBlock();

        public FullScreenRenderPass(string passName)
        {
            profilingSampler = new ProfilingSampler(passName);
        }

        public void SetupMembers(Material material, int passIndex, bool copyActiveColor, bool bindDepthStencilAttachment)
        {
            m_Material = material;
            m_PassIndex = passIndex;
            m_CopyActiveColor = copyActiveColor;
            m_BindDepthStencilAttachment = bindDepthStencilAttachment;
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
			// FullScreenPass manages its own RenderTarget.
            // ResetTarget here so that ScriptableRenderer's active attachement can be invalidated when processing this ScriptableRenderPass.
            ResetTarget();

            if (m_CopyActiveColor)
                ReAllocate(renderingData.cameraData.cameraTargetDescriptor);
        }

        internal void ReAllocate(RenderTextureDescriptor desc)
        {
            desc.msaaSamples = 1;
#if OPTIMISATION_ENUM
            desc.depthBufferBits = DepthBits.None.ToInt();
#else
            desc.depthBufferBits = (int)DepthBits.None;
#endif // OPTIMISATION_ENUM
            RenderingUtils.ReAllocateIfNeeded(ref m_CopiedColor, desc, name: "_FullscreenPassColorCopy");
        }

        public void Dispose()
        {
            m_CopiedColor?.Release();
        }

        private static void ExecuteCopyColorPass(CommandBuffer cmd, RTHandle sourceTexture)
        {
            Blitter.BlitTexture(cmd, sourceTexture, new Vector4(1, 1, 0, 0), 0.0f, false);
        }

        private static void ExecuteMainPass(CommandBuffer cmd, RTHandle sourceTexture, Material material, int passIndex)
        {
            s_SharedPropertyBlock.Clear();
            if(sourceTexture != null)
                s_SharedPropertyBlock.SetTexture(ShaderPropertyId.blitTexture, sourceTexture);

            // We need to set the "_BlitScaleBias" uniform for user materials with shaders relying on core Blit.hlsl to work
            s_SharedPropertyBlock.SetVector(ShaderPropertyId.blitScaleBias, new Vector4(1, 1, 0, 0));

            cmd.DrawProcedural(Matrix4x4.identity, material, passIndex, MeshTopology.Triangles, 3, 1, s_SharedPropertyBlock);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            ref var cameraData = ref renderingData.cameraData;
            var cmd = renderingData.commandBuffer;

            using (new ProfilingScope(cmd, profilingSampler))
            {
                if (m_CopyActiveColor)
                {
                    CoreUtils.SetRenderTarget(cmd, m_CopiedColor);
                    ExecuteCopyColorPass(cmd, cameraData.renderer.cameraColorTargetHandle);
                }

                if(m_BindDepthStencilAttachment)
                    CoreUtils.SetRenderTarget(cmd, cameraData.renderer.cameraColorTargetHandle, cameraData.renderer.cameraDepthTargetHandle);
                else
                    CoreUtils.SetRenderTarget(cmd, cameraData.renderer.cameraColorTargetHandle);

                ExecuteMainPass(cmd, m_CopyActiveColor ? m_CopiedColor : null, m_Material, m_PassIndex);
            }
        }
    }
}
