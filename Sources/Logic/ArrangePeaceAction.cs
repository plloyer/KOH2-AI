using System.Collections.Generic;

namespace Logic;

public class ArrangePeaceAction : CharacterOpportunity
{
	public ArrangePeaceAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new ArrangePeaceAction(owner as Character, def);
	}

	public override string Validate(bool quick_out = false)
	{
		if (own_kingdom.is_pagan)
		{
			return "is_pagan";
		}
		return base.Validate(quick_out);
	}

	public override bool ValidateTarget(Object target)
	{
		if (!(target is Kingdom kingdom))
		{
			return false;
		}
		if (kingdom.religion != own_kingdom.religion)
		{
			return false;
		}
		if (kingdom.is_player)
		{
			return false;
		}
		if (kingdom.wars.Count == 0)
		{
			return false;
		}
		return base.ValidateTarget(target);
	}

	public override bool ValidateArg(Value value, int def_type)
	{
		if (!(value.obj_val is War war))
		{
			return false;
		}
		if (war.IsConcluded() || war.GetLeader(0).is_player || war.GetLeader(1).is_player)
		{
			return false;
		}
		return base.ValidateArg(value, def_type);
	}

	public override List<Value>[] GetPossibleArgs()
	{
		Kingdom kingdom = base.target as Kingdom;
		List<Value>[] array = new List<Value>[1]
		{
			new List<Value>()
		};
		for (int i = 0; i < kingdom.wars.Count; i++)
		{
			War war = kingdom.wars[i];
			AddArg(ref array[0], war, 0);
		}
		if (array[0].Count == 0)
		{
			return null;
		}
		return array;
	}

	public override List<Object> GetPossibleTargets()
	{
		List<Object> targets = null;
		foreach (Kingdom neighbor in own_kingdom.neighbors)
		{
			AddTarget(ref targets, neighbor);
		}
		return targets;
	}

	public override void Run()
	{
		if (args[0].obj_val is War war)
		{
			war.GetLeader(0)?.EndWarWith(war.GetLeader(1), null, "white_peace");
		}
		base.Run();
	}

	public override Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		if (key == "a_war_leader_is_our_vassal")
		{
			if (args == null || args.Count == 0)
			{
				return false;
			}
			if (!(args[0].obj_val is War war))
			{
				return false;
			}
			return war.GetLeader(0).sovereignState == own_kingdom || war.GetLeader(1).sovereignState == own_kingdom;
		}
		return base.GetVar(key, vars, as_value);
	}
}
