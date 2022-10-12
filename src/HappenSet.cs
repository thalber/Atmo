using System;
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
    internal TwoPools<string, string> RoomsToGroups = new();
    internal TwoPools<string, Happen> GroupsToHappens = new();
    internal TwoPools<string, Happen> SpecificIncludeToHappens = new();
    internal TwoPools<string, Happen> SpecificExcludeToHappens = new();
    #endregion
    private HappenSet(IO.FileInfo? file = null)
    {
        if (file is null) return;
        HappenParser.Parse(file, this);
    }
    internal static HappenSet? TryCreate(World world)
    {
        HappenSet? res = null;
        #if REMIX
        throw new NotImplementedException("REMIX behaviour is not implemented yet!");
        #else 
        try
        {
            var packs = CRS.API.InstalledPacks;
            foreach (var kvp in packs)
            {
                var name = kvp.Key;
                var data = kvp.Value;
                if (!data.activated) continue;
                if (!data.regions.Contains(world.region.name)) continue;
                var tarpath = CRS.API.BuildPath(name, "World", world.name, $"{world.name}.atmo", null, true);
                var tarfile = new IO.FileInfo(tarpath);
                if (tarfile.Exists)
                {
                    HappenSet gathered = new(tarfile);
                    if (res is null) res = gathered;
                    else res += gathered;
                }
            }


            return res;
        }
        catch (Exception ex)
        {
            inst?.Plog.LogError($"Could not load event setup for {world.name}:\n{ex}");
            return null;
        }
        #endif
    }
    internal IEnumerable<Happen> GetEventsForRoom(string roomname)
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
    public static HappenSet operator +(HappenSet l, HappenSet r)
    {
        HappenSet res = new()
        {
            SpecificIncludeToHappens = TwoPools<string, Happen>.Stitch(l.SpecificIncludeToHappens, r.SpecificIncludeToHappens),
            RoomsToGroups = TwoPools<string, string>.Stitch(l.RoomsToGroups, r.RoomsToGroups),
            GroupsToHappens = TwoPools<string, Happen>.Stitch(l.GroupsToHappens, r.GroupsToHappens),
            SpecificExcludeToHappens = TwoPools<string, Happen>.Stitch(l.SpecificExcludeToHappens, r.SpecificExcludeToHappens),
        };
        throw new NotImplementedException();
    }
}
