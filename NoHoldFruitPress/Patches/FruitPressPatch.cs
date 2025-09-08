using HarmonyLib;
using System.Runtime.CompilerServices;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace NoHoldFruitPress.Patches
{
    internal class AutomationState
    {
        public bool Active { get; set; } = false;
        public float SecondsActive { get; set; } = 0f;
        public IPlayer Player { get; set; }
        public BlockEntityFruitPress FruitPressEntity { get; set; }
        public long AutomationListenerId { get; set; }

        private const float MaxActiveSeconds = 13f;


        public void AutoPressTick(float dt)
        {
            if (!Active || FruitPressEntity == null) return;

            if (SecondsActive < MaxActiveSeconds)
            {
                SecondsActive += dt;
                FruitPressEntity.OnBlockInteractStep(SecondsActive, Player, EnumFruitPressSection.Screw);
            }
            else
            {
                Active = false;
                FruitPressEntity.UnregisterGameTickListener(AutomationListenerId);
                AutomationListenerId = 0;
                FruitPressPatch.AutomatedPresses.Remove(FruitPressEntity);
            }
        }
    }

    [HarmonyPatchCategory("noholdfruitpress")]
    internal static class FruitPressPatch
    {
        internal static ConditionalWeakTable<BlockEntityFruitPress, AutomationState> AutomatedPresses = new();

        [HarmonyPrefix()]
        [HarmonyPatch(typeof(BlockFruitPressTop), "OnBlockInteractStart")]
        public static bool ModifyOnBlockInteractStart(BlockFruitPressTop __instance, ref bool __result,
            IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (AutomateIt(byPlayer, blockSel))
            {
                StartAutoPress(byPlayer, world, blockSel);
                __result = true;
                return false;
            }

            return true;
        }

        [HarmonyPrefix()]
        [HarmonyPatch(typeof(BlockFruitPressTop), "OnBlockInteractStep")]
        public static bool ModifyOnBlockInteractStep(BlockFruitPressTop __instance, ref bool __result,
            float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (AutomateIt(byPlayer, blockSel))
            {
                world.Api.Logger.Debug("Automated, InteractStep, no need to proceed!");
                __result = true;
                return false;
            }

            return true;
        }


        [HarmonyPrefix()]
        [HarmonyPatch(typeof(BlockFruitPressTop), "OnBlockInteractStop")]
        public static bool ModifyOnBlockInteractStop(BlockFruitPressTop __instance, float secondsUsed,
            IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (AutomateIt(byPlayer, blockSel))
            {
                world.Api.Logger.Debug("Automated, InteractStop, no need to proceed!");
                return false;
            }

            return true;
        }

        [HarmonyPrefix()]
        [HarmonyPatch(typeof(BlockFruitPressTop), "OnBlockInteractCancel")]
        public static bool ModifyOnBlockInteractCancel(BlockFruitPressTop __instance, ref bool __result,
            float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel,
            EnumItemUseCancelReason cancelReason)
        {
            if (AutomateIt(byPlayer, blockSel))
            {
                world.Api.Logger.Debug("Automated, InteractCancel, no need to proceed!");
                __result = true;
                return false;
            }

            return true;
        }

        [HarmonyPrefix()]
        [HarmonyPatch(typeof(BlockFruitPressTop), "OnBlockBroken")]
        public static bool ModifyOnBlockBroken(BlockFruitPressTop __instance, IWorldAccessor world, BlockPos pos)
        {
            var be = world.BlockAccessor.GetBlockEntity(pos.DownCopy()) as BlockEntityFruitPress;
            if (be != null)
            {
                AutomatedPresses.Remove(be);
            }

            return true;
        }


        private static bool AutomateIt(IPlayer byPlayer, BlockSelection blockSel)
        {
            if (blockSel.SelectionBoxIndex != 1)
            {
                return false;
            }

            return byPlayer.Entity.Controls.ShiftKey;
        }

        private static void StartAutoPress(IPlayer byPlayer, IWorldAccessor world, BlockSelection blockSel)
        {
            var be = world.BlockAccessor.GetBlockEntity(blockSel.Position.DownCopy()) as BlockEntityFruitPress;
            var state = AutomatedPresses.GetOrCreateValue(be);
            if (state.Active)
            {
                return;
            }

            state.Active = true;
            AutomatedPresses.AddOrUpdate(be, state);
            state.SecondsActive = 0f;
            state.Player = byPlayer;
            state.FruitPressEntity = be;
            be.OnBlockInteractStart(byPlayer, null, EnumFruitPressSection.Screw, true);
            if (state.AutomationListenerId == 0)
            {
                state.AutomationListenerId = be.RegisterGameTickListener(state.AutoPressTick, 25);
            }
        }
    }
}