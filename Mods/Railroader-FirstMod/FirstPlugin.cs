using JetBrains.Annotations;
using Railroader.ModInterfaces;
using Serilog;

namespace Railroader.FirstMod
{
    [PublicAPI]
    public class FirstPlugin : PluginBase<FirstPlugin>, IHarmonyPlugin
    {
        public ILogger Logger { get; }

        public FirstPlugin(IModdingContext moddingContext, IMod mod)
            : base(moddingContext, mod) {
            Logger = mod.CreateLogger();
            Logger.Information("FirstPlugin ctor : " + mod.Definition.Identifier);
        }

        protected override void OnIsEnabledChanged() {
            base.OnIsEnabledChanged();
            Logger.Information("FirstPlugin: OnIsEnabledChanged: " + IsEnabled);
            DoSomething();
        }

        public void DoSomething() {
            Logger.Information("FirstPlugin: DoSomething called");
        }
    }
}
