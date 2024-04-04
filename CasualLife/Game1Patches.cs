using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Mods;
using StardewValley.TokenizableStrings;
using StardewValley.Tools;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CasualLife
{
    class Game1Patches
    {
        public static IModHelper helper;
        public static ModConfig Config;
        public static int MillisecondsPerSecond { get { return Config.MillisecondsPerSecond; } set { Config.MillisecondsPerSecond = value; } }
        public static bool DoLighting { get { return Config.ControlDayLightLevels; } set { Config.ControlDayLightLevels = value; } }
        public static bool DisplaySunTimes { get { return Config.DisplaySunTimes; } set { Config.DisplaySunTimes = value; } }
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
                if (DoLighting)
                {
                    if (lightDay != Game1.dayOfMonth)
                    {
                        int multiplier = 300;
                        if (Game1.currentSeason == "spring")
                        {
                            seasonColor = 254 - multiplier * ((float)Math.Abs((14 - (29 - Game1.dayOfMonth) - 27) * -1) / 100);
                        }
                        else if (Game1.currentSeason == "summer")
                        {
                            seasonColor = 254 - multiplier * (((float)Math.Abs((14 - Game1.dayOfMonth) * -1)) / 100);
                        }
                        else if (Game1.currentSeason == "fall")
                        {
                            seasonColor = 254 - multiplier * (((float)Math.Abs((14 - Game1.dayOfMonth - 27) * -1)) / 100);
                        }
                        else if (Game1.currentSeason == "winter")
                        {
                            seasonColor = 254 - multiplier * (((float)(55 - Math.Abs((Game1.dayOfMonth - 14) * -1))) / 100);
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
                            Game1.addHUDMessage(HUDMessage.ForCornerTextbox(helper.Translation.Get("draw.sunsetTime",
                            new
                            {
                                sunrise = sunriseStr.Insert(sunriseStr.Length - 2, ":"),
                                sunset = sunsetStr.Insert(sunsetStr.Length - 2, ":")
                            })));
                        }
                    }

                    float timeOfDayDivisable = Game1.timeOfDay / 100 * 100 + (Game1.timeOfDay % 100 / 60f * 100) + ((float)Game1.gameTimeInterval / MillisecondsPerSecond);
                    float baseCalc = 1 - (float)((Math.Cos(Math.Sqrt(Math.Pow((timeOfDayDivisable - 2500) * -1, 2)) / 100 / 12 * Math.PI) / 2 + 0.5) / 1.1 + 0.05);
                    float lightByTime = 241 - (seasonColor * baseCalc);
                    int secondsOfDay = getTimeInSeconds(Game1.timeOfDay);
                    int sunRiseSeconds = getTimeInSeconds(sunRiseTime);
                    int sunSetSeconds = getTimeInSeconds(sunSetTime);
                    int R;
                    int B;
                    int G;
                    if (secondsOfDay < sunRiseSeconds + 60)
                    {
                        float difference = 1 - (float)(sunRiseSeconds + 60 - secondsOfDay) / (sunRiseSeconds + 60);
                        R = (int)MathHelper.Lerp(Color.LightBlue.R, lightByTime, difference);
                        G = (int)MathHelper.Lerp(Color.LightBlue.G, lightByTime, difference);
                        B = (int)MathHelper.Lerp(Color.LightBlue.B, lightByTime, difference);
                    }
                    else if (secondsOfDay < sunSetSeconds)
                    {
                        R = (int)lightByTime;
                        G = (int)lightByTime;
                        B = (int)lightByTime;
                    }
                    else if (secondsOfDay < sunSetSeconds + 180)
                    {
                        float difference = 1 - (sunSetSeconds + 180 - secondsOfDay) / 180f;
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
                    if (Game1.timeOfDay >= Game1.getTrulyDarkTime(Game1.currentLocation))
                    {
                        int adjustedTime2 = (int)((float)(Game1.timeOfDay - Game1.timeOfDay % 100) + (float)(Game1.timeOfDay % 100 / 10) * 16.66f);
                        float transparency2 = Math.Min(0.93f, 0.75f + ((float)(adjustedTime2 - Game1.getTrulyDarkTime(Game1.currentLocation)) + (float)Game1.gameTimeInterval / (float)Game1.realMilliSecondsPerGameTenMinutes * 16.6f) * 0.000625f);
                        Game1.outdoorLight = (Game1.IsRainingHere() ? Game1.ambientLight : Game1.eveningColor) * transparency2;
                    }
                    else if (Game1.timeOfDay >= Game1.getStartingToGetDarkTime(Game1.currentLocation))
                    {
                        int adjustedTime = (int)((float)(Game1.timeOfDay - Game1.timeOfDay % 100) + (float)(Game1.timeOfDay % 100 / 10) * 16.66f);
                        float transparency = Math.Min(0.93f, 0.3f + ((float)(adjustedTime - Game1.getStartingToGetDarkTime(Game1.currentLocation)) + (float)Game1.gameTimeInterval / 7000f * 16.6f) * 0.00225f);
                        Game1.outdoorLight = (Game1.IsRainingHere() ? Game1.ambientLight : Game1.eveningColor) * transparency;
                    }
                    else if (Game1.IsRainingHere())
                    {
                        Game1.outdoorLight = Game1.ambientLight * 0.3f;
                    }
                    else
                    {
                        Game1.outdoorLight = Game1.ambientLight;
                    }
                }
                lastLightUpdate = Game1.timeOfDay;
            }

            int num = Game1.gameTimeInterval;
            int num2 = Game1.realMilliSecondsPerGameTenMinutes * MillisecondsPerSecond / 7000;
            GameLocation gameLocation = Game1.currentLocation;
            if (num > num2 + ((gameLocation != null) ? new int?(gameLocation.ExtraMillisecondsPerInGameMinute * 10) : null))
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
                int num = Game1.getTrulyDarkTime(Game1.currentLocation) - 100;
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

                if (Game1.timeOfDay == num)
                {
                    Game1.currentLocation.switchOutNightTiles();
                }
                else if (Game1.timeOfDay == Game1.getModeratelyDarkTime(Game1.currentLocation) && Game1.currentLocation.IsOutdoors && !Game1.currentLocation.IsRainingHere())
                {
                    Game1.ambientLight = Color.White;
                }
                if (!Game1.eventUp && Game1.isDarkOut(Game1.currentLocation) && Game1.IsPlayingBackgroundMusic)
                {
                    Game1.changeMusicTrack("none", track_interruptable: true);
                }
                if (Game1.weatherIcon == 1)
                {
                    Dictionary<string, string> dictionary = Game1.temporaryContent.Load<Dictionary<string, string>>("Data\\Festivals\\" + Game1.currentSeason + Game1.dayOfMonth);
                    string[] array = dictionary["conditions"].Split('/');
                    int num2 = Convert.ToInt32(ArgUtility.SplitBySpaceAndGet(array[1], 0));
                    if (Game1.whereIsTodaysFest == null)
                    {
                        Game1.whereIsTodaysFest = array[0];
                    }
                    if (Game1.timeOfDay == num2)
                    {
                        if (dictionary.TryGetValue("startedMessage", out var value))
                        {
                            Game1.showGlobalMessage(TokenParser.ParseText(value));
                        }
                        else
                        {
                            if (!dictionary.TryGetValue("locationDisplayName", out var value2))
                            {
                                value2 = array[0];
                                value2 = value2 switch
                                {
                                    "Forest" => Game1.IsWinter ? Game1.content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.2634") : Game1.content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.2635"),
                                    "Town" => Game1.content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.2637"),
                                    "Beach" => Game1.content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.2639"),
                                    _ => TokenParser.ParseText(GameLocation.GetData(value2)?.DisplayName) ?? value2,
                                };
                            }
                            Game1.showGlobalMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.2640", dictionary["name"]) + value2);
                        }
                    }
                }
                Game1.player.performTenMinuteUpdate();
                switch (Game1.timeOfDay)
                {
                    case 1200:
                        if (Game1.currentLocation.IsOutdoors && !Game1.currentLocation.IsRainingHere() && (Game1.IsPlayingOutdoorsAmbience || Game1.currentSong == null || Game1.isMusicContextActiveButNotPlaying()))
                        {
                            Game1.playMorningSong();
                        }

                        break;
                    case 2000:
                        if (Game1.IsPlayingTownMusic)
                        {
                            Game1.changeMusicTrack("none", track_interruptable: true);
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
                        Game1.player.mount?.dismount();

                        if (Game1.player.IsSitting())
                        {
                            Game1.player.StopSitting(animate: false);
                        }

                        if (Game1.player.UsingTool && (Game1.player.CurrentTool is not FishingRod fishingRod || (!fishingRod.isReeling && !fishingRod.pullingOutOfWater)))
                        {
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

                foreach (string current in Game1.netWorldState.Value.ActivePassiveFestivals)
                {
                    if (Utility.TryGetPassiveFestivalData(current, out var data) && Game1.timeOfDay == data.StartTime && (!data.OnlyShowMessageOnFirstDay || Utility.GetDayOfPassiveFestival(current) == 1))
                    {
                        Game1.showGlobalMessage(TokenParser.ParseText(data.StartMessage));
                    }
                }

                foreach (GameLocation location in Game1.locations)
                {
                    GameLocation current2 = location;
                    if (current2.NameOrUniqueName == Game1.currentLocation.NameOrUniqueName)
                    {
                        current2 = Game1.currentLocation;
                    }
                    current2.performTenMinuteUpdate(Game1.timeOfDay);
                    current2.timeUpdate(10);
                }

                MineShaft.UpdateMines10Minutes(Game1.timeOfDay);
                VolcanoDungeon.UpdateLevels10Minutes(Game1.timeOfDay);
                if (Game1.IsMasterGame && Game1.farmEvent == null)
                {
                    Game1.netWorldState.Value.UpdateFromGame1();
                }
                for (int num3 = Game1.currentLightSources.Count - 1; num3 >= 0; num3--)
                {
                    if (Game1.currentLightSources.ElementAt(num3).color.A <= 0)
                    {
                        Game1.currentLightSources.Remove(Game1.currentLightSources.ElementAt(num3));
                    }
                }
            });
            return false;
        }
    }
}
