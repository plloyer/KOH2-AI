using System.Collections.Generic;

namespace Logic;

public class AskForRansomAction : PrisonAction
{
	public AskForRansomAction(Kingdom owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new AskForRansomAction(owner as Kingdom, def);
	}

	public override string Validate(bool quick_out = false)
	{
		if (!(base.target is Character character))
		{
			return "no_target";
		}
		if (character.GetKingdom() == own_kingdom)
		{
			return "own_kingdom";
		}
		if (character.GetKingdom().type != Kingdom.Type.Regular)
		{
			return "non_regular_faction";
		}
		if (character.IsMercenary())
		{
			return "non_regular_faction";
		}
		if (Timer.Find(character, "ransom_cooldown") != null)
		{
			return "_cooldown";
		}
		if (Offer.GetCachedOffer("AskForPrisonerRansom", own_kingdom, target_kingdom, null, 0, character, 0)?.Validate() == "pending_offer_exists")
		{
			return "_pending_offer_exists";
		}
		return base.Validate(quick_out);
	}

	public override List<OutcomeDef> DecideOutcomes()
	{
		if (!(base.target is Character character))
		{
			return null;
		}
		int num = (int)character.RansomPrice().Get(ResourceType.Gold);
		if (target_kingdom.resources.Get(ResourceType.Gold) < (float)num)
		{
			ForceOutcomes("no_money");
			return forced_outcomes;
		}
		return base.DecideOutcomes();
	}

	public override void Run()
	{
		if (base.target is Character character)
		{
			int ransom_default = (int)character.RansomPrice().Get(ResourceType.Gold);
			new AskForPrisonerRansom(own_kingdom, target_kingdom, character, ransom_default).Send();
			base.Run();
		}
	}

	public override bool ApplyOutcome(OutcomeDef outcome)
	{
		if (outcome.key == "fail")
		{
			Character character = base.target as Character;
			if (character == null)
			{
				character = base.owner as Character;
				if (character == null)
				{
					return false;
				}
			}
			Timer.Start(character, "ransom_cooldown", character.prison_kingdom.royal_dungeon.def.ransom_cooldown_time);
			character.FireEvent("force_refresh_actions", null);
			return true;
		}
		return base.ApplyOutcome(outcome);
	}

	public override Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		if (!(key == "success_chance_text"))
		{
			if (key == "ransom_agreement_chance")
			{
				if (!(base.target is Character character))
				{
					return Value.Unknown;
				}
				float num = (target_kingdom.IsEnemy(own_kingdom) ? ((!character.IsKingOrPrince()) ? own_kingdom.royal_dungeon.def.ransom_war_not_royal : own_kingdom.royal_dungeon.def.ransom_war_royal) : ((!character.IsKingOrPrince()) ? own_kingdom.royal_dungeon.def.ransom_no_war_not_royal : own_kingdom.royal_dungeon.def.ransom_no_war_royal));
				return num;
			}
			return base.GetVar(key, vars, as_value);
		}
		if (target_kingdom != null && !target_kingdom.is_player)
		{
			return SuccessChanceValue(non_trivial_only: true, vars);
		}
		return Value.Null;
	}
}
