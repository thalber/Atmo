using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Atmo
{
    public record struct EventConfig
    {
        public string name;
        public float chance;
        public TriggerType when;
        public string[] groups;

        public enum TriggerType
        {
            Always,
            OnRain
        }
    }
}
