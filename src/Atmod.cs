using BepInEx;
using System.Collections.Generic;
using System;

namespace Atmo;

/// <summary>
/// Main plugin class.
/// </summary>
[BepInPlugin("thalber.atmod", "atmod", "0.1")]
public sealed partial class Atmod : BaseUnityPlugin
{
    /// <summary>
    /// Static singleton
    /// </summary>
    public static Atmod? single;
    /// <summary>
    /// omg rain world reference
    /// </summary>
    public RainWorld? rw;
    /// <summary>
    /// publicized logger
    /// </summary>
    internal BepInEx.Logging.ManualLogSource Plog => this.Logger;

    public void OnEnable()
    {
        try
        {
            //apply hooks
            On.OverWorld.WorldLoaded += FetchHappenSet;
            On.Room.Update += RunHappensRealUpd;
            On.AbstractRoom.Update += RunHappensAbstUpd;
            On.RainWorldGame.Update += DoBodyUpdates;
        }
        finally
        {
            single = this;
        }
    }
    /// <summary>
    /// Sends an Update call to all events for loaded world
    /// </summary>
    /// <param name="orig"></param>
    /// <param name="self"></param>
    private void DoBodyUpdates(On.RainWorldGame.orig_Update orig, RainWorldGame self)
    {
        orig(self);
        if (!setsByAcro.TryGetValue(self.world.name, out var set)) return;
        foreach (var ha in set.GroupsToHappens.EnumerateRight())
        {
            try
            {
                ha.CoreUpdate(self);
            }
            catch (Exception e) {
                Logger.LogError($"Error doing body update for {ha.cfg.name}:\n{e}");
            }
        }
    }
    /// <summary>
    /// Runs abstract world update for events in a room
    /// </summary>
    /// <param name="orig"></param>
    /// <param name="self"></param>
    /// <param name="timePassed"></param>
    private void RunHappensAbstUpd(On.AbstractRoom.orig_Update orig, AbstractRoom self, int timePassed)
    {
        orig(self, timePassed);
        if (!setsByAcro.TryGetValue(self.world.name, out var set)) return;
        var haps = set.GetEventsForRoom(self);
        foreach (var ha in haps)
        {
            try
            {
                if (ha.IsOn(self.world.game))
                {
                    if (!ha.InitRan) { ha.Call_Init(self.world); ha.InitRan = true; }
                    ha.Call_AbstUpdate(self, timePassed);
                }
            }
            catch (Exception e)
            {
                Logger.LogError($"Error running event abstupdate for room {self.name}:\n{e}");
            }
        }
    }
    /// <summary>
    /// Runs realized updates for events in a room
    /// </summary>
    /// <param name="orig"></param>
    /// <param name="self"></param>
    private void RunHappensRealUpd(On.Room.orig_Update orig, Room self)
    {
        orig(self);
        if (!setsByAcro.TryGetValue(self.world.name, out var set)) return;
        var haps = set.GetEventsForRoom(self.abstractRoom);
        foreach (var ha in haps)
        {
            try
            {
                if (!ha.InitRan) continue;
                if (ha.IsOn(self.world.game)) ha.Call_RealUpdate(self);
            }
            catch (Exception e)
            {
                Logger.LogError($"Error running event realupdate for room {self.abstractRoom.name}:\n{e}");
            }
        }

    }

    internal Dictionary<string, HappenSet> setsByAcro = new();

    private void FetchHappenSet(On.OverWorld.orig_WorldLoaded orig, OverWorld self)
    {
        //todo: create a happenset here or elsewhere?
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
            //undo hooks
            On.OverWorld.WorldLoaded -= FetchHappenSet;
            On.Room.Update -= RunHappensRealUpd;
            On.AbstractRoom.Update -= RunHappensAbstUpd;
            On.RainWorldGame.Update -= DoBodyUpdates;
        }
        finally
        {
            single = null;
        }
        
    }
}
