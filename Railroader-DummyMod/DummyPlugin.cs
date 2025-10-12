using Railroader.ModInterfaces;

namespace Railroader.DummyMod
{
    internal sealed class DummyPlugin : PluginBase
    {
        public DummyPlugin(IModdingContext moddingContext) 
            : base(moddingContext) {
            Logger.Information("DummyPlugin ctor");
        }

        public override void OnEnable() {
            base.OnEnable();
            Logger.Information("OnEnable");
        }

        public override void OnDisable() {
            base.OnDisable();
            Logger.Information("OnDisable");
        }
    }
}