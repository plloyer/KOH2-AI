using System;
using System.Collections.Generic;
using System.Reflection;

namespace Logic;

public class SuccessAndFail
{
	public class Factor
	{
		public class Def
		{
			public DT.Field field;

			public bool show_always;

			public bool perc;

			public CalcFunc calc_func;

			public bool valid = true;

			public Def(Game game, SuccessAndFail.Def sf_def, DT.Field field)
			{
				this.field = field;
				show_always = field.GetBool("show_always", null, show_always);
				perc = field.Type() == "perc" || field.GetBool("perc");
				calc_func = FindCalcFunc(game, sf_def, field);
				if (calc_func == null && !(field.Value(null, calc_expression: false).obj_val is Expression))
				{
					Game.Log(field.Path(include_file: true) + ": unknown success/fail factor", Game.LogType.Error);
					valid = false;
				}
			}

			public override string ToString()
			{
				string text = field.ToString();
				if (calc_func == null)
				{
					text = "[No C#] " + text;
				}
				return text;
			}
		}

		public Def def;

		public int value;

		public Factor(Def def, int value)
		{
			this.def = def;
			this.value = value;
		}

		public override string ToString()
		{
			return def.ToString() + ": " + value + (def.perc ? "%" : "");
		}
	}

	public class Def
	{
		public DT.Field field;

		public Logic.Def parent_def;

		public List<Factor.Def> factors;

		public bool valid => factors != null;

		public Def(Game game, DT.Field field)
		{
			this.field = field;
			ResolveParent(game);
			factors = null;
			LoadFactors(game, field);
		}

		public override string ToString()
		{
			return field.Path() + " {" + ((factors == null) ? "null" : factors.Count.ToString()) + "}";
		}

		private void ResolveParent(Game game)
		{
			parent_def = Logic.Def.Get(field.parent);
		}

		public int MinVal(IVars vars)
		{
			return field.Int(0, vars);
		}

		public int MaxVal(IVars vars)
		{
			return field.Int(1, vars, 100);
		}

		private void LoadFactors(Game game, DT.Field field)
		{
			List<DT.Field> list = field?.Children();
			if (list == null)
			{
				return;
			}
			for (int i = 0; i < list.Count; i++)
			{
				DT.Field field2 = list[i];
				if (string.IsNullOrEmpty(field2.key))
				{
					continue;
				}
				if (field2.Type() == "include")
				{
					LoadFactors(game, field2);
					continue;
				}
				Factor.Def def = LoadFactor(game, field2);
				if (def != null)
				{
					if (factors == null)
					{
						factors = new List<Factor.Def>();
					}
					factors.Add(def);
				}
			}
		}

		private Factor.Def LoadFactor(Game game, DT.Field ff)
		{
			Factor.Def def = new Factor.Def(game, this, ff);
			if (!def.valid)
			{
				return null;
			}
			return def;
		}
	}

	public delegate string GetFactorsText(SuccessAndFail sf);

	public delegate int CalcFunc(SuccessAndFail sf, Factor.Def factor);

	public Game game;

	public Def def;

	public IVars vars;

	public List<Factor> success_factors;

	public List<Factor> success_perc_factors;

	public List<Factor> no_factors;

	public List<Factor> no_perc_factors;

	public List<Factor> fail_factors;

	public List<Factor> fail_perc_factors;

	public int SP;

	public int FP;

	public int SP_perc;

	public int FP_perc;

	public int value;

	private static SuccessAndFail instance = new SuccessAndFail();

	public static GetFactorsText get_factors_texts = null;

	public static Type[] CalcFuncParams = new Type[2]
	{
		typeof(SuccessAndFail),
		typeof(Factor.Def)
	};

	private SuccessAndFail()
	{
	}

	public static SuccessAndFail Get(Game game, Def def, IVars vars, bool keep_factors)
	{
		if (def == null)
		{
			return null;
		}
		instance.game = game;
		instance.def = def;
		instance.vars = vars;
		instance.Calc(keep_factors);
		return instance;
	}

	public static SuccessAndFail Get(Action action, bool keep_factors, IVars vars = null)
	{
		return Get(action.game, action.def.success_fail, vars ?? action, keep_factors);
	}

	public override string ToString()
	{
		string text = def.ToString() + " -> " + value + " (" + SP + " - " + FP;
		if (SP_perc != 0)
		{
			text = text + " + " + SP_perc + "%";
		}
		if (FP_perc != 0)
		{
			text = text + " - " + FP_perc + "%";
		}
		return text + ")";
	}

	public string FactorsText()
	{
		if (get_factors_texts == null)
		{
			return null;
		}
		string text = get_factors_texts(this);
		if (string.IsNullOrEmpty(text))
		{
			return text;
		}
		return "#" + text;
	}

	public List<Vars> FactorsTextVars(bool success_factors = true, bool no_factors = true, bool fail_factors = true)
	{
		List<Vars> list = new List<Vars>();
		if (success_factors)
		{
			AddFactorsTextVars(list, this.success_factors);
			AddFactorsTextVars(list, success_perc_factors);
		}
		if (no_factors)
		{
			AddFactorsTextVars(list, this.no_factors);
			AddFactorsTextVars(list, no_perc_factors);
		}
		if (fail_factors)
		{
			AddFactorsTextVars(list, this.fail_factors);
			AddFactorsTextVars(list, fail_perc_factors);
		}
		return list;
	}

	private void AddFactorsTextVars(List<Vars> res, List<Factor> factors)
	{
		if (factors != null)
		{
			for (int i = 0; i < factors.Count; i++)
			{
				Factor factor = factors[i];
				AddFactorTextVars(res, factor);
			}
		}
	}

	private void AddFactorTextVars(List<Vars> res, Factor factor)
	{
		if (factor != null)
		{
			Vars vars = new Vars();
			_ = factor.value;
			vars.Set("value", factor.value);
			vars.Set("perc", factor.def.perc);
			string val = $"@[{'{'}{factor.def.field.Path()}.name{'}'}|{'{'}SuccessAndFail.{factor.def.field.key}.name{'}'}]";
			vars.Set("descr", val);
			res.Add(vars);
		}
	}

	public int Chance()
	{
		if (value < 0)
		{
			return 0;
		}
		if (value >= 100)
		{
			return 100;
		}
		return value;
	}

	public bool Decide()
	{
		if (value <= 0)
		{
			return false;
		}
		if (value >= 100)
		{
			return true;
		}
		return game.Random(0, 100) < value;
	}

	private void Calc(bool keep_factors)
	{
		success_factors = null;
		success_perc_factors = null;
		no_factors = null;
		no_perc_factors = null;
		fail_factors = null;
		fail_perc_factors = null;
		SP = 0;
		FP = 0;
		SP_perc = 0;
		FP_perc = 0;
		value = 0;
		if (this.def == null || this.def.factors == null)
		{
			return;
		}
		for (int i = 0; i < this.def.factors.Count; i++)
		{
			Factor.Def def = this.def.factors[i];
			int num = CalcValue(def);
			if (num == 0 && !def.show_always && !keep_factors)
			{
				continue;
			}
			if (num > 0)
			{
				if (def.perc)
				{
					SP_perc += num;
				}
				else
				{
					SP += num;
				}
			}
			else if (num < 0)
			{
				if (def.perc)
				{
					FP_perc -= num;
				}
				else
				{
					FP -= num;
				}
			}
			if (!keep_factors)
			{
				continue;
			}
			Factor item = new Factor(def, num);
			if (num > 0)
			{
				if (def.perc)
				{
					if (success_perc_factors == null)
					{
						success_perc_factors = new List<Factor>();
					}
					success_perc_factors.Add(item);
				}
				else
				{
					if (success_factors == null)
					{
						success_factors = new List<Factor>();
					}
					success_factors.Add(item);
				}
			}
			else if (num < 0)
			{
				if (def.perc)
				{
					if (fail_perc_factors == null)
					{
						fail_perc_factors = new List<Factor>();
					}
					fail_perc_factors.Add(item);
				}
				else
				{
					if (fail_factors == null)
					{
						fail_factors = new List<Factor>();
					}
					fail_factors.Add(item);
				}
			}
			else if (def.perc)
			{
				if (no_perc_factors == null)
				{
					no_perc_factors = new List<Factor>();
				}
				no_perc_factors.Add(item);
			}
			else
			{
				if (no_factors == null)
				{
					no_factors = new List<Factor>();
				}
				no_factors.Add(item);
			}
		}
		value = Math.Max(SP - FP, 0);
		float num2 = SP_perc - FP_perc;
		if (num2 != 0f)
		{
			value = (int)((float)value * (1f + num2 * 0.01f));
		}
		int num3 = this.def.MinVal(vars);
		int num4 = this.def.MaxVal(vars);
		if (value < num3)
		{
			value = num3;
		}
		else if (value > num4)
		{
			value = num4;
		}
	}

	private int CalcValue(Factor.Def factor)
	{
		if (factor.calc_func != null)
		{
			return factor.calc_func(this, factor);
		}
		return factor.field.Int(vars);
	}

	public static CalcFunc FindCalcFunc(Game game, Def sf_def, DT.Field field)
	{
		string key = field.key;
		for (Reflection.TypeInfo typeInfo = sf_def.parent_def?.obj_type; typeInfo != null; typeInfo = typeInfo.base_rtti)
		{
			CalcFunc calcFunc = FindCalcFunc(typeInfo.type, key);
			if (calcFunc != null)
			{
				return calcFunc;
			}
		}
		return FindCalcFunc(typeof(SuccessAndFail), key);
	}

	public static CalcFunc FindCalcFunc(Type type, string name)
	{
		MethodInfo method = type.GetMethod(name, CalcFuncParams);
		if (method == null || !method.IsStatic || !method.IsPublic || method.ReturnType != typeof(int))
		{
			return null;
		}
		try
		{
			if (Delegate.CreateDelegate(typeof(CalcFunc), method) is CalcFunc result)
			{
				return result;
			}
		}
		catch (Exception ex)
		{
			Game.Log("Error creating SuccessAndFail factor calculator delegate for " + name + ": " + ex.ToString(), Game.LogType.Error);
		}
		return null;
	}

	public static int sf_wars(SuccessAndFail sf, Factor.Def factor)
	{
		Kingdom kingdom = sf.vars?.GetVar("src_kingdom").Get<Kingdom>();
		if (kingdom == null)
		{
			return 0;
		}
		int num = factor.field.Int(sf.vars);
		return kingdom.wars.Count * num;
	}

	public static int sf_wars_with_catholics(SuccessAndFail sf, Factor.Def factor)
	{
		Kingdom kingdom = sf.vars?.GetVar("src_kingdom").Get<Kingdom>();
		if (kingdom == null)
		{
			return 0;
		}
		if (kingdom.game?.religions?.catholic?.crusade?.target == kingdom)
		{
			return factor.field.Int(1, sf.vars);
		}
		int num = 0;
		int num2 = factor.field.Int(0, sf.vars);
		for (int i = 0; i < kingdom.wars.Count; i++)
		{
			if (kingdom.wars[i].GetEnemyLeader(kingdom).is_catholic)
			{
				num += num2;
			}
		}
		return num;
	}

	public static int sf_crown_authority(SuccessAndFail sf, Factor.Def factor)
	{
		Kingdom kingdom = sf.vars?.GetVar("src_kingdom").Get<Kingdom>();
		if (kingdom == null)
		{
			return 0;
		}
		CrownAuthority crownAuthority = kingdom.GetCrownAuthority();
		int num = crownAuthority.GetValue();
		if (num == 0)
		{
			return 0;
		}
		if (num < 0)
		{
			int num2 = factor.field.Int(0, sf.vars);
			return num * num2 / crownAuthority.Min();
		}
		int num3 = factor.field.Int(1, sf.vars);
		return num * num3 / crownAuthority.Max();
	}

	public static int sf_crown_authority2(SuccessAndFail sf, Factor.Def factor)
	{
		return sf_crown_authority(sf, factor);
	}

	public static int sf_target_crown_authority(SuccessAndFail sf, Factor.Def factor)
	{
		Kingdom kingdom = sf.vars?.GetVar("mission_kingdom").Get<Kingdom>();
		if (kingdom == null)
		{
			kingdom = sf.vars?.GetVar("tgt_kingdom").Get<Kingdom>();
		}
		if (kingdom == null)
		{
			return 0;
		}
		CrownAuthority crownAuthority = kingdom.GetCrownAuthority();
		int num = crownAuthority.GetValue();
		if (num == 0)
		{
			return 0;
		}
		if (num < 0)
		{
			int num2 = factor.field.Int(0, sf.vars);
			return num * num2 / crownAuthority.Min();
		}
		int num3 = factor.field.Int(1, sf.vars);
		return num * num3 / crownAuthority.Max();
	}

	public static int sf_king_class(SuccessAndFail sf, Factor.Def factor)
	{
		Kingdom kingdom = sf.vars?.GetVar("src_kingdom").Get<Kingdom>();
		if (kingdom == null)
		{
			return 0;
		}
		Character king = kingdom.GetKing();
		if (king == null)
		{
			return 0;
		}
		int result = factor.field.Int(0, sf.vars);
		string text = factor.field.String(1, sf.vars);
		if (string.IsNullOrEmpty(text))
		{
			return 0;
		}
		if (king.class_name != text)
		{
			return 0;
		}
		return result;
	}

	public static int sf_king_class_level(SuccessAndFail sf, Factor.Def factor)
	{
		Kingdom kingdom = sf.vars?.GetVar("src_kingdom").Get<Kingdom>();
		if (kingdom == null)
		{
			return 0;
		}
		Character king = kingdom.GetKing();
		if (king == null)
		{
			return 0;
		}
		float num = factor.field.Float(sf.vars, 1f);
		return (int)((float)king.GetClassLevel() * num);
	}

	public static int sf_king_skills(SuccessAndFail sf, Factor.Def factor)
	{
		Kingdom kingdom = sf.vars?.GetVar("src_kingdom").Get<Kingdom>();
		if (kingdom == null)
		{
			return 0;
		}
		Character king = kingdom.GetKing();
		if (king == null)
		{
			return 0;
		}
		int num = factor.field.NumValues();
		int num2 = factor.field.GetInt("skill_value");
		if (num == 0)
		{
			return king.GetSkillsCount() * num2;
		}
		int num3 = 0;
		for (int i = 0; i < num; i++)
		{
			string skill_name = factor.field.String(i);
			if (king.GetSkill(skill_name) != null)
			{
				num3 += num2;
			}
		}
		return num3;
	}

	public static int sf_king_spy_level(SuccessAndFail sf, Factor.Def factor)
	{
		Kingdom kingdom = factor.field.GetValue("kingdom").Get<Kingdom>();
		if (kingdom == null)
		{
			kingdom = sf.vars?.GetVar("src_kingdom").Get<Kingdom>();
		}
		Character character = kingdom?.GetKing();
		if (character == null)
		{
			return 0;
		}
		int classLevel = character.GetClassLevel("Spy");
		int idx = (character.IsSpy() ? 1 : 0);
		float num = factor.field.Float(idx, sf.vars, 1f);
		return (int)Math.Round((float)classLevel * num);
	}

	public static float king_spy_level(Kingdom k, float mul_if_not_spy, float mul_if_spy)
	{
		Character character = k?.GetKing();
		if (character == null)
		{
			return 0f;
		}
		float num = character.GetClassLevel("Spy");
		if (character.IsSpy())
		{
			return num * mul_if_spy;
		}
		return num * mul_if_not_spy;
	}

	public static int sf_target_king_spy_level(SuccessAndFail sf, Factor.Def factor)
	{
		Kingdom kingdom = sf.vars?.GetVar("mission_kingdom").Get<Kingdom>();
		if (kingdom == null)
		{
			kingdom = sf.vars?.GetVar("tgt_kingdom").Get<Kingdom>();
		}
		float mul_if_not_spy = factor.field.Float(0, sf.vars, 1f);
		float mul_if_spy = factor.field.Float(1, sf.vars, 1f);
		return (int)Math.Round(king_spy_level(kingdom, mul_if_not_spy, mul_if_spy));
	}

	private static int best_spy_class_level(Kingdom k)
	{
		List<Character> list = k?.court;
		if (list == null)
		{
			return 0;
		}
		int num = 0;
		for (int i = 0; i < list.Count; i++)
		{
			Character character = list[i];
			if (character != null && character.IsSpy())
			{
				int classLevel = character.GetClassLevel();
				if (classLevel > num)
				{
					num = classLevel;
				}
			}
		}
		return num;
	}

	public static int sf_target_best_spy_class_level(SuccessAndFail sf, Factor.Def factor)
	{
		Kingdom kingdom = sf.vars?.GetVar("mission_kingdom").Get<Kingdom>();
		if (kingdom == null)
		{
			kingdom = sf.vars?.GetVar("tgt_kingdom").Get<Kingdom>();
		}
		int num = best_spy_class_level(kingdom);
		if (num == 0)
		{
			return 0;
		}
		float num2 = factor.field.Float(0, sf.vars, 1f);
		return (int)Math.Round((float)num * num2);
	}

	public static int sf_target_rebel_armies(SuccessAndFail sf, Factor.Def factor)
	{
		Kingdom kingdom = sf.vars?.GetVar("mission_kingdom").Get<Kingdom>();
		if (kingdom == null)
		{
			kingdom = sf.vars?.GetVar("tgt_kingdom").Get<Kingdom>();
		}
		if (kingdom == null)
		{
			return 0;
		}
		int rebelsCount = kingdom.GetRebelsCount();
		if (rebelsCount == 0)
		{
			return 0;
		}
		float num = factor.field.Float(sf.vars, 1f);
		return (int)Math.Round((float)rebelsCount * num);
	}

	public static int sf_our_influence(SuccessAndFail sf, Factor.Def factor)
	{
		Kingdom kingdom = sf.vars?.GetVar("src_kingdom").Get<Kingdom>();
		Kingdom kingdom2 = sf.vars?.GetVar("mission_kingdom").Get<Kingdom>();
		if (kingdom2 == null)
		{
			kingdom2 = sf.vars?.GetVar("tgt_kingdom").Get<Kingdom>();
		}
		if (kingdom == null || kingdom2 == null || kingdom == kingdom2)
		{
			return 0;
		}
		float num = kingdom.GetInfluenceIn(kingdom2);
		float num2 = factor.field.Float(0, sf.vars);
		if (num2 > 0f && num > num2)
		{
			num = num2;
		}
		float num3 = factor.field.Float(1, sf.vars, 1f);
		return (int)Math.Round(num * num3);
	}

	public static int sf_our_princess_in_tgt_kingdom(SuccessAndFail sf, Factor.Def factor)
	{
		Kingdom kingdom = sf.vars?.GetVar("src_kingdom").Get<Kingdom>();
		Kingdom kingdom2 = sf.vars?.GetVar("mission_kingdom").Get<Kingdom>();
		if (kingdom2 == null)
		{
			kingdom2 = sf.vars?.GetVar("tgt_kingdom").Get<Kingdom>();
		}
		if (kingdom?.marriages == null || kingdom2?.marriages == null || kingdom == kingdom2)
		{
			return 0;
		}
		for (int i = 0; i < kingdom2.marriages.Count; i++)
		{
			Marriage marriage = kingdom2.marriages[i];
			if (!marriage.wife.IsQueen() && marriage.wife.original_kingdom_id == kingdom.id)
			{
				return factor.field.Int(sf.vars);
			}
		}
		return 0;
	}

	public static int sf_our_queen_in_tgt_kingdom(SuccessAndFail sf, Factor.Def factor)
	{
		Kingdom kingdom = sf.vars?.GetVar("src_kingdom").Get<Kingdom>();
		Kingdom kingdom2 = sf.vars?.GetVar("mission_kingdom").Get<Kingdom>();
		if (kingdom2 == null)
		{
			kingdom2 = sf.vars?.GetVar("tgt_kingdom").Get<Kingdom>();
		}
		if (kingdom == null || kingdom2 == null || kingdom == kingdom2)
		{
			return 0;
		}
		Character queen = kingdom2.GetQueen();
		if (queen != null && queen.original_kingdom_id == kingdom.id)
		{
			return factor.field.Int(sf.vars);
		}
		return 0;
	}

	public static int sf_their_princess_in_our_kingdom(SuccessAndFail sf, Factor.Def factor)
	{
		Kingdom kingdom = sf.vars?.GetVar("src_kingdom").Get<Kingdom>();
		Kingdom kingdom2 = sf.vars?.GetVar("mission_kingdom").Get<Kingdom>();
		if (kingdom2 == null)
		{
			kingdom2 = sf.vars?.GetVar("tgt_kingdom").Get<Kingdom>();
		}
		if (kingdom?.marriages == null || kingdom2?.marriages == null || kingdom == kingdom2)
		{
			return 0;
		}
		for (int i = 0; i < kingdom.marriages.Count; i++)
		{
			Marriage marriage = kingdom.marriages[i];
			if (!marriage.wife.IsQueen() && marriage.wife.original_kingdom_id == kingdom2.id)
			{
				return factor.field.Int(sf.vars);
			}
		}
		return 0;
	}

	public static int sf_their_queen_in_our_kingdom(SuccessAndFail sf, Factor.Def factor)
	{
		Kingdom kingdom = sf.vars?.GetVar("src_kingdom").Get<Kingdom>();
		Kingdom kingdom2 = sf.vars?.GetVar("mission_kingdom").Get<Kingdom>();
		if (kingdom2 == null)
		{
			kingdom2 = sf.vars?.GetVar("tgt_kingdom").Get<Kingdom>();
		}
		if (kingdom == null || kingdom2 == null || kingdom == kingdom2)
		{
			return 0;
		}
		Character queen = kingdom.GetQueen();
		if (queen != null && queen.original_kingdom_id == kingdom2.id)
		{
			return factor.field.Int(sf.vars);
		}
		return 0;
	}

	public static int sf_local_rebellion_risk(SuccessAndFail sf, Factor.Def factor)
	{
		Realm realm = sf.vars?.GetVar("owner").Get<Realm>();
		if (realm == null)
		{
			return 0;
		}
		return (int)Math.Round((0f - realm.rebellionRisk.value) * factor.field.Float(sf.vars));
	}

	public static int sf_kingdom_rebellion_risk(SuccessAndFail sf, Factor.Def factor)
	{
		Kingdom kingdom = sf.vars?.GetVar("src_kingdom").Get<Kingdom>();
		if (kingdom == null)
		{
			return 0;
		}
		return (int)Math.Round(kingdom.GetAvgRebellionRiskTotal() * factor.field.Float(sf.vars));
	}

	public static int sf_population_majority(SuccessAndFail sf, Factor.Def factor)
	{
		Realm realm = sf.vars?.GetVar("owner").Get<Realm>();
		if (realm == null)
		{
			return 0;
		}
		if (realm.pop_majority.kingdom != realm.GetKingdom())
		{
			return 0;
		}
		return (int)realm.game.Map(realm.pop_majority.strength, 0f, 100f, factor.field.Int(0, sf.vars), factor.field.Int(1, sf.vars));
	}

	public static int sf_we_hold_Rome(SuccessAndFail sf, Factor.Def factor)
	{
		Kingdom kingdom = sf.vars?.GetVar("src_kingdom").Get<Kingdom>();
		if (kingdom == null)
		{
			return 0;
		}
		if (!kingdom.realms.Contains(kingdom.game.religions.catholic.hq_realm))
		{
			return 0;
		}
		return factor.field.Int(sf.vars);
	}

	public static int sf_we_hold_Constantinople(SuccessAndFail sf, Factor.Def factor)
	{
		Kingdom kingdom = sf.vars?.GetVar("src_kingdom").Get<Kingdom>();
		if (kingdom == null)
		{
			return 0;
		}
		if (!kingdom.realms.Contains(kingdom.game.religions.orthodox.hq_realm))
		{
			return 0;
		}
		return factor.field.Int(sf.vars);
	}
}
