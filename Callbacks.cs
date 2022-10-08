using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Atmo
{
    public static class Callbacks
    {
        /// <summary>
        /// delegate for calling by events on abstract updates
        /// </summary>
        /// <param name="room">absroom</param>
        /// <param name="time">absupdate step</param>
        public delegate void AbstractUpdate(AbstractRoom room, int time);
        /// <summary>
        /// delegate for being called by events on realized updates
        /// </summary>
        /// <param name="room"></param>
        /// <param name="eu"></param>
        public delegate void RealizedUpdate(Room room);
        /// <summary>
        /// delegate for being called once when event is first created
        /// </summary>
        /// <param name="absroom"></param>
        public delegate void Init(World world);

        /// <summary>
        /// callback for adding callbacks to events.
        /// </summary>
        /// <param name="ev"></param>
        public delegate void CallbackAdd(Event ev);


        internal static void NewEvent(Event e)
        {
            GetDefaultCallbacks(in e, out var au, out var ru, out var oi);
            e.on_abst_update += au;
            e.on_real_update += ru;
            e.on_init += oi;
            RegisterCBForNewEvent?.Invoke(e);
        }
        internal static void GetDefaultCallbacks(in Event e, out AbstractUpdate au, out RealizedUpdate ru, out Init oi)
        {
            //todo: add default cases
            throw new NotImplementedException();
        }
        /// <summary>
        /// Subscribe to this to attach your custom callbacks to newly created event objects.
        /// </summary>
        public static event CallbackAdd? RegisterCBForNewEvent;
    }
}
