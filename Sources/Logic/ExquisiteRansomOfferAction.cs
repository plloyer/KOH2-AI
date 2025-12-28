using System.Collections.Generic;

namespace Logic;

public class ExquisiteRansomOfferAction : MerchantOpportunity
{
	public ExquisiteRansomOfferAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new ExquisiteRansomOfferAction(owner as Character, def);
	}

	public override string Validate(bool quick_out = false)
	{
		Character character = base.own_character;
		if (character == null)
		{
			return "not_a_character";
		}
		if (character.mission_kingdom == null)
		{
			return "not_in_a_mission_kingdom";
		}
		return base.Validate(quick_out);
	}

	public override bool NeedsTarget()
	{
		return true;
	}

	public override bool ValidateTarget(Object target)
	{
		if (!(target is Character character))
		{
			return false;
		}
		if (!character.IsPrisoner())
		{
			return false;
		}
		if (character.IsMercenary())
		{
			return false;
		}
		if (character.IsDead())
		{
			return false;
		}
		if (!character.IsOwnStance(base.own_character))
		{
			return false;
		}
		return true;
	}

	public override List<Object> GetPossibleTargets()
	{
		List<Object> targets = null;
		Kingdom mission_kingdom = base.own_character.mission_kingdom;
		for (int i = 0; i < mission_kingdom.royal_dungeon.prisoners.Count; i++)
		{
			Character character = mission_kingdom.royal_dungeon.prisoners[i];
			AddTarget(ref targets, character);
		}
		for (int j = 0; j < mission_kingdom.foreigners.Count; j++)
		{
			Character character2 = mission_kingdom.foreigners[j];
			if (character2 != null && character2.IsMerchant() && !character2.IsOwnStance(base.own_character))
			{
				Kingdom kingdom = character2.GetKingdom();
				for (int k = 0; k < kingdom.royal_dungeon.prisoners.Count; k++)
				{
					Character character3 = kingdom.royal_dungeon.prisoners[k];
					AddTarget(ref targets, character3);
				}
			}
		}
		return targets;
	}

	public override void Run()
	{
		Character character = base.target as Character;
		Kingdom kingdom = character?.prison_kingdom;
		if (kingdom != null)
		{
			Resource cost = GetCost(character);
			character.Imprison(null, recall: true, send_state: true, "ransomed");
			Vars vars = new Vars();
			vars.Set("kingdom", own_kingdom);
			vars.Set("character", character);
			vars.Set("cost", cost);
			kingdom.FireEvent("exquisite_ransom", vars);
			character.FireEvent("prisoner_ransomed", null);
			base.Run();
		}
	}

	public override Resource GetCost(Object target, IVars vars = null)
	{
		if (def.cost == null)
		{
			return null;
		}
		if (vars == null)
		{
			if (target == null)
			{
				vars = this;
			}
			else
			{
				Vars vars2 = new Vars(this);
				vars2.Set("target", target);
				vars = vars2;
			}
		}
		return Resource.Parse(def.cost, vars);
	}
}
