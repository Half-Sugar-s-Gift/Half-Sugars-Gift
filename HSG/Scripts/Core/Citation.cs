using Nebula.Utilities;
using Virial;
using Virial.Assignable;
using Virial.Text;
using Color = Virial.Color;

namespace hvtXsvc.Core
{

    public static class Citations
    {

        public static Citation hvtXsvc_hsg { get; private set; } = new(
            "hvtXsvc",
            NebulaAPI.AddonAsset.GetResource("Citat/HalfSugarGift.png")?.AsImage(125f),
            new ColorTextComponent(
                new UnityEngine.Color(0f, 1f, 1f),                   
                new RawTextComponent("hvtXsvc")
            ),
            "http://www.sanctuaryqwq.site/"
        );
        public static Citation AmongUs { get; private set; } = new(
            "Innersloth",
            NebulaAPI.AddonAsset.GetResource("Citat/AmongUs.png")?.AsImage(125f),
            new ColorTextComponent(
                new UnityEngine.Color(0f, 0f, 0f),
                new RawTextComponent("Innersloth")
            ),
            "https://www.innersloth.com/games/among-us/"
        );
    }
}