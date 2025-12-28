using System.Collections.Generic;

namespace Logic;

public class Rumor : BaseObject, IVars
{
	public class Def : Logic.Def
	{
		public struct OpportunityParams
		{
			public DT.Field field;

			public Action.Def adef;

			public int min_targets;

			public int max_targets;
		}

		public List<Query> args;

		public Condition condition;

		public DT.Field chance_field;

		public Action.Def opportunity;

		public List<OpportunityParams> opportunities;

		public int min_opportunities;

		public int max_opportunities;

		public override bool Load(Game game)
		{
			DT.Field field = base.field;
			args = Query.LoadAll(game, field.FindChild("args"));
			condition = Condition.Load(field.FindChild("condition"));
			chance_field = field.FindChild("chance");
			LoadOpportunities(game);
			return true;
		}

		private void LoadOpportunities(Game game)
		{
			opportunity = null;
			opportunities = null;
			min_opportunities = (max_opportunities = 0);
			DT.Field field = base.field.GetRef("opportunity");
			if (field != null)
			{
				opportunity = Logic.Def.Get<Action.Def>(field);
			}
			DT.Field field2 = base.field.FindChild("opportunities");
			if (field2?.children == null)
			{
				return;
			}
			for (int i = 0; i < field2.children.Count; i++)
			{
				DT.Field field3 = field2.children[i];
				if (!string.IsNullOrEmpty(field3.key))
				{
					AddOpportunity(game, field3);
				}
			}
			if (opportunities != null)
			{
				switch (field2.NumValues())
				{
				case 0:
					min_opportunities = opportunities.Count;
					max_opportunities = opportunities.Count;
					break;
				case 1:
					min_opportunities = (max_opportunities = field2.Int());
					break;
				default:
					min_opportunities = field2.Int(0, null, opportunities.Count);
					max_opportunities = field2.Int(1, null, min_opportunities);
					break;
				}
				if (max_opportunities > opportunities.Count)
				{
					max_opportunities = opportunities.Count;
				}
				if (min_opportunities > max_opportunities)
				{
					min_opportunities = max_opportunities;
				}
			}
		}

		private void AddOpportunity(Game game, DT.Field f)
		{
			OpportunityParams item = new OpportunityParams
			{
				field = f,
				adef = game.defs.Get<Action.Def>(f.key)
			};
			item.min_targets = f.Int(0, null, 1);
			item.max_targets = f.Int(1, null, item.min_targets);
			if (opportunities == null)
			{
				opportunities = new List<OpportunityParams>();
			}
			opportunities.Add(item);
		}
	}

	public class FullData : Data
	{
		public string def;

		public int tgt_kingdom;

		public int src_kingdom;

		public NID spy;

		public Data args;

		public Data opportunities;

		public static FullData Create()
		{
			return new FullData();
		}

		public override bool InitFrom(object obj)
		{
			if (!(obj is Rumor rumor))
			{
				return false;
			}
			def = rumor.def?.id;
			tgt_kingdom = ((rumor.tgt_kingdom != null) ? rumor.tgt_kingdom.id : 0);
			src_kingdom = ((rumor.src_kingdom != null) ? rumor.src_kingdom.id : 0);
			spy = rumor.spy;
			args = Data.CreateFull(rumor.args);
			opportunities = Data.CreateFull(opportunities);
			return true;
		}

		public override void Save(Serialization.IWriter ser)
		{
			ser.WriteStr(def, "def");
			ser.Write7BitUInt(tgt_kingdom, "tgt_kingdom");
			ser.Write7BitUInt(src_kingdom, "src_kingdom");
			ser.WriteNID<Character>(spy, "spy");
			ser.WriteData(args, "args");
			ser.WriteData(opportunities, "opportunities");
		}

		public override void Load(Serialization.IReader ser)
		{
			def = ser.ReadStr("def");
			tgt_kingdom = ser.Read7BitUInt("tgt_kingdom");
			src_kingdom = ser.Read7BitUInt("src_kingdom");
			spy = ser.ReadNID<Character>("spy");
			args = ser.ReadData("args");
			opportunities = ser.ReadData("opportunities");
		}

		public override object GetObject(Game game)
		{
			return new Rumor(null, null);
		}

		public override bool ApplyTo(object obj, Game game)
		{
			if (!(obj is Rumor rumor))
			{
				return false;
			}
			rumor.def = game.defs.Get<Def>(def);
			rumor.tgt_kingdom = game.GetKingdom(tgt_kingdom);
			rumor.src_kingdom = game.GetKingdom(src_kingdom);
			rumor.spy = spy.Get<Character>(game);
			rumor.args = Data.RestoreObject<Vars>(args, game);
			rumor.opportunities = Data.RestoreObject<List<Opportunity>>(opportunities, game);
			return true;
		}
	}

	public Def def;

	public Kingdom tgt_kingdom;

	public Kingdom src_kingdom;

	public Character spy;

	public Vars args;

	public int args_valid = -1;

	public List<Opportunity> opportunities;

	public float chance = -1f;

	private static Rumor tmp_rumor = new Rumor(null, null);

	private static List<Rumor> tmp_rumors = new List<Rumor>();

	private static List<Rumor> tmp_backup_rumors = new List<Rumor>();

	private static List<Object> tmp_targets = new List<Object>();

	public Rumor(Def def, Kingdom tgt_kingdom, Kingdom src_kingdom = null, Character spy = null)
	{
		Set(def, tgt_kingdom, src_kingdom, spy);
	}

	public Rumor(Rumor r)
	{
		def = r.def;
		tgt_kingdom = r.tgt_kingdom;
		src_kingdom = r.src_kingdom;
		spy = r.spy;
		args_valid = r.args_valid;
		if (args_valid > 0)
		{
			args = r.args;
		}
		opportunities = null;
		chance = r.chance;
	}

	public void Set(Def def, Kingdom tgt_kingdom, Kingdom src_kingdom = null, Character spy = null)
	{
		this.def = def;
		this.tgt_kingdom = tgt_kingdom;
		this.src_kingdom = src_kingdom;
		this.spy = spy ?? tgt_kingdom?.GetSpyFrom(src_kingdom);
		opportunities = null;
	}

	public bool Validate(bool recalc = false)
	{
		CalcChance(recalc);
		if (chance == 0f)
		{
			return false;
		}
		if (!CalcArgs(recalc))
		{
			return false;
		}
		if (def.condition == null)
		{
			return true;
		}
		if (!def.condition.GetValue(this).Bool())
		{
			return false;
		}
		return true;
	}

	public float CalcChance(bool force_recalc = false)
	{
		if (chance >= 0f && !force_recalc)
		{
			return chance;
		}
		chance = 100f;
		if (def.chance_field != null)
		{
			chance = def.chance_field.Float(this);
		}
		if (chance <= 0f)
		{
			return chance = 0f;
		}
		return chance;
	}

	public bool CalcArgs(bool force_recalc = false)
	{
		if (args_valid >= 0 && !force_recalc)
		{
			return args_valid != 0;
		}
		if (def.args == null)
		{
			args_valid = 1;
			args = null;
			return true;
		}
		Vars out_vars = new Vars();
		if (!Query.FillVars(out_vars, def.args, this))
		{
			args_valid = 0;
			args = null;
			return false;
		}
		args_valid = 1;
		args = out_vars;
		return true;
	}

	public static List<Rumor> DecideNewRumors(Character spy, int max_count = 1, int min_count = 0)
	{
		if (spy?.mission_kingdom == null || max_count <= 0)
		{
			return null;
		}
		List<Def> defs = spy.game.defs.GetDefs<Def>();
		if (defs == null || defs.Count == 0)
		{
			return null;
		}
		List<Rumor> list = tmp_rumors;
		list.Clear();
		List<Rumor> list2 = tmp_backup_rumors;
		list2.Clear();
		tmp_rumor.Set(null, spy.mission_kingdom, spy.GetKingdom(), spy);
		float num = 0f;
		float num2 = 0f;
		float num3 = spy.game.Random(0f, 100f);
		for (int i = 0; i < defs.Count; i++)
		{
			tmp_rumor.def = defs[i];
			if (tmp_rumor.Validate(recalc: true))
			{
				if (tmp_rumor.chance >= num3)
				{
					list.Add(new Rumor(tmp_rumor));
					num += tmp_rumor.chance;
				}
				else if (min_count > 0)
				{
					list2.Add(new Rumor(tmp_rumor));
					num2 += tmp_rumor.chance;
				}
			}
		}
		if (list.Count == 0 && list2.Count == 0)
		{
			return null;
		}
		List<Rumor> list3 = new List<Rumor>(max_count);
		for (int j = 0; j < max_count; j++)
		{
			int num4 = ChooseRumor(spy.game, list, num);
			if (num4 < 0)
			{
				break;
			}
			Rumor item = list[num4];
			list.RemoveAt(num4);
			list3.Add(item);
		}
		while (list3.Count < min_count)
		{
			int num5 = ChooseRumor(spy.game, list2, num2);
			if (num5 < 0)
			{
				break;
			}
			Rumor item2 = list2[num5];
			list2.RemoveAt(num5);
			list3.Add(item2);
		}
		return list3;
	}

	private static int ChooseRumor(Game game, List<Rumor> lst, float total)
	{
		if (total <= 0f || lst.Count == 0)
		{
			return -1;
		}
		float num = game.Random(0f, total);
		for (int i = 0; i < lst.Count; i++)
		{
			Rumor rumor = lst[i];
			if (rumor.chance >= num)
			{
				return i;
			}
			num -= rumor.chance;
		}
		return game.Random(0, lst.Count);
	}

	public static void SpreadRumors(List<Rumor> rumors)
	{
		if (rumors == null || rumors.Count == 0)
		{
			return;
		}
		if (rumors.Count == 1)
		{
			rumors[0].Spread();
			return;
		}
		Character character = rumors[0].spy;
		if (character == null)
		{
			Game.Log("Trying to spread rumors with unknown spy", Game.LogType.Error);
			return;
		}
		Kingdom kingdom = character.GetKingdom();
		Kingdom kingdom2 = character.mission_kingdom ?? kingdom;
		List<Opportunity> list = null;
		for (int i = 0; i < rumors.Count; i++)
		{
			Rumor rumor = rumors[i];
			if (rumor.spy != character || rumor.src_kingdom != kingdom || rumor.tgt_kingdom != kingdom2)
			{
				Game.Log("Trying to spread rumors with different parameters", Game.LogType.Error);
				rumors.RemoveAt(i);
				i--;
				continue;
			}
			rumor.AddOpportunities();
			if (rumor.opportunities != null && rumor.opportunities.Count != 0)
			{
				if (list == null)
				{
					list = new List<Opportunity>();
				}
				list.AddRange(rumor.opportunities);
			}
		}
		Vars vars = new Vars(character);
		vars.Set("spy", character);
		vars.Set("src_kingdom", kingdom);
		vars.Set("tgt_kingdom", kingdom2);
		vars.Set("rumors", rumors);
		vars.Set("opportunities", list);
		kingdom.FireEvent("new_rumors", vars, kingdom.id);
	}

	public bool Spread()
	{
		if (src_kingdom == null)
		{
			return false;
		}
		AddOpportunities();
		src_kingdom.FireEvent("new_rumor", this, src_kingdom.id);
		return true;
	}

	private List<Object> GetPossibleTargets()
	{
		tmp_targets.Clear();
		if (args == null)
		{
			return null;
		}
		Value raw = args.GetRaw("target");
		if (!raw.is_unknown)
		{
			Object item = raw.Get<Object>();
			tmp_targets.Add(item);
			return tmp_targets;
		}
		raw = args.GetRaw("targets");
		if (!raw.is_unknown && raw.obj_val is List<Value> list)
		{
			for (int i = 0; i < list.Count; i++)
			{
				Object item2 = list[i].Get<Object>();
				tmp_targets.Add(item2);
			}
			return tmp_targets;
		}
		return null;
	}

	public void AddOpportunities()
	{
		if (opportunities != null)
		{
			return;
		}
		opportunities = new List<Opportunity>();
		if (spy?.actions == null)
		{
			return;
		}
		List<Object> possibleTargets = GetPossibleTargets();
		if (def.opportunities != null)
		{
			int num = spy.game.Random(def.min_opportunities, def.max_opportunities + 1);
			for (int i = 0; i < def.opportunities.Count; i++)
			{
				if (opportunities.Count >= num)
				{
					break;
				}
				Def.OpportunityParams opportunityParams = def.opportunities[i];
				Action action = spy.actions.Find(opportunityParams.adef);
				if (action == null)
				{
					continue;
				}
				int num2 = spy.game.Random(opportunityParams.min_targets, opportunityParams.max_targets + 1);
				for (int j = 0; j < num2; j++)
				{
					Opportunity opportunity = spy.actions.TryActivateOpportunity(action, forced: true, silent: true, send_state: false, possibleTargets);
					if (opportunity == null)
					{
						break;
					}
					opportunities.Add(opportunity);
					possibleTargets?.Remove(opportunity.target);
				}
			}
		}
		if (def.opportunity != null)
		{
			Action action2 = spy.actions.Find(def.opportunity);
			if (action2 != null)
			{
				Opportunity opportunity2 = spy.actions.TryActivateOpportunity(action2, forced: true, silent: true, send_state: false, possibleTargets);
				if (opportunity2 != null)
				{
					opportunities.Add(opportunity2);
				}
			}
		}
		if (opportunities.Count > 0)
		{
			spy.NotifyListeners("opportunities_changed");
			spy.SendState<Object.ActionsState>();
		}
	}

	public override bool IsRefSerializable()
	{
		return false;
	}

	public override bool IsFullSerializable()
	{
		if (!Data.IsFullSerializable(args))
		{
			return false;
		}
		if (!Data.IsFullSerializable(opportunities))
		{
			return false;
		}
		return true;
	}

	public Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		switch (key)
		{
		case "kingdom":
		case "tgt_kingdom":
		case "target_kingdom":
			return tgt_kingdom;
		case "spy_kingdom":
		case "src_kingdom":
			return src_kingdom;
		case "owner":
		case "spy":
			return spy;
		case "args":
			return args;
		case "opportunities":
			if (opportunities == null || opportunities.Count == 0)
			{
				return Value.Null;
			}
			return new Value(opportunities);
		default:
		{
			Value var;
			if (args != null)
			{
				var = args.GetVar(key, vars ?? this, as_value);
				if (!var.is_unknown)
				{
					return var;
				}
			}
			var = tgt_kingdom.GetVar(key, vars ?? this, as_value);
			if (!var.is_unknown)
			{
				return var;
			}
			var = def.field.GetVar(key, vars ?? this, as_value);
			if (!var.is_unknown)
			{
				return var;
			}
			return Value.Unknown;
		}
		}
	}

	public override string ToString()
	{
		return "Rumor " + def?.id + " from " + Object.ToString(spy) + " in " + Object.ToString(tgt_kingdom);
	}
}
