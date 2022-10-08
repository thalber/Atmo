using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using static Atmo.Atmod;

namespace Atmo
{
    internal sealed class HappenSet
    {
        internal static HappenSet? TryCreate(World world)
        {
            //todo: decide where to load happens from. make sure it works with any amount of regpacks intersecting
            //fileinfo of the target file
            FileInfo setup = default;

            if (!setup.Exists) { single?.plog.LogError($"No events setup file for {regioncode}"); return null; }

            try
            {
                HappenSet res = new(setup);
                return res;
            }
            catch (Exception ex)
            {
                single?.plog.LogError($"Could not load event setup for {regioncode}:\n{ex}");
                return null;
            }
        }

        private HappenSet(FileInfo setup)
        {
            //todo: add file parsing
            throw new NotImplementedException();
        }

        internal void TryGetEventsForRoom(AbstractRoom absr, out List<Happen> res)
        {
            res = new List<Happen>();
            if (!roomGroups.TryGetValue(absr.name, out var group)) return;
            foreach (var ev in happens ) { if (ev.cfg.groups.Contains(group)) res.Add(ev); }
        }

        //key: room; value: group
        internal Dictionary<string, string> roomGroups;// = new();
        internal List<Happen> happens;
    }
}
