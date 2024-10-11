
using Microsoft.Xna.Framework;
using Netcode;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Events;
using StardewValley.GameData;
using StardewValley.Locations;
using StardewValley.Menus;
using StardewValley.Mods;
using StardewValley.Network;
using StardewValley.TokenizableStrings;
using StardewValley.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Xml.Linq;
using xTile.Dimensions;

namespace CasualLife
{
    class Game1Patches
    {
        public static ModConfig Config;
        private static IMonitor Monitor;

        public static int MillisecondsPerSecond { get { return Config.MillisecondsPerSecond; } set { Config.MillisecondsPerSecond = value; } }
        public static bool DoLighting { get { return Config.ControlDayLightLevels; } set { Config.ControlDayLightLevels = value; } }
        public static bool DisplaySunTimes { get { return Config.DisplaySunTimes; } set { Config.DisplaySunTimes = value; } }

        #region Accessors
        public static int dayOfMonth { get { return Game1.dayOfMonth; } }
        public static int realMilliSecondsPerGameTenMinutes { get { return Game1.realMilliSecondsPerGameTenMinutes; } }
        public static int realMilliSecondsPerGameMinute { get { return Game1.realMilliSecondsPerGameMinute; } }
        public static int timeOfDay { get { return Game1.timeOfDay; } }

        public static GameLocation currentLocation { get { return Game1.currentLocation; } }

        public static string currentSeason { get { return Game1.currentSeason; } }

        public static int gameTimeInterval { get { return Game1.gameTimeInterval; } }

        public static bool IsClient { get { return Game1.IsClient; } }
        
        public static Color outdoorLight { get { return Game1.outdoorLight; } private set { Game1.outdoorLight = value; } }
        public static Color ambientLight { get { return Game1.ambientLight; } private set { Game1.ambientLight = value; } }
        public static Color eveningColor { get { return Game1.eveningColor; } private set { Game1.eveningColor = value; } }
        public static Color bgColor { get { return Game1.bgColor; } private set { Game1.bgColor = value; } }
        public static bool panMode { get { return Game1.panMode; } private set { Game1.panMode = value; } }
        public static bool IsWinter { get { return Game1.IsWinter; } }
        public static Farmer player { get { return Game1.player; } private set { Game1.player = value; } }

        public static bool isLightning { get { return Game1.isLightning; } private set { Game1.isLightning = value; } }
        public static bool IsMasterGame { get { return Game1.IsMasterGame; } }
        public static bool IsPlayingBackgroundMusic { get { return Game1.IsPlayingBackgroundMusic; } private set { Game1.IsPlayingBackgroundMusic = value; } }
        public static bool eventUp { get { return Game1.eventUp; } }
        public static int weatherIcon { get { return Game1.weatherIcon; } }
        public static LocalizedContentManager temporaryContent { get { return Game1.temporaryContent; } }
        public static LocalizedContentManager content { get { return Game1.content; } }

        public static string whereIsTodaysFest { get { return Game1.whereIsTodaysFest; } private set { Game1.whereIsTodaysFest = value; } }
        public static bool IsPlayingOutdoorsAmbience { get { return Game1.IsPlayingOutdoorsAmbience; } }
        public static ICue currentSong { get { return Game1.currentSong; } }
        public static bool IsPlayingTownMusic { get { return Game1.IsPlayingTownMusic; } }
        public static DayTimeMoneyBox dayTimeMoneyBox { get { return Game1.dayTimeMoneyBox; } }
        public static IClickableMenu activeClickableMenu { get { return Game1.activeClickableMenu; } }
        public static NetRoot<NetWorldState> netWorldState { get { return Game1.netWorldState; } }
        public static IList<GameLocation> locations => Game1.locations;
        public static FarmEvent farmEvent { get { return Game1.farmEvent; } }
        public static HashSet<LightSource> currentLightSources { get { return Game1.currentLightSources; } }

        public static bool isMusicContextActiveButNotPlaying()
        {
            return Game1.isMusicContextActiveButNotPlaying();
        }
        public static void playMorningSong()
        {
            Game1.playMorningSong();
        }
        public static void exitActiveMenu()
        {
            Game1.exitActiveMenu();
        }

        public static void showGlobalMessage(string message)
        {
            Game1.showGlobalMessage(message);
        }        
        public static void changeMusicTrack(string track, bool track_interruptable)
        {
            Game1.changeMusicTrack(track, track_interruptable);
        }
        public static bool isDarkOut(GameLocation _currentLocation)
        {
            return Game1.isDarkOut(_currentLocation);
        }
        public static int getStartingToGetDarkTime(GameLocation _currentLocation)
        {
            return Game1.getStartingToGetDarkTime(_currentLocation);
        }
        public static int getModeratelyDarkTime(GameLocation _currentLocation)
        {
            return Game1.getModeratelyDarkTime(_currentLocation);
        }
        public static bool IsRainingHere()
        {
            return Game1.IsRainingHere();
        }

        private static int getTrulyDarkTime(GameLocation _currentLocation)
        {
            return Game1.getTrulyDarkTime(_currentLocation);
        }
        private static bool shouldTimePass()
        {
            return Game1.shouldTimePass();
        }
        #endregion
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
            if(Game1.realMilliSecondsPerGameMinute!=MillisecondsPerSecond)
                Game1.realMilliSecondsPerGameMinute = MillisecondsPerSecond;
            if (shouldTimePass() && !IsClient)
            {
                Game1.gameTimeInterval += time.ElapsedGameTime.Milliseconds;
            }

            if (lastLightUpdate != timeOfDay)
            {

                if (DoLighting)// && Game1.IsMasterGame)
                {
                    if (lightDay != dayOfMonth)
                    {
                        int multiplier = 300;
                        if (currentSeason == "spring")
                        {
                            seasonColor = (254 - multiplier * ((float)(Math.Abs((14 - (29 - dayOfMonth) - 27) * -1)) / 100));
                        }
                        else if (currentSeason == "summer")
                        {
                            seasonColor = 254 - multiplier * (((float)Math.Abs((14 - dayOfMonth) * -1)) / 100);
                        }
                        else if (currentSeason == "fall")
                        {
                            seasonColor = (254 - multiplier * (((float)(Math.Abs((14 - (dayOfMonth) - 27) * -1))) / 100));
                        }
                        else if (currentSeason == "winter")
                        {
                            seasonColor = (254 - multiplier * (((float)(55 - Math.Abs(((dayOfMonth) - 14) * -1))) / 100));
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
                            lightDay = dayOfMonth;
                            string sunriseStr = sunRiseTime.ToString();
                            string sunsetStr = sunSetTime.ToString();
                            Game1.addHUDMessage(new HUDMessage($"Today the sun will rise at {sunriseStr.Insert(sunriseStr.Length - 2, ":")} and set at {sunsetStr.Insert(sunsetStr.Length - 2, ":")}", 3500f));
                        }

                    }



                    float timeOfDayDivisable = timeOfDay / 100 * 100 + ((timeOfDay % 100) / 60f * 100) + ((float)gameTimeInterval / MillisecondsPerSecond);
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
                        R = (int)MathHelper.Lerp(bgColor.R, lightByTime, difference);
                        G = (int)MathHelper.Lerp(bgColor.G, lightByTime, difference);
                        B = (int)MathHelper.Lerp(bgColor.B, lightByTime, difference);
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
                        R = (int)MathHelper.Lerp(lightByTime, eveningColor.R, difference);
                        G = (int)MathHelper.Lerp(lightByTime, eveningColor.G, difference);
                        B = (int)MathHelper.Lerp(lightByTime, eveningColor.B, difference);
                    }
                    else
                    {
                        R = eveningColor.R;
                        G = eveningColor.G;
                        B = eveningColor.B;
                    }
                    outdoorLight = new Color(R, G, B, 254);
                }
                else
                {
                    lightDay = 0;

                    if (timeOfDay >= getTrulyDarkTime(currentLocation))
                    {
                        int num = (int)((float)(timeOfDay - timeOfDay % 100) + (float)(timeOfDay % 100 / 10) * 16.66f);
                        float num2 = Math.Min(0.93f, 0.75f + ((float)(num - getTrulyDarkTime(currentLocation)) + (float)gameTimeInterval / (float)realMilliSecondsPerGameTenMinutes * 16.6f) * 0.000625f);
                        outdoorLight = (IsRainingHere() ? ambientLight : eveningColor) * num2;
                    }
                    else if (timeOfDay >= getStartingToGetDarkTime(currentLocation))
                    {
                        int num3 = (int)((float)(timeOfDay - timeOfDay % 100) + (float)(timeOfDay % 100 / 10) * 16.66f);
                        float num4 = Math.Min(0.93f, 0.3f + ((float)(num3 - getStartingToGetDarkTime(currentLocation)) + (float)gameTimeInterval / (float)realMilliSecondsPerGameTenMinutes * 16.6f) * 0.00225f);
                        outdoorLight = (IsRainingHere() ? ambientLight : eveningColor) * num4;
                    }
                    else if (IsRainingHere())
                    {
                        outdoorLight = ambientLight * 0.3f;
                    }
                    else
                    {
                        outdoorLight = ambientLight;
                    }
                }
                lastLightUpdate = timeOfDay;
            }

            GameLocation gameLocation = currentLocation;
            if (gameTimeInterval > realMilliSecondsPerGameMinute + ((gameLocation != null) ? new int?(gameLocation.ExtraMillisecondsPerInGameMinute) : null))
            {
                if (panMode)
                {
                    Game1.gameTimeInterval = 0;
                }
                else
                {
                    Game1.performTenMinuteClockUpdate();
                }
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
                int trulyDarkTime = getTrulyDarkTime(currentLocation) - 100;
                Game1.gameTimeInterval = 0;

                if (IsMasterGame)
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

                if (timeOfDay % 100 >= 60)
                {
                    Game1.timeOfDay = timeOfDay - timeOfDay % 100 + 100;
                }

                Game1.timeOfDay = Math.Min(timeOfDay, 2600);
                if (isLightning && timeOfDay < 2400 && IsMasterGame)
                {
                    Utility.performLightningUpdate(timeOfDay);
                }

                if (timeOfDay == trulyDarkTime)
                {
                    currentLocation.switchOutNightTiles();
                }
                else if (timeOfDay == getModeratelyDarkTime(currentLocation) && currentLocation.IsOutdoors && !currentLocation.IsRainingHere())
                {
                    ambientLight = Color.White;
                }

                if (!eventUp && isDarkOut(currentLocation) && IsPlayingBackgroundMusic)
                {
                    changeMusicTrack("none", track_interruptable: true);
                }


                if (weatherIcon == 1)
                {
                    Dictionary<string, string> dictionary = temporaryContent.Load<Dictionary<string, string>>("Data\\Festivals\\" + currentSeason + dayOfMonth);
                    string[] array = dictionary["conditions"].Split('/');
                    int num2 = Convert.ToInt32(ArgUtility.SplitBySpaceAndGet(array[1], 0));
                    if (whereIsTodaysFest == null)
                    {
                        whereIsTodaysFest = array[0];
                    }

                    if (timeOfDay == num2)
                    {
                        if (dictionary.TryGetValue("startedMessage", out var value))
                        {
                            showGlobalMessage(TokenParser.ParseText(value));
                        }
                        else
                        {
                            if (!dictionary.TryGetValue("locationDisplayName", out var value2))
                            {
                                value2 = array[0];
                                value2 = value2 switch
                                {
                                    "Forest" => IsWinter ? content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.2634") : content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.2635"),
                                    "Town" => content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.2637"),
                                    "Beach" => content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.2639"),
                                    _ => TokenParser.ParseText(GameLocation.GetData(value2)?.DisplayName) ?? value2,
                                };
                            }

                            showGlobalMessage(content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.2640", dictionary["name"]) + value2);
                        }
                    }
                }

                player.performTenMinuteUpdate();
                switch (timeOfDay)
                {
                    case 1200:
                        if ((bool)currentLocation.isOutdoors && !currentLocation.IsRainingHere() && (IsPlayingOutdoorsAmbience || currentSong == null || isMusicContextActiveButNotPlaying()))
                        {
                            playMorningSong();
                        }

                        break;
                    case 2000:
                        if (IsPlayingTownMusic)
                        {
                            changeMusicTrack("none", track_interruptable: true);
                        }

                        break;
                    case 2400:
                        dayTimeMoneyBox.timeShakeTimer = 2000;
                        player.doEmote(24);
                        showGlobalMessage(content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.2652"));
                        break;
                    case 2500:
                        dayTimeMoneyBox.timeShakeTimer = 2000;
                        player.doEmote(24);
                        break;
                    case 2600:
                        dayTimeMoneyBox.timeShakeTimer = 2000;
                        player.mount?.dismount();
                        if (player.IsSitting())
                        {
                            player.StopSitting(animate: false);
                        }

                        if (player.UsingTool && (!(player.CurrentTool is FishingRod fishingRod) || (!fishingRod.isReeling && !fishingRod.pullingOutOfWater)))
                        {
                            player.completelyStopAnimatingOrDoingAction();
                        }

                        break;
                    case 2800:
                        if (activeClickableMenu != null)
                        {
                            activeClickableMenu.emergencyShutDown();
                            exitActiveMenu();
                        }

                        player.startToPassOut();
                        player.mount?.dismount();
                        break;
                }

                foreach (string activePassiveFestival in Game1.netWorldState.Value.ActivePassiveFestivals)
                {
                    if (Utility.TryGetPassiveFestivalData(activePassiveFestival, out var data) && timeOfDay == data.StartTime && (!data.OnlyShowMessageOnFirstDay || Utility.GetDayOfPassiveFestival(activePassiveFestival) == 1))
                    {
                        showGlobalMessage(TokenParser.ParseText(data.StartMessage));
                    }
                }

                foreach (GameLocation location in Game1.locations)
                {
                    GameLocation current2 = location;
                    if (current2.NameOrUniqueName == currentLocation.NameOrUniqueName)
                    {
                        current2 = currentLocation;
                    }

                    current2.performTenMinuteUpdate(timeOfDay);
                    current2.timeUpdate(10);
                }

                MineShaft.UpdateMines10Minutes(timeOfDay);
                VolcanoDungeon.UpdateLevels10Minutes(timeOfDay);
                if (IsMasterGame && farmEvent == null)
                {
                    netWorldState.Value.UpdateFromGame1();
                }

                for (int num3 = currentLightSources.Count - 1; num3 >= 0; num3--)
                {
                    if (currentLightSources.ElementAt(num3).color.A <= 0)
                    {
                        currentLightSources.Remove(currentLightSources.ElementAt(num3));
                    }
                }
            });
            return false;
        }

        public static void create(MineShaft __instance)
        {
            if (!Game1.IsMultiplayer || (Game1.IsMultiplayer &&
    Game1.otherFarmers.Any() &&
    Game1.otherFarmers.Roots.All
    (f => ((NetFarmerRoot)f.Value).Value.currentLocation is MineShaft
    && ((MineShaft)((NetFarmerRoot)f.Value).Value.currentLocation).mineLevel == MineShaft.desertArea)))
            {
                __instance.ExtraMillisecondsPerInGameMinute = 200;
            }
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
