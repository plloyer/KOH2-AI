using System.Collections.Generic;

namespace Logic;

public class Opinions : Component, IVars
{
	public class Def : Logic.Def
	{
		public float passive_increase_min_time = 300f;

		public float passive_increase_max_time = 600f;

		public float passive_increase_amount = 2f;

		public override bool Load(Game game)
		{
			DT.Field field = dt_def.field;
			passive_increase_min_time = field.GetFloat("passive_increase_min_time", null, passive_increase_min_time);
			passive_increase_max_time = field.GetFloat("passive_increase_max_time", null, passive_increase_max_time);
			passive_increase_amount = field.GetFloat("passive_increase_amount", null, passive_increase_amount);
			return true;
		}
	}

	public Def def;

	public List<Opinion> opinions = new List<Opinion>();

	public Dictionary<string, Opinion> by_name = new Dictionary<string, Opinion>();

	public Kingdom kingdom => obj as Kingdom;

	public Opinions(Kingdom k)
		: base(k)
	{
		this.def = base.game.defs.Find<Def>("Opinions");
		if (!k.stats.def.loaded)
		{
			return;
		}
		List<Opinion.Def> all = Opinion.Def.all;
		if (all != null)
		{
			for (int i = 0; i < all.Count; i++)
			{
				Opinion.Def def = all[i];
				Opinion opinion = new Opinion(def, this);
				opinions.Add(opinion);
				by_name.Add(def.id, opinion);
			}
		}
	}

	public override void OnStart()
	{
		UpdateAfter(GetNextPassiveIncreaseTime());
	}

	public float GetNextPassiveIncreaseTime()
	{
		return base.game.Random(def.passive_increase_min_time, def.passive_increase_max_time);
	}

	public Opinion Find(string name)
	{
		if (string.IsNullOrEmpty(name))
		{
			return null;
		}
		if (!by_name.TryGetValue(name, out var value))
		{
			return null;
		}
		return value;
	}

	public Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		Opinion opinion = Find(key);
		if (opinion != null)
		{
			if (as_value)
			{
				return opinion.value;
			}
			return opinion;
		}
		return Value.Unknown;
	}

	public void TryIncreaseOpinion(Opinion opinion)
	{
		Vars vars = new Vars();
		vars.Set("opinion_value", opinion.value);
		vars.Set("opinion_id", opinion.def.id);
		vars.Set("max_value", opinion.def.max_value);
		if (kingdom.improveOpinionsDiplomat != null)
		{
			vars.Set("diplomat", kingdom.improveOpinionsDiplomat);
		}
		float num = def.field.GetFloat("passive_increase_chance", vars) + (kingdom.improveOpinionsDiplomat?.GetStat(Stats.cs_diplomat_improve_opinions_CTS) ?? 0f);
		if ((float)base.game.Random(0, 100) < num)
		{
			vars.Set("opinion_amount", def.passive_increase_amount);
			if (kingdom.improveOpinionsDiplomat != null)
			{
				kingdom.NotifyListeners("passive_increase_opinion_diplomat", vars);
			}
			else
			{
				kingdom.NotifyListeners("passive_increase_opinion", vars);
			}
		}
	}

	public void TryIncreaseRandomOpinion()
	{
		int num = base.game.Random(0, opinions.Count);
		int num2 = num + opinions.Count;
		for (int i = num; i < num2; i++)
		{
			Opinion opinion = opinions[i % opinions.Count];
			if (opinion.value < opinion.def.max_value)
			{
				TryIncreaseOpinion(opinion);
				break;
			}
		}
	}

	public override void OnUpdate()
	{
		if (kingdom.IsAuthority())
		{
			TryIncreaseRandomOpinion();
		}
		UpdateAfter(GetNextPassiveIncreaseTime());
	}

	public override string ToString()
	{
		return $"Opinions of {kingdom}";
	}

	public string Dump()
	{
		string text = ToString();
		for (int i = 0; i < opinions.Count; i++)
		{
			Opinion opinion = opinions[i];
			text += $"\n  {opinion.def.id}: {opinion.value}";
		}
		return text;
	}
}
