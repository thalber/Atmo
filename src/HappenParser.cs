using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using static Atmo.Atmod;

using IO = System.IO;
using CRS = CustomRegions.Mod;
using TXT = System.Text.RegularExpressions;

namespace Atmo;
internal class HappenParser
{
    //god this is a mess
    //todo: find a way to make the parser less painful

    #region fields
    #region statfields
    private const TXT.RegexOptions options = TXT.RegexOptions.IgnoreCase;
    private readonly static Dictionary<LineKind, TXT.Regex> LineMatchers;
    private readonly static TXT.Regex roomsep = new TXT.Regex("\\s*,\\s*", options);
    private readonly static LineKind[] happenProps = new[] { LineKind.HappenWhere, LineKind.HappenWhen, LineKind.HappenWhat, LineKind.HappenEnd };
    #endregion statfields
    private Dictionary<string, List<string>> allGroupContents = new();
    private List<HappenConfig> retrievedHappens = new();

    private string[] allLines;
    private int index = 0;
    public bool done => index >= allLines.Length;

    private IO.FileInfo file;
    private HappenSet set;
    private RainWorldGame rwg;
    //private IO.StreamReader lines;
    private string cline;
    private ParsePhase phase = ParsePhase.None;
    private string? cGroup = null;
    private HappenConfig cHapp;
    private List<string> cGroupRooms = new();
    #endregion fields

    internal HappenParser(IO.FileInfo file, HappenSet set, RainWorldGame rwg)
    {
        allLines = IO.File.ReadAllLines(file.FullName);
        this.file = file;
        this.set = set;
        this.rwg = rwg;
    }

    ~HappenParser()
    {
        //lines.Dispose();
    }
    internal static void Parse(IO.FileInfo file, HappenSet set, RainWorldGame rwg)
    {
        //inst.Plog.LogDebug("Beginning parse");
        HappenParser p = new(file, set, rwg);
        //inst.Plog.LogDebug(p.allLines.Aggregate(Utils.JoinWithComma));
        for (int i = 0; i < p.allLines.Length; i++)
        {
            p.Advance();
        }
        foreach (KeyValuePair<string, List<string>> gc in p.allGroupContents)
        {
            set.RoomsToGroups.InsertRight(gc.Key);
            set.RoomsToGroups.InsertRangeLeft(gc.Value);
            foreach (string rm in gc.Value) set.RoomsToGroups.AddLink(rm, gc.Key);
            set.GroupsToHappens.InsertLeft(gc.Key);
        }
        foreach (HappenConfig hac in p.retrievedHappens)
        {
            Happen ha = new(hac, set, rwg);
            set.GroupsToHappens.InsertRight(ha);
            foreach (string g in hac.groups)
            {
                set.GroupsToHappens.AddLink(g, ha);
            }
            if ((hac.include?.Count ?? 0) > 0)
            {
                set.SpecificIncludeToHappens.InsertRangeLeft(hac.include);
                set.SpecificIncludeToHappens.InsertRight(ha);
                foreach (var incl in hac.include) set.SpecificIncludeToHappens.AddLink(incl, ha);
            }
            if ((hac.exclude?.Count ?? 0) > 0)
            {
                set.SpecificExcludeToHappens.InsertRangeLeft(hac.include);
                set.SpecificExcludeToHappens.InsertRight(ha);
                foreach (var incl in hac.include) set.SpecificExcludeToHappens.AddLink(incl, ha);
            }
        }
    }

    internal void Advance()
    {
        cline = allLines[index];//lines.ReadLine();
        //if (cline is null) return;
        TXT.Match
                group_que = LineMatchers[LineKind.GroupBegin].Match(cline),
                comm_que = LineMatchers[LineKind.Comment].Match(cline),
                happn_que = LineMatchers[LineKind.HappenBegin].Match(cline);
        if (comm_que.Success && comm_que.Success) return;
        switch (phase)
        {
            case ParsePhase.None:
                {
                    if (group_que.Success && group_que.Success)
                    {
                        cGroup = cline.Substring(group_que.Length);
                        phase = ParsePhase.Group;
                    }
                    else if (happn_que.Success && happn_que.Success)
                    {
                        cHapp = new(cline.Substring(happn_que.Length));
                        phase = ParsePhase.Happen;
                    }
                }
                break;
            case ParsePhase.Group:
                {
                    ParseGroup();
                }
                break;
            case ParsePhase.Happen:
                {
                    ParseHappen();
                    //ParseHappen(cl, ref currentHappen, retrievedHappens, ref phase);
                }
                break;
            default:
                break;
        }
        //inst.Plog.LogDebug($"line {cline}, phase {phase}, cgroup {cGroup}");
        index++;
    }

    private void ParseGroup()
    {
        if (LineMatchers[LineKind.GroupEnd].Match(cline).Success)
        {
            allGroupContents.Add(cGroup, cGroupRooms);
            ResetGroup();
            phase = ParsePhase.None;
            return;
        }
        foreach (string ss in roomsep.Split(cline))
        {
            if (ss.Length is 0) continue;
            cGroupRooms.Add(ss);
        }
    }
    private void ParseHappen()
    {
        foreach (var prop in happenProps)
        {
            TXT.Match match;
            TXT.Regex matcher = LineMatchers[prop];
            if ((match = matcher.Match(cline)).Success)
            {
                string payload = cline.Substring(match.Length);
                switch (prop)
                {
                    case LineKind.HappenWhere:
                        {
                            WhereOps c = WhereOps.Group;
                            string[] items = TXT.Regex.Split(payload, "\\s");
                            foreach (string i in items) switch (i)
                                {
                                    case "+":
                                        c = WhereOps.Include; break;
                                    case "-":
                                        c = WhereOps.Exclude; break;
                                    case "*":
                                        c = WhereOps.Group; break;
                                    default:
                                        switch (c)
                                        {
                                            case WhereOps.Group:
                                                cHapp.groups.Add(i);
                                                break;
                                            case WhereOps.Include:
                                                cHapp.include.Add(i);
                                                break;
                                            case WhereOps.Exclude:
                                                cHapp.include.Add(i);
                                                break;
                                        }
                                        break;
                                }
                        }
                        break;
                    case LineKind.HappenWhat:
                        {
                            //string[] items = TXT.Regex.Split(payload, "\\s");
                            var tokens = PredicateInlay.Tokenize(payload).ToArray();
                            for (int i = 0; i < tokens.Length; i++)
                            {
                                PredicateInlay.Token tok = tokens[i];
                                if (tok.type == PredicateInlay.TokenType.Word)
                                {
                                    PredicateInlay.Leaf leaf = PredicateInlay.MakeLeaf(tokens, in i) ?? new();
                                    cHapp.actions.AddOrUpdate(leaf.funcName, leaf.args);
                                }
                            }
                        }
                        break;
                    case LineKind.HappenWhen:
                        try
                        {
                            cHapp.conditions = new PredicateInlay(payload, null);
                        }
                        catch (Exception ex)
                        {
                            inst.Plog.LogError($"HappenParse: Error creating eval tree for {cHapp.name}:\n{ex}");
                        }
                        break;
                    case LineKind.HappenEnd:
                        retrievedHappens.Add(cHapp);
                        cHapp = default;
                        phase = ParsePhase.None;
                        break;
                    default:
                        break;
                }
                break;
            }
        }
    }

    private void ResetGroup(/*ref string? currGroup, ref List<string> currGroupRooms*/)
    {
        cGroup = null;
        cGroupRooms = new();
    }

    #region nested
    private enum WhereOps
    {
        Group,
        Include,
        Exclude,
    }
    private enum LineKind
    {
        Comment,
        GroupBegin,
        GroupEnd,
        HappenBegin,
        HappenWhat,
        HappenWhen,
        HappenWhere,
        HappenEnd,
        Other,
    }
    private enum ParsePhase
    {
        None,
        Group,
        Happen
    }
    #endregion
    #region regex
    private static TXT.Regex MatcherForLK(LineKind lk)
        => lk switch
        {
            LineKind.Comment => new TXT.Regex("//", options),
            LineKind.HappenWhat => new TXT.Regex("WHAT\\s*:\\s*", options),
            LineKind.HappenBegin => new TXT.Regex("HAPPEN\\s*:\\s*", options),
            LineKind.HappenWhen => new TXT.Regex("WHEN\\s*:\\s*", options),
            LineKind.HappenWhere => new TXT.Regex("WHERE\\s*:\\s*", options),
            LineKind.GroupBegin => new TXT.Regex("GROUP\\s*:\\s*", options),
            LineKind.GroupEnd => new TXT.Regex("END\\s+GROUP", options),
            LineKind.HappenEnd => new TXT.Regex("END\\s+HAPPEN", options),
            LineKind.Other => new TXT.Regex(".*", options),
            _ => throw new ArgumentException("Invalid LineKind state supplied!"),
        };
    #endregion

    static HappenParser()
    {
        LineMatchers = new();
        foreach (LineKind lk in Enum.GetValues(typeof(LineKind))) LineMatchers.Add(lk, MatcherForLK(lk));
    }
}