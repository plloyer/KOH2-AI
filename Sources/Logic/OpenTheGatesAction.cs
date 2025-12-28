using System.Collections.Generic;

namespace Logic;

public class OpenTheGatesAction : SpyPlot
{
	public OpenTheGatesAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new OpenTheGatesAction(owner as Character, def);
	}

	public override string Validate(bool quick_out = false)
	{
		if (base.own_character.mission_kingdom == null)
		{
			return "not_on_mission";
		}
		if (!base.own_character.mission_kingdom.IsEnemy(own_kingdom))
		{
			return "not_enemies";
		}
		return base.Validate(quick_out);
	}

	public override bool ValidateTarget(Object target)
	{
		Realm realm = target as Realm;
		if (realm?.castle?.battle == null)
		{
			return false;
		}
		if (realm == null || realm.castle?.battle.type != Battle.Type.Siege)
		{
			return false;
		}
		if (realm.castle.battle.assault_gate_action_succeeded)
		{
			return false;
		}
		bool flag = false;
		if (realm.castle.battle?.attackers != null)
		{
			for (int i = 0; i < realm.castle.battle.attackers.Count; i++)
			{
				if (realm.castle.battle.attackers[i].IsOwnStance(own_kingdom) || realm.castle.battle.attackers[i].IsAllyOrTeammate(own_kingdom))
				{
					flag = true;
					break;
				}
			}
		}
		if (!flag)
		{
			return false;
		}
		return true;
	}

	public override Kingdom CalcTargetKingdom(Object target)
	{
		Realm realm = target as Realm;
		if (realm?.castle?.battle?.attacker == null)
		{
			return null;
		}
		return realm.castle.battle.attacker.GetKingdom();
	}

	public override List<Object> GetPossibleTargets()
	{
		List<Object> targets = null;
		Kingdom mission_kingdom = base.own_character.mission_kingdom;
		if (mission_kingdom == null)
		{
			return null;
		}
		for (int i = 0; i < mission_kingdom.realms.Count; i++)
		{
			AddTarget(ref targets, mission_kingdom.realms[i]);
		}
		return targets;
	}

	public override void Run()
	{
		((base.target as Realm)?.castle?.battle)?.AssaultGate();
		base.Run();
	}
}
