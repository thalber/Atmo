using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using static Atmo.Atmod;

namespace Atmo
{
    internal sealed class RegionSetup
    {

        internal static RegionSetup? TryCreate(World world, string regioncode)
        {
            //todo: decide where to load events from

            //fileinfo of the target file
            FileInfo setup = default;

            if (!setup.Exists) { single?.plog.LogError($"No events setup file for {regioncode}"); return null; }

            try
            {
                RegionSetup res = new(setup);
                return res;
            }
            catch (Exception ex)
            {
                single?.plog.LogError($"Could not load event setup for {regioncode}:\n{ex}");
                return null;
            }
        }

        private RegionSetup(FileInfo setup)
        {
            //todo: add file parsing
            throw new NotImplementedException();
        }

        internal void TryGetEventsForRoom(AbstractRoom absr, out List<Event> res)
        {
            res = new List<Event>();
            if (!roomGroups.TryGetValue(absr.name, out var group)) return;
            foreach (var ev in events ) { if (ev.cfg.groups.Contains(group)) res.Add(ev); }
        }

        //key: room; value: group
        internal Dictionary<string, string> roomGroups;// = new();
        internal List<Event> events;
    }
}
