using BepInEx;
using System.Collections.Generic;
using System;

namespace Atmo;

[BepInPlugin("thalber.atmod", "atmod", "0.1")]
public sealed partial class Atmod : BaseUnityPlugin
{
    public static Atmod? single;
    public RainWorld? rw;
    public BepInEx.Logging.ManualLogSource plog => this.Logger;

    public void OnEnable()
    {
        try
        {
            On.OverWorld.WorldLoaded += FetchHappenSet;
            On.Room.Update += RunHappensRealUpd;
            On.AbstractRoom.Update += RunHappensAbstUpd;
        }
        finally
        {
            single = this;
        }
    }

    private void RunHappensAbstUpd(On.AbstractRoom.orig_Update orig, AbstractRoom self, int timePassed)
    {
        orig(self, timePassed);
        if (!Setups.TryGetValue(self.world.name, out var reg)) return;
        reg.TryGetEventsForRoom(self, out var evs);
        foreach (var ev in evs)
        {
            try
            {
                if (ev.is_on(self.world.game))
                {
                    if (!ev.init_ran) { ev.call_init(self.world); ev.init_ran = true; }
                    ev.call_abst_update(self, timePassed);
                }
            }
            catch (Exception e)
            {
                Logger.LogError($"Error running event abstupdate for room {self.name}:\n{e}");
            }
        }
    }

    private void RunHappensRealUpd(On.Room.orig_Update orig, Room self)
    {
        orig(self);
        if (!Setups.TryGetValue(self.world.name, out var reg)) return;
        reg.TryGetEventsForRoom(self.abstractRoom, out var evs);
        foreach (var ev in evs)
        {
            try
            {
                if (!ev.init_ran) continue;
                if (ev.is_on(self.world.game)) ev.call_real_update(self);
            }
            catch (Exception e)
            {
                Logger.LogError($"Error running event realupdate for room {self.abstractRoom.name}:\n{e}");
            }
        }

    }

    internal Dictionary<string, HappenSet> Setups = new();

    private void FetchHappenSet(On.OverWorld.orig_WorldLoaded orig, OverWorld self)
    {
        //RegionSetup res = new RegionSetup()
        orig(self);
    }

    public void Update()
    {
        rw ??= FindObjectOfType<RainWorld>();
    }

    public void OnDisable()
    {
        try
        {
            On.OverWorld.WorldLoaded -= FetchHappenSet;
            On.Room.Update -= RunHappensRealUpd;
            On.AbstractRoom.Update -= RunHappensAbstUpd;
        }
        finally
        {
            single = null;
        }
        
    }
}
