using System;
using System.Collections.Generic;

namespace Logic;

public class WanderPeasant : MapObject
{
	[Flags]
	public enum AttractorFlags
	{
		None = 0,
		Spawn = 1,
		Despawn = 2,
		Idle = 4,
		Move = 8,
		Sit = 0x10,
		Shoveling = 0x20,
		Hammering = 0x40,
		Woodcutting = 0x80,
		Grasscutting = 0x100,
		MoveWithSack = 0x200,
		MoveWithWheat = 0x400,
		MoveWithMenci = 0x800,
		MoveWithBasket = 0x1000,
		Sweeping = 0x2000,
		Run = 0x4000,
		All = 0xFFFF
	}

	public class Def : Logic.Def
	{
		public AttractorFlags allowed_animations;

		public AttractorFlags move_animations;

		public float idle_duration_min = 5f;

		public float idle_duration_max = 10f;

		public float move_speed = 3.45f;

		public float squad_run_away_threshold_dist = 40f;

		public override bool Load(Game game)
		{
			allowed_animations = AttractorFlags.Spawn | AttractorFlags.Despawn | LoadFlags(base.field.FindChild("allowed_animations"));
			move_animations = AttractorFlags.Move | LoadFlags(base.field.FindChild("allowed_move_animations"));
			move_speed = base.field.GetFloat("move_speed", null, move_speed);
			squad_run_away_threshold_dist = base.field.GetFloat("squad_run_away_threshold_dist", null, squad_run_away_threshold_dist);
			DT.Field field = base.field.FindChild("idle_duration");
			if (field == null)
			{
				return true;
			}
			if (field.NumValues() == 2)
			{
				idle_duration_min = field.Float(0);
				idle_duration_max = field.Float(1);
			}
			else
			{
				idle_duration_min = (idle_duration_max = field.Float());
			}
			return true;
		}

		private AttractorFlags LoadFlags(DT.Field field)
		{
			if (field == null)
			{
				return AttractorFlags.None;
			}
			AttractorFlags attractorFlags = AttractorFlags.None;
			for (int i = 0; i < field.NumValues(); i++)
			{
				if (Enum.TryParse<AttractorFlags>(field.Value(i), out var result))
				{
					attractorFlags |= result;
				}
			}
			return attractorFlags;
		}
	}

	public enum State
	{
		None,
		MoveToMainPoint,
		Idle,
		MoveToMainPointAfterIdle,
		MoveToDestination,
		MoveToControlPoint,
		COUNT
	}

	public Def def;

	public AttractorFlags move_flag = AttractorFlags.Move;

	public Battle battle;

	public int battle_side;

	public bool running_away;

	private Timer anim_timer;

	public State state;

	public bool HasFlag(AttractorFlags flag)
	{
		return (def.allowed_animations & flag) != 0;
	}

	public bool HasMoveFlag(AttractorFlags flag)
	{
		return (def.move_animations & flag) != 0;
	}

	public override void SetPosition(PPos position)
	{
		base.position = position;
	}

	public float GetTripProgress()
	{
		if (movement == null || movement.path == null)
		{
			return 1f;
		}
		return movement.path.t / movement.path.path_len;
	}

	public float DistToEnd()
	{
		if (movement == null || movement.path == null)
		{
			return 1f;
		}
		return movement.path.path_len - movement.path.t;
	}

	public WanderPeasant(Game game, Point pos, int kingdom_id, Def def, Battle battle, int battle_side)
		: base(game, pos, kingdom_id)
	{
		this.def = def;
		movement = new Movement(this, PathData.PassableArea.Type.None);
		movement.speed = def.move_speed;
		this.battle = battle;
		this.battle_side = battle_side;
	}

	protected override void OnStart()
	{
		base.OnStart();
		UpdateInBatch(game.update_1sec);
	}

	public override void DestinationReached(MapObject dst_obj)
	{
		base.DestinationReached(dst_obj);
		NotifyListeners("reached_destination");
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();
	}

	public override void OnUpdate()
	{
		base.OnUpdate();
		CheckForEnemies();
	}

	private void CheckForEnemies()
	{
		if (!running_away && EnemiesNearby(battle, battle_side, position, def.squad_run_away_threshold_dist))
		{
			NotifyListeners("run_away");
		}
	}

	public static bool EnemiesNearby(Battle battle, int battle_side, Point position, float squad_run_away_threshold_dist)
	{
		List<Squad> list = battle.squads.Get(1 - battle_side);
		bool result = false;
		for (int i = 0; i < list.Count; i++)
		{
			Squad squad = list[i];
			if (squad != null && !squad.IsDefeated() && squad.position.Dist(position) < squad_run_away_threshold_dist)
			{
				result = true;
				break;
			}
		}
		return result;
	}

	public void WaitForAnimation(float duration, bool force_restart = false)
	{
		if (!IsWaitingAnim() || force_restart)
		{
			anim_timer = Timer.Start(this, "wait_for_animation", duration, restart: true, send_state: false);
		}
	}

	public bool IsWaitingAnim()
	{
		if (anim_timer != null)
		{
			return anim_timer.IsRegisteredForUpdate();
		}
		return false;
	}

	public override void OnTimer(Timer timer)
	{
		string name = timer.name;
		if (name == "wait_for_animation")
		{
			NotifyListeners("finished_wait");
		}
	}

	public override void OnMove(Point prev_pos)
	{
		NotifyListeners("moved");
	}
}
