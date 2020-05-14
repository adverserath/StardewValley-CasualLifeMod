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
        private static int gameSpeed = 50;
        private static int lightDay = 0;
        private static float seasonColor;
        private static float inverseSeasonColor;
        private static int sunRiseTime;
        private static int sunRiseDec;
        private static int sunSetTime;
        private static int sunSetDec;
        public static bool UpdateGameClock(GameTime time)
        {
            if (lightDay != Game1.dayOfMonth)
            {
                lightDay = Game1.dayOfMonth;
                int multiplier = 300;
                if (Game1.currentSeason == "spring")
                {
                    seasonColor = (254 - multiplier * ((float)(Math.Abs((14 - (29 - Game1.dayOfMonth) - 27) * -1)) / 100));
                    inverseSeasonColor = (254 - multiplier * (((float)(Math.Abs((14 - (Game1.dayOfMonth) - 27) * -1)) / 100)));
                }
                else if (Game1.currentSeason == "summer")
                {
                    seasonColor = 254 - multiplier * (((float)Math.Abs((14 - Game1.dayOfMonth) * -1)) / 100);
                    inverseSeasonColor = (270 - multiplier * (((float)(55 - Math.Abs(((Game1.dayOfMonth) - 14) * -1))) / 100));
                }
                else if (Game1.currentSeason == "fall")
                {
                    seasonColor = (254 - multiplier * (((float)(Math.Abs((14 - (Game1.dayOfMonth) - 27) * -1))) / 100));
                    inverseSeasonColor = (254 - multiplier * ((float)((Math.Abs((14 - (29 - Game1.dayOfMonth) - 27) * -1))) / 100));
                }
                else if (Game1.currentSeason == "winter")
                {
                    seasonColor = (254 - multiplier * (((float)(55 - Math.Abs(((Game1.dayOfMonth) - 14) * -1))) / 100));
                    inverseSeasonColor = (254 - multiplier * (((float)Math.Abs((14 - Game1.dayOfMonth) * -1)) / 100));
                }
                sunRiseTime = (int)(700 + (400 - (seasonColor - 90) * 5) / 2);
                if (sunRiseTime % 100 >= 60)
                {
                    sunRiseTime = sunRiseTime - sunRiseTime % 100 + 100 + sunRiseTime % 100 % 60;
                }
                sunSetTime = (int)(2000 - (400 - (seasonColor - 90) * 5));
                if (sunSetTime % 100 >= 60)
                {
                    sunSetTime = sunSetTime - sunSetTime % 100 + 100 + sunSetTime % 100 % 60;

                }
            }

            if (Game1.shouldTimePass() && !Game1.IsClient)
            {
                Game1.gameTimeInterval = Game1.gameTimeInterval + time.ElapsedGameTime.Milliseconds;
            }


            float timeOfDayDivisable = Game1.timeOfDay / 100 * 100 + ((Game1.timeOfDay % 100) / 60f * 100) + ((float)Game1.gameTimeInterval / gameSpeed);
            float baseCalc = (1 - (float)((Math.Cos(Math.Sqrt(Math.Pow((timeOfDayDivisable - 2500) * -1, 2)) / 100 / 12 * Math.PI) / 2 + 0.5) / 1.1 + 0.05));
            float lightByTime = ((241 - (seasonColor * baseCalc)));

            int R = (int)lightByTime;
            int B = (int)lightByTime;
            int G = (int)lightByTime;

            int difference = sunRiseTime - Game1.timeOfDay;
            difference = Math.Abs(sunRiseTime - Game1.timeOfDay) - ((Math.Abs(sunRiseTime / 100 - Game1.timeOfDay / 100)) * 40);

            if (Game1.timeOfDay <= sunRiseTime || Game1.timeOfDay > sunSetTime)
            {
                R = 230;
                G = 230;
                B = 230;
            }
            else if (Game1.timeOfDay <= (sunRiseTime + 100)|| Game1.timeOfDay > (sunSetTime - 100))
            {
                R =B=G= (int)(230 - ((230 - lightByTime) * (float)difference/ 60f));

            }

            Game1.outdoorLight = new Color(R,G,B, 255);

            if (Game1.bloom != null && Game1.bloom.Visible)
            {
                Game1.bloom.Settings.BloomThreshold = Math.Min(1f, Game1.bloom.Settings.BloomThreshold + lightByTime);
            }


            if (Game1.currentLocation != null && Game1.gameTimeInterval > gameSpeed + Game1.currentLocation.getExtraMillisecondsPerInGameMinuteForThisLocation())
            {
                //Game1.timeOfDay +=10;

                //if (Game1.timeOfDay > 2540)
                //{
                //    Game1.dayOfMonth++;
                //    Game1.timeOfDay = 600;
                //}
                //if (Game1.dayOfMonth > 28)
                //{
                //    Game1.dayOfMonth = 1;
                //    if (Game1.currentSeason == "spring")
                //    {
                //        Game1.currentSeason = "summer";
                //    }
                //    else if (Game1.currentSeason == "summer")
                //    {
                //        Game1.currentSeason = "fall";
                //    }
                //    else if (Game1.currentSeason == "fall")
                //    {
                //        Game1.currentSeason = "winter";
                //    }
                //    else if (Game1.currentSeason == "winter")
                //    {
                //        Game1.currentSeason = "spring";
                //    }
                //}
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
