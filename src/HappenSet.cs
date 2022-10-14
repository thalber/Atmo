using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using static Atmo.Atmod;

using IO = System.IO;
using CRS = CustomRegions.Mod;
using TXT = System.Text.RegularExpressions;

namespace Atmo;
public sealed class HappenSet
{
    //todo: unify adding items to multiple pools
    #region fields
    private RainWorldGame rwg;
    internal TwoPools<string, string> RoomsToGroups = new();
    internal TwoPools<string, Happen> GroupsToHappens = new();
    internal TwoPools<string, Happen> SpecificIncludeToHappens = new();
    internal TwoPools<string, Happen> SpecificExcludeToHappens = new();
    internal readonly List<Happen> AllHappens = new();
    #endregion
    private HappenSet(RainWorldGame rwg, IO.FileInfo? file = null)
    {
        this.rwg = rwg;
        if (file is null) return;
        HappenParser.Parse(file, this, rwg);
    }

    public IEnumerable<Happen> GetEventsForRoom(string roomname)
    {
        List<Happen> returned = new();
        //goto _specific;
        if (!RoomsToGroups.LeftContains(roomname)) goto _specific;
        foreach (var group in RoomsToGroups.IndexFromLeft(roomname))
        {
            if (!GroupsToHappens.LeftContains(group)) continue;
            foreach (var ha in GroupsToHappens.IndexFromLeft(group))
            {
                //exclude the minused
                if (SpecificExcludeToHappens.RightContains(ha) && SpecificExcludeToHappens.IndexFromRight(ha).Contains(roomname)) continue;
                returned.Add(ha);
                yield return ha;
            }
        }
    _specific:
        if (!SpecificIncludeToHappens.LeftContains(roomname)) yield break;
        foreach (var ha in SpecificIncludeToHappens.IndexFromLeft(roomname))
        {
            if (!returned.Contains(ha)) yield return ha;
        }
    }
    public IEnumerable<Happen.Perf> GetPerfRecords()
    {
        foreach (var ha in AllHappens)
        {
            yield return ha.PerfRecord();
        }
    }
    #region statics
    internal static HappenSet? TryCreate(World world)
    {
        HappenSet? res = null;
        #if REMIX
        throw new NotImplementedException("REMIX behaviour is not implemented yet!");
        #else 
        try
        {
            var pl = inst.Plog;
            var packs = CRS.API.InstalledPacks;
            var active = CRS.API.ActivatedPacks;
            foreach (KeyValuePair<string, CRS.CustomWorldStructs.RegionPack> kvp in packs)
            {

                var name = kvp.Key;
                var data = kvp.Value;
                //skip inactive
                if (!active.ContainsKey(name)) continue;

                var tarpath = CRS.API.BuildPath(
                    regionPackFolder: name,
                    folderName: "World",
                    regionID: world.name,
                    file: $"{world.name}.atmo",
                    folder: IO.Path.Combine("Regions", world.name),
                    includeRoot: true);
                var tarfile = new IO.FileInfo(tarpath);
                if (tarfile.Exists)
                {
                    //pl.LogError("File found! adding");
                    HappenSet gathered = new(world.game, tarfile);
                    if (res is null) res = gathered;
                    else res += gathered;
                    //pl.LogWarning(gathered);
                }
                else
                {
                    pl.LogError("Nope");
                }
            }
            return res;
        }
        catch (Exception ex)
        {
            inst.Plog.LogError($"Could not load event setup for {world.name}:\n{ex}");
            return null;
        }
        #endif
    }
    public static HappenSet operator +(HappenSet l, HappenSet r)
    {
        HappenSet res = new(l.rwg ?? r.rwg)
        {
            SpecificIncludeToHappens = TwoPools<string, Happen>.Stitch(l.SpecificIncludeToHappens, r.SpecificIncludeToHappens),
            RoomsToGroups = TwoPools<string, string>.Stitch(l.RoomsToGroups, r.RoomsToGroups),
            GroupsToHappens = TwoPools<string, Happen>.Stitch(l.GroupsToHappens, r.GroupsToHappens),
            SpecificExcludeToHappens = TwoPools<string, Happen>.Stitch(l.SpecificExcludeToHappens, r.SpecificExcludeToHappens),
        };
        throw new NotImplementedException();
    }

    #endregion statics
}
