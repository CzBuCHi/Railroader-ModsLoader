using Markroader;
using UI.Builder;
using UI.Common;
using UI.Menu;
using UnityEngine;

namespace Railroader.ModInjector.Features.Menu;

public class ModsMenu : BuilderMenuBase
{
    protected override void BuildPanelContent(UIPanelBuilder builder)
    {
        var text = TMPMarkupRenderer.Render(
            Parser.Parse("""

                         <align="center">
                         # Credits

                         ### Creator/Lead Developer
                         Adam Preble

                         ### Producer
                         Connor Doornbos
                         
                         </align>

                         """)
            )!;
        builder.AddTextArea(text, obj => { }).FlexibleHeight();
        builder.Spacer(16f);
        builder.HStack(uIPanelBuilder => {
            uIPanelBuilder.Spacer().FlexibleWidth(1f);
            uIPanelBuilder.AddButton("Back", () => { this.NavigationController().Pop(); });
            uIPanelBuilder.Spacer(22f);
            uIPanelBuilder.Spacer().FlexibleWidth(1f);
        });
        builder.Spacer(8f);
    }
}


