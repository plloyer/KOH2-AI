using System.Collections.Generic;

namespace Logic;

public class PuppetRebelAttackOtherKingdomAction : PuppetPlot
{
	public PuppetRebelAttackOtherKingdomAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new PuppetRebelAttackOtherKingdomAction(owner as Character, def);
	}

	public override string ValidatePuppet(Character puppet)
	{
		if (puppet == null)
		{
			return "no_puuppet";
		}
		if (puppet.GetArmy()?.rebel?.rebellion?.leader?.character != puppet)
		{
			return "not_a_leader";
		}
		return base.ValidatePuppet(puppet);
	}

	public override List<Value>[] GetPossibleArgs()
	{
		Character character = base.target as Character;
		Realm realm = character?.GetArmy()?.rebel?.rebellion?.GetOriginRealm();
		Kingdom kingdom = realm?.GetKingdom();
		if (realm == null)
		{
			return null;
		}
		float num = def.field.GetFloat("rebels_max_distance", null, 70f);
		character.GetKingdom();
		List<Value>[] array = new List<Value>[1]
		{
			new List<Value>()
		};
		for (int i = 0; i < base.game.kingdoms.Count; i++)
		{
			Kingdom kingdom2 = base.game.kingdoms[i];
			if (!kingdom2.IsDefeated() && kingdom2 != own_kingdom && kingdom2 != kingdom && (kingdom2.IsEnemy(own_kingdom) || !(kingdom2.GetRelationship(own_kingdom) >= 0f)) && !((float)base.game.KingdomAndRealmDistance(kingdom2.id, realm.id) > num))
			{
				AddArg(ref array[0], kingdom2, 0);
			}
		}
		if (array[0].Count == 0)
		{
			return null;
		}
		return array;
	}

	public override Kingdom CalcTargetKingdom(Object target)
	{
		if (args != null && args.Count > 0 && args[0].is_valid)
		{
			return args[0].Get<Kingdom>();
		}
		return base.CalcTargetKingdom(target);
	}

	public override void Run()
	{
		Character character = base.target as Character;
		Rebellion rebellion = character.GetArmy().rebel.rebellion;
		Realm originRealm = rebellion.GetOriginRealm();
		Kingdom kingdom = args[0].obj_val as Kingdom;
		Realm newOrigin = null;
		float num = float.MaxValue;
		for (int i = 0; i < kingdom.realms.Count; i++)
		{
			Castle castle = kingdom.realms[i].castle;
			float num2 = castle.position.Dist(originRealm.castle.position);
			if (num2 < num)
			{
				num = num2;
				newOrigin = castle.GetRealm();
			}
		}
		rebellion.MoveOriginRealm(newOrigin);
		base.own_character.DelPuppet(character);
		base.Run();
	}

	public override Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		if (key == "attacked_kingdom")
		{
			if (args != null && args.Count >= 1)
			{
				return args[0].obj_val as Kingdom;
			}
			return Value.Unknown;
		}
		return base.GetVar(key, vars, as_value);
	}
}
