using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class MinimalHizBuildRenderFeature : ScriptableRendererFeature
{
    class HizBuildRenderPass : ScriptableRenderPass
    {
        private ComputeShader _HizMapBuildComputeShader;

        private  RenderTargetIdentifier CameraDepthTexture = "_CameraDepthTexture";

        public RenderTexture _HizMap;
        private CommandBuffer _HizMapBuildCmd;

        public HizBuildRenderPass(ComputeShader computeShader)
        {
            _HizMapBuildComputeShader = computeShader;
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            _HizMapBuildCmd = CommandBufferPool.Get("HizMapBuild");
        }

     
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            //Check hiz map resolution;
            int width = renderingData.cameraData.camera.pixelWidth;
            int height = renderingData.cameraData.camera.pixelHeight;
            
            Vector2Int hizMapSize = new Vector2Int();
            hizMapSize.x = Mathf.Max(MathfExtension.RoundUpToPowerOfTwo(width) >> 1, 1);
            hizMapSize.y = Mathf.Max(MathfExtension.RoundUpToPowerOfTwo(height) >> 1, 1);

            int numMips = Mathf.Max(Mathf.FloorToInt(Mathf.Log(hizMapSize.x,2)),Mathf.FloorToInt(Mathf.Log(hizMapSize.y,2)));

            ConfigureHizMap(hizMapSize, numMips);
                
            //Blit depth to HizMap
            {
                Vector4 srcTexelSize = new Vector4(renderingData.cameraData.camera.pixelWidth, renderingData.cameraData.camera.pixelHeight,0,0);
                Vector4 destTexelSize = new Vector4(_HizMap.width, _HizMap.height, 0, 0);
                
                _HizMapBuildCmd.SetComputeTextureParam(_HizMapBuildComputeShader, HizComputeShaderVars.BlitKernel, HizComputeShaderVars.InTex, CameraDepthTexture);
                _HizMapBuildCmd.SetComputeTextureParam(_HizMapBuildComputeShader, HizComputeShaderVars.BlitKernel, HizComputeShaderVars.OutTex, _HizMap, 0);
                _HizMapBuildCmd.SetComputeVectorParam(_HizMapBuildComputeShader, HizComputeShaderVars.SrcTexelSize, srcTexelSize);
                _HizMapBuildCmd.SetComputeVectorParam(_HizMapBuildComputeShader, HizComputeShaderVars.DestTexelSize, destTexelSize);

                //TODO: check thread group size
                var threadgroupx = _HizMap.width / 8;
                var threadgroupy = _HizMap.height / 8;
                _HizMapBuildCmd.DispatchCompute(_HizMapBuildComputeShader, HizComputeShaderVars.BlitKernel, threadgroupx, threadgroupy, 1);
            }

            //Reduce first mip
            {

            }

            //Reduce next mips
            {

            }

            _HizMapBuildCmd.SetGlobalTexture(HizComputeShaderVars.HizMapGlobalName, _HizMap);
            context.ExecuteCommandBuffer(_HizMapBuildCmd);
        }

     
        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            _HizMap?.Release();
            _HizMapBuildCmd?.Release();
        }
    
        private void ConfigureHizMap(Vector2Int hizMapSize, int numMips)
        {
            if(_HizMap && _HizMap.width == hizMapSize.x && _HizMap.height == hizMapSize.y)
                return;

            if(_HizMap)
                RenderTexture.ReleaseTemporary(_HizMap);

            var hizMapDesc = new RenderTextureDescriptor(hizMapSize.x, hizMapSize.y, RenderTextureFormat.RFloat, 0, numMips);

            _HizMap = RenderTexture.GetTemporary(hizMapDesc);
            _HizMap.name = "HizMap";
            _HizMap.autoGenerateMips = false;
            _HizMap.useMipMap = numMips > 1;
            _HizMap.enableRandomWrite = true;
            _HizMap.filterMode = FilterMode.Point;
        }

        static class HizComputeShaderVars
        {
            public const string InTex = "inTex";
            public const string OutTex = "outTex";
            public const string SrcTexelSize = "srcTexelSize";
            public const string DestTexelSize = "destTexelSize";
            public const string HizMapGlobalName = "_HizMap";
            public static readonly string[] OutTextures = {"OutTextures_0", "OutTextures_1", "OutTextures_2", "OutTextures_3"};
            public const int BlitKernel = 0;
            public const int HizMapBuildKernel = 1;
            public const int MaxBatchSize = 4;
        }
    }

    [SerializeField] ComputeShader m_HizMapBuildComputeShader;
    [SerializeField] RenderTexture m_HizMap;
    HizBuildRenderPass m_ScriptablePass;

    public override void Create()
    {
        m_ScriptablePass = new HizBuildRenderPass(m_HizMapBuildComputeShader);
        m_HizMap = m_ScriptablePass._HizMap; 
        m_ScriptablePass.renderPassEvent = RenderPassEvent.BeforeRenderingTransparents;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(m_ScriptablePass);
    }
}


