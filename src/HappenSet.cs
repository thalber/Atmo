using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using static Atmo.Atmod;

namespace Atmo;
internal sealed class HappenSet
{
    private HappenSet()
    {
        throw new NotImplementedException("where load");
    }
    internal static HappenSet? TryCreate(World world)
    {
        try
        {
            HappenSet res = new();
            return res;
        }
        catch (Exception ex)
        {
            single?.Plog.LogError($"Could not load event setup for {world.name}:\n{ex}");
            return null;
        }
    }

    internal IEnumerable<Happen> GetEventsForRoom(AbstractRoom absr)
    {
        if (!RoomsToGroups.LeftContains(absr.name)) yield break;
        foreach (var group in RoomsToGroups.IndexFromLeft(absr.name))
        {
            if (!GroupsToHappens.LeftContains(group)) continue;
            foreach (var ha in GroupsToHappens.IndexFromLeft(group))
            {
                yield return ha;
            }
        }
    }

    public static HappenSet operator +(HappenSet l, HappenSet r)
    {
        HappenSet res = new()
        {
            SpecificRoomsToHappens = TwoPools<string, Happen>.Stitch(l.SpecificRoomsToHappens, r.SpecificRoomsToHappens),
            RoomsToGroups = TwoPools<string, string>.Stitch(l.RoomsToGroups, r.RoomsToGroups),
            GroupsToHappens = TwoPools<string, Happen>.Stitch(l.GroupsToHappens, r.GroupsToHappens)
        };
        throw new NotImplementedException();
    }

    #region fields
    internal TwoPools<string, string> RoomsToGroups = new();
    internal TwoPools<string, Happen> GroupsToHappens = new();
    internal TwoPools<string, Happen> SpecificRoomsToHappens = new();
    #endregion
}
