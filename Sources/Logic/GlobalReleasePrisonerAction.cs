using System.Collections.Generic;

namespace Logic;

public class GlobalReleasePrisonerAction : Action
{
	private List<Kingdom> involvedKingdoms;

	private Dictionary<Kingdom, List<Character>> prisonersPerKingdom;

	public GlobalReleasePrisonerAction(Kingdom owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new GlobalReleasePrisonerAction(owner as Kingdom, def);
	}

	public override string Validate(bool quick_out = false)
	{
		if (own_kingdom.prisoners.Count == 0)
		{
			return "no prisoners";
		}
		for (int num = own_kingdom.prisoners.Count - 1; num >= 0; num--)
		{
			if (own_kingdom.prisoners[num].IsValid())
			{
				return "ok";
			}
		}
		return "no valid prisoners";
	}

	public override void CreateOutcomeVars()
	{
		base.CreateOutcomeVars();
		if (prisonersPerKingdom == null)
		{
			prisonersPerKingdom = new Dictionary<Kingdom, List<Character>>();
		}
		if (involvedKingdoms == null)
		{
			involvedKingdoms = new List<Kingdom>();
		}
		for (int num = own_kingdom.prisoners.Count - 1; num >= 0; num--)
		{
			Character character = own_kingdom.prisoners[num];
			if (character != null && character.IsValid())
			{
				Kingdom kingdom = character.GetKingdom();
				if (kingdom.IsRegular() && kingdom != own_kingdom)
				{
					List<Character> value = null;
					if (!prisonersPerKingdom.TryGetValue(kingdom, out value))
					{
						involvedKingdoms.Add(kingdom);
						value = new List<Character>();
						prisonersPerKingdom.Add(kingdom, value);
					}
					value.Add(character);
				}
			}
		}
		outcome_vars.Set("involved_kingdoms", involvedKingdoms);
	}

	public override void Run()
	{
		using (Game.Profile("Release all prisoners"))
		{
			for (int num = own_kingdom.prisoners.Count - 1; num >= 0; num--)
			{
				Character character = own_kingdom.prisoners[num];
				if (character != null && character.IsValid())
				{
					character.OnPrisonActionAnalytics("freed");
					character.Imprison(null);
					Kingdom kingdom = character.GetKingdom();
					if (kingdom != own_kingdom)
					{
						Vars vars = new Vars();
						vars.SetVar("own_kingdom", own_kingdom);
						vars.GetVar("target_kingdom", kingdom);
						GetVar("prisoner", character);
						own_kingdom.AddRelationModifier(kingdom, "rel_released_prisoner", null, own_kingdom.royal_dungeon.GetVar("relationship_release", vars));
					}
				}
			}
			if (prisonersPerKingdom != null)
			{
				foreach (KeyValuePair<Kingdom, List<Character>> item in prisonersPerKingdom)
				{
					Kingdom key = item.Key;
					List<Character> value = item.Value;
					Vars vars2 = new Vars();
					vars2.Set("kingdom", own_kingdom);
					vars2.Set("prisoners", value);
					key.FireEvent("prisoners_released", value, key.id);
				}
			}
		}
		base.Run();
	}
}
