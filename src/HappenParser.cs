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
    private readonly static TXT.Regex roomsep = new TXT.Regex("[\\s\\t]*,[\\s\\t]*|[\\s\\t]+", options);
    private readonly static LineKind[] happenProps = new[] { LineKind.HappenWhere, LineKind.HappenWhen, LineKind.HappenWhat, LineKind.HappenEnd };
    #endregion statfields
    private Dictionary<string, List<string>> allGroupContents = new();
    private List<HappenConfig> retrievedHappens = new();

    private string[] allLines;
    private int index = 0;
    public bool done => aborted || index >= allLines.Length;

    private bool aborted;
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
        inst.Plog.LogDebug($"HappenParse: booting for file: {file.FullName}");
        allLines = IO.File.ReadAllLines(file.FullName, Encoding.UTF8);
        this.file = file;
        this.set = set;
        this.rwg = rwg;
    }
    internal void Advance()
    {
        cline = allLines[index];
        TXT.Match
                group_que = LineMatchers[LineKind.GroupBegin].Match(cline),
                happn_que = LineMatchers[LineKind.HappenBegin].Match(cline);
        if (cline.StartsWith("//") || aborted) goto stop;
        try
        {
            switch (phase)
            {
                case ParsePhase.None:
                    {
                        if (group_que.Success)
                        {
                            cGroup = cline.Substring(group_que.Length);
                            inst.Plog.LogDebug($"HappenParse: Beginning group block: {cGroup}");
                            phase = ParsePhase.Group;
                        }
                        else if (happn_que.Success)
                        {
                            cHapp = new(cline.Substring(happn_que.Length));
                            inst.Plog.LogDebug($"HappenParse: Beginning happen block: {cHapp.name}");
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
        catch (Exception ex)
        {
            inst.Plog.LogError($"HappenParse: Irrecoverable error:" +
                $"\n{ex}" +
                $"\nAborting");
            aborted = true;
        }
    stop:
        index++;
    }
    private void ParseGroup()
    {
        TXT.Match ge;
        if ((ge = LineMatchers[LineKind.GroupEnd].Match(cline)).Success && ge.Index == 0)
        {
            inst.Plog.LogDebug($"ParseHappen: ending group: {cGroup}");
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
            if ((match = matcher.Match(cline)).Success && match.Index == 0)
            {
                string payload = cline.Substring(match.Length);
                switch (prop)
                {
                    case LineKind.HappenWhere:
                        {
                            inst.Plog.LogDebug("HappenParse: Recognized WHERE clause");
                            WhereOps c = WhereOps.Group;
                            string[] items = TXT.Regex.Split(payload, "\\s+");
                            foreach (string i in items)
                            {
                                if (i.Length == 0) continue;
                                switch (i)
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
                                                cHapp.exclude.Add(i);
                                                break;
                                        }
                                        break;
                                }
                            }
                        }
                        break;
                    case LineKind.HappenWhat:
                        {
                            inst.Plog.LogDebug("HappenParse: Recognized WHAT clause");
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
                            if (cHapp.conditions is not null)
                            {
                                inst.Plog.LogWarning("HappenParse: Duplicate WHERE clause! Skipping! (Did you forget to close a previous Happen with END HAPPEN?)");
                                break;
                            }
                            inst.Plog.LogDebug("HappenParse: Recognized WHEN clause");
                            cHapp.conditions = new PredicateInlay(payload, null);
                        }
                        catch (Exception ex)
                        {
                            inst.Plog.LogError($"HappenParse: Error creating eval tree from a WHEN block for {cHapp.name}:\n{ex}");
                            cHapp.conditions = null;
                        }
                        break;
                    case LineKind.HappenEnd:
                        inst.Plog.LogDebug("HappenParse: finishing a happen block");
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
    private void ResetGroup()
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
    internal static void Parse(IO.FileInfo file, HappenSet set, RainWorldGame rwg)
    {
        HappenParser p = new(file, set, rwg);
        for (int i = 0; i < p.allLines.Length; i++)
        {
            p.Advance();
        }
        set.InsertGroups(p.allGroupContents);
        foreach (HappenConfig cfg in p.retrievedHappens)
        {
            var ha = new Happen(cfg, set, rwg);
            set.InsertHappens(new[] { ha });
            set.AddGrouping(ha, cfg.groups);
            set.AddExcludes(ha, cfg.exclude);
            set.AddIncludes(ha, cfg.include);
            
        }
    }
    static HappenParser()
    {
        LineMatchers = new();
        foreach (LineKind lk in Enum.GetValues(typeof(LineKind))) LineMatchers.Add(lk, MatcherForLK(lk));
    }
}