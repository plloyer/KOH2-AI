using System.Collections.Generic;

namespace Logic;

public class FormVassalAction : Action
{
	public FormVassalAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new FormVassalAction(owner as Character, def);
	}

	public override string Validate(bool quick_out = false)
	{
		Character character = base.own_character;
		if (character == null)
		{
			return "not_a_character";
		}
		if (!character.IsAlive())
		{
			return "dead";
		}
		if (own_kingdom == null)
		{
			return "no_kingodm";
		}
		if (character.sex != Character.Sex.Male)
		{
			return "not_a_male";
		}
		if (!own_kingdom.court.Contains(character))
		{
			return "not_in_court";
		}
		if (!character.IsKing())
		{
			return "not_king";
		}
		if (character.IsPope())
		{
			return "is_pope";
		}
		if (character?.GetArmy() != null)
		{
			return "leading_army";
		}
		if (!own_kingdom.is_player)
		{
			return "is_ai";
		}
		return "ok";
	}

	public override List<Value>[] GetPossibleArgs()
	{
		List<Value> list = new List<Value>(own_kingdom.realms.Count);
		List<Value> list2 = new List<Value>(own_kingdom.realms.Count);
		if (args == null)
		{
			if (own_kingdom.realms.Count <= 1)
			{
				return null;
			}
			for (int i = 0; i < own_kingdom.realms.Count; i++)
			{
				Realm realm = own_kingdom.realms[i];
				bool flag = false;
				if (realm.rebellions.Count == 0 && !realm.IsDisorder() && own_kingdom != realm.pop_majority.kingdom && realm != base.game.religions.catholic.hq_realm)
				{
					Battle battle = realm.castle.battle;
					if (battle == null || battle.type != Battle.Type.Siege)
					{
						Battle battle2 = realm.castle.battle;
						if (battle2 == null || battle2.type != Battle.Type.Assault)
						{
							Battle battle3 = realm.castle.battle;
							if (battle3 == null || battle3.type != Battle.Type.BreakSiege)
							{
								flag = true;
							}
						}
					}
				}
				bool flag2 = false;
				Kingdom kingdom = realm.pop_majority.kingdom;
				Kingdom kingdom2 = base.game.GetKingdom(realm.init_kingdom_id);
				Kingdom kingdom3 = base.game.GetKingdom(realm.name);
				if (kingdom.IsDefeated() || kingdom2.IsDefeated() || kingdom3.IsDefeated())
				{
					flag2 = true;
				}
				if (flag && flag2)
				{
					list.Add(realm);
				}
			}
			list2.Add(own_kingdom);
		}
		else if (args.Count == 1)
		{
			Realm realm2 = args[0].Get<Realm>();
			list.Add(realm2);
			Kingdom kingdom4 = realm2.pop_majority.kingdom;
			Kingdom kingdom5 = base.game.GetKingdom(realm2.init_kingdom_id);
			Kingdom kingdom6 = base.game.GetKingdom(realm2.name);
			if (kingdom4.IsDefeated() && !list2.Contains(kingdom4))
			{
				list2.Add(kingdom4);
			}
			if (kingdom5.IsDefeated() && !list2.Contains(kingdom5))
			{
				list2.Add(kingdom5);
			}
			if (kingdom6.IsDefeated() && !list2.Contains(kingdom6))
			{
				list2.Add(kingdom6);
			}
		}
		if (list.Count > 0)
		{
			return new List<Value>[2] { list, list2 };
		}
		return null;
	}

	public override List<Vars> GetPossibleArgVars(List<Value> possibleTargets = null, int arg_type = 0)
	{
		if (possibleTargets == null)
		{
			return null;
		}
		List<Vars> list = new List<Vars>(possibleTargets.Count);
		if (arg_type == 0)
		{
			foreach (Value possibleTarget in possibleTargets)
			{
				Vars vars = new Vars(possibleTarget);
				vars.Set("argument_type", "Realm");
				vars.Set("owner", base.own_character);
				vars.Set("Realm", possibleTarget.Get<Realm>().name);
				list.Add(vars);
			}
		}
		if (arg_type == 1)
		{
			foreach (Value possibleTarget2 in possibleTargets)
			{
				Vars vars2 = new Vars(possibleTarget2);
				vars2.Set("argument_type", "Kingdom");
				vars2.Set("owner", base.own_character);
				vars2.Set("Kingdom", possibleTarget2.Get<Kingdom>().Name);
				list.Add(vars2);
			}
		}
		return list;
	}

	public override bool ValidateArg(Value value, int def_type)
	{
		if (!NeedsArgs())
		{
			return true;
		}
		if (value == Value.Unknown || value == Value.Null)
		{
			return false;
		}
		if (def_type >= def.arg_types.Count)
		{
			return false;
		}
		Object obj = value.Get<Object>();
		switch (def.arg_types[def_type])
		{
		case "Realm":
			if (!(obj is Realm realm) || realm.GetKingdom().IsDefeated())
			{
				return false;
			}
			return true;
		case "realm":
			if (!(obj is Realm))
			{
				return false;
			}
			return true;
		case "Kingdom":
			if (!(obj is Kingdom))
			{
				return false;
			}
			return true;
		default:
			return false;
		}
	}

	public override string CheckEnabled()
	{
		if (base.own_character.prison_kingdom != null)
		{
			return "imprisoned";
		}
		return "ok";
	}

	public override void Run()
	{
		if (args == null || args.Count < 2)
		{
			return;
		}
		Realm item = args[0].Get<Realm>();
		Kingdom kingdom = args[1].Get<Kingdom>();
		kingdom?.DeclareIndependenceOrJoin(new List<Realm> { item }, null, null, null, null, go_to_war: false);
		own_kingdom.AddVassalState(kingdom);
		KingdomAndKingdomRelation.Modify("rel_form_vassal", kingdom, own_kingdom, null);
		for (int i = 0; i < base.game.kingdoms.Count; i++)
		{
			Kingdom kingdom2 = base.game.kingdoms[i];
			if (kingdom2 != null && !kingdom2.IsDefeated() && kingdom2 != kingdom && kingdom2 != own_kingdom)
			{
				KingdomAndKingdomRelation.AddRelationship(kingdom, kingdom2, 0f, 100f + KingdomAndKingdomRelation.GetRelationship(own_kingdom, kingdom2));
			}
		}
	}
}
