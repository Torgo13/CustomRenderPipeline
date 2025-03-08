#if HDRP_1_OR_NEWER
namespace UnityEngine.Rendering.HighDefinition
{
    [GenerateHLSL(needAccessors = false, generateCBuffer = true, constantRegister = (int)ConstantRegister.RayTracingLightLoop)]
    unsafe struct ShaderVariablesRaytracingLightLoop
    {
        public Vector3 _MinClusterPos;
        public uint _LightPerCellCount;
        public Vector3 _MaxClusterPos;
        public uint _PunctualLightCountRT;
        public uint _AreaLightCountRT;
        public uint _EnvLightCountRT;
    }
}
#endif // HDRP_1_OR_NEWER
