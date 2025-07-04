using System;
using System.Diagnostics;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.RenderGraphModule
{
    /// <summary>
    /// Texture resource handle.
    /// </summary>
    [DebuggerDisplay("Texture ({handle.index})")]
    public struct TextureHandle
    {
        private static TextureHandle s_NullHandle = new TextureHandle();

        /// <summary>
        /// Returns a null texture handle
        /// </summary>
        /// <returns>A null texture handle.</returns>
        public static TextureHandle nullHandle { get { return s_NullHandle; } }

        internal ResourceHandle handle;

        internal TextureHandle(int handle, bool shared = false) { this.handle = new ResourceHandle(handle, RenderGraphResourceType.Texture, shared); }

        /// <summary>
        /// Cast to RenderTargetIdentifier
        /// </summary>
        /// <param name="texture">Input TextureHandle.</param>
        /// <returns>Resource as a RenderTargetIdentifier.</returns>
        public static implicit operator RenderTargetIdentifier(TextureHandle texture) => texture.IsValid() ? RenderGraphResourceRegistry.current.GetTexture(texture) : default(RenderTargetIdentifier);

        /// <summary>
        /// Cast to Texture
        /// </summary>
        /// <param name="texture">Input TextureHandle.</param>
        /// <returns>Resource as a Texture.</returns>
        public static implicit operator Texture(TextureHandle texture) => texture.IsValid() ? RenderGraphResourceRegistry.current.GetTexture(texture) : null;

        /// <summary>
        /// Cast to RenderTexture
        /// </summary>
        /// <param name="texture">Input TextureHandle.</param>
        /// <returns>Resource as a RenderTexture.</returns>
        public static implicit operator RenderTexture(TextureHandle texture) => texture.IsValid() ? RenderGraphResourceRegistry.current.GetTexture(texture) : null;

        /// <summary>
        /// Cast to RTHandle
        /// </summary>
        /// <param name="texture">Input TextureHandle.</param>
        /// <returns>Resource as a RTHandle.</returns>
        public static implicit operator RTHandle(TextureHandle texture) => texture.IsValid() ? RenderGraphResourceRegistry.current.GetTexture(texture) : null;

        /// <summary>
        /// Return true if the handle is valid.
        /// </summary>
        /// <returns>True if the handle is valid.</returns>
        public bool IsValid() => handle.IsValid();
    }

    /// <summary>
    /// The mode that determines the size of a Texture.
    /// </summary>
    public enum TextureSizeMode
    {
        ///<summary>Explicit size.</summary>
        Explicit,
        ///<summary>Size automatically scaled by a Vector.</summary>
        Scale,
        ///<summary>Size automatically scaled by a Functor.</summary>
        Functor
    }

#if UNITY_2020_2_OR_NEWER
    /// <summary>
    /// Subset of the texture desc containing information for fast memory allocation (when platform supports it)
    /// </summary>
    public struct FastMemoryDesc
    {
        ///<summary>Whether the texture will be in fast memory.</summary>
        public bool inFastMemory;
        ///<summary>Flag to determine what parts of the render target is spilled if not fully resident in fast memory.</summary>
        public FastMemoryFlags flags;
        ///<summary>How much of the render target is to be switched into fast memory (between 0 and 1).</summary>
        public float residencyFraction;
    }
#endif

    /// <summary>
    /// Descriptor used to create texture resources
    /// </summary>
    public struct TextureDesc
#if OPTIMISATION_IEQUATABLE
        : System.IEquatable<TextureDesc>
#endif // OPTIMISATION_IEQUATABLE
    {
        ///<summary>Texture sizing mode.</summary>
        public TextureSizeMode sizeMode;
        ///<summary>Texture width.</summary>
        public int width;
        ///<summary>Texture height.</summary>
        public int height;
        ///<summary>Number of texture slices.</summary>
        public int slices;
        ///<summary>Texture scale.</summary>
        public Vector2 scale;
        ///<summary>Texture scale function.</summary>
        public ScaleFunc func;
        ///<summary>Depth buffer bit depth.</summary>
        public DepthBits depthBufferBits;
        ///<summary>Color format.</summary>
        public GraphicsFormat colorFormat;
        ///<summary>Filtering mode.</summary>
        public FilterMode filterMode;
        ///<summary>Addressing mode.</summary>
        public TextureWrapMode wrapMode;
        ///<summary>Texture dimension.</summary>
        public TextureDimension dimension;
        ///<summary>Enable random UAV read/write on the texture.</summary>
        public bool enableRandomWrite;
        ///<summary>Texture needs mip maps.</summary>
        public bool useMipMap;
        ///<summary>Automatically generate mip maps.</summary>
        public bool autoGenerateMips;
        ///<summary>Texture is a shadow map.</summary>
        public bool isShadowMap;
        ///<summary>Anisotropic filtering level.</summary>
        public int anisoLevel;
        ///<summary>Mip map bias.</summary>
        public float mipMapBias;
        ///<summary>Number of MSAA samples.</summary>
        public MSAASamples msaaSamples;
        ///<summary>Bind texture multi sampled.</summary>
        public bool bindTextureMS;
        ///<summary>Texture uses dynamic scaling.</summary>
        public bool useDynamicScale;
        ///<summary>Memory less flag.</summary>
        public RenderTextureMemoryless memoryless;
        ///<summary>Special treatment of the VR eye texture used in stereoscopic rendering.</summary>
        public VRTextureUsage vrUsage;
        ///<summary>Texture name.</summary>
        public string name;
#if UNITY_2020_2_OR_NEWER
        ///<summary>Descriptor to determine how the texture will be in fast memory on platform that supports it.</summary>
        public FastMemoryDesc fastMemoryDesc;
#endif
        ///<summary>Determines whether the texture will fall back to a black texture if it is read without ever writing to it.</summary>
        public bool fallBackToBlackTexture;
        ///<summary>
        ///If all passes writing to a texture are culled by Dynamic Render Pass Culling, it will automatically fall back to a similar preallocated texture.\n
        ///Set this to false to force the allocation.
        ///</summary>
        public bool disableFallBackToImportedTexture;

        // Initial state. Those should not be used in the hash
        ///<summary>Texture needs to be cleared on first use.</summary>
        public bool clearBuffer;
        ///<summary>Clear color.</summary>
        public Color clearColor;

        void InitDefaultValues(bool dynamicResolution, bool xrReady)
        {
            useDynamicScale = dynamicResolution;
            vrUsage = VRTextureUsage.None;
            // XR Ready
            if (xrReady)
            {
                slices = TextureXR.slices;
                dimension = TextureXR.dimension;
            }
            else
            {
                slices = 1;
                dimension = TextureDimension.Tex2D;
            }
        }

        /// <summary>
        /// TextureDesc constructor for a texture using explicit size
        /// </summary>
        /// <param name="width">Texture width</param>
        /// <param name="height">Texture height</param>
        /// <param name="dynamicResolution">Use dynamic resolution</param>
        /// <param name="xrReady">Set this to true if the Texture is a render texture in an XR setting.</param>
        public TextureDesc(int width, int height, bool dynamicResolution = false, bool xrReady = false)
            : this()
        {
            // Size related init
            sizeMode = TextureSizeMode.Explicit;
            this.width = width;
            this.height = height;
            // Important default values not handled by zero construction in this()
            msaaSamples = MSAASamples.None;
            InitDefaultValues(dynamicResolution, xrReady);
        }

        /// <summary>
        /// TextureDesc constructor for a texture using a fixed scaling
        /// </summary>
        /// <param name="scale">RTHandle scale used for this texture</param>
        /// <param name="dynamicResolution">Use dynamic resolution</param>
        /// <param name="xrReady">Set this to true if the Texture is a render texture in an XR setting.</param>
        public TextureDesc(Vector2 scale, bool dynamicResolution = false, bool xrReady = false)
            : this()
        {
            // Size related init
            sizeMode = TextureSizeMode.Scale;
            this.scale = scale;
            // Important default values not handled by zero construction in this()
            msaaSamples = MSAASamples.None;
            dimension = TextureDimension.Tex2D;
            InitDefaultValues(dynamicResolution, xrReady);
        }

        /// <summary>
        /// TextureDesc constructor for a texture using a functor for scaling
        /// </summary>
        /// <param name="func">Function used to determine the texture size</param>
        /// <param name="dynamicResolution">Use dynamic resolution</param>
        /// <param name="xrReady">Set this to true if the Texture is a render texture in an XR setting.</param>
        public TextureDesc(ScaleFunc func, bool dynamicResolution = false, bool xrReady = false)
            : this()
        {
            // Size related init
            sizeMode = TextureSizeMode.Functor;
            this.func = func;
            // Important default values not handled by zero construction in this()
            msaaSamples = MSAASamples.None;
            dimension = TextureDimension.Tex2D;
            InitDefaultValues(dynamicResolution, xrReady);
        }

        /// <summary>
        /// Copy constructor
        /// </summary>
        /// <param name="input"></param>
        public TextureDesc(TextureDesc input)
        {
            this = input;
        }

        /// <summary>
        /// Hash function
        /// </summary>
        /// <returns>The texture descriptor hash.</returns>
        public override int GetHashCode()
        {
            var hashCode = HashFNV1A32.Create();
            switch (sizeMode)
            {
                case TextureSizeMode.Explicit:
                    hashCode.Append(width);
                    hashCode.Append(height);
                    break;
                case TextureSizeMode.Functor:
                    if (func != null)
                        hashCode.Append(func);
                    break;
                case TextureSizeMode.Scale:
                    hashCode.Append(scale);
                    break;
            }

            hashCode.Append(mipMapBias);
            hashCode.Append(slices);
            hashCode.Append((int) depthBufferBits);
            hashCode.Append((int) colorFormat);
            hashCode.Append((int) filterMode);
            hashCode.Append((int) wrapMode);
            hashCode.Append((int) dimension);
            hashCode.Append((int) memoryless);
            hashCode.Append((int) vrUsage);
            hashCode.Append(anisoLevel);
            hashCode.Append(enableRandomWrite);
            hashCode.Append(useMipMap);
            hashCode.Append(autoGenerateMips);
            hashCode.Append(isShadowMap);
            hashCode.Append(bindTextureMS);
            hashCode.Append(useDynamicScale);
            hashCode.Append((int) msaaSamples);
#if UNITY_2020_2_OR_NEWER
            hashCode.Append(fastMemoryDesc.inFastMemory);
#endif
            return hashCode.value;
        }

#if OPTIMISATION_IEQUATABLE
        public bool Equals(TextureDesc other)
        {
            return sizeMode == other.sizeMode && width == other.width && height == other.height
                   && slices == other.slices && scale.Equals(other.scale) && Equals(func, other.func)
                   && depthBufferBits == other.depthBufferBits && colorFormat == other.colorFormat
                   && filterMode == other.filterMode && wrapMode == other.wrapMode && dimension == other.dimension
                   && enableRandomWrite == other.enableRandomWrite && useMipMap == other.useMipMap
                   && autoGenerateMips == other.autoGenerateMips && isShadowMap == other.isShadowMap
                   && anisoLevel == other.anisoLevel && mipMapBias.Equals(other.mipMapBias)
                   && msaaSamples == other.msaaSamples && bindTextureMS == other.bindTextureMS
                   && useDynamicScale == other.useDynamicScale && memoryless == other.memoryless
                   && vrUsage == other.vrUsage && name == other.name && fastMemoryDesc.Equals(other.fastMemoryDesc)
                   && fallBackToBlackTexture == other.fallBackToBlackTexture
                   && disableFallBackToImportedTexture == other.disableFallBackToImportedTexture
                   && clearBuffer == other.clearBuffer && clearColor.Equals(other.clearColor);
        }

        public override bool Equals(object obj)
        {
            return obj is TextureDesc other && Equals(other);
        }

        public static bool operator ==(TextureDesc left, TextureDesc right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(TextureDesc left, TextureDesc right)
        {
            return !left.Equals(right);
        }
#endif // OPTIMISATION_IEQUATABLE
    }

    [DebuggerDisplay("TextureResource ({desc.name})")]
    class TextureResource : RenderGraphResource<TextureDesc, RTHandle>
    {
        static int m_TextureCreationIndex;

        public override string GetName()
        {
            if (imported && !shared)
                return graphicsResource != null ? graphicsResource.name : "null resource";
            else
                return desc.name;
        }

        // NOTE:
        // Next two functions should have been implemented in RenderGraphResource<DescType, ResType> but for some reason,
        // when doing so, it's impossible to break in the Texture version of the virtual function (with VS2017 at least), making this completely un-debuggable.
        // To work around this, we just copy/pasted the implementation in each final class...

        public override void CreatePooledGraphicsResource()
        {
            Debug.Assert(m_Pool != null, "TextureResource: CreatePooledGraphicsResource should only be called for regular pooled resources");

            int hashCode = desc.GetHashCode();

            if (graphicsResource != null)
                throw new InvalidOperationException(string.Format("TextureResource: Trying to create an already created resource ({0}). Resource was probably declared for writing more than once in the same pass.", GetName()));

            var pool = m_Pool as TexturePool;
            if (!pool.TryGetResource(hashCode, out graphicsResource))
            {
                CreateGraphicsResource(desc.name);
            }

            cachedHash = hashCode;
            pool.RegisterFrameAllocation(cachedHash, graphicsResource);
            graphicsResource.m_Name = desc.name;
        }

        public override void ReleasePooledGraphicsResource(int frameIndex)
        {
            if (graphicsResource == null)
                throw new InvalidOperationException($"TextureResource: Tried to release a resource ({GetName()}) that was never created. Check that there is at least one pass writing to it first.");

            // Shared resources don't use the pool
            var pool = m_Pool as TexturePool;
            if (pool != null)
            {
                pool.ReleaseResource(cachedHash, graphicsResource, frameIndex);
                pool.UnregisterFrameAllocation(cachedHash, graphicsResource);
            }

            Reset(null);
        }

        public override void CreateGraphicsResource(string name = "")
        {
            // Textures are going to be reused under different aliases along the frame so we can't provide a specific name upon creation.
            // The name in the desc is going to be used for debugging purpose and render graph visualization.
            if (name == "")
                name = $"RenderGraphTexture_{m_TextureCreationIndex++}";

            switch (desc.sizeMode)
            {
                case TextureSizeMode.Explicit:
                    graphicsResource = RTHandles.Alloc(desc.width, desc.height, desc.slices, desc.depthBufferBits, desc.colorFormat, desc.filterMode, desc.wrapMode, desc.dimension, desc.enableRandomWrite,
                        desc.useMipMap, desc.autoGenerateMips, desc.isShadowMap, desc.anisoLevel, desc.mipMapBias, desc.msaaSamples, desc.bindTextureMS, desc.useDynamicScale, desc.memoryless, desc.vrUsage, name);
                    break;
                case TextureSizeMode.Scale:
                    graphicsResource = RTHandles.Alloc(desc.scale, desc.slices, desc.depthBufferBits, desc.colorFormat, desc.filterMode, desc.wrapMode, desc.dimension, desc.enableRandomWrite,
                        desc.useMipMap, desc.autoGenerateMips, desc.isShadowMap, desc.anisoLevel, desc.mipMapBias, desc.msaaSamples, desc.bindTextureMS, desc.useDynamicScale, desc.memoryless, desc.vrUsage, name);
                    break;
                case TextureSizeMode.Functor:
                    graphicsResource = RTHandles.Alloc(desc.func, desc.slices, desc.depthBufferBits, desc.colorFormat, desc.filterMode, desc.wrapMode, desc.dimension, desc.enableRandomWrite,
                        desc.useMipMap, desc.autoGenerateMips, desc.isShadowMap, desc.anisoLevel, desc.mipMapBias, desc.msaaSamples, desc.bindTextureMS, desc.useDynamicScale, desc.memoryless, desc.vrUsage, name);
                    break;
            }
        }

        public override void ReleaseGraphicsResource()
        {
            if (graphicsResource != null)
                graphicsResource.Release();
            base.ReleaseGraphicsResource();
        }

        public override void LogCreation(RenderGraphLogger logger)
        {
            logger.LogLine($"Created Texture: {desc.name} (Cleared: {desc.clearBuffer})");
        }

        public override void LogRelease(RenderGraphLogger logger)
        {
            logger.LogLine($"Released Texture: {desc.name}");
        }
    }

    class TexturePool : RenderGraphResourcePool<RTHandle>
    {
        protected override void ReleaseInternalResource(RTHandle res)
        {
            res.Release();
        }

        protected override string GetResourceName(RTHandle res)
        {
            return res.rt.name;
        }

        protected override long GetResourceSize(RTHandle res)
        {
            return Profiling.Profiler.GetRuntimeMemorySizeLong(res.rt);
        }

        override protected string GetResourceTypeName()
        {
            return "Texture";
        }

        override protected int GetSortIndex(RTHandle res)
        {
            return res.GetInstanceID();
        }

        // Another C# nicety.
        // We need to re-implement the whole thing every time because:
        // - obj.resource.Release is Type specific so it cannot be called on a generic (and there's no shared interface for resources like RTHandle, ComputeBuffers etc.)
        // - We can't use a virtual release function because it will capture 'this' in the lambda for RemoveAll generating GCAlloc in the process.
        override public void PurgeUnusedResources(int currentFrameIndex)
        {
            // Update the frame index for the lambda. Static because we don't want to capture.
            s_CurrentFrameIndex = currentFrameIndex;
            m_RemoveList.Clear();

            foreach (var kvp in m_ResourcePool)
            {
                // WARNING: No foreach here. Sorted list GetEnumerator generates garbage...
                var list = kvp.Value;
                var keys = list.Keys;
                var values = list.Values;
                for (int i = 0; i < list.Count; ++i)
                {
                    var value = values[i];
                    if (ShouldReleaseResource(value.frameIndex, s_CurrentFrameIndex))
                    {
                        value.resource.Release();
                        m_RemoveList.Add(keys[i]);
                    }
                }

                foreach (var key in m_RemoveList)
                    list.Remove(key);
            }
        }
    }
}
