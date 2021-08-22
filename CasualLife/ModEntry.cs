using System;
using HarmonyLib;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Menus;

namespace CasualLife
{
    public class ModEntry : Mod
    {
        private ModConfig Config;
        public override void Entry(IModHelper helper)
        {
            this.Config = this.Helper.ReadConfig<ModConfig>();
            Game1Patches.DoLighting = Config.ControlDayLightLevels;

            Harmony harmony = new Harmony(this.ModManifest.UniqueID);
            harmony.Patch(
               original: AccessTools.Method(typeof(Game1), nameof(Game1.performTenMinuteClockUpdate)),
               prefix: new HarmonyMethod(typeof(Game1Patches), nameof(Game1Patches.performTenMinuteClockUpdate))
            );

            if (Config.ControlDayLightLevels)
            {
                harmony.Patch(
                   original: AccessTools.Method(typeof(Game1), nameof(Game1.UpdateGameClock)),
                   prefix: new HarmonyMethod(typeof(Game1Patches), nameof(Game1Patches.UpdateGameClock))
                );
            }

            harmony.Patch(
                original: AccessTools.Method(typeof(DayTimeMoneyBox), "draw", new Type[] { typeof(SpriteBatch) }, null),
                prefix: new HarmonyMethod(typeof(DayTimeMoneyBoxPatch), nameof(DayTimeMoneyBoxPatch.drawFromDecom))
            );

            harmony.Patch(
                original: AccessTools.Method(typeof(DayTimeMoneyBox), "receiveRightClick"),
                prefix: new HarmonyMethod(typeof(DayTimeMoneyBoxPatch), nameof(DayTimeMoneyBoxPatch.receiveRightClick))
            );
            harmony.Patch(
                original: AccessTools.Method(typeof(MineShaft), "getExtraMillisecondsPerInGameMinuteForThisLocation"),
                prefix: new HarmonyMethod(typeof(Game1Patches), nameof(Game1Patches.getExtraMillisecondsPerInGameMinuteForThisLocation))
            );
        }
    }
}