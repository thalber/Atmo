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
[BepInPlugin("thalber.atmod", "Atmo", "0.3")]
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
    public HappenSet? CurrentSet { get; private set; }
    #endregion
    public void OnEnable()
    {
        try
        {
            On.AbstractRoom.Update += RunHappensAbstUpd;
            On.RainWorldGame.Update += DoBodyUpdates;
            On.Room.Update += RunHappensRealUpd;
            On.World.ctor += FetchHappenSet;
        }
        catch (Exception ex)
        {
            Logger.LogFatal($"Error on enable!\n{ex}");
        }
        finally
        {
            inst = this;
        }
    }

    #region lifecycle
    /// <summary>
    /// Sends an Update call to all events for loaded world
    /// </summary>
    /// <param name="orig"></param>
    /// <param name="self"></param>
    private void DoBodyUpdates(On.RainWorldGame.orig_Update orig, RainWorldGame self)
    {
        orig(self);
        if (CurrentSet is null) return;
        if (self.pauseMenu != null) return;
        foreach (var ha in CurrentSet.AllHappens)
        {
            if (ha is null) continue;
            try
            {
                ha.CoreUpdate(self);
            }
            catch (Exception e)
            {
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
        if (CurrentSet is null) return;
        var haps = CurrentSet.GetHappensForRoom(self.name);
        foreach (var ha in haps)
        {
            if (ha is null) continue;
            try
            {
                if (ha.Active)
                {
                    if (!ha.InitRan) { ha.Init(self.world); ha.InitRan = true; }
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
        //DBG.Stopwatch sw = DBG.Stopwatch.StartNew();
        if (CurrentSet is null) return;
        var haps = CurrentSet.GetHappensForRoom(self.abstractRoom.name);
        foreach (var ha in haps)
        {
            //Logger.LogDebug($"update {ha} ({haps.Count()})");
            try
            {
                if (ha.Active)
                {
                    if (!ha.InitRan) { ha.Init(self.world); ha.InitRan = true; }
                    ha.RealUpdate(self);
                }
            }
            catch (Exception e)
            {
                Logger.LogError($"Error running event realupdate for room {self.abstractRoom.name}:\n{e}");
            }
        }
    }
    //todo: make sure everything works with region switching
    private void FetchHappenSet(On.World.orig_ctor orig, World self, RainWorldGame game, Region region, string name, bool singleRoomWorld)
    {
        orig(self, game, region, name, singleRoomWorld);
        if (singleRoomWorld) return;

        //Logger.LogError("Fetching hapset!");
        try
        {
            CurrentSet = HappenSet.TryCreate(self);
        }
        catch (Exception e)
        {
            Logger.LogError($"Could not create a happenset: {e}");
        }
    }
    #endregion lifecycle
    public void Update()
    {
        rw ??= FindObjectOfType<RainWorld>();
        if (!setupRan && rw is not null)
        {
            //maybe put something here
            setupRan = true;
        }
        if (rw is null || CurrentSet is null) return;
        if (rw.processManager.currentMainLoop is RainWorldGame) return;
        foreach (var proc in rw.processManager.sideProcesses) if (proc is RainWorldGame) return;
        Logger.LogDebug("No RainWorldGame in processmanager, erasing currentset");
        CurrentSet = null;
    }
    public void OnDisable()
    {
        try
        {
            On.World.ctor -= FetchHappenSet;
            On.Room.Update -= RunHappensRealUpd;
            On.RainWorldGame.Update -= DoBodyUpdates;
            On.AbstractRoom.Update -= RunHappensAbstUpd;

            System.Threading.ThreadPool.QueueUserWorkItem((_) =>
            {
                using (var logger = BepInEx.Logging.Logger.CreateLogSource("Atmo_Cleanup"))
                {
                    System.Diagnostics.Stopwatch sw = new();
                    sw.Start();
                    logger.LogMessage("Starting.");
                    foreach (var t in typeof(Atmod).Assembly.GetTypes())
                    {
                        try
                        {
                            t.CleanupStatic();
                        }
                        catch (Exception ex) {
                            logger.LogError(ex);
                        }
                    }
                    sw.Stop();
                    logger.LogMessage($"Finished statics cleanup: {sw.Elapsed}");
                };
            });
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error on disable!\n{ex}");
        }
        finally
        {
            inst = null;
        }
    }
}
