using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Atmo
{
    public static class Callbacks
    {
        /// <summary>
        /// delegate for calling by happens on abstract updates
        /// </summary>
        /// <param name="absroom">absroom</param>
        /// <param name="time">absupdate step</param>
        public delegate void AbstractUpdate(AbstractRoom absroom, int time);
        /// <summary>
        /// delegate for being called by happens on realized updates
        /// </summary>
        /// <param name="room">room</param>
        /// <param name="eu"></param>
        public delegate void RealizedUpdate(Room room);
        /// <summary>
        /// Delegate for being called on first abstract update
        /// </summary>
        /// <param name="world"></param>
        public delegate void Init(World world);

        /// <summary>
        /// callback for adding callbacks to happens.
        /// </summary>
        /// <param name="ev"></param>
        public delegate void CallbackAdd(Happen ev);


        internal static void NewEvent(Happen e)
        {
            GetDefaultCallbacks(in e, out var au, out var ru, out var oi);
            e.on_abst_update += au;
            e.on_real_update += ru;
            e.on_init += oi;
            RegisterNewHappen?.Invoke(e);
        }
        internal static void GetDefaultCallbacks(in Happen e, out AbstractUpdate au, out RealizedUpdate ru, out Init oi)
        {
            //todo: add default cases
            throw new NotImplementedException();
        }
        /// <summary>
        /// Subscribe to this to attach your custom callbacks to newly created event objects.
        /// </summary>
        public static event CallbackAdd? RegisterNewHappen;
    }
}
