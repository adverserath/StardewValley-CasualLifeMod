
using System;
using System.Text;
using GenericModConfigMenu;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Menus;

namespace CasualLife
{
    public class ModEntry : Mod
    {
        private ModConfig Config;
        private Harmony harmony;
        public override void Entry(IModHelper helper)
        {
            this.Config = this.Helper.ReadConfig<ModConfig>();

            Game1Patches.Config = Config;
            DayTimeMoneyBoxPatch.Config = Config;

            harmony = new Harmony(this.ModManifest.UniqueID);
            Game1.realMilliSecondsPerGameMinute = this.Config.MillisecondsPerSecond;
            Game1.realMilliSecondsPerGameTenMinutes = this.Config.MillisecondsPerSecond * 10;

            if (!this.Config.DisableCLock)
            {
                harmony.Patch(
                   original: AccessTools.Method(typeof(Game1), nameof(Game1.UpdateGameClock)),
                   prefix: new HarmonyMethod(typeof(Game1Patches), nameof(Game1Patches.UpdateGameClock))
                );
            }
            if (!IsAndroid() && !this.Config.DisableCLock)
            {
                harmony.Patch(
                    original: AccessTools.Method(typeof(DayTimeMoneyBox), "draw", new Type[] { typeof(SpriteBatch) }, null),
                    prefix: new HarmonyMethod(typeof(DayTimeMoneyBoxPatch), nameof(DayTimeMoneyBoxPatch.drawFromDecom))
                    );
            }
            helper.Events.Input.ButtonPressed += this.OnButtonPressed;
            helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
            helper.Events.Multiplayer.PeerConnected += this.FixEventBug;
            helper.Events.Player.Warped += Player_Warped;
        }

        private void Player_Warped(object sender, WarpedEventArgs e)
        {
            Game1Patches.CheckFestivalsFix();
        }

        public static bool IsAndroid()
        {
            return Environment.OSVersion.Platform == PlatformID.Unix &&
                   System.IO.File.Exists("/system/build.prop"); // Android-specific file
        }

        private void FixEventBug(object sender, PeerConnectedEventArgs e)
        {
            Game1Patches.CheckFestivalsFix();
        }

        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            var configMenu = this.Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");

            if (configMenu is null)
            {
                Monitor.Log("GMCM not found! Is it installed?", LogLevel.Warn);
                return;
            }
            configMenu.Register(
                 mod: this.ModManifest,
                 reset: () => this.Config = new ModConfig(),
                 save: () => this.Helper.WriteConfig(this.Config)
             );
            configMenu.AddPageLink(this.ModManifest, "General", text: () => "General");
            configMenu.AddPageLink(this.ModManifest, "Harmony", text: () => "Harmony Patches");

            configMenu.AddPage(ModManifest, "General", () => "General Settings");

            configMenu.AddBoolOption(
                 mod: this.ModManifest,
                 name: () => "Enable debug time controls",
                 tooltip: () => "Using arrows on the keyboard you can change time/day/seasons",
                 getValue: () => this.Config.ControlDayWithKeys,
                 setValue: value => this.Config.ControlDayWithKeys = value
            );
            if (IsAndroid())
            {
                configMenu.AddTextOption(
                    mod: this.ModManifest,
                    name: () => "Milliseconds per clock tick",
                    getValue: () => this.Config.MillisecondsPerSecond.ToString(),
                    setValue: value =>
                    {
                        try
                        {
                            int parsed = int.Parse(value);
                            this.Config.MillisecondsPerSecond = parsed;
                            Game1.realMilliSecondsPerGameMinute = parsed;
                            Game1.realMilliSecondsPerGameTenMinutes = parsed * 10;
                        }
                        catch (Exception)
                        {
                        }
                    }
                );
            }
            else
            {
                configMenu.AddNumberOption(
    mod: this.ModManifest,
    name: () => "Milliseconds per clock tick",
    getValue: () => this.Config.MillisecondsPerSecond,
    setValue: value =>
    {
        this.Config.MillisecondsPerSecond = value;
        Game1.realMilliSecondsPerGameMinute = value;
        Game1.realMilliSecondsPerGameTenMinutes = value * 10;
    }
);
            }


            configMenu.AddPage(ModManifest, "Harmony", () => "Harmony Patches");
            configMenu.AddParagraph(
    mod: this.ModManifest,
    text: () => "Disable all Harmony patches. This will stop custom lighting, 24 hour clock and seconds on clock, " +
    "but you can still control Game Speed. Do this if your game runs slow."
);
            configMenu.AddBoolOption(
     mod: this.ModManifest,
     name: () => "Disable harmony patches",
     tooltip: () => "Harmony patches are disabled, No seconds showing on clock, no lighting changes. Better for performance on low end machines.",
     getValue: () => this.Config.DisableCLock,
     setValue: value =>
     {
         Game1.timeOfDay = (Game1.timeOfDay / 10) * 10;
         this.Config.DisableCLock = value;
         if (value)
         {
             harmony.Unpatch(original: AccessTools.Method(typeof(Game1), nameof(Game1.UpdateGameClock)), HarmonyPatchType.Prefix, "*");
             if (!IsAndroid())
                 harmony.Unpatch(original: AccessTools.Method(typeof(DayTimeMoneyBox), "draw", new Type[] { typeof(SpriteBatch) }, null), HarmonyPatchType.Prefix, "*");

         }
         else
         {
             harmony.Patch(
               original: AccessTools.Method(typeof(Game1), nameof(Game1.UpdateGameClock)),
               prefix: new HarmonyMethod(typeof(Game1Patches), nameof(Game1Patches.UpdateGameClock))
            );
             if (!IsAndroid())
                 harmony.Patch(
                     original: AccessTools.Method(typeof(DayTimeMoneyBox), "draw", new Type[] { typeof(SpriteBatch) }, null),
                     prefix: new HarmonyMethod(typeof(DayTimeMoneyBoxPatch), nameof(DayTimeMoneyBoxPatch.drawFromDecom))
                     );
         }
     }
 );
            configMenu.AddParagraph(
    mod: this.ModManifest,
    text: () => "Everything below only applied when Harmony patches are enabled."
);
            //configMenu.AddPage(this.ModManifest, "Game Speed", pageTitle: () => "Game Speed");
            //
            if (IsAndroid())
            {
                configMenu.AddParagraph(
                    mod: this.ModManifest,
                    text: () => "The UI override for the clock doesn't work on Android, because it uses a different clock to PC. As such, seconds 1 to 9 will not show as 01-09."
                );
            }
            else
            {
                configMenu.AddBoolOption(
                    mod: this.ModManifest,
                    name: () => "24 Hour Clock",
                    tooltip: () => "Sets clock to 24 hours.",
                    getValue: () => this.Config.Is24HourDefault,
                    setValue: value => this.Config.Is24HourDefault = value
                 );
            }


            configMenu.AddBoolOption(
                 mod: this.ModManifest,
                 name: () => "Enable custom lighting",
                 tooltip: () => "Use the mods lighting rebuild.",
                 getValue: () => this.Config.ControlDayLightLevels,
                 setValue: value => this.Config.ControlDayLightLevels = value
             );

            configMenu.AddBoolOption(
                 mod: this.ModManifest,
                 name: () => "Show Sun rise/set times",
                 tooltip: () => "Print out the sun rise and sunset times, when custom lighting is on",
                 getValue: () => this.Config.DisplaySunTimes,
                 setValue: value => this.Config.DisplaySunTimes = value
             );
        }

        private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            if (!Context.IsWorldReady || !Config.ControlDayWithKeys)
                return;


            if (e.IsDown(SButton.LeftControl))
            {
                if (e.Button == SButton.Left)
                {
                    Game1.timeOfDay -= 100;
                    if (Game1.timeOfDay < 600)
                        Game1.timeOfDay = 600;
                    return;
                }

                if (e.Button == SButton.Right)
                {
                    Game1.timeOfDay += 100;

                    return;

                }

                if (e.Button == SButton.Up)
                {
                    if (Game1.timeOfDay % 100 >= 59)
                    {
                        Game1.timeOfDay += 41;
                    }
                    else
                    {
                        Game1.timeOfDay += 1;
                    }
                    return;
                }

                if (e.Button == SButton.Down)
                {
                    if (Game1.timeOfDay % 100 <= 0)
                    {
                        Game1.timeOfDay -= 41;
                        Game1.ticks = 0;
                    }
                    else
                    {
                        Game1.timeOfDay -= 1;
                        Game1.ticks = 0;
                    }
                    return;
                }
            }
            if (e.Button == SButton.Left)
            {
                if (Game1.dayOfMonth > 1)
                {
                    Game1.dayOfMonth--;
                }
                else if (Game1.dayOfMonth == 1)
                {
                    Game1.dayOfMonth = 28;
                    ShiftSeasonDown();
                }
                return;
            }
            if (e.Button == SButton.Right)
            {
                if (Game1.dayOfMonth < 28)
                {
                    Game1.dayOfMonth++;
                }
                else if (Game1.dayOfMonth == 28)
                {
                    Game1.dayOfMonth = 1;
                    ShiftSeasonUp();
                }
            }

            if (e.Button == SButton.Up)
            {
                ShiftSeasonUp();
            }
            if (e.Button == SButton.Down)
            {
                ShiftSeasonDown();
            }

            if (e.IsDown(SButton.LeftControl) && e.IsDown(SButton.LeftShift))
            {
                switch (e.Button)
                {
                    case SButton.F1:
                        Game1.timeOfDay = 1300;
                        break;
                    case SButton.F2:
                        Game1.timeOfDay = 1400;
                        break;
                    case SButton.F3:
                        Game1.timeOfDay = 1500;
                        break;
                    case SButton.F4:
                        Game1.timeOfDay = 1600;
                        break;
                    case SButton.F5:
                        Game1.timeOfDay = 1700;
                        break;
                    case SButton.F6:
                        Game1.timeOfDay = 1800;
                        break;
                    case SButton.F7:
                        Game1.timeOfDay = 1900;
                        break;
                    case SButton.F8:
                        Game1.timeOfDay = 2000;
                        break;
                    case SButton.F9:
                        Game1.timeOfDay = 2100;
                        break;
                    case SButton.F10:
                        Game1.timeOfDay = 2200;
                        break;
                    case SButton.F11:
                        Game1.timeOfDay = 2300;
                        break;
                    case SButton.F12:
                        Game1.timeOfDay = 0;
                        break;
                }
                if (e.Button == SButton.NumPad9)
                {
                    Game1.timeOfDay = 2100;
                }
            }
            else if (e.IsDown(SButton.LeftControl))
            {
                switch (e.Button)
                {
                    case SButton.F1:
                        Game1.timeOfDay = 100;
                        break;
                    case SButton.F2:
                        Game1.timeOfDay = 200;
                        break;
                    case SButton.F3:
                        Game1.timeOfDay = 300;
                        break;
                    case SButton.F4:
                        Game1.timeOfDay = 400;
                        break;
                    case SButton.F5:
                        Game1.timeOfDay = 500;
                        break;
                    case SButton.F6:
                        Game1.timeOfDay = 600;
                        break;
                    case SButton.F7:
                        Game1.timeOfDay = 700;
                        break;
                    case SButton.F8:
                        Game1.timeOfDay = 800;
                        break;
                    case SButton.F9:
                        Game1.timeOfDay = 900;
                        break;
                    case SButton.F10:
                        Game1.timeOfDay = 1000;
                        break;
                    case SButton.F11:
                        Game1.timeOfDay = 1100;
                        break;
                    case SButton.F12:
                        Game1.timeOfDay = 1200;
                        break;
                }
            }
        }

        private void ShiftSeasonUp()
        {
            if (Game1.currentSeason == "spring")
            {
                Game1.currentSeason = "summer";
            }
            else if (Game1.currentSeason == "summer")
            {
                Game1.currentSeason = "fall";
            }
            else if (Game1.currentSeason == "fall")
            {
                Game1.currentSeason = "winter";
            }
            else if (Game1.currentSeason == "winter")
            {
                Game1.currentSeason = "spring";
            }
            Game1.setGraphicsForSeason();

        }

        private void ShiftSeasonDown()
        {
            if (Game1.currentSeason == "spring")
            {
                Game1.currentSeason = "winter";
            }
            else if (Game1.currentSeason == "summer")
            {
                Game1.currentSeason = "spring";
            }
            else if (Game1.currentSeason == "fall")
            {
                Game1.currentSeason = "summer";
            }
            else if (Game1.currentSeason == "winter")
            {
                Game1.currentSeason = "fall";
            }
            Game1.setGraphicsForSeason();

        }
    }
}
