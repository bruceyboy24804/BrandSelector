﻿﻿
using BrandSelector.Systems;
using Colossal.Logging;
using Game;
using Game.Modding;
using Game.SceneFlow;
using Colossal.IO.AssetDatabase;
using Unity.Entities;

namespace BrandSelector
{
    public class Mod : IMod
    {
        public static ILog log = LogManager.GetLogger($"{nameof(BrandSelector)}.{nameof(Mod)}")
            .SetShowsErrorsInUI(false);

        private Setting m_Setting;
        public static Mod Instance { get; private set; }
        public static readonly string ID = "BrandSelector";

        public void OnLoad(UpdateSystem updateSystem)
        {
            Instance = this;
            log.Info(nameof(OnLoad));
#if DEBUG
            log.effectivenessLevel = Level.Debug;
#endif

            if (GameManager.instance.modManager.TryGetExecutableAsset(this, out var asset))
                log.Info($"Current mod asset at {asset.path}");

            m_Setting = new Setting(this);
            m_Setting.RegisterInOptionsUI();
            GameManager.instance.localizationManager.AddSource("en-US", new LocaleEN(m_Setting));


            AssetDatabase.global.LoadSettings(nameof(BrandSelector), m_Setting, new Setting(this));
            BrandListSection brandListSection  = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<BrandListSection>();
        }

        public void OnDispose()
        {
            log.Info(nameof(OnDispose));
            if (m_Setting != null)
            {
                m_Setting.UnregisterInOptionsUI();
                m_Setting = null;
            }
        }
    }
}