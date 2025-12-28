using System;
using System.Collections.Generic;

namespace Logic;

public class Religion : BaseObject, IVars, IListener
{
	public class Def : Logic.Def
	{
		public string name;

		public bool christian;

		public bool muslim;

		public bool pagan;

		public string hq_realm;

		public string hq_kingdom;

		public DT.Field head_names;

		public List<StatModifier.Def> mods = new List<StatModifier.Def>();

		public List<CharacterBonus> random_pope_bonuses;

		public List<CharacterBonus> own_cleric_patriarch_bonuses;

		public List<CharacterBonus> random_patriarch_bonuses;

		public int max_pagan_beliefs = 4;

		public List<PaganBelief> pagan_beliefs;

		private List<PaganBelief> old_pagan_beliefs;

		public List<float> pagan_beliefs_upkeep_base;

		public List<float> pagan_beliefs_upkeep_ssum1_perc;

		public OutcomeDef religion_change_cleric_outcomes;

		public DT.Field jihad_army_morale_bonus;

		public List<DT.Field> jihad_caliph_bonuses = new List<DT.Field>();

		public DT.Field religious_tension;

		public override bool Load(Game game)
		{
			DT.Field field = base.field;
			name = field.key;
			christian = field.GetBool("christian", null, christian);
			muslim = field.GetBool("muslim", null, muslim);
			pagan = !christian && !muslim;
			hq_realm = field.GetString("hq_realm", null, hq_realm);
			hq_kingdom = field.GetString("hq_kingdom", null, hq_kingdom);
			string text = field.GetString("head_names");
			max_pagan_beliefs = field.GetInt("max_beliefs", null, max_pagan_beliefs);
			if (!string.IsNullOrEmpty(text))
			{
				head_names = game.dt.Find(text);
				if (head_names == null)
				{
					Game.Log(field.Path(include_file: true) + ": field not found: " + text, Game.LogType.Error);
				}
				else if (head_names.Keys().Count < 1)
				{
					Game.Log(field.Path(include_file: true) + ": " + text + " is empty", Game.LogType.Error);
				}
			}
			religious_tension = field.FindChild("religious_tension");
			mods.Clear();
			random_pope_bonuses = null;
			own_cleric_patriarch_bonuses = null;
			random_patriarch_bonuses = null;
			if (IsBase())
			{
				DT.Field field2 = field.FindChild("religion_change_cleric_outcomes");
				if (field2 != null)
				{
					DT.Field defaults = null;
					religion_change_cleric_outcomes = new OutcomeDef(game, field2, defaults);
				}
				jihad_army_morale_bonus = game.dt.Find("Jihad.army_morale_bonus");
				LoadJihadCaliphBonuses(game);
			}
			else
			{
				Def def = game.defs.GetBase<Def>();
				religion_change_cleric_outcomes = def.religion_change_cleric_outcomes;
				jihad_army_morale_bonus = def.jihad_army_morale_bonus;
				jihad_caliph_bonuses = def.jihad_caliph_bonuses;
			}
			LoadUpkeeps(game, field);
			return true;
		}

		public override bool Validate(Game game)
		{
			LoadMods(game);
			return true;
		}

		public float CalcPaganBliefsUpkeep(Kingdom k)
		{
			if (!pagan)
			{
				return 0f;
			}
			return CalcPaganBliefsUpkeep(k, k.pagan_beliefs.Count);
		}

		public float CalcPaganBliefsUpkeep(Kingdom k, int numBeliefs)
		{
			if (!pagan)
			{
				return 0f;
			}
			if (pagan_beliefs == null || pagan_beliefs_upkeep_base == null || pagan_beliefs_upkeep_ssum1_perc == null)
			{
				return 0f;
			}
			if (numBeliefs <= 0 || numBeliefs > pagan_beliefs_upkeep_base.Count || numBeliefs > pagan_beliefs_upkeep_ssum1_perc.Count)
			{
				return 0f;
			}
			float num = pagan_beliefs_upkeep_base[numBeliefs - 1];
			float num2 = pagan_beliefs_upkeep_ssum1_perc[numBeliefs - 1] * k.GetSSum(1f);
			return (float)(Math.Ceiling((double)(num + num2) / 5.0) * 5.0);
		}

		public PaganBelief FindPaganBelief(string name)
		{
			if (pagan_beliefs == null)
			{
				return null;
			}
			for (int i = 0; i < pagan_beliefs.Count; i++)
			{
				PaganBelief paganBelief = pagan_beliefs[i];
				if (paganBelief.name == name)
				{
					return paganBelief;
				}
			}
			return null;
		}

		public float ReligiousTension(Religion other)
		{
			if (religious_tension == null || other == null || other.def == this)
			{
				return 0f;
			}
			return religious_tension.GetFloat(other.def.id);
		}

		private void LoadMods(Game game)
		{
			DT.Field field = base.field;
			Stats.Def stats_def = game.defs.Get<Stats.Def>("KingdomStats");
			old_pagan_beliefs = pagan_beliefs;
			pagan_beliefs = null;
			LoadMods(game, field, mods, stats_def);
			old_pagan_beliefs = null;
			if (name == "Catholic")
			{
				random_pope_bonuses = new List<CharacterBonus>();
				LoadCharacterBonuses(game, field.FindChild("random_pope_bonuses"), random_pope_bonuses, stats_def);
			}
			else if (name == "Orthodox")
			{
				own_cleric_patriarch_bonuses = new List<CharacterBonus>();
				LoadCharacterBonuses(game, field.FindChild("own_cleric_patriarch_bonuses"), own_cleric_patriarch_bonuses, stats_def);
				random_patriarch_bonuses = new List<CharacterBonus>();
				LoadCharacterBonuses(game, field.FindChild("random_patriarch_bonuses"), random_patriarch_bonuses, stats_def);
			}
			else
			{
				LoadMods(game, field.FindChild("non_orthodox_ecumenical_patriarch"), mods, stats_def);
			}
		}

		public void LoadMods(Game game, DT.Field f, List<StatModifier.Def> mods, Stats.Def stats_def)
		{
			if (f == null)
			{
				return;
			}
			List<string> list = f.Keys();
			for (int i = 0; i < list.Count; i++)
			{
				string key = list[i];
				DT.Field field = f.FindChild(key);
				if (PaganBelief.IsPaganBeliefDef(field))
				{
					PaganBelief paganBelief = null;
					if (old_pagan_beliefs != null)
					{
						paganBelief = old_pagan_beliefs.Find((PaganBelief b) => b.name == key);
						paganBelief.mods?.Clear();
					}
					if (paganBelief == null)
					{
						paganBelief = new PaganBelief();
					}
					paganBelief.name = key;
					LoadMods(game, field, paganBelief.mods, stats_def);
					if (pagan_beliefs == null)
					{
						pagan_beliefs = new List<PaganBelief>();
					}
					pagan_beliefs.Add(paganBelief);
				}
				else if (!(field.type != "mod") || !(field.type != "instant"))
				{
					string key2 = field.key;
					if (field.type == "mod" && (stats_def == null || !stats_def.HasStat(key2)))
					{
						Game.Log(field.Path(include_file: true) + ": unknown stat: 'KingdomStats." + key2 + "'", Game.LogType.Error);
						continue;
					}
					StatModifier.Def item = new StatModifier.Def(this, field, key2);
					mods.Add(item);
				}
			}
		}

		private void LoadCharacterBonuses(Game game, DT.Field f, List<CharacterBonus> bonuses, Stats.Def stats_def)
		{
			if (f != null)
			{
				List<string> list = f.Keys();
				for (int i = 0; i < list.Count; i++)
				{
					string path = list[i];
					DT.Field f2 = f.FindChild(path);
					CharacterBonus characterBonus = new CharacterBonus
					{
						field = f2
					};
					LoadMods(game, f2, characterBonus.mods, stats_def);
					bonuses.Add(characterBonus);
				}
			}
		}

		private void LoadJihadCaliphBonuses(Game game)
		{
			jihad_caliph_bonuses.Clear();
			DT.Field field = game.dt.Find("Jihad.caliph_bonuses");
			if (field == null)
			{
				return;
			}
			List<string> list = field.Keys();
			for (int i = 0; i < list.Count; i++)
			{
				string path = list[i];
				DT.Field field2 = field.FindChild(path);
				if (!(field2.type != "mod"))
				{
					jihad_caliph_bonuses.Add(field2);
				}
			}
		}

		public void LoadUpkeeps(Game game, DT.Field f)
		{
			if (!pagan)
			{
				return;
			}
			pagan_beliefs_upkeep_base?.Clear();
			pagan_beliefs_upkeep_ssum1_perc?.Clear();
			DT.Field field = f.FindChild("upkeep_belief_base");
			DT.Field field2 = f.FindChild("upkeep_belief_ssum1_perc");
			if (field != null)
			{
				int num = field.NumValues();
				for (int i = 0; i < num; i++)
				{
					if (pagan_beliefs_upkeep_base == null)
					{
						pagan_beliefs_upkeep_base = new List<float>(num);
					}
					pagan_beliefs_upkeep_base.Add(field.Value(i));
				}
			}
			if (field2 == null)
			{
				return;
			}
			int num2 = field2.NumValues();
			for (int j = 0; j < num2; j++)
			{
				if (pagan_beliefs_upkeep_ssum1_perc == null)
				{
					pagan_beliefs_upkeep_ssum1_perc = new List<float>(num2);
				}
				pagan_beliefs_upkeep_ssum1_perc.Add(field2.Value(j));
			}
		}
	}

	public class PaganBelief
	{
		public string name;

		public List<StatModifier.Def> mods = new List<StatModifier.Def>();

		public override string ToString()
		{
			return name;
		}

		public string GetNameKey(IVars vars = null, string form = "")
		{
			return "Pagan." + name + ".name";
		}

		public static bool IsPaganBeliefDef(DT.Field f)
		{
			if (f == null)
			{
				return false;
			}
			return f.BaseRoot().key == "PaganBelief";
		}
	}

	public class CharacterBonus
	{
		public DT.Field field;

		public List<StatModifier.Def> mods = new List<StatModifier.Def>();

		public override string ToString()
		{
			return field.Path();
		}
	}

	public class StatModifier : Stat.Modifier
	{
		public class Def
		{
			public Religion.Def religion;

			public DT.Field field;

			public string stat_name;

			public Type type;

			public DT.Field condition_field;

			public Def(Religion.Def religion, DT.Field field, string stat_name)
			{
				this.religion = religion;
				this.field = field;
				this.stat_name = stat_name;
				if (field.FindChild("perc") != null)
				{
					type = Type.Perc;
				}
				else if (field.FindChild("base") != null)
				{
					type = Type.Base;
				}
				else if (field.FindChild("unscaled") != null)
				{
					type = Type.Unscaled;
				}
				condition_field = field.FindChild("condition");
			}

			public DT.Field GetValueField(Kingdom kingdom, out bool const_key)
			{
				const_key = true;
				DT.Field field = this.field;
				if (field.key == "ks_liege_rein_perc")
				{
					if (kingdom.sovereignState != null)
					{
						DT.Field field2 = field.FindChild(kingdom.sovereignState.religion.def.field.key);
						if (field2 != null)
						{
							return field2;
						}
					}
					return field;
				}
				bool flag = kingdom.HasEcumenicalPatriarch();
				if (field.parent.key == "non_orthodox_ecumenical_patriarch")
				{
					if (flag)
					{
						return field;
					}
					return null;
				}
				if (field.children == null || field.children.Count == 0)
				{
					return field;
				}
				if (kingdom.is_catholic)
				{
					if (kingdom.excommunicated)
					{
						DT.Field field3 = field.FindChild("excommunicated");
						if (field3 != null)
						{
							return field3;
						}
					}
					else
					{
						DT.Field field4 = field.FindChild("non_excommunicated");
						if (field4 != null)
						{
							return field4;
						}
					}
					if (kingdom == kingdom.religion.hq_kingdom)
					{
						DT.Field field5 = field.FindChild("papacy");
						if (field5 != null)
						{
							return field5;
						}
					}
					else if (kingdom.court.Contains(kingdom.religion.head))
					{
						DT.Field field6 = field.FindChild("pope_in_court");
						if (field6 != null)
						{
							const_key = false;
							return field6;
						}
					}
				}
				if (flag && !kingdom.is_orthodox)
				{
					DT.Field field7 = field.FindChild("non_orthodox");
					if (field7 != null)
					{
						return field7;
					}
				}
				if (kingdom.is_orthodox)
				{
					if (flag)
					{
						DT.Field field8 = field.FindChild("ecumenical_patriarch");
						if (field8 != null)
						{
							return field8;
						}
					}
					if (kingdom.subordinated)
					{
						DT.Field field9 = field.FindChild("subordinated");
						if (field9 != null)
						{
							return field9;
						}
					}
					else
					{
						DT.Field field10 = field.FindChild("independent");
						if (field10 != null)
						{
							return field10;
						}
					}
				}
				if (kingdom.is_muslim && kingdom.IsCaliphate())
				{
					DT.Field field11 = field.FindChild("caliphate");
					if (field11 != null)
					{
						return field11;
					}
				}
				DT.Field field12 = field.FindChild("holy_lands");
				if (field12 != null)
				{
					const_key = false;
					return field12;
				}
				return field;
			}

			public float CalcValue(Kingdom kingdom)
			{
				if (kingdom == null)
				{
					return 0f;
				}
				if (condition_field != null && !condition_field.Bool(kingdom))
				{
					return 0f;
				}
				bool const_key;
				return GetValueField(kingdom, out const_key)?.Float(kingdom) ?? 0f;
			}

			public override string ToString()
			{
				return field.Path();
			}
		}

		public Def def;

		public Kingdom kingdom => base.owner as Kingdom;

		public Game game => kingdom?.game;

		public Religion religion => game?.religions.Get(def.religion);

		public StatModifier(Def def)
		{
			this.def = def;
		}

		public override DT.Field GetField()
		{
			return def.field;
		}

		public override DT.Field GetNameField()
		{
			if (PaganBelief.IsPaganBeliefDef(def.field?.parent))
			{
				return def.field.parent;
			}
			return def.religion?.field;
		}

		public override string ToString()
		{
			string text = "[" + ConstStr() + "] " + religion.name;
			if (def.field.parent != religion.def.field)
			{
				text = text + "." + def.field.parent.key;
			}
			text = text + "." + def.field.key;
			DT.Field valueField = GetValueField();
			if (valueField != null && valueField != def.field)
			{
				text = text + "." + valueField.key;
			}
			return text + ": " + Stat.Modifier.ToString(value, type);
		}

		public override bool IsConst()
		{
			if (def.condition_field != null)
			{
				return false;
			}
			bool const_key;
			DT.Field valueField = def.GetValueField(kingdom, out const_key);
			if (!const_key)
			{
				return false;
			}
			if (valueField == null)
			{
				return true;
			}
			if (valueField.value.obj_val is Expression)
			{
				return false;
			}
			return true;
		}

		public override float CalcValue(Stats stats, Stat stat)
		{
			return def.CalcValue(kingdom);
		}

		public DT.Field GetValueField()
		{
			bool const_key;
			return def.GetValueField(kingdom, out const_key);
		}
	}

	public delegate string GeReligiontModsText(Kingdom k, string new_line = "\n");

	public delegate string GetCharacterBonusesText(Kingdom k, string new_line = "\n");

	public delegate string GetPatriarchCandidateBonusesText(Kingdom k, Orthodox.PatriarchCandidate pc, string new_line = "\n", bool dark = false);

	public class RefData : Data
	{
		public int id = -1;

		public static RefData Create()
		{
			return new RefData();
		}

		public override string ToString()
		{
			return base.ToString() + $"RefData(game.religions.all[{id}])";
		}

		public override bool InitFrom(object obj)
		{
			if (!(obj is Religion religion))
			{
				return false;
			}
			int num = religion.game?.religions?.all.IndexOf(religion) ?? (-1);
			if (num < 0)
			{
				return false;
			}
			id = num;
			return true;
		}

		public override void Save(Serialization.IWriter ser)
		{
			ser.Write7BitSigned(id, "id");
		}

		public override void Load(Serialization.IReader ser)
		{
			id = ser.Read7BitSigned("id");
		}

		public override object GetObject(Game game)
		{
			int? num = game?.religions?.all.Count;
			if (!num.HasValue || num == 0 || num <= id || id < 0)
			{
				return null;
			}
			return game.religions.all[id];
		}

		public override bool ApplyTo(object obj, Game game)
		{
			if (!(obj is Religion))
			{
				return false;
			}
			int? num = game?.religions?.all.Count;
			if (!num.HasValue || num == 0 || num <= id || id < 0)
			{
				return false;
			}
			return true;
		}
	}

	public Game game;

	public Def def;

	public Kingdom hq_kingdom;

	public Realm hq_realm;

	public Character head;

	public Kingdom head_kingdom;

	public Dictionary<string, int> used_names = new Dictionary<string, int>();

	public static GeReligiontModsText get_religion_mods_text;

	public static GetCharacterBonusesText get_patriarch_bonuses_text;

	public static GetCharacterBonusesText get_pope_bonuses_text;

	public static GetPatriarchCandidateBonusesText get_patriarch_candidate_bonuses_text;

	public string name => def.name;

	public Religion(Game game, Def def)
	{
		this.game = game;
		this.def = def;
	}

	public virtual Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		switch (key)
		{
		case "key":
			return def.field.key;
		case "name":
			return name + ".name";
		case "full_name":
			return GetFullNameKey(vars);
		case "christian":
			return def.christian;
		case "muslim":
			return def.muslim;
		case "pagan":
			return def.pagan;
		case "hq_kingdom":
			return hq_kingdom;
		case "hq_realm":
			return hq_realm;
		case "head_kingdom":
			return head_kingdom;
		case "head":
			return head;
		case "pope":
			return game.religions.catholic.head;
		case "cardinals":
			return new Value(game.religions.catholic.cardinals);
		case "ecumenical_patriarch":
			return game.religions.orthodox.head;
		default:
		{
			Religion religion = game.religions.Get(key);
			if (religion != null)
			{
				return religion == this;
			}
			return Value.Unknown;
		}
		}
	}

	public bool HasTag(string tag)
	{
		for (DT.Field field = def.field; field != null; field = field.based_on)
		{
			if (field.key == tag)
			{
				return true;
			}
		}
		if (tag.StartsWith("Not", StringComparison.Ordinal))
		{
			string tag2 = tag.Substring(3);
			return !HasTag(tag2);
		}
		return false;
	}

	public int DistTo(Religion r)
	{
		if (r == null)
		{
			return 2;
		}
		if (r == this)
		{
			return 0;
		}
		if (def.christian && r.def.christian)
		{
			return 1;
		}
		if (def.muslim && r.def.muslim)
		{
			return 1;
		}
		return 2;
	}

	public string GetNameKey(IVars vars = null, string form = "")
	{
		return def.id + ".name";
	}

	public string GetFullNameKey(IVars vars = null, string form = "")
	{
		if (vars != null)
		{
			Kingdom kingdom = null;
			Value var = vars.GetVar("kingdom");
			if (var.is_object)
			{
				kingdom = var.obj_val as Kingdom;
			}
			if (kingdom != null && kingdom.religion == this)
			{
				if (kingdom.is_catholic)
				{
					if (!kingdom.excommunicated)
					{
						return "Catholic.full_name.default";
					}
					return "Catholic.full_name.excommunicated";
				}
				if (kingdom.is_orthodox)
				{
					if (!kingdom.HasEcumenicalPatriarch())
					{
						if (!kingdom.subordinated)
						{
							return "Orthodox.full_name.independent";
						}
						return "Orthodox.full_name.subordinated";
					}
					return "Orthodox.full_name.ecumenical";
				}
				if (kingdom.is_sunni)
				{
					if (!kingdom.caliphate)
					{
						return "Sunni.full_name.default";
					}
					return "Sunni.full_name.caliphate";
				}
				if (kingdom.is_shia)
				{
					if (!kingdom.caliphate)
					{
						return "Shia.full_name.default";
					}
					return "Shia.full_name.caliphate";
				}
				if (kingdom.is_pagan)
				{
					return "Pagan.full_name.default";
				}
			}
		}
		return GetNameKey();
	}

	public virtual string GetKingdomType(Kingdom k)
	{
		if (k.caliphate)
		{
			return name + ".titles.Caliphate";
		}
		return k.nobility_key + "." + k.nobility_level;
	}

	public virtual string GetTitle(Character c)
	{
		if (c.IsPope())
		{
			return "Catholic.titles.Pope";
		}
		if (c.IsCrusader())
		{
			return "Catholic.titles.Crusader";
		}
		if (this is Catholic catholic && catholic.cardinals.Contains(c))
		{
			return "Catholic.titles.CardinalElect";
		}
		if (c.title == "Cardinal")
		{
			return "Catholic.titles.Cardinal";
		}
		if (c.IsEcumenicalPatriarch())
		{
			return "Orthodox.titles.EcumenicalPatriarch";
		}
		if (c.IsPatriarch())
		{
			return "Orthodox.titles.Patriarch";
		}
		if (c.IsCaliph())
		{
			return name + ".titles.Caliph";
		}
		Kingdom kingdom = c.GetKingdom();
		if (kingdom != null && kingdom.patriarch_candidates != null)
		{
			for (int i = 0; i < kingdom.patriarch_candidates.Count; i++)
			{
				if (kingdom.patriarch_candidates[i].cleric == c)
				{
					return "Orthodox.titles.Cleric";
				}
			}
		}
		if (c.title != null && def.field.FindChild("titles." + c.title) != null)
		{
			return name + ".titles." + c.title;
		}
		if (c.IsCleric() && (c.title == "Knight" || c.title == null))
		{
			return name + ".titles.Cleric";
		}
		return null;
	}

	public static string ReligiousSettlementType(Game game, Religion religion)
	{
		DT.Field field = game.dt.Find("ReligiousSettlement.type_per_religion");
		if (field == null)
		{
			return null;
		}
		if (religion == null)
		{
			return null;
		}
		return field.GetRef(religion.name)?.key;
	}

	public static bool MatchPietyType(string piety_type, Kingdom kingdom)
	{
		if (string.IsNullOrEmpty(piety_type) || kingdom?.religion == null)
		{
			return true;
		}
		if (kingdom.religion.HasTag(piety_type))
		{
			return true;
		}
		return false;
	}

	public static bool FixPietyType(Resource production, string piety_type, Kingdom kingdom)
	{
		if (production == null || production[ResourceType.Piety] == 0f)
		{
			return false;
		}
		if (MatchPietyType(piety_type, kingdom))
		{
			return false;
		}
		production[ResourceType.Piety] = 0f - production[ResourceType.Piety];
		return true;
	}

	public override string ToString()
	{
		return name;
	}

	public virtual void Init(bool new_game)
	{
		if (def.hq_kingdom != null)
		{
			head_kingdom = (hq_kingdom = game.GetKingdom(def.hq_kingdom));
			if (hq_kingdom == null)
			{
				Game.Log(name + " HQ kingdom not found: " + def.hq_kingdom, Game.LogType.Error);
			}
			else if (new_game)
			{
				hq_kingdom.religion = this;
			}
		}
		if (def.hq_realm == null)
		{
			return;
		}
		hq_realm = game.GetRealm(def.hq_realm);
		if (hq_realm == null)
		{
			Game.Log(name + " HQ realm not found: " + def.hq_realm, Game.LogType.Error);
			return;
		}
		hq_realm.AddListener(this);
		if (new_game)
		{
			Kingdom kingdom = hq_realm.GetKingdom();
			if (kingdom != null)
			{
				head_kingdom = kingdom;
				head_kingdom.subordinated = false;
			}
			kingdom = game.GetKingdom(hq_realm.id);
			if (kingdom != null)
			{
				kingdom.religion = this;
				kingdom.subordinated = false;
			}
		}
	}

	public virtual void InitCharacters(bool new_game)
	{
		if (game.IsAuthority() && head == null)
		{
			Character character = CreateHead(head_kingdom, new_game);
			SetHead(character);
		}
	}

	public virtual void AddModifiers(Kingdom k)
	{
		Stats stats = k.stats;
		if (stats == null)
		{
			return;
		}
		for (int i = 0; i < this.def.mods.Count; i++)
		{
			StatModifier.Def def = this.def.mods[i];
			StatModifier statModifier = new StatModifier(def);
			stats.AddModifier(def.stat_name, statModifier);
			if (k.religion_mods == null)
			{
				k.religion_mods = new List<StatModifier>(this.def.mods.Count);
			}
			k.religion_mods.Add(statModifier);
		}
	}

	public void DelModifiers(Kingdom k)
	{
		if (k.religion_mods == null)
		{
			return;
		}
		for (int i = 0; i < k.religion_mods.Count; i++)
		{
			StatModifier statModifier = k.religion_mods[i];
			if (statModifier.stat != null)
			{
				statModifier.stat.DelModifier(statModifier);
			}
		}
		k.religion_mods.Clear();
	}

	public static void RefreshModifiers(Kingdom k)
	{
		if (k.religion_mods != null)
		{
			for (int i = 0; i < k.religion_mods.Count; i++)
			{
				StatModifier statModifier = k.religion_mods[i];
				if (statModifier.stat != null)
				{
					statModifier.stat.RefreshModifier(statModifier);
				}
			}
		}
		if (k.patriarch_mods == null)
		{
			return;
		}
		for (int j = 0; j < k.patriarch_mods.Count; j++)
		{
			StatModifier statModifier2 = k.patriarch_mods[j];
			if (statModifier2.stat != null)
			{
				statModifier2.stat.RefreshModifier(statModifier2);
			}
		}
	}

	public static bool IsSkilledCaliph(Character c)
	{
		if (c == null)
		{
			return false;
		}
		Kingdom kingdom = c.GetKingdom();
		if (kingdom == null)
		{
			return false;
		}
		DT.Field field = kingdom.religion?.def?.field?.FindChild("caliph_skills");
		if (field == null)
		{
			return false;
		}
		int num = field.NumValues();
		if (num <= 0)
		{
			return false;
		}
		for (int i = 0; i < num; i++)
		{
			string skill_name = field.String(i);
			if (c.GetSkill(skill_name) == null)
			{
				return false;
			}
		}
		return true;
	}

	private void ApplyReligionChangeOutcomes(Kingdom k)
	{
		Character character = game.religions.catholic.head;
		if (character != null && character.GetSpecialCourtKingdom() == k)
		{
			game.religions.catholic.PopeLeave();
		}
		if (k != game.religions.orthodox.head_kingdom)
		{
			game.religions.orthodox.SetSubordinated(k, subordinated: true);
		}
		bool flag = k.excommunicated;
		k.excommunicated = false;
		if (k.caliphate)
		{
			flag = true;
			if (k.jihad != null)
			{
				War.EndJihad(k, "caliphate_abandoned", end_war: false);
			}
			game.religions.FireEvent("caliphate_abandoned", k);
		}
		if (k.is_muslim && k.jihad_attacker != null)
		{
			War.EndJihad(k.jihad.GetEnemyLeader(k), "religion_changed", end_war: false);
		}
		k.caliphate = false;
		if (flag)
		{
			k.SendState<Kingdom.ReligionState>();
			RefreshModifiers(k);
		}
	}

	private static void AlterClericOutcomeChannceFunc(OutcomeDef outcome, IVars vars)
	{
	}

	private void ApplyReligionChangeClericAccept(Kingdom k, Character c)
	{
		OnMissionInRomeStatus onMissionInRomeStatus = c.FindStatus<OnMissionInRomeStatus>();
		OnMissionInConstantinopleStatus onMissionInConstantinopleStatus = c.FindStatus<OnMissionInConstantinopleStatus>();
		Army army = c.GetArmy();
		if (army != null)
		{
			if (!c.CanLeadArmy())
			{
				c.SetLocation(null);
				army.SetLeader(null);
				army.BecomeMercenary(c.GetKingdom().id);
				c.Recall();
			}
		}
		else if ((onMissionInConstantinopleStatus == null && onMissionInRomeStatus == null) || !k.is_christian)
		{
			c.Recall();
		}
		c.UpdateAutomaticStatuses();
	}

	private void ApplyReligionChangeClericOutcomes(Kingdom k, Character c)
	{
		if (c == null || !c.IsCleric())
		{
			return;
		}
		if (c.IsKing())
		{
			ApplyReligionChangeClericAccept(k, c);
		}
		else if (c.prison_kingdom != null)
		{
			c.Die(new DeadStatus("unknown", c));
		}
		else
		{
			if (!c.IsInCourt())
			{
				return;
			}
			Vars vars = new Vars(this);
			vars.Set("cleric", c);
			List<OutcomeDef> list = def.religion_change_cleric_outcomes.DecideOutcomes(game, vars, null, AlterClericOutcomeChannceFunc);
			if (list == null)
			{
				return;
			}
			for (int i = 0; i < list.Count; i++)
			{
				OutcomeDef outcomeDef = list[i];
				switch (outcomeDef.field.key)
				{
				case "accept":
					ApplyReligionChangeClericAccept(k, c);
					return;
				case "rebel":
					c.TurnIntoRebel("GeneralRebels", "ReligionSpawnCondition");
					return;
				case "exile":
					c.Die(new DeadStatus("denied_new_religion", c));
					return;
				}
				Game.Log("Unhandled religion change cleric outcome: " + outcomeDef.field.Path(), Game.LogType.Warning);
			}
		}
	}

	public void ApplyReligionChangeClericOutcomes(Kingdom k)
	{
		if (def.religion_change_cleric_outcomes == null || k.court == null)
		{
			return;
		}
		for (int i = 0; i < k.court.Count; i++)
		{
			Character character = k.court[i];
			if (character != null && character.IsAlive())
			{
				ApplyReligionChangeClericOutcomes(k, character);
			}
		}
	}

	public void OnReligionChanged(Kingdom k)
	{
		ApplyReligionChangeOutcomes(k);
	}

	public virtual void OnHQRealmChangedKingdom()
	{
	}

	public virtual string NewName()
	{
		if (def.head_names == null)
		{
			return null;
		}
		List<string> list = def.head_names.Keys();
		if (list.Count == 0)
		{
			return null;
		}
		return list[game.Random(0, list.Count)];
	}

	private void UpdateHeadNameIndex()
	{
		if (head.IsAuthority())
		{
			string key = head.GetName();
			used_names.TryGetValue(key, out var value);
			value++;
			used_names[key] = value;
			head.name_idx = value;
			game.religions.SendState<Religions.UsedNamesState>();
			head.SendState<Character.NameState>();
		}
	}

	public virtual void SetHead(Character head, bool from_state = false)
	{
		bool flag = head != null && head.name_idx == 0;
		this.head = head;
		if (from_state)
		{
			return;
		}
		game.religions.SendState<Religions.CharactersState>();
		if (head == null)
		{
			return;
		}
		if (flag)
		{
			string text = NewName();
			if (text != null)
			{
				head.Name = text;
			}
			UpdateHeadNameIndex();
		}
		head.Recall();
		head.EnableAging(can_die: true);
	}

	public virtual Character CreateCharacter(Kingdom kingdom, bool initial = false)
	{
		if (kingdom == null)
		{
			return null;
		}
		Character character = CharacterFactory.CreateCharacter(kingdom.game, kingdom.id, "Cleric", "Knight", Character.Age.Adult);
		character.next_age_check -= game.Random(0f, character.GetAgeupDuration(character.age));
		character.ethnicity = kingdom.default_ethnicity;
		character.InitDefaultSkill();
		return character;
	}

	public virtual Character CreateHead(Kingdom kingdom, bool initial = false)
	{
		return CreateCharacter(kingdom, initial);
	}

	public virtual void OnCharacterDied(Character c)
	{
		if (c == head)
		{
			OnHeadDied();
		}
	}

	public virtual void OnHeadDied()
	{
		Vars vars = new Vars(head);
		vars.Set("title", head.GetTitle());
		Character character = CreateHead(head.GetKingdom());
		if (character != null)
		{
			vars.Set("replacement", character);
			vars.Set("old_title", GetTitle(character));
			vars.Set("old_name", character.GetName());
			if (character.name_idx > 0)
			{
				vars.Set("old_name_idx", character.name_idx);
			}
		}
		SetHead(character);
		game.religions.NotifyListeners("character_died", vars);
	}

	public void OnMessage(object obj, string message, object param)
	{
		if (message == "kingdom_changed" && obj == hq_realm)
		{
			OnHQRealmChangedKingdom();
		}
	}

	public virtual void OnUpdate()
	{
	}

	public override void DumpState(StateDump dump, int verbosity)
	{
		dump.OpenSection(name);
		DumpInnerState(dump, verbosity);
		dump.CloseSection(name);
	}

	public override void DumpInnerState(StateDump dump, int verbosity)
	{
		dump.Append("hq_kingdom", hq_kingdom?.Name);
		dump.Append("hq_realm", hq_realm?.name);
		dump.Append("head", head);
		dump.Append("head_kingdom", head_kingdom?.Name);
	}
}
