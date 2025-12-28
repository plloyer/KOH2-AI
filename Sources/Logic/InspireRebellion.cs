using System.Collections.Generic;

namespace Logic;

public class InspireRebellion : SpyPlot
{
	public InspireRebellion(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new InspireRebellion(owner as Character, def);
	}

	public override string Validate(bool quick_out = false)
	{
		if (base.own_character.mission_kingdom == null)
		{
			return "not_on_mission";
		}
		if ((float)base.own_character.mission_kingdom.GetRebelPopulation() < def.field.GetFloat("min_rebel_population"))
		{
			return "not_enough_rebel_population";
		}
		if (!base.own_character.GetKingdom().is_player && base.game.session_time.minutes < base.game.GetMinRebelPopTime())
		{
			return "too_early_for_rebels_from_non_players";
		}
		return base.Validate(quick_out);
	}

	public override bool ValidateArg(Value value, int def_type)
	{
		Realm obj = value.obj_val as Realm;
		Kingdom mission_kingdom = base.own_character.mission_kingdom;
		if (obj.GetKingdom() != mission_kingdom)
		{
			return false;
		}
		return base.ValidateArg(value, def_type);
	}

	public override List<Value>[] GetPossibleArgs()
	{
		List<Value>[] array = new List<Value>[1]
		{
			new List<Value>()
		};
		Kingdom mission_kingdom = base.own_character.mission_kingdom;
		if (mission_kingdom == null || mission_kingdom.realms.Count < 1)
		{
			return null;
		}
		Realm realm = mission_kingdom.realms[0];
		int num = realm.castle.population.GetRebels();
		for (int i = 1; i < mission_kingdom.realms.Count; i++)
		{
			int rebels = mission_kingdom.realms[i].castle.population.GetRebels();
			if (num < rebels)
			{
				num = rebels;
				realm = mission_kingdom.realms[i];
			}
		}
		if (realm != null)
		{
			AddArg(ref array[0], realm, 0);
		}
		if (array[0].Count == 0)
		{
			return null;
		}
		return array;
	}

	public override void Run()
	{
		(args[0].obj_val as Realm).rebellionRisk.ForceRebel();
		base.Run();
	}
}
