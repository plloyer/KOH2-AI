using System.Collections.Generic;

namespace Logic;

public class DestroyWallCommand : SquadCommand
{
	private List<Fortification> walls = new List<Fortification>();

	private Fortification target_wall;

	public DestroyWallCommand(BattleAI ai, MapObject squad = null)
		: base(ai, squad)
	{
		walls = GetAllWalls();
		target_wall = GetClosestWall();
	}

	private List<Fortification> GetAllWalls()
	{
		List<Fortification> list = new List<Fortification>();
		if (ai.battle.fortifications != null)
		{
			for (int i = 0; i < ai.battle.fortifications.Count; i++)
			{
				Fortification fortification = ai.battle.fortifications[i];
				if (fortification.def.type == Fortification.Type.Wall && !fortification.IsDefeated() && fortification.battle_side != ai.battle_side)
				{
					list.Add(fortification);
				}
			}
		}
		return list;
	}

	private Fortification GetClosestWall()
	{
		Fortification result = null;
		float num = float.MaxValue;
		for (int i = 0; i < walls.Count; i++)
		{
			Fortification fortification = walls[i];
			if (fortification != null && fortification.def.type == Fortification.Type.Wall && !fortification.IsDefeated() && fortification.IsValid())
			{
				float num2 = base.target.position.Dist(fortification.position);
				if (num2 < num)
				{
					result = fortification;
					num = num2;
				}
			}
		}
		return result;
	}

	public override bool SingleSquad()
	{
		return true;
	}

	public override float Priority()
	{
		float num = ai.def.destroy_wall_base;
		if (ai.battle.fortification_destroyed)
		{
			num *= ai.def.destroy_wall_destroyed_mod;
		}
		return num;
	}

	public override bool Validate()
	{
		Squad squad = base.target as Squad;
		if (!ValidateTarget(squad))
		{
			return false;
		}
		if (walls.Count == 0 || target_wall == null || target_wall.battle_side == squad.battle_side)
		{
			return false;
		}
		return base.Validate();
	}

	public static bool ValidateTarget(Squad target)
	{
		if (target == null || !target.IsValid() || target.IsDefeated() || target.simulation.state >= BattleSimulation.Squad.State.Fled || !target.def.is_siege_eq || !target.can_attack_walls)
		{
			return false;
		}
		return true;
	}

	public override bool Validate(Squad squad)
	{
		if (!base.Validate(squad))
		{
			return false;
		}
		if (!squad.def.can_attack_melee && squad.melee_squads.Count > 0)
		{
			return false;
		}
		return ValidateTarget(squad);
	}

	public override void RemoveSquad(int i)
	{
		base.RemoveSquad(i);
	}

	public override void RemoveSquad(Squad squad)
	{
		base.RemoveSquad(squad);
	}

	public override void Reset()
	{
		base.Reset();
	}

	public override void Execute()
	{
		base.Execute();
		if (squads.Count != 0 && target_wall != null)
		{
			if (target_wall.IsDefeated() || !target_wall.IsValid())
			{
				target_wall = GetClosestWall();
			}
			Squad squad = squads[0];
			if (squad.movement.path == null || (squad.movement.path != null && squad.target != target_wall))
			{
				squad.Attack(target_wall, double_time: false);
			}
		}
	}
}
