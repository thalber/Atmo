using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Atmo;
public struct HappenConfig
{
    public string name;
    //public float chance;
    //public TriggerType when;
    //public string[] groups;
    public string[] actions;
    public HappenTrigger[] when;

    public HappenConfig(string name, string[] actions, HappenTrigger[] when)
    {
        this.name = name;
        this.actions = actions;
        this.when = when;
    }
    //public enum TriggerType
    //{
    //    Always,
    //    AfterRain,
    //    BeforeRain
    //}
}
