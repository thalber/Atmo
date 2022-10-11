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
        List<Happen> happens = new() { new Happen(
            new HappenConfig(
                "test",
                new string[0],
                new[] { new HappenTrigger.Always() })
            )};
        var th = happens[0];
        HappenCallbacks.NewEvent(th);
        SpecificRoomsToHappens.InsertRight(th);
        SpecificRoomsToHappens.InsertLeft("SU_C04");
        SpecificRoomsToHappens.AddLink("SU_C04", th);
        //RoomsToGroups.InsertLeft()

        //inst.Plog.LogWarning("sample happenset created");
        //throw new NotImplementedException("where load");

        //inst.Plog.LogWarning($"{SpecificRoomsToHappens}, {RoomsToGroups}, {GroupsToHappens}");
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
            inst?.Plog.LogError($"Could not load event setup for {world.name}:\n{ex}");
            return null;
        }
    }

    internal IEnumerable<Happen> GetEventsForRoom(string roomname)
    {
        List<Happen> returned = new();
        goto _specific;
        if (!RoomsToGroups.LeftContains(roomname)) goto _specific;
        foreach (var group in RoomsToGroups.IndexFromLeft(roomname))
        {
            if (!GroupsToHappens.LeftContains(group)) continue;
            foreach (var ha in GroupsToHappens.IndexFromLeft(group))
            {
                returned.Add(ha);
                yield return ha;
            }
        }
    _specific:
        if (!SpecificRoomsToHappens.LeftContains(roomname)) yield break;
        foreach (var ha in SpecificRoomsToHappens.IndexFromLeft(roomname))
        {
            if (!returned.Contains(ha)) yield return ha;
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
