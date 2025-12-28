using System.Collections.Generic;

namespace Logic;

public class Pagan : Religion
{
	public Pagan(Game game, Def def)
		: base(game, def)
	{
	}

	public override void Init(bool new_game)
	{
		base.Init(new_game);
	}

	public override void AddModifiers(Kingdom k)
	{
		base.AddModifiers(k);
		if (k.pagan_beliefs == null)
		{
			return;
		}
		Stats stats = k.stats;
		for (int i = 0; i < k.pagan_beliefs.Count; i++)
		{
			PaganBelief paganBelief = k.pagan_beliefs[i];
			for (int j = 0; j < paganBelief.mods.Count; j++)
			{
				StatModifier.Def def = paganBelief.mods[j];
				StatModifier statModifier = new StatModifier(def);
				stats.AddModifier(def.stat_name, statModifier);
				if (k.religion_mods == null)
				{
					k.religion_mods = new List<StatModifier>(base.def.mods.Count);
				}
				k.religion_mods.Add(statModifier);
			}
		}
	}
}
