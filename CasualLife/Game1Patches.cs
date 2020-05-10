using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Network;
using StardewValley.Tools;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CasualLife
{
    public class Game1Patches
    {
        private static IMonitor Monitor;

        public static void Initialize(IMonitor monitor)
        {
            Monitor = monitor;
        }
        private static int gameSpeed = 1000;

        public static bool UpdateGameClock(GameTime time)
        {
            
            if (Game1.shouldTimePass() && !Game1.IsClient)
            {
                Game1.gameTimeInterval = Game1.gameTimeInterval + time.ElapsedGameTime.Milliseconds;
            }
            if (Game1.timeOfDay >= Game1.getTrulyDarkTime())
            {
                int num = (int)((float)(Game1.timeOfDay - Game1.timeOfDay % 100) + (float)(Game1.timeOfDay % 100 / 10) * 16.66f);
                float single = Math.Min(0.93f, 0.75f + ((float)(num - Game1.getTrulyDarkTime()) + (float)Game1.gameTimeInterval / gameSpeed * 16.6f) * 0.000625f);
                Game1.outdoorLight = (Game1.isRaining ? Game1.ambientLight : Game1.eveningColor) * single;
            }
            else if (Game1.timeOfDay >= Game1.getStartingToGetDarkTime())
            {
                int num1 = (int)((float)(Game1.timeOfDay - Game1.timeOfDay % 100) + (float)(Game1.timeOfDay % 100 / 10) * 16.66f);
                float single1 = Math.Min(0.93f, 0.3f + ((float)(num1 - Game1.getStartingToGetDarkTime()) + (float)Game1.gameTimeInterval / gameSpeed * 16.6f) * 0.00225f);
                Game1.outdoorLight = (Game1.isRaining ? Game1.ambientLight : Game1.eveningColor) * single1;
            }
            else if (Game1.bloom != null && Game1.timeOfDay >= Game1.getStartingToGetDarkTime() - 100 && Game1.bloom.Visible)
            {
                Game1.bloom.Settings.BloomThreshold = Math.Min(1f, Game1.bloom.Settings.BloomThreshold + 0.0004f);
            }
            else if (Game1.isRaining)
            {
                Game1.outdoorLight = Game1.ambientLight * 0.3f;
            }
            if (Game1.currentLocation != null && Game1.gameTimeInterval > gameSpeed + Game1.currentLocation.getExtraMillisecondsPerInGameMinuteForThisLocation())
            {

                if (Game1.panMode)
                {
                    Game1.gameTimeInterval = 0;
                }
                Game1.performTenMinuteClockUpdate();
            }
            return false;
        }

        public static bool performTenMinuteClockUpdate(ref ModHooks ___hooks)
        {
            ___hooks.OnGame1_PerformTenMinuteClockUpdate(() =>
            {
                int trulyDarkTime = Game1.getTrulyDarkTime();
                Game1.gameTimeInterval = 0;
                if (Game1.IsMasterGame)
                {
                    Game1.timeOfDay++;
                }
                if (Game1.timeOfDay % 10 != 0)
                {
                    return;
                }
                if (Game1.timeOfDay % 100 >= 60)
                {
                    Game1.timeOfDay = Game1.timeOfDay - Game1.timeOfDay % 100 + 100;
                }
                Game1.timeOfDay = Math.Min(Game1.timeOfDay, 2600);
                if (Game1.isLightning && Game1.timeOfDay < 2400 && Game1.IsMasterGame)
                {
                    Utility.performLightningUpdate();
                }
                if (Game1.timeOfDay == trulyDarkTime)
                {
                    Game1.currentLocation.switchOutNightTiles();
                }
                else if (Game1.timeOfDay == Game1.getModeratelyDarkTime())
                {
                    if (Game1.currentLocation.IsOutdoors && !Game1.isRaining)
                    {
                        Game1.ambientLight = Color.White;
                    }
                    if (!Game1.isRaining && !(Game1.currentLocation is MineShaft) && Game1.currentSong != null && !Game1.currentSong.Name.Contains("ambient") && Game1.currentLocation is Town)
                    {
                        Game1.changeMusicTrack("none", false, Game1.MusicContext.Default);
                    }
                }
                if (Game1.getMusicTrackName(Game1.MusicContext.Default).StartsWith(Game1.currentSeason) && !Game1.getMusicTrackName(Game1.MusicContext.Default).Contains("ambient") && !Game1.eventUp && Game1.isDarkOut())
                {
                    Game1.changeMusicTrack("none", true, Game1.MusicContext.Default);
                }
                if (Game1.currentLocation.isOutdoors && !Game1.isRaining && !Game1.eventUp && Game1.getMusicTrackName(Game1.MusicContext.Default).Contains("day") && Game1.isDarkOut())
                {
                    Game1.changeMusicTrack("none", true, Game1.MusicContext.Default);
                }
                if (Game1.weatherIcon == 1)
                {
                    int num = Convert.ToInt32(Game1.temporaryContent.Load<Dictionary<string, string>>(string.Concat(string.Concat("Data\\Festivals\\", Game1.currentSeason), Game1.dayOfMonth))["conditions"].Split(new char[] { '/' })[1].Split(new char[] { ' ' })[0]);
                    if (Game1.whereIsTodaysFest == null)
                    {
                        Game1.whereIsTodaysFest = Game1.temporaryContent.Load<Dictionary<string, string>>(string.Concat(string.Concat("Data\\Festivals\\", Game1.currentSeason), Game1.dayOfMonth))["conditions"].Split(new char[] { '/' })[0];
                    }
                    if (Game1.timeOfDay == num)
                    {
                        string str = Game1.temporaryContent.Load<Dictionary<string, string>>(string.Concat(string.Concat("Data\\Festivals\\", Game1.currentSeason), Game1.dayOfMonth))["conditions"].Split(new char[] { '/' })[0];
                        if (str == "Forest")
                        {
                            str = (Game1.currentSeason.Equals("winter") ? Game1.content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.2634") : Game1.content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.2635"));
                        }
                        else if (str == "Town")
                        {
                            str = Game1.content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.2637");
                        }
                        else if (str == "Beach")
                        {
                            str = Game1.content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.2639");
                        }
                        Game1.showGlobalMessage(string.Concat(Game1.content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.2640", Game1.temporaryContent.Load<Dictionary<string, string>>(string.Concat(string.Concat("Data\\Festivals\\", Game1.currentSeason), Game1.dayOfMonth))["name"]), str));
                    }
                }
                Game1.player.performTenMinuteUpdate();
                int num1 = Game1.timeOfDay;
                if (num1 <= 2400)
                {
                    if (num1 == 1200)
                    {
                        if (Game1.currentLocation.isOutdoors && !Game1.isRaining && (Game1.currentSong == null || Game1.currentSong.IsStopped || Game1.currentSong.Name.ToLower().Contains("ambient")))
                        {
                            Game1.playMorningSong();
                        }
                    }
                    else if (num1 != 2000)
                    {
                        if (num1 == 2400)
                        {
                            Game1.dayTimeMoneyBox.timeShakeTimer = 2000;
                            Game1.player.doEmote(24);
                            Game1.showGlobalMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.2652"));
                        }
                    }
                    else if (!Game1.isRaining && Game1.currentLocation is Town)
                    {
                        Game1.changeMusicTrack("none", false, Game1.MusicContext.Default);
                    }
                }
                else if (num1 == 2500)
                {
                    Game1.dayTimeMoneyBox.timeShakeTimer = 2000;
                    Game1.player.doEmote(24);
                }
                else if (num1 == 2600)
                {
                    Game1.dayTimeMoneyBox.timeShakeTimer = 2000;
                    if (Game1.player.mount != null)
                    {
                        Game1.player.mount.dismount(false);
                    }
                    if (Game1.player.UsingTool)
                    {
                        if (Game1.player.CurrentTool != null)
                        {
                            FishingRod currentTool = Game1.player.CurrentTool as FishingRod;
                            FishingRod fishingRod = currentTool;
                            if (currentTool != null && (fishingRod.isReeling || fishingRod.pullingOutOfWater))
                            {
                                foreach (GameLocation location in Game1.locations)
                                {
                                    location.performTenMinuteUpdate(Game1.timeOfDay);
                                    if (!(location is Farm))
                                    {
                                        continue;
                                    }
                                    ((Farm)location).timeUpdate(10);
                                }
                                MineShaft.UpdateMines10Minutes(Game1.timeOfDay);
                                if (Game1.IsMasterGame && Game1.farmEvent == null)
                                {
                                    Game1.netWorldState.Value.UpdateFromGame1();
                                }
                                return;
                            }
                        }
                        Game1.player.completelyStopAnimatingOrDoingAction();
                    }
                }
                else if (num1 == 2800)
                {
                    if (Game1.activeClickableMenu != null)
                    {
                        Game1.activeClickableMenu.emergencyShutDown();
                        Game1.exitActiveMenu();
                    }
                    Game1.player.startToPassOut();
                    if (Game1.player.mount != null)
                    {
                        Game1.player.mount.dismount(false);
                    }
                }
                foreach (GameLocation gameLocation in Game1.locations)
                {
                    gameLocation.performTenMinuteUpdate(Game1.timeOfDay);
                    if (!(gameLocation is Farm))
                    {
                        continue;
                    }
                    ((Farm)gameLocation).timeUpdate(10);
                }
                MineShaft.UpdateMines10Minutes(Game1.timeOfDay);
                if (Game1.IsMasterGame && Game1.farmEvent == null)
                {
                    Game1.netWorldState.Value.UpdateFromGame1();
                }
            });
            return false;
        }
        public static bool getExtraMillisecondsPerInGameMinuteForThisLocation(MineShaft __instance, ref int __result)
        {
            if (!Game1.IsMultiplayer || (Game1.IsMultiplayer &&
                Game1.otherFarmers.Any() &&
                Game1.otherFarmers.Roots.All
                (f => ((NetFarmerRoot)f.Value).Value.currentLocation is MineShaft
                && ((MineShaft)((NetFarmerRoot)f.Value).Value.currentLocation).mineLevel == MineShaft.desertArea)))
            {
                int returnVal = (int)(gameSpeed * 1.285);
                __result = returnVal;
            }
            if (__instance.getMineArea(-1) != MineShaft.desertArea)
            {
                __result = 0;
            }

            return false;
        }
    }
}
