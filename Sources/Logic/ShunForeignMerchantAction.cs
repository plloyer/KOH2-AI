using System.Collections.Generic;

namespace Logic;

public class ShunForeignMerchantAction : MerchantOpportunity
{
	public ShunForeignMerchantAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new ShunForeignMerchantAction(owner as Character, def);
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
		if (character.mission_kingdom.is_player)
		{
			return "mission_kingdom_is_a_player";
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
		if (!character.IsMerchant())
		{
			return false;
		}
		if (character.IsOwnStance(base.own_character))
		{
			return false;
		}
		if (character.mission_kingdom == null || character.mission_kingdom != base.own_character.mission_kingdom)
		{
			return false;
		}
		if (!base.own_character.IsEnemy(character) && own_kingdom.GetRelationship(character.GetKingdom()) > def.field.GetFloat("relationship_treshold"))
		{
			return false;
		}
		if (character.trade_level > base.own_character.trade_level)
		{
			return false;
		}
		return true;
	}

	public override List<Object> GetPossibleTargets()
	{
		List<Object> targets = null;
		Kingdom mission_kingdom = base.own_character.mission_kingdom;
		for (int i = 0; i < mission_kingdom.foreigners.Count; i++)
		{
			Character character = mission_kingdom.foreigners[i];
			AddTarget(ref targets, character);
		}
		return targets;
	}

	public override void Run()
	{
		Character obj = base.target as Character;
		Kingdom kingdom = obj.GetKingdom();
		obj.Recall();
		base.own_character.mission_kingdom.AddRelationModifier(kingdom, "rel_merchant_shunned", null);
		base.own_character.mission_kingdom.UnsetStance(kingdom, RelationUtils.Stance.Trade);
		obj.NotifyListeners("merchant_shunned", base.own_character.mission_kingdom);
		base.Run();
	}
}
