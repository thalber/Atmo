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

    #region fields
    #region statfields
    private const TXT.RegexOptions options = TXT.RegexOptions.Compiled | TXT.RegexOptions.IgnoreCase;
    private readonly static Dictionary<LineKind, TXT.Regex> LineMatchers;
    private readonly static TXT.Regex roomsep = new TXT.Regex("\\s*,\\s*", options);
    private readonly static LineKind[] happenProps = new[] { LineKind.HappenWhere, LineKind.HappenWhen, LineKind.HappenWhat };
    #endregion statfields

    private IO.FileInfo file;
    private HappenSet set;
    private RainWorldGame rwg;
    private IO.StreamReader lines;
    private string cline;
    private ParsePhase phase;
    private string? cGroup = null;
    private HappenConfig cHapp;
    private List<string> cGroupRooms = new();
    private Dictionary<string, List<string>> allGroupContents = new();
    private List<Happen> retrievedHappens = new();
    #endregion fields

    internal HappenParser(IO.FileInfo file, HappenSet set, RainWorldGame rwg)
    {
        lines = file.OpenText();
        this.file = file;
    }

    ~HappenParser()
    {
        lines.Dispose();
    }
    internal static void Parse(IO.FileInfo file, HappenSet set, RainWorldGame rwg)
    {
#warning test this fucking mess for god's sake
        HappenParser p = new(file, set, rwg);
        do
        {
            p.Advance();
        }
        while (p.cline != null);
        //while (p.cl )
    }

    internal void Advance()
    {
        cline = lines.ReadLine();
        if (cline is null) return;
        TXT.Match
                group_que = LineMatchers[LineKind.GroupBegin].Match(cline),
                comm_que = LineMatchers[LineKind.Comment].Match(cline),
                happn_que = LineMatchers[LineKind.HappenBegin].Match(cline);
        if (comm_que.Index == 0) return;
        switch (phase)
        {
            case ParsePhase.None:
                {
                    if (group_que.Index == 0)
                    {
                        cGroup = cline.Substring(group_que.Length - 1);
                        phase = ParsePhase.Group;
                    }
                    else if (happn_que.Index == 0)
                    {
                        cHapp = new();
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
    }

    private void ParseGroup()
    {
        if (LineMatchers[LineKind.GroupEnd].Match(cline).Index == 0)
        {
            //todo: add check for duplicate groups
            allGroupContents.Add(cGroup, cGroupRooms);
            ResetGroup();
            phase = ParsePhase.Group;
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
            if ((match = matcher.Match(cline)).Index == 0)
            {
                string payload = cline.Substring(match.Length - 1);
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
                                    cHapp.actions.Add(leaf.funcName, leaf.args);
                                }
                            }
                        }
                        break;
                    case LineKind.HappenWhen:
                        try
                        {
#warning edit predinlay to handle null exchanger
                            cHapp.conditions = new PredicateInlay(payload, (x, args) => null);
                        }
                        catch (Exception ex)
                        {
                            inst.Plog.LogError($"HappenParse: Error creating eval tree for {cHapp.name}:\n{ex}");
                        }
                        break;
                    case LineKind.HappenEnd:
                        retrievedHappens.Add(
                            new Happen(
                                cHapp,
                                set,
                                rwg));
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
            LineKind.HappenWhat => new TXT.Regex("\\s*WHAT\\s*:\\s*", options),
            LineKind.HappenBegin => new TXT.Regex("\\s*HAPPEN\\s*:\\s*", options),
            LineKind.HappenWhen => new TXT.Regex("\\s*WHEN\\s*:\\s*", options),
            LineKind.HappenWhere => new TXT.Regex("\\s*WHERE\\s*:\\s*", options),
            LineKind.GroupBegin => new TXT.Regex("\\s*GROUP\\s*:\\s*", options),
            LineKind.GroupEnd => new TXT.Regex("\\s*END\\s+GROUP", options),
            LineKind.HappenEnd => new TXT.Regex("\\s*END\\s+HAPPEN", options),
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