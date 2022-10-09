using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Atmo;
public struct HappenConfig
{
    public string name;
    public float chance;
    //public TriggerType when;
    public string[] groups;
    public HappenTrigger[] when;
    //public enum TriggerType
    //{
    //    Always,
    //    AfterRain,
    //    BeforeRain
    //}
}
