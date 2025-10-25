using System;
using JetBrains.Annotations;
using Railroader.FirstMod;
using Railroader.ModInterfaces;
using Serilog;

namespace Railroader.SecondMod
{
    [UsedImplicitly]
    public class SecondPlugin : PluginBase<SecondPlugin>, ITopRightButtonPlugin
    {
        public ILogger Logger { get; }

        public SecondPlugin(IModdingContext moddingContext, IMod mod)
            : base(moddingContext, mod) {
            Logger = mod.CreateLogger();
            Logger.Information("SecondPlugin ctor : " + mod.Definition.Identifier);
        }

        protected override void OnIsEnabledChanged() {
            base.OnIsEnabledChanged();
            Logger.Information("SecondPlugin: OnIsEnabledChanged: " + IsEnabled);

            FirstPlugin.Instance.DoSomething();
        }

        string ITopRightButtonPlugin.IconName => "IconName";
        string ITopRightButtonPlugin.Tooltip  => "Tooltip";
        int ITopRightButtonPlugin.   Index    => 1;
        Action ITopRightButtonPlugin.OnClick  => () => { };
    }
}
