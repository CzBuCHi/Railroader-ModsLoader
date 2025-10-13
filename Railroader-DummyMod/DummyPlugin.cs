using Railroader.DummyMod.Harmony;
using Railroader.ModInterfaces;
using Serilog;

namespace Railroader.DummyMod
{
    public sealed class DummyPlugin : SingletonPluginBase<DummyPlugin>
    {
        private readonly ILogger _Logger;

        public DummyPlugin(IModdingContext moddingContext, IModDefinition modDefinition)
            : base(moddingContext, modDefinition) {
            
            _Logger = CreateLogger();
            _Logger.Information("DummyPlugin ctor : " + modDefinition.Id);
        }

        public override void OnEnable() {
            base.OnEnable();
            _Logger.Information("OnEnable: " + MainMenuPatch.Number);

            var harmony = new HarmonyLib.Harmony("Railroader.ModInjector");
            harmony.PatchAll(typeof(DummyPlugin).Assembly);
        }

        public override void OnDisable() {
            base.OnDisable();
            _Logger.Information("OnDisable");

            var harmony = new HarmonyLib.Harmony("Railroader.ModInjector");
            harmony.UnpatchAll("Railroader.ModInjector");
        }
    }
}
