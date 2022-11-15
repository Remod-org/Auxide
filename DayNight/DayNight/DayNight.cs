using Auxide;

public class DayNight : RustScript
{
    private static ConfigData configData;
    private bool limitCleared = true;
    public System.Timers.Timer limitTimer;

    public DayNight()
    {
        Author = "RFC1920";
        Version = new VersionNumber(1, 0, 1);
        Description = "Provides day, night, and timeset/settime commands, with a limit timer.";
    }

    public class ConfigData
    {
        public bool debug;
        public bool adminOnly;
        public string dayTime;
        public string nightTime;
        public float frequencyLimitMinutes;
    }

    public void SaveConfig(ConfigData configuration)
    {
        config.WriteObject(configuration);
    }

    public void LoadConfig()
    {
        if (config.Exists())
        {
            configData = config.ReadObject<ConfigData>();
            return;
        }
        LoadDefaultConfig();
    }

    public void LoadDefaultConfig()
    {
        configData = new ConfigData()
        {
            debug = false,
            adminOnly = true,
            dayTime = "6",
            nightTime = "19",
            frequencyLimitMinutes = 5
        };

        SaveConfig(configData);
    }

    public override void Initialize()
    {
        LoadConfig();
        limitTimer = new System.Timers.Timer();
    }

    //public void LoadLang()
    //{
    //    lang.ReadObject<>(Name, "en");
    //}

    private void StartTimer()
    {
        limitCleared = false;
        limitTimer.Dispose();

        limitTimer = new System.Timers.Timer
        {
            Interval = configData.frequencyLimitMinutes > 0 ? configData.frequencyLimitMinutes : 5f
        };
        limitTimer.Elapsed += TimerElapsed;
        limitTimer.AutoReset = true;
        limitTimer.Enabled = true;
    }

    private void TimerElapsed(object source, System.Timers.ElapsedEventArgs e)
    {
        limitCleared = true;
    }

    public void OnChatCommand(BasePlayer player, string command, string[] args = null)
    {
        if (configData.adminOnly && !player.IsAdmin) return;

        switch (command)
        {
            case "day":
                if (limitCleared || player.IsAdmin)
                {
                    TOD_Sky.Instance.Cycle.Hour = float.Parse(configData.dayTime);
                    StartTimer();
                }
                break;
            case "night":
                if (limitCleared || player.IsAdmin)
                {
                    TOD_Sky.Instance.Cycle.Hour = float.Parse(configData.nightTime);
                    StartTimer();
                }
                break;
            case "timeset":
            case "settime":
                if (args.Length > 1)
                {
                    if (limitCleared || player.IsAdmin)
                    {
                        TOD_Sky.Instance.Cycle.Hour = float.Parse(args[1]);
                        StartTimer();
                    }
                }
                break;
        }
    }
}
