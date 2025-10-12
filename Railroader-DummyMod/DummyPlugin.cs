using Railroader.DummyMod.Harmony;
using Railroader.ModInterfaces;

namespace Railroader.DummyMod
{
    public sealed class DummyPlugin : SingletonPluginBase<DummyPlugin>
    {
        public DummyPlugin(IModdingContext moddingContext)
            : base(moddingContext) {
            Logger.Information("DummyPlugin ctor");
        }

        public override void OnEnable() {
            base.OnEnable();
            Logger.Information("OnEnable: " + MainMenuPatch.Number);

            var harmony = new HarmonyLib.Harmony("Railroader.ModInjector");
            harmony.PatchAll(typeof(DummyPlugin).Assembly);
        }

        public override void OnDisable() {
            base.OnDisable();
            Logger.Information("OnDisable");

            var harmony = new HarmonyLib.Harmony("Railroader.ModInjector");
            harmony.UnpatchAll("Railroader.ModInjector");
        }
    }
}
