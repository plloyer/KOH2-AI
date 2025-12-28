using System;
using System.Collections.Generic;

namespace Logic;

public class ResourceInfo
{
	public enum Availability
	{
		Available,
		DirectlyObtainable,
		IndirectlyObtainable,
		Impossible,
		Unknown,
		Calculating
	}

	public struct ProducerRef : IEquatable<ProducerRef>
	{
		public string type;

		public string name;

		public Castle castle;

		public Building.Def bdef;

		public Building building;

		public Building.State state
		{
			get
			{
				if (bdef != null)
				{
					if (building != null)
					{
						return building.state;
					}
					return Building.State.Invalid;
				}
				return Building.State.Working;
			}
		}

		public bool Equals(ProducerRef p2)
		{
			if (type == p2.type && name == p2.name && castle == p2.castle && bdef == p2.bdef)
			{
				return building == p2.building;
			}
			return false;
		}

		public override string ToString()
		{
			if (type == "Castle")
			{
				return type + " " + castle?.name;
			}
			return $"[{state}] {type} {name} in {castle?.name}";
		}
	}

	public static bool enabled = true;

	public string type;

	public string name;

	public Def def;

	public Object owner;

	public Availability availability;

	public Availability own_availability;

	public int version;

	public List<ProducerRef> producers;

	public Character importer;

	public List<Castle>[] options;

	public List<Castle>[] own_options;

	public static int total_recalcs = 0;

	public static int per_kingdom_recalcs = 0;

	public static int per_castle_recalcs = 0;

	public static int per_building_recalcs = 0;

	public static int per_kingdom_allocs = 0;

	public static int per_castle_allocs = 0;

	public static int dictionary_allocs = 0;

	public static int dictionary_adds = 0;

	public static int dictionary_lookups = 0;

	public static int list_allocs = 0;

	public static int list_adds = 0;

	public static int list_lookups = 0;

	public static int resolves = 0;

	public static int find_buildings = 0;

	public Castle castle => owner as Castle;

	public Kingdom kingdom
	{
		get
		{
			object obj = owner as Kingdom;
			if (obj == null)
			{
				Castle obj2 = castle;
				if (obj2 == null)
				{
					return null;
				}
				obj = obj2.GetKingdom();
			}
			return (Kingdom)obj;
		}
	}

	public static void ClearStats()
	{
		total_recalcs = 0;
		per_kingdom_recalcs = 0;
		per_castle_recalcs = 0;
		per_building_recalcs = 0;
		per_kingdom_allocs = 0;
		per_castle_allocs = 0;
		dictionary_allocs = 0;
		dictionary_adds = 0;
		dictionary_lookups = 0;
		list_allocs = 0;
		list_adds = 0;
		list_lookups = 0;
		resolves = 0;
		find_buildings = 0;
	}

	public static string StatsText(string prefix = "", string new_line = "\n", string ident = "    ")
	{
		return prefix + $"Full Recalcs: {total_recalcs}{new_line}" + $"{ident}Per Kingdom Resource: {per_kingdom_recalcs}{new_line}" + $"{ident}Per Castle Resource: {per_castle_recalcs}{new_line}" + $"{ident}Per building: {per_building_recalcs}{new_line}" + $"Allocs: {per_kingdom_allocs + per_castle_allocs + dictionary_allocs + list_allocs}{new_line}" + $"{ident}Per Kingdom Resource: {per_kingdom_allocs}{new_line}" + $"{ident}Per Castle Resource: {per_castle_allocs}{new_line}" + $"{ident}Dictionaries: {dictionary_allocs}{new_line}" + $"{ident}Lists: {list_allocs}{new_line}" + $"Adds: {dictionary_adds + list_adds}{new_line}" + $"{ident}To Dictionaries: {dictionary_adds}{new_line}" + $"{ident}To Lists: {list_adds}{new_line}" + $"Lookups: {dictionary_lookups + list_lookups + resolves + find_buildings}{new_line}" + $"{ident}In Dictionaries: {dictionary_lookups}{new_line}" + $"{ident}In Lists: {list_lookups}{new_line}" + $"{ident}Name resolves: {resolves}{new_line}" + $"{ident}Find buildings: {find_buildings}{new_line}";
	}

	public ResourceInfo(Kingdom kingdom, string name)
	{
		per_kingdom_allocs++;
		this.name = name;
		owner = kingdom;
		Resolve(kingdom.game, name, out type, out def);
		availability = (own_availability = Availability.Unknown);
		options = new List<Castle>[4];
		own_options = new List<Castle>[4];
		int capacity = Math.Max(kingdom.realms.Count, 32);
		for (int i = 0; i < options.Length; i++)
		{
			options[i] = new List<Castle>(capacity);
			own_options[i] = new List<Castle>(capacity);
		}
	}

	public ResourceInfo(Castle castle, string name)
	{
		per_castle_allocs++;
		this.name = name;
		owner = castle;
		Resolve(castle.game, name, out type, out def);
		availability = (own_availability = Availability.Unknown);
	}

	public void ClearIfOutOfDate()
	{
		Kingdom kingdom = this.kingdom;
		if (kingdom != null && version != kingdom.resources_info_version)
		{
			version = kingdom.resources_info_version;
			Clear(full: false);
		}
	}

	public void Clear(bool full)
	{
		using (Game.Profile("ResourceInfo.Clear"))
		{
			availability = (own_availability = Availability.Unknown);
			if (full)
			{
				producers?.Clear();
			}
			importer = null;
			if (options != null)
			{
				for (int i = 0; i < options.Length; i++)
				{
					options[i]?.Clear();
				}
			}
			if (own_options != null)
			{
				for (int j = 0; j < own_options.Length; j++)
				{
					own_options[j]?.Clear();
				}
			}
		}
	}

	public Availability GetAvailability(bool own)
	{
		if (!own)
		{
			return availability;
		}
		return own_availability;
	}

	public static void Resolve(Game game, string name, out string type, out Def def)
	{
		resolves++;
		DT.Def def2 = game.dt.FindDef(name);
		if (def2 == null)
		{
			type = "Tag";
			def = null;
		}
		else
		{
			type = def2.field?.BaseRoot()?.key;
			def = def2.def;
		}
	}

	public bool AddProducer(ProducerRef producer)
	{
		using (Game.Profile("ResourceInfo.AddProducer"))
		{
			if (producers == null)
			{
				list_allocs++;
				producers = new List<ProducerRef>();
			}
			list_lookups++;
			if (Container.Contains(producers, producer))
			{
				return false;
			}
			list_adds++;
			producers.Add(producer);
			return true;
		}
	}

	public void AddOption(Availability availability, Castle castle)
	{
		using (Game.Profile("ResourceInfo.AddOption"))
		{
			if (options == null)
			{
				Game.Log($"ResourceInfo.AddOption({availability}, {castle}) called for {this}", Game.LogType.Error);
			}
			else if ((int)availability < options.Length)
			{
				List<Castle> list = options[(int)availability];
				list_lookups++;
				if (!list.Contains(castle))
				{
					list_adds++;
					list.Add(castle);
				}
			}
		}
	}

	public void AddOwnOption(Availability availability, Castle castle)
	{
		using (Game.Profile("ResourceInfo.AddOwnOption"))
		{
			if (availability >= Availability.Impossible)
			{
				return;
			}
			if (own_options == null)
			{
				Game.Log($"ResourceInfo.AddOwnOption({availability}, {castle}) called for {this}", Game.LogType.Error);
				return;
			}
			List<Castle> list = own_options[(int)availability];
			list_lookups++;
			if (!list.Contains(castle))
			{
				list_adds++;
				list.Add(castle);
			}
		}
	}

	public static string GetColorKey(Availability availability)
	{
		return availability switch
		{
			Availability.Available => "requirement_met", 
			Availability.DirectlyObtainable => "requirement_not_met", 
			Availability.IndirectlyObtainable => "requirement_not_met_indirect", 
			Availability.Impossible => "requirement_currently_impossible", 
			_ => null, 
		};
	}

	public string GetColorKey()
	{
		if (importer != null)
		{
			return "requirement_met_indirect";
		}
		if (availability == Availability.Impossible && castle != null && (type == "ProvinceFeature" || type == "Settlement" || type == "District"))
		{
			return "requirement_impossible";
		}
		string text = GetColorKey(availability);
		if (availability < Availability.Impossible && availability < own_availability && !text.EndsWith("_indirect", StringComparison.Ordinal))
		{
			text += "_indirect";
		}
		return text;
	}

	public bool GetColorTags(out string open_tag, out string close_tag)
	{
		string colorKey = GetColorKey();
		if (colorKey == null)
		{
			open_tag = null;
			close_tag = null;
			return false;
		}
		open_tag = colorKey;
		close_tag = "/" + colorKey;
		return true;
	}

	public void SetColorVars(Vars vars)
	{
		if (GetColorTags(out var open_tag, out var close_tag))
		{
			vars.Set("availability_color", open_tag);
			vars.Set("/availability_color", close_tag);
		}
	}

	public void SetTextVars(Vars vars, string availability_texts_path)
	{
		using (Game.Profile("ResourceInfo.SetTextVars"))
		{
			vars.Set("importer", importer);
			Availability availability = ((importer != null) ? own_availability : this.availability);
			List<Castle>[] array = ((importer != null) ? own_options : options);
			string text = availability.ToString();
			vars.Set("availability_key", text);
			text = availability_texts_path + "." + text;
			SetColorVars(vars);
			List<Castle> list = null;
			if (array != null && (int)availability < array.Length)
			{
				list = array[(int)availability];
			}
			int num = list?.Count ?? 0;
			vars.Set("availability_castles_count", num);
			if (num == kingdom.realms.Count)
			{
				vars.Set("availability_text", text + "_all");
			}
			else if (num > 3)
			{
				vars.Set("availability_text", text + "_many");
				num = 3;
			}
			else
			{
				vars.Set("availability_text", $"{text}_{num}");
			}
			for (int i = 0; i < num; i++)
			{
				Castle val = list[i];
				vars.Set($"availability_castle_{i + 1}", val);
				if (i >= 2)
				{
					break;
				}
			}
		}
	}

	public override string ToString()
	{
		string text = castle?.name ?? kingdom?.Name ?? owner?.ToString();
		return $"{type} {name} in {text}: {availability}";
	}

	public string DumpOptions(bool own)
	{
		string text = "";
		List<Castle>[] array = (own ? own_options : options);
		if (array == null)
		{
			return text;
		}
		if ((int)availability >= array.Length)
		{
			return text;
		}
		List<Castle> list = array[(int)availability];
		if (list == null)
		{
			return text;
		}
		int num = 0;
		for (int i = 0; i < list.Count; i++)
		{
			Castle castle = list[i];
			text = ((i != 0) ? (text + ", ") : (text + $" in {list.Count}: "));
			if (++num > 3)
			{
				text += "...";
				break;
			}
			text += castle.name;
		}
		return text;
	}

	public string Dump()
	{
		string text = ToString();
		text += DumpOptions(own: false);
		if (own_availability != availability)
		{
			text = text + $" /  {own_availability}" + DumpOptions(own: true);
		}
		return text;
	}

	public static int AddResourceDependency(ref List<Def> deps, Def dep)
	{
		if (dep == null)
		{
			return 0;
		}
		if (deps == null)
		{
			deps = new List<Def>();
		}
		if (deps.Contains(dep))
		{
			return 0;
		}
		deps.Add(dep);
		return 1;
	}

	public static int AddResourceDependencies(ref List<Def> deps, List<Building.Def> defs, List<Def> alts = null)
	{
		if (defs == null)
		{
			return 0;
		}
		int num = 0;
		for (int i = 0; i < defs.Count; i++)
		{
			Building.Def dep = defs[i];
			int num2 = AddResourceDependency(ref deps, dep);
			if (num2 != 0)
			{
				if (i == 0)
				{
					num += num2;
				}
				else if (alts != null)
				{
					AddResourceDependency(ref alts, dep);
				}
			}
		}
		return num;
	}

	public static int AddResourceDependencies(ref List<Def> deps, List<Building.Def.RequirementInfo> reqs, List<Def> alts = null)
	{
		if (reqs == null)
		{
			return 0;
		}
		int num = 0;
		for (int i = 0; i < reqs.Count; i++)
		{
			Def dep = Def.Get(reqs[i].def);
			int num2 = AddResourceDependency(ref deps, dep);
			if (num2 != 0)
			{
				if (i == 0)
				{
					num += num2;
				}
				else if (alts != null)
				{
					AddResourceDependency(ref alts, dep);
				}
			}
		}
		return num;
	}

	public static List<Def> GetResourceDependencies(Def def, List<Def> alts)
	{
		List<Def> deps = null;
		if (def != null)
		{
			if (!(def is Resource.Def def2))
			{
				if (!(def is Building.Def def3))
				{
					if (def is District.Def def4)
					{
						District.Def def5 = def4;
						AddResourceDependencies(ref deps, def5.requires);
						AddResourceDependencies(ref deps, def5.requires_or, alts);
					}
				}
				else
				{
					Building.Def def6 = def3;
					AddResourceDependencies(ref deps, def6.requires);
					AddResourceDependencies(ref deps, def6.requires_or);
					if (def6.districts != null)
					{
						for (int i = 0; i < def6.districts.Count; i++)
						{
							District.Def def7 = def6.districts[i];
							AddResourceDependencies(ref deps, def6.GetPrerequisites(def7), (i == 0) ? null : alts);
							AddResourceDependencies(ref deps, def6.GetPrerequisitesOr(def7), alts);
							AddResourceDependency(ref deps, def7);
						}
					}
				}
			}
			else
			{
				Resource.Def def8 = def2;
				AddResourceDependencies(ref deps, def8.produced_in, alts);
			}
		}
		return deps;
	}

	public static string DumpResourceDependencies(Def def, string ident = "", string prefix = "", List<Def> processed = null)
	{
		if (def == null)
		{
			return null;
		}
		string text = def.field?.BaseRoot()?.key;
		string text2 = ident + prefix + text + " " + def.id;
		List<Def> list = new List<Def>();
		List<Def> resourceDependencies = GetResourceDependencies(def, list);
		if (resourceDependencies == null)
		{
			return text2;
		}
		if (processed != null && processed.Contains(def))
		{
			return text2 + " ...";
		}
		AddResourceDependency(ref processed, def);
		ident += "    ";
		for (int i = 0; i < resourceDependencies.Count; i++)
		{
			Def item = resourceDependencies[i];
			prefix = ((!list.Contains(item)) ? "" : "or ");
			text2 = text2 + "\n" + DumpResourceDependencies(item, ident, prefix, processed);
		}
		return text2;
	}

	public static int ListResourceDependencies(Def def, List<Def> lst, List<Def> alts, bool all_alt = false)
	{
		if (def == null)
		{
			return 0;
		}
		List<Def> resourceDependencies = GetResourceDependencies(def, alts);
		if (resourceDependencies == null)
		{
			return 0;
		}
		int num = 0;
		for (int i = 0; i < resourceDependencies.Count; i++)
		{
			Def def2 = resourceDependencies[i];
			int num2 = AddResourceDependency(ref lst, def2);
			if (num2 != 0)
			{
				bool flag = all_alt || alts.Contains(def2);
				if (all_alt)
				{
					AddResourceDependency(ref alts, def2);
				}
				if (!flag)
				{
					num += num2;
				}
				num2 = ListResourceDependencies(def2, lst, alts, flag);
				if (!flag)
				{
					num += num2;
				}
			}
		}
		return num;
	}
}
