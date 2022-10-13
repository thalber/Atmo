﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using static Atmo.Atmod;

using IO = System.IO;
using CRS = CustomRegions.Mod;
using TXT = System.Text.RegularExpressions;

namespace Atmo;
internal sealed class HappenSet
{
    #region fields
    private RainWorldGame rwg;
    internal TwoPools<string, string> RoomsToGroups = new();
    internal TwoPools<string, Happen> GroupsToHappens = new();
    internal TwoPools<string, Happen> SpecificIncludeToHappens = new();
    internal TwoPools<string, Happen> SpecificExcludeToHappens = new();
    #endregion
    private HappenSet(RainWorldGame rwg, IO.FileInfo? file = null)
    {
        this.rwg = rwg;
        if (file is null) return;
        HappenParser.Parse(file, this, rwg);
    }
    internal static HappenSet? TryCreate(World world)
    {
        HappenSet? res = null;
        #if REMIX
        throw new NotImplementedException("REMIX behaviour is not implemented yet!");
        #else 
        try
        {
#warning unchecked crs interaction
            var pl = inst.Plog;
            var packs = CRS.API.InstalledPacks;
            pl.LogError(packs.Count);
            foreach (KeyValuePair<string, CRS.CustomWorldStructs.RegionPack> kvp in packs)
            {
                pl.LogError("!!!");
                var name = kvp.Key;
                var data = kvp.Value;
                
                //if (!data.activated) continue;
                //if (!data.regions.Contains(world.region.name)) continue;
                var tarpath = CRS.API.BuildPath(
                    regionPackFolder: name,
                    folderName: "World",
                    regionID: world.name,
                    file: $"{world.name}.atmo",
                    folder: IO.Path.Combine("Regions", world.name),
                    includeRoot: true);
                pl.LogError($"Checking pack {name}. path: {tarpath}");
                var tarfile = new IO.FileInfo(tarpath);
                if (tarfile.Exists)
                {
                    pl.LogError("File found! adding");
                    HappenSet gathered = new(world.game, tarfile);
                    if (res is null) res = gathered;
                    else res += gathered;
                    pl.LogWarning(gathered);
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
    internal IEnumerable<Happen> GetEventsForRoom(string roomname)
    {
#warning lc events broken again
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

    internal void InsertHappens(IEnumerable<Happen> collection)
    {
        SpecificIncludeToHappens.InsertRangeRight(collection);
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
}
