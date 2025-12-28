using System.Collections.Generic;

namespace Logic;

public class KingdomRankingCategories : Component, IVars
{
	public List<KingdomRankingCategory> categories = new List<KingdomRankingCategory>();

	public Kingdom kingdom => obj as Kingdom;

	public KingdomRankingCategories(Kingdom kingdom)
		: base(kingdom)
	{
		Defs.Registry registry = base.game.defs.Get(typeof(KingdomRankingCategory.Def));
		if (registry == null)
		{
			return;
		}
		foreach (KeyValuePair<string, Def> def in registry.defs)
		{
			KingdomRankingCategory item = new KingdomRankingCategory(def.Value as KingdomRankingCategory.Def, kingdom);
			categories.Add(item);
		}
	}

	public KingdomRankingCategory Find(KingdomRankingCategory.Def def)
	{
		for (int i = 0; i < categories.Count; i++)
		{
			KingdomRankingCategory kingdomRankingCategory = categories[i];
			if (kingdomRankingCategory.def == def)
			{
				return kingdomRankingCategory;
			}
		}
		return null;
	}

	public KingdomRankingCategory FindByName(string name)
	{
		for (int i = 0; i < categories.Count; i++)
		{
			KingdomRankingCategory kingdomRankingCategory = categories[i];
			if (kingdomRankingCategory.def.Name == name)
			{
				return kingdomRankingCategory;
			}
		}
		return null;
	}

	public KingdomRankingCategory FindById(string def_id)
	{
		for (int i = 0; i < categories.Count; i++)
		{
			KingdomRankingCategory kingdomRankingCategory = categories[i];
			if (kingdomRankingCategory.def.id == def_id)
			{
				return kingdomRankingCategory;
			}
		}
		return null;
	}

	public int CalcScore()
	{
		int num = 0;
		for (int i = 0; i < categories.Count; i++)
		{
			int score = categories[i].GetScore();
			num += score;
		}
		return num;
	}

	public override void OnDestroy()
	{
		for (int i = 0; i < categories.Count; i++)
		{
			categories[i].OnDestroy();
		}
		base.OnDestroy();
	}

	public Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		KingdomRankingCategory kingdomRankingCategory = FindByName(key);
		if (kingdomRankingCategory != null)
		{
			return kingdomRankingCategory;
		}
		return Value.Unknown;
	}
}
