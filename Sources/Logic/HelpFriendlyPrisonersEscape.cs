using System.Collections.Generic;

namespace Logic;

public class HelpFriendlyPrisonersEscape : SpyPlot
{
	private List<Character> prisoners = new List<Character>();

	public HelpFriendlyPrisonersEscape(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new HelpFriendlyPrisonersEscape(owner as Character, def);
	}

	public bool ValidatePrisoner(Character c)
	{
		Kingdom k = c.GetKingdom();
		if (k == null || k == own_kingdom)
		{
			return false;
		}
		if (own_kingdom.IsFriend(k))
		{
			return true;
		}
		if (base.game?.teams?.Get(own_kingdom)?.players?.Find((Game.Player p) => p.kingdom_id == k.id) != null)
		{
			return true;
		}
		if (own_kingdom.GetRelationship(k) >= def.field.GetFloat("min_relationship"))
		{
			return true;
		}
		if (own_kingdom.GetRoyalMarriage(k))
		{
			return true;
		}
		return false;
	}

	public override string Validate(bool quick_out = false)
	{
		if (base.own_character == null || !base.own_character.IsSpy())
		{
			return "no_spy";
		}
		Kingdom mission_kingdom = base.own_character.mission_kingdom;
		if (mission_kingdom == null)
		{
			return "not_in_a_kingdom";
		}
		bool flag = false;
		for (int num = mission_kingdom.prisoners.Count - 1; num >= 0; num--)
		{
			Character character = mission_kingdom.prisoners[num];
			if (character != null && ValidatePrisoner(character))
			{
				flag = true;
				break;
			}
		}
		if (!flag)
		{
			return "_no_possible_targets";
		}
		return base.Validate(quick_out);
	}

	public override bool ApplyEarlyOutcome(OutcomeDef outcome)
	{
		string key = outcome.key;
		if (key == "success")
		{
			prisoners.Clear();
			if (base.own_character != null)
			{
				Kingdom mission_kingdom = base.own_character.mission_kingdom;
				if (mission_kingdom != null)
				{
					List<Kingdom> list = null;
					for (int num = mission_kingdom.prisoners.Count - 1; num >= 0; num--)
					{
						Character character = mission_kingdom.prisoners[num];
						if (character != null && ValidatePrisoner(character))
						{
							character.Imprison(null);
							prisoners.Add(character);
							if (list == null)
							{
								list = new List<Kingdom>();
							}
							if (!list.Contains(character.GetKingdom()))
							{
								list.Add(character.GetKingdom());
							}
						}
					}
					base.own_character.NotifyListeners("helped_friendly_prisoners_escape", mission_kingdom);
					if (list != null)
					{
						Vars vars = new Vars();
						vars.Set("prisoners", prisoners);
						vars.Set("owner", base.own_character);
						for (int i = 0; i < list.Count; i++)
						{
							list[i].FireEvent("friend_helped_our_prisoners_escape", vars, list[i].id);
						}
					}
				}
			}
		}
		return base.ApplyEarlyOutcome(outcome);
	}

	public override void CreateOutcomeVars()
	{
		base.CreateOutcomeVars();
		outcome_vars.Set("prisoner", (prisoners.Count == 1) ? prisoners[0] : null);
		outcome_vars.Set("prisoners", prisoners);
	}

	public override void Run()
	{
		prisoners.Clear();
		base.Run();
	}
}
