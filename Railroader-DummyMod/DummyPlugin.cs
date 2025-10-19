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

    public abstract class CustomPluginBase<TPlugin> : PluginBase<TPlugin>, IHarmonyPlugin
        where TPlugin : CustomPluginBase<TPlugin>
    {
        protected CustomPluginBase(IModdingContext moddingContext, IMod mod) : base(moddingContext, mod) {
        }
    }

    public class CustomPlugin : CustomPluginBase<CustomPlugin>
    {
        public CustomPlugin(IModdingContext moddingContext, IMod mod) : base(moddingContext, mod) {
        }
    }
}
