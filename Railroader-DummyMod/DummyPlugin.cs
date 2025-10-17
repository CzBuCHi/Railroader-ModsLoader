using System;
using JetBrains.Annotations;
using Railroader.ModInterfaces;
using Serilog;

namespace Railroader.DummyMod
{
    [UsedImplicitly]
    public class DummyPlugin : SingletonPluginBase<DummyPlugin>, IHarmonyPlugin, ITopRightButtonPlugin
    {
        public ILogger Logger { get; }

        public DummyPlugin(IModdingContext moddingContext, IModDefinition modDefinition)
            : base(moddingContext, modDefinition) {
            Logger = CreateLogger();
            Logger.Information("DummyPlugin ctor : " + modDefinition.Identifier);
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
