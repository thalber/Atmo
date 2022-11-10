using System.Text;
using Atmo.Body;

namespace Atmo.Gen;
internal class HappenParser
{
	//god this is a mess
	//todo: find a way to make the parser less painful
	#region fields
	#region statfields
	private const TXT.RegexOptions Options = TXT.RegexOptions.IgnoreCase;
	private static readonly Dictionary<LineKind, TXT.Regex> LineMatchers;
	private static readonly TXT.Regex roomsep = new("[\\s\\t]*,[\\s\\t]*|[\\s\\t]+", Options);
	private static readonly LineKind[] happenProps = new[] { LineKind.HappenWhere, LineKind.HappenWhen, LineKind.HappenWhat, LineKind.HappenEnd };
	#endregion statfields
	private readonly Dictionary<string, GroupContents> allGroupContents = new();
	private readonly List<HappenConfig> retrievedHappens = new();

	private readonly string[] allLines;
	private int index = 0;
	public bool done => aborted || index >= allLines.Length;
	private bool aborted;
	private readonly IO.FileInfo file;
	private readonly HappenSet set;
	private readonly RainWorldGame rwg;
	//private IO.StreamReader lines;
	private string cline;
	private ParsePhase phase = ParsePhase.None;
	private HappenConfig cHapp;
	private string? cGroupName = null;
	private GroupContents cGroupContents = new();
	#endregion fields
	internal HappenParser(IO.FileInfo file, HappenSet set, RainWorldGame rwg)
	{
		plog.DbgVerbose($"HappenParse: booting for file: {file.FullName}");
		allLines = IO.File.ReadAllLines(file.FullName, Encoding.UTF8);
		this.file = file;
		this.set = set;
		this.rwg = rwg;
		cline = string.Empty;
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
					cGroupName = cline.Substring(group_que.Length);
					plog.DbgVerbose($"HappenParse: Beginning group block: {cGroupName}");
					phase = ParsePhase.Group;
				}
				else if (happn_que.Success)
				{
					cHapp = new(cline.Substring(happn_que.Length));
					plog.DbgVerbose($"HappenParse: Beginning happen block: {cHapp.name}");
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
			plog.LogError($"HappenParse: Irrecoverable error:" +
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
		if (cGroupName is null)
		{
			plog.LogWarning($"Error parsing group: current name is null! aborting!");
			phase = ParsePhase.None;
			return;
		}

		if ((ge = LineMatchers[LineKind.GroupEnd].Match(cline)).Success && ge.Index == 0)
		{
			plog.DbgVerbose($"HappenParse: ending group: {cGroupName}. " +
				$"Regex patterns: {cGroupContents.matchers.Count}, " +
				$"Literal rooms: {cGroupContents.rooms.Count}");
			allGroupContents.Add(cGroupName, cGroupContents);
			ResetGroup();
			phase = ParsePhase.None;
			return;
		}
		if (cline?.Length > 4 && cline.StartsWith("./") && cline.EndsWith("/."))
		{
			try
			{
				cGroupContents.matchers.Add(new TXT.Regex(cline.Substring(2, cline.Length - 4)));
				plog.DbgVerbose($"HappenParse: Created a regex matcher for: {cline}");
			}
			catch (Exception ex)
			{
				plog.LogWarning($"HappenParse: error creating a regular expression in group block!" +
					$"\n{ex}" +
					$"\nSource line: {cline}");
			}
			return;
		}
		foreach (var ss in roomsep.Split(cline))
		{
			if (ss.Length is 0) continue;
			cGroupContents.rooms.Add(ss);
		}
	}
	private void ParseHappen()
	{
		foreach (LineKind prop in happenProps)
		{
			TXT.Match match;
			TXT.Regex matcher = LineMatchers[prop];
			if ((match = matcher.Match(cline)).Success && match.Index == 0)
			{
				var payload = cline.Substring(match.Length);
				switch (prop)
				{
				case LineKind.HappenWhere:
				{
					plog.DbgVerbose("HappenParse: Recognized WHERE clause");
					WhereOps c = WhereOps.Group;
					string[]? items = TXT.Regex.Split(payload, "\\s+");
					foreach (var i in items)
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
					plog.DbgVerbose("HappenParse: Recognized WHAT clause");
					PredicateInlay.Token[]? tokens = PredicateInlay.Tokenize(payload).ToArray();
					for (var i = 0; i < tokens.Length; i++)
					{
						PredicateInlay.Token tok = tokens[i];
						if (tok.type == PredicateInlay.TokenType.Word)
						{
							PredicateInlay.Leaf leaf = PredicateInlay.MakeLeaf(tokens, in i) ?? new();
							cHapp.actions.Set(leaf.funcName, leaf.args.Select(x => x.ApplyEscapes()).ToArray());
						}
					}
				}
				break;
				case LineKind.HappenWhen:
					try
					{
						if (cHapp.conditions is not null)
						{
							plog.LogWarning("HappenParse: Duplicate WHEN clause! Skipping! (Did you forget to close a previous Happen with END HAPPEN?)");
							break;
						}
						plog.DbgVerbose("HappenParse: Recognized WHEN clause");
						cHapp.conditions = new PredicateInlay(payload, null);
					}
					catch (Exception ex)
					{
						plog.LogError($"HappenParse: Error creating eval tree from a WHEN block for {cHapp.name}:\n{ex}");
						cHapp.conditions = null;
					}
					break;
				case LineKind.HappenEnd:
					plog.DbgVerbose("HappenParse: finishing a happen block");
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
		cGroupName = null;
		cGroupContents = new();
	}
	#region nested
	private struct GroupContents
	{
		internal List<string> rooms = new();
		internal List<TXT.Regex> matchers = new();
		public GroupContents()
		{
		}
		public GroupContents Finalize(World w)
		{
			foreach (TXT.Regex? matcher in matchers)
			{
				for (var i = 0; i < w.abstractRooms.Length; i++)
				{
					if (matcher.IsMatch(w.abstractRooms[i].name)) rooms.Add(w.abstractRooms[i].name);
				}
			}
			return this;
		}
	}
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
	{
		return lk switch
		{
			LineKind.Comment => new TXT.Regex("//", Options),
			LineKind.HappenWhat => new TXT.Regex("WHAT\\s*:\\s*", Options),
			LineKind.HappenBegin => new TXT.Regex("HAPPEN\\s*:\\s*", Options),
			LineKind.HappenWhen => new TXT.Regex("WHEN\\s*:\\s*", Options),
			LineKind.HappenWhere => new TXT.Regex("WHERE\\s*:\\s*", Options),
			LineKind.GroupBegin => new TXT.Regex("GROUP\\s*:\\s*", Options),
			LineKind.GroupEnd => new TXT.Regex("END\\s+GROUP", Options),
			LineKind.HappenEnd => new TXT.Regex("END\\s+HAPPEN", Options),
			LineKind.Other => new TXT.Regex(".*", Options),
			_ => throw new ArgumentException("Invalid LineKind state supplied!"),
		};
	}
	#endregion
	internal static void Parse(IO.FileInfo file, HappenSet set, RainWorldGame rwg)
	{
		HappenParser p = new(file, set, rwg);
		for (var i = 0; i < p.allLines.Length; i++)
		{
			p.Advance();
		}
		Dictionary<string, IEnumerable<string>> groupsFinal = new();
		foreach (KeyValuePair<string, GroupContents> groupPre in p.allGroupContents)
		{
			GroupContents fin = groupPre.Value.Finalize(set.world);
			groupsFinal.Add(groupPre.Key, fin.rooms);
		}
		set.InsertGroups(groupsFinal);
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
