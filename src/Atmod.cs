using BepInEx;
using System.Linq;
using System.Collections.Generic;
using System;
using System.Text;

using static Atmo.API;
using static Atmo.HappenTrigger;

namespace Atmo;

/// <summary>
/// Main plugin class.
/// </summary>
[BepInPlugin("thalber.atmod", "atmod", "0.2")]
public sealed partial class Atmod : BaseUnityPlugin
{
    #region fields
    /// <summary>
    /// Static singleton
    /// </summary>
    public static Atmod inst;
    /// <summary>
    /// omg rain world reference
    /// </summary>
    public RainWorld? rw;
    /// <summary>
    /// publicized logger
    /// </summary>
    internal BepInEx.Logging.ManualLogSource Plog => Logger;
    private bool setupRan = false;

    internal static HappenSet? currentSet;
    #endregion

    public void OnEnable()
    {
        try
        {
            //PredicateInlay.compiled = System.Text.RegularExpressions.RegexOptions.None;
            string thing = """
                C:\Program Files (x86)\Steam\steamapps\common\Rain World - Dev\Mods\CustomResources\Better Shelters\World\Regions\SU\SU.atmo
                """;
            
            On.World.ctor += FetchHappenSet;
            On.Room.Update += RunHappensRealUpd;
            On.AbstractRoom.Update += RunHappensAbstUpd;
            On.RainWorldGame.Update += DoBodyUpdates;
        }
        catch ( Exception ex)
        {
            Logger.LogError("Error on enable!\n" + ex);
        }
        finally
        {
            inst = this;
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
        if (currentSet is null) return;
        foreach (var ha in currentSet.GroupsToHappens.EnumerateRight())
        {
            if (ha is null) continue;
            try
            {
                ha.CoreUpdate(self);
            }
            catch (Exception e) {
                Logger.LogError($"Error doing body update for {ha.name}:\n{e}");
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
        if (currentSet is null) return;
        var haps = currentSet.GetEventsForRoom(self.name);
        foreach (var ha in haps)
        {
            if (ha is null) continue;
            try
            {
                if (ha.IsOn(self.world.game))
                {
                    if (!ha.initRan) { ha.Init(self.world); ha.initRan = true; }
                    ha.AbstUpdate(self, timePassed);
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
        //#warning issue: for some reason geteventsforroom always returns none on real update
        //in my infinite wisdom i set SU_S04 as test room instead of SU_C04. everything worked as intended except for my brain
        orig(self);
        if (currentSet is null) return;
        var haps = currentSet.GetEventsForRoom(self.abstractRoom.name);
        foreach (var ha in haps)
        {
            //Logger.LogDebug($"update {ha} ({haps.Count()})");
            try
            {
                if (ha.IsOn(self.world.game)) {
                    if (!ha.initRan) { ha.Init(self.world); ha.initRan = true; }
                    ha.RealUpdate(self);
                }   
            }
            catch (Exception e)
            {
                Logger.LogError($"Error running event realupdate for room {self.abstractRoom.name}:\n{e}");
            }
        }

    }
    private void FetchHappenSet(On.World.orig_ctor orig, World self, RainWorldGame game, Region region, string name, bool singleRoomWorld)
    {
        orig(self, game, region, name, singleRoomWorld);
        if (singleRoomWorld) return;

        Logger.LogError("Fetching hapset!");
        try
        {
            currentSet = HappenSet.TryCreate(self);
        }
        catch (Exception e)
        {
            Logger.LogError($"Could not create a happenset: {e}");
        }
    }
    public void Update()
    {
        rw ??= FindObjectOfType<RainWorld>();
        if (!setupRan && rw is not null)
        {
            //maybe put something here
            setupRan = true;
        }
        if (rw is null) return;
        if (rw.processManager.currentMainLoop is RainWorldGame) return;
        foreach (var proc in rw.processManager.sideProcesses) if (proc is RainWorldGame) return;
        currentSet = null;
    }
    public void OnDisable()
    {
        try
        {
            On.World.ctor -= FetchHappenSet;
            On.Room.Update -= RunHappensRealUpd;
            On.AbstractRoom.Update -= RunHappensAbstUpd;
            On.RainWorldGame.Update -= DoBodyUpdates;
        }
        catch (Exception ex)
        {
            Logger.LogError("Error on disable!\n" + ex);
        }
        finally
        {
            inst = null;
        }
    }
}
