namespace CasualLife
{
    class ModConfig
    {
        public bool ControlDayLightLevels { get; set; } = false;
        public bool ControlDayWithKeys { get; set; } = false;
        public bool Is24HourDefault { get; set; } = true;
        public bool DisplaySunTimes { get; set; } = true;
        public int MillisecondsPerSecond { get; set; } = 1000;

    }
}
