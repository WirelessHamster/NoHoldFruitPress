using HarmonyLib;
using Vintagestory.API.Common;

namespace NoHoldFruitPress.Patches
{
    public class NoHoldFruitPressModSystem : ModSystem
    {
        private Harmony patcher;


        public override void Start(ICoreAPI api)
        {
            if (!Harmony.HasAnyPatches(Mod.Info.ModID))
            {
                patcher = new Harmony(Mod.Info.ModID);
                patcher.PatchCategory(Mod.Info.ModID);
            }
        }

        public override void Dispose()
        {
            patcher?.UnpatchAll(Mod.Info.ModID);
        }

    }
}
