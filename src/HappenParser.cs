using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using static Atmo.Atmod;

using IO = System.IO;
using CRS = CustomRegions.Mod;
using TXT = System.Text.RegularExpressions;

namespace Atmo;
internal static class HappenParser
{
    #region fields

    //internal static Dictionary<>
    //internal static TXT.Regex Parse_BeginGroup = new("GROUP\\s*:\\s*", TXT.RegexOptions.Compiled);
    #endregion
    internal static void Parse(IO.FileInfo file, HappenSet self)
    {
        using IO.StreamReader? fh = file.OpenText();
        string cl;
        while ((cl = fh.ReadLine()) is not null)
        {
            
        }
    }

    #region nested
    private enum Linetype
    {
        BeginGroup,
        //BeginDef
    }
    #endregion;
}