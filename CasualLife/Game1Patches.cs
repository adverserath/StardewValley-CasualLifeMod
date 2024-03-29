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
    class Game1Patches
    {
        public static ModConfig Config;
        private static IMonitor Monitor;

        public static int MillisecondsPerSecond { get { return Config.MillisecondsPerSecond; } set { Config.MillisecondsPerSecond = value; } }
        public static bool DoLighting { get { return Config.ControlDayLightLevels; } set { Config.ControlDayLightLevels = value; } }
        public static bool DisplaySunTimes { get { return Config.DisplaySunTimes; } set { Config.DisplaySunTimes = value; } }



        public static void Initialize(IMonitor monitor)
        {
            Monitor = monitor;
        }

        private static int lightDay = 0;
        private static float seasonColor;
        private static int sunRiseTime;
        private static int sunSetTime;
        private static int lastLightUpdate = 0;
        public static bool UpdateGameClock(GameTime time)
        {
            if (Game1.shouldTimePass() && !Game1.IsClient)
            {
                Game1.gameTimeInterval += time.ElapsedGameTime.Milliseconds;
            }

            if (lastLightUpdate != Game1.timeOfDay)
            {

                if (DoLighting)// && Game1.IsMasterGame)
                {
                    if (lightDay != Game1.dayOfMonth)
                    {
                        int multiplier = 300;
                        if (Game1.currentSeason == "spring")
                        {
                            seasonColor = (254 - multiplier * ((float)(Math.Abs((14 - (29 - Game1.dayOfMonth) - 27) * -1)) / 100));
                        }
                        else if (Game1.currentSeason == "summer")
                        {
                            seasonColor = 254 - multiplier * (((float)Math.Abs((14 - Game1.dayOfMonth) * -1)) / 100);
                        }
                        else if (Game1.currentSeason == "fall")
                        {
                            seasonColor = (254 - multiplier * (((float)(Math.Abs((14 - (Game1.dayOfMonth) - 27) * -1))) / 100));
                        }
                        else if (Game1.currentSeason == "winter")
                        {
                            seasonColor = (254 - multiplier * (((float)(55 - Math.Abs(((Game1.dayOfMonth) - 14) * -1))) / 100));
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
                        if (DisplaySunTimes)
                        {
                            lightDay = Game1.dayOfMonth;
                            string sunriseStr = sunRiseTime.ToString();
                            string sunsetStr = sunSetTime.ToString();
                            Game1.addHUDMessage(new HUDMessage($"Today the sun will rise at {sunriseStr.Insert(sunriseStr.Length - 2, ":")} and set at {sunsetStr.Insert(sunsetStr.Length - 2, ":")}", ""));
                        }

                    }



                    float timeOfDayDivisable = Game1.timeOfDay / 100 * 100 + ((Game1.timeOfDay % 100) / 60f * 100) + ((float)Game1.gameTimeInterval / MillisecondsPerSecond);
                    float baseCalc = (1 - (float)((Math.Cos(Math.Sqrt(Math.Pow((timeOfDayDivisable - 2500) * -1, 2)) / 100 / 12 * Math.PI) / 2 + 0.5) / 1.1 + 0.05));
                    float lightByTime = ((241 - (seasonColor * baseCalc)));

                    int R = (int)lightByTime;
                    int B = (int)lightByTime;
                    int G = (int)lightByTime;
                    int secondsOfDay = getTimeInSeconds(Game1.timeOfDay);
                    int sunRiseSeconds = getTimeInSeconds(sunRiseTime);
                    int sunSetSeconds = getTimeInSeconds(sunSetTime);

                    if (secondsOfDay < sunRiseSeconds + 60)
                    {
                        float difference = 1 - (float)((sunRiseSeconds + 60) - secondsOfDay) / (sunRiseSeconds + 60);
                        R = (int)MathHelper.Lerp(Game1.morningColor.R, lightByTime, difference);
                        G = (int)MathHelper.Lerp(Game1.morningColor.G, lightByTime, difference);
                        B = (int)MathHelper.Lerp(Game1.morningColor.B, lightByTime, difference);
                    }
                    else if (secondsOfDay < sunSetSeconds)
                    {
                        R = (int)lightByTime;
                        G = (int)lightByTime;
                        B = (int)lightByTime;
                    }
                    else if (secondsOfDay < sunSetSeconds + 180)
                    {
                        float difference = 1 - (float)(sunSetSeconds + 180 - secondsOfDay) / 180f;
                        R = (int)MathHelper.Lerp(lightByTime, Game1.eveningColor.R, difference);
                        G = (int)MathHelper.Lerp(lightByTime, Game1.eveningColor.G, difference);
                        B = (int)MathHelper.Lerp(lightByTime, Game1.eveningColor.B, difference);
                    }
                    else
                    {
                        R = Game1.eveningColor.R;
                        G = Game1.eveningColor.G;
                        B = Game1.eveningColor.B;
                    }
                    Game1.outdoorLight = new Color(R, G, B, 254);
                }
                else
                {
                    lightDay = 0;
                    if (Game1.timeOfDay >= Game1.getTrulyDarkTime())
                    {
                        int adjustedTime2 = (int)((float)(Game1.timeOfDay - Game1.timeOfDay % 100) + (float)(Game1.timeOfDay % 100 / 10) * 16.66f);
                        float transparency2 = Math.Min(0.93f, 0.75f + ((float)(adjustedTime2 - Game1.getTrulyDarkTime()) + (float)Game1.gameTimeInterval / 7000f * 16.6f) * 0.000625f);
                        Game1.outdoorLight = (Game1.IsRainingHere() ? Game1.ambientLight : Game1.eveningColor) * transparency2;
                    }
                    else if (Game1.timeOfDay >= Game1.getStartingToGetDarkTime())
                    {
                        int adjustedTime = (int)((float)(Game1.timeOfDay - Game1.timeOfDay % 100) + (float)(Game1.timeOfDay % 100 / 10) * 16.66f);
                        float transparency = Math.Min(0.93f, 0.3f + ((float)(adjustedTime - Game1.getStartingToGetDarkTime()) + (float)Game1.gameTimeInterval / 7000f * 16.6f) * 0.00225f);
                        Game1.outdoorLight = (Game1.IsRainingHere() ? Game1.ambientLight : Game1.eveningColor) * transparency;
                    }
                    else if (Game1.IsRainingHere())
                    {
                        Game1.outdoorLight = Game1.ambientLight * 0.3f;
                    }
                }
                lastLightUpdate = Game1.timeOfDay;
            }

            if (Game1.currentLocation != null && Game1.gameTimeInterval > MillisecondsPerSecond + Game1.currentLocation.getExtraMillisecondsPerInGameMinuteForThisLocation())
            {
                if (Game1.panMode)
                {
                    Game1.gameTimeInterval = 0;
                }
                Game1.performTenMinuteClockUpdate();
            }
            
            return false;
        }

        private static int getTimeInSeconds(int time)
        {
            return (time / 100 * 60) + time % 100; ;
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
                    if (Game1.IsMasterGame && Game1.farmEvent == null)
                    {
                        Game1.netWorldState.Value.UpdateFromGame1();
                    }
                    return;
                }
                if (Game1.timeOfDay % 100 >= 60)
                {
                    Game1.timeOfDay = Game1.timeOfDay - Game1.timeOfDay % 100 + 100;
                }
                Game1.timeOfDay = Math.Min(Game1.timeOfDay, 2600);
                if (Game1.isLightning && Game1.timeOfDay < 2400 && Game1.IsMasterGame)
                {
                    Utility.performLightningUpdate(Game1.timeOfDay);
                }

                if (Game1.timeOfDay == trulyDarkTime)
                {
                    Game1.currentLocation.switchOutNightTiles();
                }
                else if (Game1.timeOfDay == Game1.getModeratelyDarkTime())
                {
                    if (Game1.currentLocation.IsOutdoors && !Game1.IsRainingHere())
                    {
                        Game1.ambientLight = Color.White;
                    }
                    if (!Game1.IsRainingHere() && !(Game1.currentLocation is MineShaft) && Game1.currentSong != null && !Game1.currentSong.Name.Contains("ambient") && Game1.currentLocation is Town)
                    {
                        Game1.changeMusicTrack("none", false, Game1.MusicContext.Default);
                    }
                }

                if (Game1.getMusicTrackName(Game1.MusicContext.Default).StartsWith(Game1.currentSeason) && !Game1.getMusicTrackName(Game1.MusicContext.Default).Contains("ambient") && !Game1.eventUp && Game1.isDarkOut())
                {
                    Game1.changeMusicTrack("none", true, Game1.MusicContext.Default);
                }
                if (Game1.currentLocation.IsOutdoors && !Game1.IsRainingHere() && !Game1.eventUp && Game1.getMusicTrackName(Game1.MusicContext.Default).Contains("day") && Game1.isDarkOut())
                {
                    Game1.changeMusicTrack("none", true, Game1.MusicContext.Default);
                }
                if (Game1.weatherIcon == 1)
                {
                    int num = Convert.ToInt32(Game1.temporaryContent.Load<Dictionary<string, string>>("Data\\Festivals\\" + Game1.currentSeason + Game1.dayOfMonth)["conditions"].Split('/')[1].Split(' ')[0]);
                    if (Game1.whereIsTodaysFest == null)
                    {
                        Game1.whereIsTodaysFest = Game1.temporaryContent.Load<Dictionary<string, string>>("Data\\Festivals\\" + Game1.currentSeason + Game1.dayOfMonth)["conditions"].Split('/')[0];
                    }
                    if (Game1.timeOfDay == num)
                    {
                        Dictionary<string, string> dictionary = Game1.temporaryContent.Load<Dictionary<string, string>>("Data\\Festivals\\" + Game1.currentSeason + Game1.dayOfMonth);
                        string text = dictionary["conditions"].Split('/')[0];
                        if (dictionary.ContainsKey("locationDisplayName"))
                        {
                            text = dictionary["locationDisplayName"];
                        }
                        else
                        {
                            switch (text)
                            {
                                case "Forest":
                                    text = (Game1.currentSeason.Equals("winter") ? Game1.content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.2634") : Game1.content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.2635"));
                                    break;
                                case "Town":
                                    text = Game1.content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.2637");
                                    break;
                                case "Beach":
                                    text = Game1.content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.2639");
                                    break;
                            }
                        }

                        Game1.showGlobalMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.2640", Game1.temporaryContent.Load<Dictionary<string, string>>("Data\\Festivals\\" + Game1.currentSeason + Game1.dayOfMonth)["name"]) + text);
                    }
                }
                Game1.player.performTenMinuteUpdate();
                switch (Game1.timeOfDay)
                {
                    case 1200:
                        if ((bool)Game1.currentLocation.isOutdoors && !Game1.IsRainingHere() && (Game1.currentSong == null || Game1.currentSong.IsStopped || Game1.currentSong.Name.ToLower().Contains("ambient")))
                        {
                            Game1.playMorningSong();
                        }

                        break;
                    case 2000:
                        if (!Game1.IsRainingHere() && Game1.currentLocation is Town)
                        {
                            Game1.changeMusicTrack("none");
                        }

                        break;
                    case 2400:
                        Game1.dayTimeMoneyBox.timeShakeTimer = 2000;
                        Game1.player.doEmote(24);
                        Game1.showGlobalMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.2652"));
                        break;
                    case 2500:
                        Game1.dayTimeMoneyBox.timeShakeTimer = 2000;
                        Game1.player.doEmote(24);
                        break;
                    case 2600:
                        Game1.dayTimeMoneyBox.timeShakeTimer = 2000;
                        if (Game1.player.mount != null)
                        {
                            Game1.player.mount.dismount();
                        }

                        if (Game1.player.IsSitting())
                        {
                            Game1.player.StopSitting(animate: false);
                        }

                        if (Game1.player.UsingTool)
                        {
                            if (Game1.player.CurrentTool != null)
                            {
                                FishingRod fishingRod = Game1.player.CurrentTool as FishingRod;
                                if (fishingRod != null && (fishingRod.isReeling || fishingRod.pullingOutOfWater))
                                {
                                    break;
                                }
                            }

                            Game1.player.completelyStopAnimatingOrDoingAction();
                        }

                        break;
                    case 2800:
                        if (Game1.activeClickableMenu != null)
                        {
                            Game1.activeClickableMenu.emergencyShutDown();
                            Game1.exitActiveMenu();
                        }

                        Game1.player.startToPassOut();
                        if (Game1.player.mount != null)
                        {
                            Game1.player.mount.dismount();
                        }

                        break;
                }

                foreach (GameLocation location in Game1.locations)
                {
                    GameLocation gameLocation = location;
                    if (gameLocation.NameOrUniqueName == Game1.currentLocation.NameOrUniqueName)
                    {
                        gameLocation = Game1.currentLocation;
                    }

                    gameLocation.performTenMinuteUpdate(Game1.timeOfDay);
                    if (gameLocation is Farm)
                    {
                        ((Farm)gameLocation).timeUpdate(10);
                    }
                }

                MineShaft.UpdateMines10Minutes(Game1.timeOfDay);
                VolcanoDungeon.UpdateLevels10Minutes(Game1.timeOfDay);
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
                int returnVal = (int)(MillisecondsPerSecond * 1.285);
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
