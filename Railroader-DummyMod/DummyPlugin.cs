using System;
using JetBrains.Annotations;
using Railroader.ModInterfaces;
using Serilog;

namespace Railroader.DummyMod
{
    [UsedImplicitly]
    public class DummyPlugin : PluginBase<DummyPlugin>, IHarmonyPlugin, ITopRightButtonPlugin
    {
        public ILogger Logger { get; }

        public DummyPlugin(IModdingContext moddingContext, IMod mod)
            : base(moddingContext, mod) {
            Logger = mod.CreateLogger();
            Logger.Information("DummyPlugin ctor : " + mod.Definition.Identifier);
        }

        protected override void OnIsEnabledChanged() {
            base.OnIsEnabledChanged();
            Logger.Information("OnIsEnabledChanged: " + IsEnabled);
        }

        string ITopRightButtonPlugin.IconName => "IconName";
        string ITopRightButtonPlugin.Tooltip  => "Tooltip";
        int ITopRightButtonPlugin.   Index    => 1;
        Action ITopRightButtonPlugin.OnClick  => () => { };
    }
}
