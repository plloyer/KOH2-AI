using System.Collections.Generic;

namespace Logic;

public class OfferRansomAction : Action
{
	public OfferRansomAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new OfferRansomAction(owner as Character, def);
	}

	public override Kingdom CalcTargetKingdom(Object target)
	{
		if (base.own_character?.prison_kingdom != null)
		{
			return base.own_character.prison_kingdom;
		}
		return base.CalcTargetKingdom(target);
	}

	public override string Validate(bool quick_out = false)
	{
		if (!(base.owner is Character character))
		{
			return "no_target";
		}
		if (character.prison_kingdom == null)
		{
			return "not_imprisoned";
		}
		if (character.GetKingdom() != own_kingdom)
		{
			return "not_my_kingdom";
		}
		if (character.IsRebel())
		{
			return "rebel";
		}
		if (character.IsMercenary())
		{
			return "non_regular_faction";
		}
		if (character.prison_kingdom == own_kingdom)
		{
			return "imprisoned_at_own_kingdom";
		}
		if (Timer.Find(character, "ransom_cooldown") != null)
		{
			return "cooldown";
		}
		int num = (int)character.RansomPrice().Get(ResourceType.Gold);
		if (character.GetKingdom().resources.Get(ResourceType.Gold) < (float)num)
		{
			return "_no_money";
		}
		if (character.prison_kingdom.GetOngoingOfferWith(own_kingdom) != null)
		{
			return "_pending_offer";
		}
		return "ok";
	}

	public override List<OutcomeDef> DecideOutcomes()
	{
		if (!(base.owner is Character character))
		{
			return null;
		}
		int num = (int)character.RansomPrice().Get(ResourceType.Gold);
		if (character.GetKingdom().resources.Get(ResourceType.Gold) < (float)num)
		{
			ForceOutcomes("no_money");
			return forced_outcomes;
		}
		return base.DecideOutcomes();
	}

	public override void Run()
	{
		if (base.owner is Character character)
		{
			int ransom_default = (int)character.RansomPrice().Get(ResourceType.Gold);
			new OfferRansomPrisoner(own_kingdom, character.prison_kingdom, character, ransom_default).Send();
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
		if (!(key == "ransom_price"))
		{
			if (key == "ransom_agreement_chance")
			{
				if (!(base.target is Character character))
				{
					return Value.Unknown;
				}
				Kingdom kingdom = character.GetKingdom();
				bool flag = character.class_name == "Spy";
				bool flag2 = kingdom.IsEnemy(own_kingdom);
				bool flag3 = character.IsKingOrPrince();
				bool flag4 = character.class_name == "Marshal";
				float num = ((flag || (flag2 && (flag3 || flag4))) ? kingdom.royal_dungeon.def.ransom_spy_or_war_and_royal_or_marshal : (flag2 ? kingdom.royal_dungeon.def.ransom_war_none : ((!flag) ? kingdom.royal_dungeon.def.ransom_no_war_no_spy : kingdom.royal_dungeon.def.ransom_no_war_spy)));
				return num;
			}
			return base.GetVar(key, vars, as_value);
		}
		if (!(base.target is Character character2))
		{
			return Value.Unknown;
		}
		return character2.RansomPrice().Get(ResourceType.Gold);
	}
}
