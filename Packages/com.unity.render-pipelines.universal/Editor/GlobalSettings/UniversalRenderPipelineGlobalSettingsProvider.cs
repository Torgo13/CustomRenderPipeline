using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    class UniversalGlobalSettingsPanelProvider : RenderPipelineGlobalSettingsProvider<UniversalRenderPipeline, UniversalRenderPipelineGlobalSettings>
    {
        public UniversalGlobalSettingsPanelProvider()
            : base("Project/Graphics/URP Global Settings")
        {
#if OPTIMISATION
            keywords = GetSearchKeywordsFromGUIContentProperties<UniversalRenderPipelineGlobalSettingsUI.Styles>();
#else
            keywords = GetSearchKeywordsFromGUIContentProperties<UniversalRenderPipelineGlobalSettingsUI.Styles>().ToArray();
#endif // OPTIMISATION
        }

        [SettingsProvider]
        public static SettingsProvider CreateSettingsProvider() => new UniversalGlobalSettingsPanelProvider();

        #region RenderPipelineGlobalSettingsProvider

        protected override void Clone(RenderPipelineGlobalSettings src, bool activateAsset)
        {
            UniversalGlobalSettingsCreator.Clone(src as UniversalRenderPipelineGlobalSettings, activateAsset: activateAsset);
        }

        protected override void Create(bool useProjectSettingsFolder, bool activateAsset)
        {
            UniversalGlobalSettingsCreator.Create(useProjectSettingsFolder: useProjectSettingsFolder, activateAsset: activateAsset);
        }

        protected override void Ensure()
        {
            UniversalRenderPipelineGlobalSettings.Ensure();
        }
        #endregion
    }
}
