using HarmonyLib;
using Microsoft.Xna.Framework;
using Netcode;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Events;
using StardewValley.Locations;
using StardewValley.Menus;
using StardewValley.Network;
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

        #region Accessors
        public static int dayOfMonth { get { return Game1.dayOfMonth; } }
        public static int realMilliSecondsPerGameTenMinutes { get { return Game1.realMilliSecondsPerGameTenMinutes; } }
        public static int realMilliSecondsPerGameMinute { get { return Game1.realMilliSecondsPerGameMinute; } }
        public static int timeOfDay { get { return Game1.timeOfDay; } set { Game1.timeOfDay = value; } }

        public static GameLocation currentLocation { get { return Game1.currentLocation; } }

        public static string currentSeason { get { return Game1.currentSeason; } }

        public static int gameTimeInterval { get { return Game1.gameTimeInterval; } set { Game1.gameTimeInterval = value; } }

        public static bool IsClient { get { return Game1.IsClient; } }

        public static Color outdoorLight { get { return Game1.outdoorLight; } private set { Game1.outdoorLight = value; } }
        public static Color ambientLight { get { return Game1.ambientLight; } private set { Game1.ambientLight = value; } }
        public static Color eveningColor { get { return Game1.eveningColor; } private set { Game1.eveningColor = value; } }
        public static Color bgColor { get { return Game1.bgColor; } private set { Game1.bgColor = value; } }
        public static bool panMode { get { return Game1.panMode; } private set { Game1.panMode = value; } }
        public static bool IsWinter { get { return Game1.IsWinter; } }
        public static Farmer player { get { return Game1.player; } }

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
        public static Dictionary<string, LightSource> currentLightSources { get { return Game1.currentLightSources; } }

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
        private static int getTimeInSeconds(int time)
        {
            return (time / 100 * 60) + time % 100;
        }

        private static int NormalizeGameTime(int t)
        {
            int minutes = t % 100;
            return minutes >= 60 ? t - minutes + 100 + minutes % 60 : t;
        }

        private static int lightDay = 0;
        private static float dayLengthFactor;
        private static int sunRiseTime;
        private static int sunSetTime;
        private static int sunRiseSeconds;
        private static int sunSetSeconds;

        [HarmonyPatch(typeof(Game1), nameof(Game1.UpdateGameClock))]
        public static bool UpdateGameClock(GameTime time)
        {
            if (shouldTimePass() && !IsClient)
            {
                Game1.gameTimeInterval += time.ElapsedGameTime.Milliseconds;
            }
            LightingCalculator();

            GameLocation gameLocation = currentLocation;
            if (gameTimeInterval > realMilliSecondsPerGameMinute + ((gameLocation != null) ? new int?(gameLocation.ExtraMillisecondsPerInGameMinute * 10) : null) && Game1.IsMasterGame)
            {
                if (panMode)
                {
                    Game1.gameTimeInterval = 0;
                }
                else
                {
                    Game1.timeOfDay += 1;
                    if ((timeOfDay % 100) % 10 == 0)
                    {
                        Game1.timeOfDay -= 10;
                        Game1.performTenMinuteClockUpdate();
                    }
                    else
                    {
                        CheckFestivalsFix();
                        gameTimeInterval = 0;
                        if (Game1.IsMultiplayer)
                        {
                            netWorldState.Value.UpdateFromGame1();
                        }
                    }
                }
            }
            return false;
        }

        private static void LightingCalculator()
        {
            if (DoLighting)
            {
                if (lightDay != dayOfMonth)
                {
                    // Smooth cosine over the 112-day year: 1.0 at summer solstice (mid-summer = yearDay 41), 0.0 at winter solstice (mid-winter = yearDay 97).
                    int seasonIndex = currentSeason == "summer" ? 1 : currentSeason == "fall" ? 2 : currentSeason == "winter" ? 3 : 0;
                    int yearDay = seasonIndex * 28 + (dayOfMonth - 1);
                    dayLengthFactor = 0.5f + 0.5f * (float)Math.Cos(2.0 * Math.PI * (yearDay - 41) / 112.0);

                    // Winter: rise 9:00 AM, set 5:00 PM. Summer: rise 5:30 AM, set 9:00 PM.
                    sunRiseTime = NormalizeGameTime((int)(900 - dayLengthFactor * 370));
                    sunSetTime  = NormalizeGameTime((int)(1700 + dayLengthFactor * 400));
                    sunRiseSeconds = getTimeInSeconds(sunRiseTime);
                    sunSetSeconds  = getTimeInSeconds(sunSetTime);

                    lightDay = dayOfMonth;
                    if (DisplaySunTimes)
                    {
                        string riseStr = sunRiseTime.ToString();
                        string setStr  = sunSetTime.ToString();
                        Game1.addHUDMessage(new HUDMessage(
                            $"Today the sun will rise at {riseStr.Insert(riseStr.Length - 2, ":")} and set at {setStr.Insert(setStr.Length - 2, ":")}", 3500f));
                    }
                }

                float timeAsFloat = timeOfDay / 100 * 100 + (timeOfDay % 100) / 60f * 100 + (float)gameTimeInterval / MillisecondsPerSecond;

                // Cosine centered on 2500 (1 AM = darkest point): yields ~0.95 around 1 PM and ~0.04 around 1 AM.
                float nightCos = (float)Math.Cos(Math.Abs(timeAsFloat - 2500) / 1200.0 * Math.PI);
                float dayFactor = 1f - (nightCos / 2f + 0.5f) / 1.1f - 0.05f;

                // Peak midday brightness scales with season: summer days are much brighter than winter days.
                float peakBrightness = 89f + dayLengthFactor * 165f;

                // outdoorLight is a darkness overlay: low values = bright scene, high values = dark scene.
                float darknessBase = 241f - peakBrightness * dayFactor;

                // Each season hues the darkness overlay. Effect is subtle at midday (near zero) and visible at dusk/night.
                float tintR, tintG, tintB;
                if (currentSeason == "spring")      { tintR = 0.90f; tintG = 1.00f; tintB = 0.92f; } // cool fresh
                else if (currentSeason == "summer") { tintR = 1.00f; tintG = 0.96f; tintB = 0.78f; } // warm golden
                else if (currentSeason == "fall")   { tintR = 1.00f; tintG = 0.82f; tintB = 0.65f; } // amber
                else                                { tintR = 0.78f; tintG = 0.88f; tintB = 1.00f; } // winter: cold blue

                int R = Math.Max(0, (int)(darknessBase * tintR));
                int G = Math.Max(0, (int)(darknessBase * tintG));
                int B = Math.Max(0, (int)(darknessBase * tintB));
                int secondsOfDay = getTimeInSeconds(Game1.timeOfDay);

                if (secondsOfDay < sunRiseSeconds + 60)
                {
                    // Pre-dawn: fade from night sky colour to daytime over the window leading up to sunrise.
                    float t = (float)secondsOfDay / (sunRiseSeconds + 60);
                    R = (int)MathHelper.Lerp(bgColor.R, R, t);
                    G = (int)MathHelper.Lerp(bgColor.G, G, t);
                    B = (int)MathHelper.Lerp(bgColor.B, B, t);
                }
                else if (secondsOfDay >= sunSetSeconds && secondsOfDay < sunSetSeconds + 180)
                {
                    // Sunset: crossfade to evening colour over 180 in-game seconds.
                    float t = 1f - (float)(sunSetSeconds + 180 - secondsOfDay) / 180f;
                    R = (int)MathHelper.Lerp(R, eveningColor.R, t);
                    G = (int)MathHelper.Lerp(G, eveningColor.G, t);
                    B = (int)MathHelper.Lerp(B, eveningColor.B, t);
                }
                else if (secondsOfDay >= sunSetSeconds + 180)
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
        }

        public static void CheckFestivalsFix()
        {
            if (weatherIcon == 1 && whereIsTodaysFest == null && IsMasterGame && farmEvent == null)
            {
                Dictionary<string, string> dictionary = temporaryContent.Load<Dictionary<string, string>>("Data\\Festivals\\" + currentSeason + dayOfMonth);
                string[] array = dictionary["conditions"].Split('/');
                whereIsTodaysFest = array[0];
                netWorldState.Value.UpdateFromGame1();
            }
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
