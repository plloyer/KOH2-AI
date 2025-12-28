using System;
using System.Collections.Generic;

namespace Logic;

public class Squad : MapObject
{
	public enum AttackSide
	{
		Center,
		Left,
		Right
	}

	public enum Command
	{
		Hold,
		Fight,
		Move,
		Attack,
		ShootMove,
		Shoot,
		Disengage,
		Charge
	}

	public enum Stance
	{
		Defensive,
		Aggressive
	}

	public enum Spacing
	{
		Shrinken,
		Default,
		Expanded
	}

	public class MoveCommand
	{
		public PPos target_pos;

		public MapObject target_obj;

		public bool attack;

		public Point direction;

		public float range;

		public bool defeated_move;

		public bool force_no_threat;

		public bool force_melee;

		public bool disengage;

		public bool double_time;

		public bool use_current_setting;
	}

	public Battle battle;

	public BattleSimulation.Squad simulation;

	public Point direction = Point.Zero;

	public Formation formation;

	public PPos avg_troops_pos;

	private float max_shoot_dist;

	public bool is_inside_walls = true;

	public bool is_on_walls = true;

	public float is_inside_walls_dist;

	public bool auto_find_target;

	public bool can_attack_walls = true;

	public bool can_attack_gates = true;

	public bool can_attack_towers;

	public bool can_auto_fire = true;

	private bool _marked_as_target;

	public Ladder cur_ladder;

	public PPos climbing_pos;

	public bool recalc_path;

	private PPos recalc_start_pt;

	public bool position_set;

	public List<SquadBuff> buffs;

	public SquadHighGroundBuff high_ground_buff;

	public SquadTreesBuff trees_buff;

	public ClimbingBuff climbing_buff;

	public SquadBuff ladder_buff;

	public Dictionary<int, Ladder> ladders = new Dictionary<int, Ladder>();

	public List<Squad> sub_squads;

	public Squad main_squad;

	private float sub_squad_path_recalc_cd;

	private float sub_squad_path_recalc_max_cd_ticks = 6f;

	public Time check_under_fire_time;

	public float last_chance_to_shock;

	public float siege_health;

	private ComputableValue stamina;

	private ComputableValue charge_progress;

	private ComputableValue charge_remainder;

	public bool overlap_with_ally;

	public bool stats_dirty;

	public SquadCommand ai_command;

	private MapObject enemy;

	public MapObject target;

	public MapObject ranged_enemy;

	public Point tgt_dir = Point.Zero;

	public int max_salvos;

	public float normal_move_speed;

	public float last_shoot_time;

	public float last_move_speed;

	private float last_elapsed;

	public PPos[] move_history;

	public bool force_melee;

	public bool double_time;

	public Unit.Def visual_def;

	public bool arrow_path_is_clear;

	public bool use_high_angle_arrow_path;

	public float actual_position_height;

	private bool fighting;

	private bool fighting_status_changed;

	private bool fighting_target;

	private bool fighting_target_status_changed;

	private List<Squad> previous_melee_squads = new List<Squad>();

	public List<Squad> melee_squads = new List<Squad>();

	public List<Squad> enemy_shooting_squads = new List<Squad>();

	public Dictionary<Squad, float> enemy_shooting_cooldowns = new Dictionary<Squad, float>();

	public const int MAX_AVOIDING_ITERATIONS = 3;

	public bool can_avoid = true;

	public bool paused;

	private float time_from_last_move;

	private List<Path.Segment> original_path_segments;

	private List<Path> stuck_validation_paths = new List<Path>();

	public AttackSide attack_side;

	public Command command;

	public Stance stance;

	public bool enemy_in_range;

	public Spacing spacing = Spacing.Default;

	public bool rearrange;

	public List<MoveCommand> command_queue = new List<MoveCommand>();

	private MoveCommand cur_command;

	private MapObject last_shot_target;

	private bool last_target_was_climbing;

	private bool _can_reserve;

	public float Max_Shoot_Dist => max_shoot_dist;

	public bool marked_as_target
	{
		get
		{
			return _marked_as_target;
		}
		set
		{
			_marked_as_target = value;
			OnMarkedAsTargetChanged();
		}
	}

	public bool climbing => cur_ladder != null;

	public int cur_ladder_paid
	{
		get
		{
			if (cur_ladder != null)
			{
				return cur_ladder.paID;
			}
			return 0;
		}
	}

	public ComputableValue pack_progress { get; private set; }

	public bool is_packed { get; private set; }

	public bool is_main_squad => main_squad == null;

	public bool is_inside_walls_or_on_walls
	{
		get
		{
			if (!is_inside_walls)
			{
				return is_on_walls;
			}
			return true;
		}
	}

	public PPos destination
	{
		get
		{
			if (movement.path != null)
			{
				return movement.path.dst_pt;
			}
			return position;
		}
	}

	public Squad enemy_squad
	{
		get
		{
			return enemy as Squad;
		}
		set
		{
			enemy = value;
		}
	}

	public Fortification enemy_melee_fortification
	{
		get
		{
			return enemy as Fortification;
		}
		set
		{
			enemy = value;
		}
	}

	public int salvos_left
	{
		get
		{
			if (simulation == null)
			{
				return 0;
			}
			return (int)simulation.remaining_salvos;
		}
		set
		{
			if (simulation != null)
			{
				simulation.remaining_salvos = value;
			}
		}
	}

	public bool is_fighting
	{
		get
		{
			return fighting;
		}
		set
		{
			if (fighting != value)
			{
				fighting_status_changed = true;
			}
			fighting = value;
		}
	}

	public bool is_fighting_target
	{
		get
		{
			return fighting_target;
		}
		set
		{
			if (fighting_target != value)
			{
				fighting_target_status_changed = true;
			}
			fighting_target = value;
		}
	}

	public float rear_threat { get; private set; }

	public float left_threat { get; private set; }

	public float right_threat { get; private set; }

	public bool is_fleeing { get; private set; }

	public bool just_moved => time_from_last_move < 5f;

	public Unit.Def def => simulation?.def;

	public SalvoData.Def salvo_def => simulation?.unit?.salvo_def;

	public int battle_side
	{
		get
		{
			if (simulation != null)
			{
				return simulation.battle_side;
			}
			return 0;
		}
	}

	public bool IsMercenary()
	{
		return simulation?.army?.mercenary != null;
	}

	public void OnMarkedAsTargetChanged()
	{
		NotifyVisuals("marked_as_target_changed");
	}

	public void AddSubSquad(Squad squad)
	{
		if (sub_squads == null)
		{
			sub_squads = new List<Squad>();
		}
		sub_squads.Add(squad);
		squad.main_squad = this;
	}

	public void DelSubSquad(Squad squad)
	{
		sub_squads.Remove(squad);
		if (sub_squads.Count == 0)
		{
			sub_squads = null;
		}
		squad.main_squad = null;
	}

	public Squad(Battle battle, BattleSimulation.Squad simulation, PPos position)
		: base(battle.batte_view_game, position, (simulation?.army == null) ? battle.GetSideKingdom(simulation.battle_side).id : simulation.army.kingdom_id)
	{
		this.battle = battle;
		this.simulation = simulation;
		if (simulation != null)
		{
			simulation.squad = this;
			direction = Point.UnitRight.GetRotated(simulation.heading);
		}
		auto_find_target = def.field.GetBool("auto_find_target", null, auto_find_target);
		can_attack_gates = def.field.GetBool("can_attack_gates", null, can_attack_gates);
		can_attack_walls = def.field.GetBool("can_attack_walls", null, can_attack_walls);
		can_attack_towers = def.field.GetBool("can_attack_towers", null, can_attack_towers);
		battle.squads.Add(this);
		PathData.PassableArea.Type allowed_area_types = PathData.PassableArea.Type.All;
		if (def.is_cavalry || def.type == Unit.Type.InventoryItem)
		{
			allowed_area_types = PathData.PassableArea.Type.Ground;
		}
		movement = new Movement(this, allowed_area_types);
		RecalcMoveSpeed();
		if (def.is_siege_eq && def.unpack_time > 0f)
		{
			pack_progress = new ComputableValue(0f, 0f, game, 0f, def.unpack_time);
		}
		is_packed = true;
		charge_progress = new ComputableValue(0f, 0f, game, 0f, def.charge_time);
		charge_remainder = new ComputableValue(0f, 0f, game, 0f, def.charge_duration);
		SetFormation(def.formation);
		InitBuffs();
		SetStance(def.default_stance);
	}

	public override string GetNameKey(IVars vars = null, string form = "")
	{
		if (simulation == null)
		{
			return "Squad";
		}
		return simulation.def.name;
	}

	protected override void OnStart()
	{
		base.OnStart();
		stamina = new ComputableValue(MaxStamina(), 0f, game, 0f, MaxStamina());
		UpdateInBatch(game.update_half_sec);
		if (simulation.unit != null)
		{
			max_shoot_dist = simulation.unit.GetVar("max_shoot_range");
			max_salvos = simulation.unit.SalvoCapacityModified();
		}
		SetMoveSpeed(normal_move_speed);
	}

	public static Unit.Def GetVisualDef(Squad squad)
	{
		Unit.Def def = squad?.def;
		if (squad.def.is_siege_eq && !string.IsNullOrEmpty(def.packed_def_key))
		{
			if (squad.is_packed)
			{
				Unit.Def def2 = squad.game.defs.Find<Unit.Def>(def.packed_def_key);
				if (def2 != null)
				{
					def = def2;
				}
			}
		}
		else if (!squad.def.is_siege_eq && squad?.simulation != null && (squad.simulation.state == BattleSimulation.Squad.State.Fled || squad.simulation.state == BattleSimulation.Squad.State.Stuck))
		{
			def = squad.game.defs.Find<Unit.Def>(def.surrender_def_key);
		}
		return def;
	}

	public void RecalcMoveSpeed()
	{
		normal_move_speed = simulation.unit.GetVar("move_speed");
		if (visual_def != def && visual_def != null)
		{
			normal_move_speed = visual_def.move_speed;
		}
		if (buffs != null)
		{
			for (int i = 0; i < buffs.Count; i++)
			{
				SquadBuff squadBuff = buffs[i];
				normal_move_speed *= 1f + squadBuff.GetMoveSpeed() / 100f;
			}
		}
	}

	public void SetMoveSpeed(float speed)
	{
		movement.speed = speed;
		if (simulation?.simulation != null)
		{
			movement.speed *= simulation.simulation.def.global_move_speed_mod;
		}
		last_move_speed = movement.speed;
	}

	public void InitBuffs()
	{
		buffs = new List<SquadBuff>();
		DT.Field field = game.dt.Find("SquadBuff");
		if (field?.def == null)
		{
			return;
		}
		for (int i = 0; i < field.def.defs.Count; i++)
		{
			Def def = field.def.defs[i].def;
			DT.Field field2 = field.def.defs[i].field;
			if (field2.type == "template")
			{
				continue;
			}
			SquadBuff squadBuff = SquadBuff.Create(this, def, field2);
			if (squadBuff != null)
			{
				buffs.Add(squadBuff);
				if (squadBuff is SquadHighGroundBuff squadHighGroundBuff)
				{
					high_ground_buff = squadHighGroundBuff;
				}
				if (squadBuff is SquadTreesBuff squadTreesBuff)
				{
					trees_buff = squadTreesBuff;
				}
				if (squadBuff is ClimbingBuff climbingBuff)
				{
					climbing_buff = climbingBuff;
				}
				if (squadBuff.field.key == "ClimbingBuff")
				{
					ladder_buff = squadBuff;
				}
			}
		}
	}

	public void CheckDirtyBuffs()
	{
		if (stats_dirty)
		{
			stats_dirty = false;
			NotifyListeners("buffs_changed");
		}
	}

	public void RecalcPowerGrids()
	{
		if (battle.power_grids != null)
		{
			battle.power_grids[battle_side]?.MarkDirty();
			if (!battle.ai[battle_side].my_squads.Contains(this))
			{
				battle.ai[battle_side].AddSquad(this);
				battle.ai[1 - battle_side].AddSquad(this);
			}
		}
	}

	public override string ToString()
	{
		return base.ToString() + " " + def?.field.key;
	}

	protected override void OnDestroy()
	{
		bool flag = is_main_squad;
		if (main_squad != null)
		{
			Squad squad = main_squad;
			main_squad.DelSubSquad(this);
			main_squad = squad;
		}
		else if (sub_squads != null)
		{
			for (int num = sub_squads.Count - 1; num >= 0; num--)
			{
				Squad squad2 = sub_squads[num];
				DelSubSquad(squad2);
			}
		}
		if (ai_command != null)
		{
			ai_command.RemoveSquad(this);
		}
		base.OnDestroy();
		if (battle.squads.Del(this) && simulation != null && simulation.army != null && flag)
		{
			simulation.army.NotifyListeners("units_changed");
		}
		SetEnemy(null);
		if (simulation != null)
		{
			if (!simulation.IsDefeated())
			{
				simulation.SetState(simulation.state, 5f);
			}
			if (simulation.state != BattleSimulation.Squad.State.Dead && game.obj_state != ObjState.Destroying)
			{
				battle.simulation.totals_dirty = true;
				if (battle.ai != null)
				{
					battle.ai[battle_side]?.DelSquad(this);
					battle.ai[1 - battle_side]?.DelSquad(this);
				}
			}
			if (simulation.spawned_in_bv && simulation.main_squad?.sub_squads != null)
			{
				simulation.main_squad.sub_squads.Remove(simulation);
				if (simulation.main_squad.sub_squads.Count == 0)
				{
					battle.simulation.GetSquads(simulation.battle_side).Remove(simulation.main_squad);
					if (simulation.army != null)
					{
						simulation.army.DelUnit(simulation.unit);
					}
					if (simulation.equipment != null)
					{
						simulation.equipment.simulation = null;
						if (simulation.temporary && simulation.army != null)
						{
							simulation.army.DelInvetoryItem(simulation.equipment);
							battle.simulation.GetEquipment(battle_side).Remove(simulation.equipment);
						}
					}
				}
			}
			simulation.squad = null;
		}
		FormationPool.Return(ref formation);
	}

	public override void Teleport(PPos pt)
	{
		base.Teleport(pt);
		RecalcPowerGrids();
		NotifyVisuals("teleported");
	}

	public float Threat()
	{
		if (simulation == null)
		{
			return 1f;
		}
		return (def.CTH / 10f + def.defense / 100f + def.move_speed / 6f) * (float)NumTroops() / 50f;
	}

	public float EnemyThreatAtPosition(Point pos)
	{
		SquadPowerGrid.Threat interpolatedCell = battle.power_grids[1 - battle_side].GetInterpolatedCell(pos.x, pos.y);
		float num = interpolatedCell.base_threat + interpolatedCell.salvos_about_to_land_threat;
		if (def.is_cavalry)
		{
			num += interpolatedCell.anti_cavalry_threat;
		}
		return num;
	}

	public float ThreatAtPosition()
	{
		return ThreatAtPosition(VisualPosition());
	}

	public float ThreatAtPosition(Point pos)
	{
		return battle.power_grids[battle_side].GetInterpolatedCell(pos.x, pos.y).base_threat;
	}

	public static Point CalcPosition(Battle battle, BattleSimulation.Squad simulation)
	{
		return battle.batte_view_game.world_size * 0.5f + simulation.army_center * battle.def.distance_between_armies + simulation.position * battle.def.distance_between_squads;
	}

	public bool IsMoving()
	{
		if (movement.path != null)
		{
			return !movement.paused;
		}
		return false;
	}

	public void Stop(bool send_state = true)
	{
		tgt_dir = Point.Zero;
		movement.Stop();
	}

	public void CheckUnderFire()
	{
		if (!(game.time_unscaled < check_under_fire_time) && UnderFire())
		{
			NotifyListeners("under_fire");
			check_under_fire_time = game.time_unscaled + def.check_under_fire_cooldown;
		}
	}

	public bool UnderFire()
	{
		if (!is_main_squad)
		{
			return false;
		}
		List<Squad> list = battle.squads.Get(1 - battle_side);
		for (int i = 0; i < list.Count; i++)
		{
			if (list[i].ranged_enemy is Squad squad && (squad == this || (sub_squads != null && sub_squads.Contains(squad))))
			{
				return true;
			}
		}
		return false;
	}

	public void CheckFlee()
	{
		if (!is_main_squad || simulation == null)
		{
			return;
		}
		if (position.paID != 0 && !fighting && !IsMoving() && game.path_finding.data.pointers.GetPA(position.paID - 1).type == PathData.PassableArea.Type.Tower)
		{
			Flee(5f, as_flee: false);
		}
		else if (is_fleeing && simulation.state != BattleSimulation.Squad.State.Retreating && simulation.state != BattleSimulation.Squad.State.Fled)
		{
			is_fleeing = false;
			Stop();
			SetCommand(Command.Hold);
			NotifyVisuals("fleeing_finished");
			NotifyListeners("fleeing_finished");
		}
		else if (!movement.IsMoving())
		{
			if (simulation.state == BattleSimulation.Squad.State.Retreating)
			{
				Flee(simulation.state_end_time - game.time);
			}
			else if (simulation.state == BattleSimulation.Squad.State.Fled)
			{
				FleeToEdgeOfMap();
			}
		}
	}

	public void Flee(float duration, bool as_flee = true)
	{
		if (!is_main_squad)
		{
			return;
		}
		if (!is_fleeing && as_flee && is_main_squad)
		{
			NotifyListeners("fleeing");
			List<Squad> list = battle.squads.Get(1 - battle_side);
			for (int i = 0; i < list.Count; i++)
			{
				list[i].OnTrigger("enemy_retreat");
			}
		}
		float num = def.move_speed * duration;
		is_fleeing = as_flee;
		SetEnemy(null);
		SetRangedEnemy(null);
		for (int j = 0; j < 3; j++)
		{
			List<Squad> list2 = ((j == 0) ? melee_squads : battle.squads.Get(1 - battle_side));
			if (list2 == null)
			{
				continue;
			}
			Point zero = Point.Zero;
			for (int k = 0; k < list2.Count; k++)
			{
				Squad squad = list2[k];
				if (squad != this && squad != null)
				{
					PPos pPos = squad.VisualPosition() - VisualPosition();
					if (j > 1 || !(pPos.Length() > num))
					{
						zero -= pPos.GetNormalized() * (1f - squad.simulation.damage / squad.simulation.max_damage);
					}
				}
			}
			if (!(zero == Point.Zero))
			{
				SetCommand(Command.Disengage);
				FleeTo(position + zero.GetNormalized() * num, 0f);
				break;
			}
		}
	}

	private void FleeToEdgeOfMap()
	{
		if (!is_fleeing && is_main_squad)
		{
			NotifyVisuals("fleeing");
		}
		is_fleeing = true;
		Point point;
		if (battle.citadel_position != PPos.Zero && is_inside_walls_or_on_walls)
		{
			point = battle.citadel_position;
		}
		else
		{
			Point world_size = battle.batte_view_game.world_size;
			Point point2 = new Point(position.x / world_size.x - 0.5f, position.y / world_size.y - 0.5f);
			Point point3 = new Point(Math.Abs(point2.x), Math.Abs(point2.y));
			point = ((point2.x > 0f && point3.y < point3.x) ? new Point(world_size.x - 50f, game.Random(50f, world_size.y - 50f)) : ((point2.x < 0f && point3.y < point3.x) ? new Point(50f, game.Random(50f, world_size.y - 50f)) : ((!(point2.y > 0f) || !(point3.x < point3.y)) ? new Point(game.Random(0.1f, world_size.x * 0.9f), world_size.y * 0.1f) : new Point(game.Random(50f, world_size.x - 50f), world_size.y - 50f))));
		}
		MoveTo(point, 0f, double_time: false, direction, disengage: true, defeated_move: true, avoiding_move: false, returning_move: false, ignore_reserve: false, -1f, important: true);
	}

	public void OnTrigger(string trigger)
	{
		for (int i = 0; i < buffs.Count; i++)
		{
			buffs[i].OnTrigger(trigger);
		}
	}

	private bool CanMove(bool defeated_move)
	{
		if (!defeated_move)
		{
			return simulation.state < BattleSimulation.Squad.State.Retreating;
		}
		return true;
	}

	public void MoveTo(PPos pt, float range, bool double_time, Point tgt_dir, bool disengage, bool defeated_move = false, bool avoiding_move = false, bool returning_move = false, bool ignore_reserve = false, float speed = -1f, bool important = false, bool add_to_command_queue = false, bool from_command_queue = false, bool force_no_threat = false)
	{
		if (movement.pf_path != null && !important && !from_command_queue)
		{
			return;
		}
		tgt_dir = tgt_dir.GetNormalized();
		bool use_current_setting = true;
		if (!from_command_queue && (!add_to_command_queue || (!movement.IsMoving() && command_queue.Count == 0)))
		{
			use_current_setting = false;
			ClearCommandQueue();
		}
		if (!from_command_queue && !avoiding_move && !returning_move)
		{
			AddToCommandQueue(pt, tgt_dir, range, defeated_move, force_no_threat, disengage, double_time, use_current_setting);
			return;
		}
		this.tgt_dir = tgt_dir;
		SetRangedEnemy(null);
		if (CanMove(defeated_move) && (pt.paID <= 0 || game.path_finding.data.pointers.GetPA(pt.paID - 1).type != PathData.PassableArea.Type.Tower))
		{
			movement.min_radius = 0f;
			movement.max_radius = formation.cur_radius;
			if (NumTroops() == 1)
			{
				movement.max_radius = formation.troop_radius;
			}
			movement.MoveTo(pt, range, look_ahead: false, low_level_only: false, ignore_reserve, force_no_threat);
			SetDoubleTime(double_time);
			SetCommand(disengage ? Command.Disengage : (this.double_time ? Command.Charge : Command.Move));
		}
	}

	public void MoveTo(MapObject obj, float range, bool double_time, Point tgt_dir, bool disengage, bool defeated_move = false, bool ignore_reserve = false, bool important = false, bool add_to_command_queue = false, bool from_command_queue = false, bool force_no_threat = false)
	{
		if ((movement.pf_path != null && !important && !from_command_queue) || obj.VisualPosition().pos == Point.Zero)
		{
			return;
		}
		tgt_dir = tgt_dir.GetNormalized();
		bool use_current_setting = true;
		if (!from_command_queue && (!add_to_command_queue || (!movement.IsMoving() && command_queue.Count == 0)))
		{
			use_current_setting = false;
			ClearCommandQueue();
		}
		if (!from_command_queue)
		{
			AddToCommandQueue(obj, attack: false, tgt_dir, range, force_no_threat, disengage, double_time, use_current_setting);
			return;
		}
		this.tgt_dir = tgt_dir;
		SetRangedEnemy(null);
		if (CanMove(defeated_move))
		{
			movement.min_radius = 0f;
			movement.max_radius = formation.cur_radius;
			if (NumTroops() == 1)
			{
				movement.max_radius = formation.troop_radius;
			}
			movement.MoveTo(obj, range, look_ahead: true, ignore_reserve, force_no_threat);
			SetDoubleTime(double_time);
			this.double_time = double_time;
			SetCommand(disengage ? Command.Disengage : (this.double_time ? Command.Charge : Command.Move));
		}
	}

	public void ValidateIfStuck(PPos[] validation_pts)
	{
		if (IsDefeated())
		{
			return;
		}
		if (stuck_validation_paths.Count > 0)
		{
			for (int i = 0; i < stuck_validation_paths.Count; i++)
			{
				Path path = stuck_validation_paths[i];
				path.onPathFindingComplete = (System.Action)Delegate.Remove(path.onPathFindingComplete, new System.Action(OnStuckPathFindingCompleted));
			}
			stuck_validation_paths.Clear();
		}
		_ = game.path_finding.data.pointers;
		for (int j = 0; j < validation_pts.Length; j++)
		{
			Path path2 = new Path(game, position, movement.allowed_area_types);
			path2.min_radius = movement.min_radius;
			path2.max_radius = movement.max_radius;
			path2.look_ahead = true;
			path2.onPathFindingComplete = (System.Action)Delegate.Combine(path2.onPathFindingComplete, new System.Action(OnStuckPathFindingCompleted));
			path2.Find(validation_pts[j]);
			stuck_validation_paths.Add(path2);
		}
	}

	private void OnStuckPathFindingCompleted()
	{
		if (simulation == null)
		{
			return;
		}
		int num = 0;
		bool flag = true;
		for (int i = 0; i < stuck_validation_paths.Count; i++)
		{
			if (stuck_validation_paths[i].state == Path.State.Succeeded)
			{
				PathData.DataPointers pointers = game.path_finding.data.pointers;
				List<Path.Segment> segments = stuck_validation_paths[i].segments;
				if (segments != null && segments.Count > 0 && pointers.BurstedTrace(segments[segments.Count - 1].pt, stuck_validation_paths[i].dst_pt, out var _, ignore_impassable_terrain: false, check_in_area: true, water_is_passable: false, movement.allowed_area_types, battle_side, is_inside_walls_or_on_walls))
				{
					flag = false;
				}
				num++;
				Path path = stuck_validation_paths[i];
				path.onPathFindingComplete = (System.Action)Delegate.Remove(path.onPathFindingComplete, new System.Action(OnStuckPathFindingCompleted));
			}
			else if (stuck_validation_paths[i].state == Path.State.Failed)
			{
				num++;
				Path path2 = stuck_validation_paths[i];
				path2.onPathFindingComplete = (System.Action)Delegate.Remove(path2.onPathFindingComplete, new System.Action(OnStuckPathFindingCompleted));
			}
		}
		if (num == stuck_validation_paths.Count)
		{
			if (flag)
			{
				Stop();
				SetCommand(Command.Hold);
				simulation.SetState(BattleSimulation.Squad.State.Stuck);
			}
			stuck_validation_paths.Clear();
		}
	}

	public bool EnemyNearby(float r = 30f)
	{
		for (int i = 0; i < battle.squads.Count; i++)
		{
			Squad squad = battle.squads[i];
			if (squad.IsEnemy(this) && !squad.IsDefeated() && squad.position.SqrDist(position) < r * r)
			{
				return true;
			}
		}
		return false;
	}

	public bool CavalryNearby(float r = 60f)
	{
		for (int i = 0; i < battle.squads.Count; i++)
		{
			Squad squad = battle.squads[i];
			if (squad.IsEnemy(this) && !squad.IsDefeated() && !squad.def.is_ranged && squad.def.is_cavalry && squad.position.SqrDist(position) < r * r)
			{
				return true;
			}
		}
		return false;
	}

	public bool RangedNearby(float r = 50f)
	{
		for (int i = 0; i < battle.squads.Count; i++)
		{
			Squad squad = battle.squads[i];
			if (squad.IsEnemy(this) && !squad.IsDefeated() && squad.def.is_ranged && squad.position.SqrDist(position) < r * r)
			{
				return true;
			}
		}
		return false;
	}

	public void LookAt(Point pt)
	{
		Point point = pt - position;
		float num = point.Length();
		if (!(num < 0.1f))
		{
			direction = point / num;
			tgt_dir = Point.Zero;
		}
	}

	public bool CanAttack(MapObject obj)
	{
		if (!IsValid())
		{
			return false;
		}
		if (obj is Squad)
		{
			return true;
		}
		if (obj is Fortification fortification)
		{
			if (fortification.IsDefeated() || (fortification.gate != null && fortification.gate.IsDefeated()))
			{
				return false;
			}
			if (!fortification.def.can_be_attacked)
			{
				return false;
			}
			if (fortification.def.type == Fortification.Type.Wall && !can_attack_walls)
			{
				return false;
			}
			if (fortification.def.type == Fortification.Type.Tower && !can_attack_towers)
			{
				return false;
			}
			if (fortification.def.type == Fortification.Type.Gate && !can_attack_gates)
			{
				return false;
			}
			return true;
		}
		return false;
	}

	public bool Attack(Fortification obj, bool double_time, bool command = false, bool force_melee = false, float equipment_melee_dist = 0f, bool important = false, bool add_to_command_queue = false, bool from_command_queue = false, bool force_no_threat = false)
	{
		if (!CanAttack(obj))
		{
			return false;
		}
		if (def.is_siege_eq && CanShoot(obj, max_dist: true))
		{
			SetRangedEnemy(obj);
			NotifyListeners("check_salvo_passability", ranged_enemy);
			arrow_path_is_clear = true;
			use_high_angle_arrow_path = false;
			if (command)
			{
				SetCommand(Command.Shoot, ranged_enemy);
			}
			FightRanged();
		}
		else if (def.is_siege_eq && CanShoot(obj))
		{
			movement.MoveTo(obj);
		}
		else if (!def.is_ranged || !def.is_siege_eq)
		{
			SetRangedEnemy(null);
			if (obj != null)
			{
				SetEnemyMeleeFortification(obj);
				SetCommand(Command.Attack, obj);
				if (is_inside_walls_or_on_walls)
				{
					movement.MoveTo(obj.attack_position_inside_wall, def.attack_range, look_ahead: false, low_level_only: false, force_no_threat);
				}
				else
				{
					movement.MoveTo(obj.attack_position_outside_wall, def.attack_range, look_ahead: false, low_level_only: false, force_no_threat);
				}
			}
		}
		return true;
	}

	public bool Attack(MapObject target, bool double_time, bool command = false, bool force_melee = false, float equipment_melee_dist = 0f, bool important = false, bool add_to_command_queue = false, bool from_command_queue = false, bool force_no_threat = false)
	{
		if (simulation != null && simulation.state >= BattleSimulation.Squad.State.Retreating)
		{
			return false;
		}
		if (target == null)
		{
			return false;
		}
		if (target.VisualPosition().pos == Point.Zero)
		{
			return false;
		}
		if (!add_to_command_queue && !from_command_queue && important)
		{
			ClearCommandQueue();
		}
		if (!from_command_queue && important)
		{
			AddToCommandQueue(target, attack: true, Point.Zero, 0f, force_no_threat, force_melee, double_time);
			return true;
		}
		if (!CanAttack(target))
		{
			return false;
		}
		if (!important)
		{
			important = target != this.target;
		}
		tgt_dir = Point.Zero;
		movement.min_radius = 0f;
		movement.max_radius = formation.cur_radius;
		if (NumTroops() == 1)
		{
			movement.max_radius = formation.troop_radius;
		}
		this.force_melee = force_melee;
		bool flag = def.move_speed > 0f;
		if (target is Fortification obj)
		{
			return Attack(obj, command, command, force_melee, equipment_melee_dist, important, add_to_command_queue, from_command_queue, force_no_threat);
		}
		bool flag2 = CanShoot(target, max_dist: true) && !force_melee && !is_fighting;
		if (flag2)
		{
			if (command)
			{
				movement.Stop();
			}
			SetRangedEnemy(target);
			NotifyListeners("check_salvo_passability", ranged_enemy);
		}
		else
		{
			SetRangedEnemy(null);
		}
		if (!force_melee && ranged_enemy != null && def.is_ranged && salvos_left > 0 && arrow_path_is_clear)
		{
			if (command)
			{
				SetCommand(Command.Shoot, ranged_enemy);
			}
			FightRanged();
		}
		else
		{
			if (flag2 && !arrow_path_is_clear && def.is_siege_eq)
			{
				Fortification fortification = null;
				if (target != null)
				{
					fortification = target as Fortification;
				}
				if (fortification != null)
				{
					Attack(target, double_time: true);
				}
				return false;
			}
			if (!force_melee && ranged_enemy != null && (!arrow_path_is_clear || !CanShootInTrees()))
			{
				if (command)
				{
					if (movement.pf_path != null && !important)
					{
						return false;
					}
					_ = salvo_def.min_shoot_range;
					movement.MoveTo(ranged_enemy, def.attack_range, look_ahead: false, ignore_reserve: false, force_no_threat);
					SetCommand(Command.ShootMove, target);
				}
				if (IsMoving() || (this.command != Command.Attack && this.command != Command.Shoot && this.command != Command.ShootMove))
				{
					return false;
				}
			}
			else if (flag && !flag2)
			{
				if (movement.pf_path != null && !important)
				{
					return false;
				}
				bool look_ahead = false;
				SetCommand(Command.Attack, target);
				attack_side = AttackSide.Center;
				if (def.is_cavalry)
				{
					movement.MoveTo(target, 0f, look_ahead, ignore_reserve: false, force_no_threat);
				}
				else
				{
					movement.MoveTo(target, def.attack_range, look_ahead, ignore_reserve: false, force_no_threat);
				}
			}
		}
		SetMoveSpeed(normal_move_speed);
		SetDoubleTime(double_time);
		this.double_time = double_time;
		return true;
	}

	public bool SetDoubleTime(bool value, bool update_stamina = true)
	{
		bool flag = double_time;
		if (value && GetStamina() > def.stamina_req_charge)
		{
			if (command == Command.Move || command == Command.Attack)
			{
				command = Command.Charge;
			}
			double_time = true;
			SetMoveSpeed(normal_move_speed * def.run_speed_mul);
		}
		else
		{
			if (command == Command.Charge)
			{
				if (target != null)
				{
					SetCommand(Command.Attack, target);
				}
				else
				{
					SetCommand(Command.Move);
				}
			}
			double_time = false;
			SetMoveSpeed(normal_move_speed);
		}
		if (update_stamina)
		{
			UpdateStamina();
		}
		if (double_time != flag)
		{
			string message = (double_time ? "started_double_time" : "finished_double_time");
			NotifyVisuals(message);
		}
		return double_time;
	}

	public bool IsDefeated()
	{
		if (simulation != null && !simulation.IsDefeated())
		{
			return NumTroops() <= 0;
		}
		return true;
	}

	public bool CanShoot(MapObject target, bool max_dist = false)
	{
		Squad squad = target as Squad;
		float num = ((squad == null) ? avg_troops_pos.Dist(target.position) : avg_troops_pos.Dist(squad.avg_troops_pos));
		bool result = target.IsValid() && def.is_ranged && salvos_left > 0 && salvo_def.min_shoot_speed > 0f && num > salvo_def.min_shoot_range && (!max_dist || num < max_shoot_dist);
		if (squad != null && squad.IsDefeated())
		{
			result = false;
		}
		Fortification fortification = target as Fortification;
		if (fortification != null && fortification.IsDefeated())
		{
			result = false;
		}
		if (fortification != null && salvo_def != null && !salvo_def.can_hit_fortification)
		{
			result = false;
		}
		if (max_dist && !CanShootInTrees())
		{
			result = false;
		}
		return result;
	}

	public void FightRanged()
	{
		if (ranged_enemy == null)
		{
			return;
		}
		Squad squad = ranged_enemy as Squad;
		Fortification fortification = ranged_enemy as Fortification;
		if ((squad != null && squad.IsDefeated()) || (fortification != null && fortification.IsDefeated() && def.type == Unit.Type.InventoryItem) || salvos_left <= 0)
		{
			ranged_enemy = null;
			SetCommand(Command.Hold);
		}
		else if (!((float)game.time.milliseconds * 0.001f < last_shoot_time + def.shoot_interval) && IsValidRangedEnemy(ranged_enemy) && arrow_path_is_clear && (!CanBePacked() || (!IsPacking() && !is_packed)) && CanShootInTrees())
		{
			if (!def.is_cavalry || movement.path?.dst_obj == ranged_enemy)
			{
				movement.Stop();
			}
			Shoot();
		}
	}

	public bool ShootingWhileMoving()
	{
		if (def.is_cavalry && ranged_enemy != null && IsMoving())
		{
			return movement.path.dst_obj != ranged_enemy;
		}
		return false;
	}

	private void Shoot(MapObject temp_enemy = null)
	{
		if (temp_enemy == null && target != null && !ShootingWhileMoving())
		{
			SetCommand(Command.Shoot, target);
		}
		if (temp_enemy == null)
		{
			temp_enemy = ranged_enemy;
		}
		NotifyListeners("shoot", temp_enemy);
		OnTrigger("shoot");
	}

	public void OnSuccessfulShoot(MapObject temp_enemy)
	{
		if (temp_enemy == target)
		{
			Point normalized = ((Point)temp_enemy.position - position).GetNormalized();
			direction = normalized;
		}
		salvos_left--;
		last_shoot_time = (float)game.time.milliseconds * 0.001f;
		if (temp_enemy is Squad squad)
		{
			squad.CheckUnderFire();
		}
		last_shot_target = temp_enemy;
	}

	public void FleeFrom(Point pt, float range)
	{
		movement.min_radius = 0f;
		movement.max_radius = formation.cur_radius;
		if (NumTroops() == 1)
		{
			movement.max_radius = formation.troop_radius;
		}
		movement.FleeFrom(pt, range);
	}

	public void FleeTo(Point pt, float range)
	{
		movement.min_radius = 0f;
		movement.max_radius = formation.cur_radius;
		if (NumTroops() == 1)
		{
			movement.max_radius = formation.troop_radius;
		}
		movement.FleeTo(pt, range);
	}

	public override void OnBeginMoving()
	{
		base.OnBeginMoving();
		if (movement != null && def != null)
		{
			CheckNeedsLadder();
			CheckNeedsPack();
			Point point = direction;
			point = ((movement == null || movement.path == null || !movement.path.GetPathPoint(movement.path.t + def.dir_look_ahead, out var pt, out var _)) ? CalcFinalDirection() : ((Point)(pt - position).GetNormalized()));
			if (movement.path != null && !movement.path.is_updated_path && game?.path_finding != null && game.path_finding.CheckValid() && !climbing && is_main_squad && !IsPacking() && !(Math.Abs(Point.AngleBetween(direction, point)) <= 30f))
			{
				direction = point;
				original_path_segments = new List<Path.Segment>(movement.path.segments);
				AdvanceTo(avg_troops_pos);
			}
		}
	}

	private void AdvanceTo(Point avg_troops_pos, float added_offset = 4f, bool based_on_speed = true)
	{
		PathData.DataPointers pointers = game.path_finding.data.pointers;
		float num = 1f;
		movement.path.GetPathPoint(movement.path.t + num, out var pt, out var _);
		float num2 = (position - avg_troops_pos).GetNormalized().Dot((position - pt).GetNormalized());
		num = (position - avg_troops_pos).Length() * num2;
		num += added_offset;
		CalcPos(out pt, out var dir, num, num, based_on_speed);
		if (pointers.BurstedTrace(position, pt, out var result, ignore_impassable_terrain: false, check_in_area: true, water_is_passable: false, movement.allowed_non_ladder_areas, battle_side, is_inside_walls_or_on_walls) && pointers.IsPassable(result))
		{
			movement.AdvanceTo(movement.path.t + num);
			direction = dir;
			FindAndResolveSquadCollisions(1.5f);
		}
	}

	public override void OnMove(Point prev_pos)
	{
		if (movement.path.GetPathPoint(movement.path.t + def.dir_look_ahead, out var pt, out var _))
		{
			direction = (pt - position).GetNormalized();
		}
		else
		{
			direction = CalcFinalDirection();
		}
		NotifyListeners("moved");
		UpdateEnemy();
	}

	public override void DestinationReached(MapObject dst_obj)
	{
		base.DestinationReached(dst_obj);
		if (simulation != null && simulation.state == BattleSimulation.Squad.State.Fled)
		{
			Destroy();
			return;
		}
		if (command == Command.Shoot || command == Command.ShootMove)
		{
			if (ranged_enemy != null)
			{
				NotifyListeners("check_salvo_passability", ranged_enemy);
				if (arrow_path_is_clear)
				{
					if ((!CanBePacked() || (!(PackProgressNormalized() >= 0f) && !is_packed)) && CanShootInTrees())
					{
						Shoot();
					}
					return;
				}
			}
			else if (target != null)
			{
				Attack(target, double_time: false);
				return;
			}
		}
		if (is_fighting_target)
		{
			SetCommand(Command.Fight, target);
		}
		else if (target != null)
		{
			is_fighting_target = true;
		}
		else
		{
			SetCommand(Command.Hold);
		}
	}

	public bool CalcPos(out PPos pos, out PPos dir, float pos_look_ahead = 0f, float dir_look_ahed = 0f, bool based_on_speed = false)
	{
		if (movement.path == null || !movement.path.IsValid() || movement.paused)
		{
			pos = position;
			dir = direction;
			return false;
		}
		float num = (based_on_speed ? movement.speed : 1f);
		last_elapsed = GetElapsedTime();
		float nextStepTime = GetNextStepTime(last_elapsed);
		if (!movement.path.GetPathPoint(nextStepTime, out var pt, out var ptDest))
		{
			pos = pt;
			dir = direction;
			return false;
		}
		float num2 = nextStepTime + pos_look_ahead * num;
		bool pathPoint;
		bool result = (pathPoint = movement.path.GetPathPoint(num2, out pos, out ptDest));
		if (dir_look_ahed > 0f)
		{
			float path_t = num2 + dir_look_ahed * num;
			pathPoint = movement.path.GetPathPoint(path_t, out ptDest, out var _);
			dir = (ptDest - pos).GetNormalized();
		}
		else
		{
			dir = (ptDest - pt).GetNormalized();
		}
		if (!pathPoint)
		{
			dir = CalcFinalDirection();
		}
		return result;
	}

	public float GetElapsedTime()
	{
		if (!movement.paused)
		{
			return game.time - movement.tmLastMove;
		}
		return last_elapsed;
	}

	public float GetNextStepTime(float elapsed)
	{
		return movement.path.t + elapsed * movement.speed;
	}

	public float GetMoveTime()
	{
		if (movement.path == null)
		{
			return 0f;
		}
		return GetNextStepTime(GetElapsedTime());
	}

	public Point CalcFinalDirection()
	{
		Squad squad = target as Squad;
		if (IsValidEnemy(squad))
		{
			return (target.position - position).GetNormalized();
		}
		if (movement.path == null || def == null)
		{
			return direction;
		}
		if (tgt_dir != Point.Zero)
		{
			return tgt_dir;
		}
		if (movement.path.GetPathPoint(movement.path.path_len - def.dir_look_ahead, out var pt, out var _))
		{
			return (movement.path.segments[movement.path.segments.Count - 1].pt - pt).GetNormalized();
		}
		return tgt_dir;
	}

	public void SetFormation(string type)
	{
		Formation formation = this.formation;
		Formation.Def def = game.defs.Get<Formation.Def>(type + "Formation");
		this.formation = FormationPool.Get(def, this);
		if (formation != null)
		{
			this.formation.CopyParamsFrom(formation);
		}
		else
		{
			this.formation.troop_radius = this.def.scaled_radius;
		}
		FormationPool.Return(ref formation);
		CalcFormation();
		rearrange = true;
	}

	private void SetSpacing(float spacing)
	{
		formation.spacing_mul = spacing;
		CalcFormation();
	}

	public int NumTroops()
	{
		if (simulation == null || simulation.unit == null)
		{
			return 0;
		}
		simulation.ValidateMaxTroops();
		return simulation.NumTroops();
	}

	public int InitialTroops()
	{
		if (simulation == null || simulation.unit == null)
		{
			return 0;
		}
		simulation.ValidateMaxTroops();
		return (int)Math.Ceiling((float)simulation.max_troops * (1f - simulation.initial_damage));
	}

	public void SetNumTroops(int alive, bool check_morale = false)
	{
		simulation.ValidateMaxTroops();
		float num = 1f - (float)alive / (float)simulation.max_troops;
		if (simulation.damage != num)
		{
			if (check_morale)
			{
				simulation.damage_acc = (float)Math.Round(num - simulation.damage, 5);
			}
			simulation.damage = num;
			if (battle != null && battle.simulation != null)
			{
				battle.simulation.totals_dirty = true;
			}
			battle.power_grids[battle_side]?.MarkDirty();
		}
	}

	public bool HasValidSubSquad()
	{
		if (sub_squads == null)
		{
			return false;
		}
		for (int i = 0; i < sub_squads.Count; i++)
		{
			Squad squad = sub_squads[i];
			if (squad.simulation.state != BattleSimulation.Squad.State.Dead && squad.simulation.state != BattleSimulation.Squad.State.Fled)
			{
				return true;
			}
		}
		return false;
	}

	public void OnDefeat(bool recalc = true, bool swap_main_squad = false)
	{
		if (!swap_main_squad || !HasValidSubSquad())
		{
			if (recalc)
			{
				battle.power_grids[battle_side].MarkDirty();
			}
			battle.ai[battle_side].DelSquad(this);
			battle.ai[1 - battle_side].DelSquad(this);
			if (ai_command != null)
			{
				ai_command.RemoveSquad(this);
			}
		}
		if (!swap_main_squad)
		{
			SetEnemy(null);
			if (simulation.state == BattleSimulation.Squad.State.Fled)
			{
				FleeToEdgeOfMap();
			}
			if (sub_squads != null)
			{
				if (simulation.state != BattleSimulation.Squad.State.Dead)
				{
					for (int i = 0; i < sub_squads.Count; i++)
					{
						Squad squad = sub_squads[i];
						if (squad.simulation != null && squad.IsValid() && !squad.simulation.IsDefeated())
						{
							squad.simulation.SetState(simulation.state);
						}
					}
				}
				if (def.type == Unit.Type.Noble)
				{
					battle.simulation.CheckSurrender(simulation);
				}
			}
		}
		NotifyListeners("defeated", swap_main_squad);
	}

	private void CalcFormation()
	{
		formation.SetCount(NumTroops());
		CalcFormation(formation);
	}

	public void CalcFormation(Formation formation, float pos_look_ahead = 0f, bool calc_positions = false)
	{
		PPos pos;
		PPos dir;
		bool moving = CalcPos(out pos, out dir, pos_look_ahead, def.dir_look_ahead);
		if (dir == PPos.Zero)
		{
			dir.pos = Point.UnitUp;
		}
		formation.Calc(pos, dir, calc_positions, moving);
	}

	public void CalcFormation(PPos pos, PPos dir, bool moving, Formation formation, float pos_look_ahead = 0f, bool calc_positions = false)
	{
		if (dir == PPos.Zero)
		{
			dir.pos = Point.UnitUp;
		}
		formation.Calc(pos, dir, calc_positions, moving);
	}

	public override IRelationCheck GetStanceObj()
	{
		if (simulation?.unit?.army?.rebel?.rebellion != null)
		{
			return simulation.unit.army.rebel.rebellion;
		}
		if (simulation?.unit?.army != null && simulation.unit.army.game?.religions?.catholic?.crusade?.army == simulation.unit.army)
		{
			return simulation.unit.army.game.religions.catholic.crusade;
		}
		return GetKingdom();
	}

	public bool IsEnemy(Squad squad)
	{
		if (squad == null)
		{
			return false;
		}
		if (squad.IsValid())
		{
			return squad.battle_side != battle_side;
		}
		return false;
	}

	public bool IsEnemy(Fortification fortif)
	{
		if (fortif == null)
		{
			return false;
		}
		if (fortif.IsValid())
		{
			return fortif.battle_side != battle_side;
		}
		return false;
	}

	public void SetCommand(Command command, MapObject target = null)
	{
		this.command = command;
		SetTarget(target);
	}

	public void SetStance(Stance stance)
	{
		this.stance = stance;
	}

	public void SetSpacing(Spacing spacing, bool force = false)
	{
		if (this.spacing != spacing || force)
		{
			this.spacing = spacing;
			switch (spacing)
			{
			case Spacing.Shrinken:
				SetSpacing(formation.min_spacing);
				break;
			case Spacing.Default:
				SetSpacing(formation.default_spacing);
				break;
			case Spacing.Expanded:
				SetSpacing(formation.max_spacing);
				break;
			}
			rearrange = true;
		}
	}

	public void SetTarget(MapObject target)
	{
		this.target = target;
	}

	public void SetEnemyMeleeFortification(Fortification fort)
	{
		if (enemy_melee_fortification != fort)
		{
			SetEnemy(null);
			target = fort;
			enemy_melee_fortification = fort;
			if (is_main_squad && enemy_melee_fortification != null && !enemy_melee_fortification.cur_attackers.Contains(this))
			{
				enemy_melee_fortification.cur_attackers.Add(this);
			}
		}
	}

	public void SetEnemy(Squad squad)
	{
		if (squad != enemy && (squad == null || (battle.stage == Battle.Stage.Ongoing && position_set && squad.position_set)))
		{
			if (squad == null && target == null)
			{
				force_melee = false;
			}
			if (enemy_melee_fortification != null)
			{
				enemy_melee_fortification.cur_attackers.Remove(this);
				enemy_melee_fortification = null;
			}
			enemy = squad;
			if (enemy != null)
			{
				ranged_enemy = null;
			}
			if (MoveTowardsEnemy())
			{
				FaceEnemy();
			}
			if (enemy != null && !IsRegisteredForUpdate())
			{
				UpdateInBatch(game.update_half_sec);
			}
			NotifyListeners("enemy_changed");
		}
	}

	public void SetRangedEnemy(MapObject squad)
	{
		if (squad != ranged_enemy)
		{
			last_shot_target = null;
			if (squad == null && target == null)
			{
				force_melee = false;
			}
			ranged_enemy = squad;
			if (ranged_enemy != null)
			{
				SetEnemy(null);
				battle.power_grids[battle_side].MarkDirty();
			}
			FaceEnemy();
			if (enemy != null && !IsRegisteredForUpdate())
			{
				UpdateInBatch(game.update_half_sec);
			}
			NotifyListeners("enemy_changed");
		}
	}

	public bool IsValidEnemy(Squad squad, bool check_distance = true)
	{
		if (squad == null)
		{
			return false;
		}
		if (!IsEnemy(squad))
		{
			return false;
		}
		if (squad.IsDefeated())
		{
			return false;
		}
		if (!def.can_attack_melee)
		{
			return false;
		}
		if (CanShoot(squad, max_dist: true) && !force_melee)
		{
			return false;
		}
		if (check_distance && !IsInSquadControlZone(squad))
		{
			return false;
		}
		return true;
	}

	public bool IsValidFortificationEnemy(Fortification fortification, bool check_distance = true)
	{
		if (fortification == null)
		{
			return false;
		}
		if (!IsEnemy(fortification))
		{
			return false;
		}
		if (fortification.IsDefeated())
		{
			return false;
		}
		if (check_distance)
		{
			float cur_width = formation.cur_width;
			float num = 1f;
			if (fortification.position.SqrDist(position) > cur_width * cur_width + num * num)
			{
				return false;
			}
		}
		return true;
	}

	public bool IsValidRangedEnemy(MapObject target, bool ignore_enemy = false, bool check_distance = true)
	{
		if (target == null)
		{
			return false;
		}
		if (is_fighting && !ignore_enemy)
		{
			return false;
		}
		if (!def.is_ranged)
		{
			return false;
		}
		if (!IsEnemy(target as Squad) && !IsEnemy(target as Fortification))
		{
			return false;
		}
		Squad squad = target as Squad;
		Fortification fortification = target as Fortification;
		if (squad != null && squad.IsDefeated())
		{
			return false;
		}
		if (fortification != null && fortification.IsDefeated())
		{
			return false;
		}
		if (check_distance)
		{
			float min_shoot_range = salvo_def.min_shoot_range;
			float num = squad?.avg_troops_pos.SqrDist(avg_troops_pos) ?? target.position.SqrDist(avg_troops_pos);
			if (num > max_shoot_dist * max_shoot_dist || num < min_shoot_range * min_shoot_range)
			{
				return false;
			}
		}
		return true;
	}

	public void ConsiderEnemy(Squad squad)
	{
		if (!IsValidEnemy(enemy_squad) && !IsValidFortificationEnemy(enemy_melee_fortification) && IsValidEnemy(squad))
		{
			SetEnemy(squad);
		}
	}

	private bool CanAutoFindTarget()
	{
		if (def.unpack_time <= 0f)
		{
			return true;
		}
		if (is_packed)
		{
			return IsAIEnabled();
		}
		return true;
	}

	private bool IsAIEnabled()
	{
		return battle.ai[battle_side].HasOwnerFlag(this, BattleAI.EnableFlags.Commands);
	}

	public void UpdateEnemy()
	{
		if (battle.stage != Battle.Stage.Ongoing)
		{
			return;
		}
		Squad squad = target as Squad;
		Fortification fortification = target as Fortification;
		if (IsDefeated())
		{
			SetTarget(null);
			ranged_enemy = null;
			SetEnemy(null);
			return;
		}
		if (!movement.IsMoving() && def.type == Unit.Type.InventoryItem && enemy_melee_fortification == null && target == null && auto_find_target && battle.fortifications != null && CanAutoFindTarget())
		{
			float num = float.MaxValue;
			Fortification fortification2 = null;
			for (int i = 0; i < battle.fortifications.Count; i++)
			{
				Fortification fortification3 = battle.fortifications[i];
				if (fortification3 == null || fortification3.IsDefeated() || !IsValidRangedEnemy(fortification3) || !CanAttack(fortification3))
				{
					continue;
				}
				float num2 = fortification3.position.Dist(position);
				if (!(num2 < num))
				{
					continue;
				}
				if (fortification2 != null && (fortification2.def.type == Fortification.Type.Tower || fortification2.def.type == Fortification.Type.Gate))
				{
					if (fortification3.def.type == Fortification.Type.Tower || fortification3.def.type == Fortification.Type.Gate)
					{
						fortification2 = fortification3;
						num = num2;
					}
				}
				else
				{
					fortification2 = fortification3;
					num = num2;
				}
			}
			if (fortification2 != null && can_auto_fire)
			{
				SetRangedEnemy(fortification2);
				if (def.walk_anim_speed == 0f)
				{
					return;
				}
			}
		}
		if (target != null)
		{
			if (enemy_melee_fortification != null && enemy_melee_fortification == target && (enemy_melee_fortification.IsDefeated() || (enemy_melee_fortification.gate != null && enemy_melee_fortification.gate.IsDefeated())))
			{
				SetEnemyMeleeFortification(null);
				SetCommand(Command.Hold);
				if (movement?.path != null)
				{
					Stop();
				}
			}
			else if (fortification != null && (fortification.IsDefeated() || (fortification.gate != null && fortification.gate.IsDefeated())))
			{
				SetCommand(Command.Hold);
				if (movement?.path?.dst_obj == fortification)
				{
					Stop();
				}
			}
			else if (!target.IsValid() || (squad != null && squad.IsDefeated()))
			{
				if (movement?.path?.dst_obj == squad)
				{
					Stop();
				}
				if (squad != null && squad.main_squad != null)
				{
					Attack(squad.main_squad, double_time, command: false, force_melee);
					return;
				}
				SetTarget(null);
				if (command == Command.Attack)
				{
					SetCommand(Command.Hold);
				}
			}
			else
			{
				if (def.can_attack_melee || !def.is_ranged)
				{
					if (IsValidEnemy(squad))
					{
						SetEnemy(squad);
					}
					else if (!force_melee && IsValidRangedEnemy(target))
					{
						SetRangedEnemy(target);
					}
					return;
				}
				float num3 = ((squad == null) ? avg_troops_pos.Dist(target.position) : avg_troops_pos.Dist(squad.avg_troops_pos));
				if (!(num3 < salvo_def.min_shoot_range))
				{
					if (IsValidEnemy(squad))
					{
						SetEnemy(squad);
					}
					else if (!force_melee && IsValidRangedEnemy(target))
					{
						SetRangedEnemy(target);
					}
					return;
				}
				if (movement?.path?.dst_obj == squad)
				{
					Stop();
				}
				SetTarget(null);
				if (command == Command.Attack || command == Command.Shoot)
				{
					SetCommand(Command.Hold);
				}
			}
		}
		if (enemy_melee_fortification != null)
		{
			if (!enemy_melee_fortification.IsDefeated() && (enemy_melee_fortification.gate == null || !enemy_melee_fortification.gate.IsDefeated()))
			{
				return;
			}
			SetEnemyMeleeFortification(null);
			SetTarget(null);
		}
		bool flag = false;
		List<Squad> list = battle.squads.Get(1 - battle_side);
		float num4 = float.MaxValue;
		int num5 = -1;
		bool flag2 = false;
		bool flag3 = false;
		bool flag4 = false;
		for (int j = 0; j < list.Count; j++)
		{
			Squad squad2 = list[j];
			if (flag)
			{
				continue;
			}
			if (IsValidEnemy(squad2) && (movement.paused || (command != Command.Disengage && (command != Command.Move || squad2.command == Command.Move))))
			{
				float num6 = squad2.position.SqrDist(position);
				if (num6 < num4)
				{
					num4 = num6;
					num5 = j;
					flag2 = false;
				}
			}
			else if (!force_melee && command != Command.Disengage && command != Command.Move && command != Command.Charge && IsValidRangedEnemy(squad2) && CanAutoFindTarget())
			{
				float num7 = squad2.position.SqrDist(position);
				if ((num7 < num4 && flag4 == squad2.marked_as_target) || (!flag4 && squad2.marked_as_target))
				{
					num4 = num7;
					num5 = j;
					flag2 = true;
					flag4 = squad2.marked_as_target;
				}
			}
			else if (def.is_cavalry && IsValidRangedEnemy(squad2) && salvos_left > 0)
			{
				float num8 = squad2.position.SqrDist(position);
				if ((num8 < num4 && flag4 == squad2.marked_as_target) || (!flag4 && squad2.marked_as_target))
				{
					num4 = num8;
					num5 = j;
					flag3 = true;
					flag4 = squad2.marked_as_target;
				}
			}
		}
		if (num5 >= 0 && can_auto_fire)
		{
			Squad squad3 = list[num5];
			if (flag3)
			{
				SetRangedEnemy(squad3);
			}
			else if (!flag2)
			{
				SetEnemy(squad3);
			}
			else
			{
				SetRangedEnemy(squad3);
			}
			flag = true;
			squad3.ConsiderEnemy(this);
		}
		if (IsValidEnemy(enemy_squad, ((target != null && target != enemy) || command != Command.Attack) && command != Command.Shoot && command != Command.Charge && command != Command.ShootMove))
		{
			if (def.move_speed > 0f)
			{
				if (stance == Stance.Aggressive && !is_fighting)
				{
					FaceEnemy();
				}
				if (enemy_squad.command == Command.Attack || enemy_squad.command == Command.Fight)
				{
					float num9 = Math.Max(formation.cur_width, formation.cur_height);
					float num10 = enemy.position.SqrDist(position);
					if (num10 < num9 * num9 && num10 > 0f)
					{
						NotifyListeners("fighting");
					}
				}
			}
			if (!force_melee && IsValidRangedEnemy(target, !IsValidEnemy(enemy_squad)))
			{
				SetCommand(Command.Shoot, target);
				SetRangedEnemy(target);
			}
		}
		else if (IsValidRangedEnemy(ranged_enemy, ignore_enemy: false, check_distance: false))
		{
			Squad squad4 = ranged_enemy as Squad;
			bool flag5 = def.move_speed > 0f;
			FaceEnemy();
			if (movement.IsMoving())
			{
				return;
			}
			float num11 = max_shoot_dist;
			float min_shoot_range = salvo_def.min_shoot_range;
			float num12 = squad4?.avg_troops_pos.SqrDist(avg_troops_pos) ?? ranged_enemy.position.SqrDist(avg_troops_pos);
			if (flag5 && num12 > num11 * num11)
			{
				if (command == Command.Shoot || command == Command.ShootMove)
				{
					MoveTo(ranged_enemy.position, min_shoot_range, double_time: false, direction, EnemyNearby());
					SetCommand(Command.ShootMove, ranged_enemy);
				}
				else
				{
					SetRangedEnemy(null);
				}
			}
			else if (num12 < min_shoot_range * min_shoot_range)
			{
				SetEnemy(squad4);
			}
		}
		else if (!flag)
		{
			if (movement.path != null && movement.path.dst_obj == enemy && enemy != null && target == null)
			{
				SetCommand(Command.Hold);
				Stop();
			}
			SetEnemy(null);
			SetRangedEnemy(null);
		}
	}

	private void FaceEnemy()
	{
		if (enemy != null)
		{
			LookAt(enemy.position);
		}
		else if (ranged_enemy != null && ranged_enemy == target)
		{
			LookAt(ranged_enemy.position);
		}
	}

	private bool MoveTowardsEnemy()
	{
		if (stance == Stance.Defensive || command != Command.Hold)
		{
			enemy_in_range = false;
			return false;
		}
		enemy_squad = enemy as Squad;
		if (enemy_squad == null)
		{
			enemy_in_range = false;
			return false;
		}
		if (def.is_ranged && salvos_left > 0)
		{
			return false;
		}
		if (IsInSquadControlZone(enemy_squad))
		{
			if (!enemy_in_range)
			{
				enemy_in_range = true;
				PPos pt = (position + enemy_squad.position) * 0.5f;
				MoveTo(pt, 0f, double_time: false, -enemy_squad.direction, disengage: false);
				return true;
			}
		}
		else
		{
			enemy_in_range = false;
		}
		return false;
	}

	private bool IsInSquadControlZone(Squad enemy_squad)
	{
		if (position.paID != enemy_squad.position.paID)
		{
			return false;
		}
		if (is_inside_walls != enemy_squad.is_inside_walls)
		{
			return false;
		}
		PPos rotated = position.GetRotated(90f - direction.Heading());
		PPos rotated2 = enemy_squad.position.GetRotated(90f - direction.Heading());
		float num = ((stance == Stance.Defensive) ? formation.def.defensive_stance_control_zone : formation.def.aggressive_mode_ground_control_zone);
		PPos pPos = new PPos(formation.cur_width + num, formation.cur_height + num);
		if (rotated2.x < rotated.x + 0.5f * pPos.x && rotated2.x > rotated.x - 0.5f * pPos.x && rotated2.y < rotated.y + 0.5f * pPos.y)
		{
			return rotated2.y > rotated.y - 0.5f * pPos.y;
		}
		return false;
	}

	private void UpdateCombat()
	{
		if (battle == null || battle.stage != Battle.Stage.Ongoing)
		{
			SetEnemy(null);
			ranged_enemy = null;
			Stop();
			return;
		}
		UpdateEnemy();
		MoveTowardsEnemy();
		if (target == null)
		{
			force_melee = false;
		}
		if (ranged_enemy != null && (can_auto_fire || ranged_enemy == target) && CanShoot(ranged_enemy, max_dist: true) && !force_melee)
		{
			Attack(ranged_enemy, double_time: false);
		}
		else if (target != null && def.is_ranged && !fighting_target && !force_melee)
		{
			float num = ((!(target is Squad squad)) ? avg_troops_pos.Dist(target.position) : avg_troops_pos.Dist(squad.avg_troops_pos));
			if (salvos_left < 1 || num > salvo_def.min_shoot_range)
			{
				Attack(target, double_time, command: false, force_melee);
			}
		}
		if (enemy != null && is_fighting_target && (command == Command.Attack || command == Command.Charge))
		{
			SetCommand(Command.Fight, target);
			Stop();
		}
	}

	public float MaxStamina()
	{
		if (simulation == null)
		{
			return 1f;
		}
		if (simulation.unit != null)
		{
			return simulation.unit.stamina_modified();
		}
		return def.stamina_max;
	}

	public float GetStamina()
	{
		if (stamina == null)
		{
			return 0f;
		}
		return stamina.Get();
	}

	public void SetStamina(float val)
	{
		if (stamina != null)
		{
			stamina.Set(val);
		}
	}

	public void UpdateStamina()
	{
		if (stamina == null)
		{
			return;
		}
		float num = 0f;
		if (main_squad == null)
		{
			bool flag = movement.speed > def.move_speed;
			if (is_fighting)
			{
				num = def.stamina_rate_fight;
			}
			else if (IsMoving())
			{
				if (flag && GetStamina() <= 0f)
				{
					flag = false;
					SetDoubleTime(value: false, update_stamina: false);
				}
				num = ((!flag) ? def.stamina_rate_move : ((command != Command.Attack) ? def.stamina_rate_run : def.stamina_rate_charge));
			}
			else
			{
				num = def.stamina_rate_idle;
			}
			if (buffs != null && num > 0f)
			{
				float num2 = 0f;
				for (int i = 0; i < buffs.Count; i++)
				{
					SquadBuff squadBuff = buffs[i];
					num2 += squadBuff.GetStaminaRecovery();
				}
				num *= 1f + num2 / 100f;
			}
		}
		if (num != stamina.GetRate())
		{
			stamina.SetRate(num);
		}
	}

	public void RecalcPath(PPos start_pt)
	{
		recalc_path = true;
		recalc_start_pt = start_pt;
	}

	private void RecalcPath()
	{
		if (movement.path == null)
		{
			Stop();
			position = recalc_start_pt;
			return;
		}
		PPos dst_pt = movement.path.dst_pt;
		MapObject dst_obj = movement.path.dst_obj;
		float range = movement.path.range;
		bool flag = double_time;
		Point point = tgt_dir;
		Stop();
		position = recalc_start_pt;
		if (dst_obj != null)
		{
			if (target != null && (command == Command.Attack || command == Command.Charge))
			{
				Attack(target, flag, command: true, force_melee);
			}
			else
			{
				MoveTo(dst_obj, range, flag, point, disengage: false);
			}
		}
		else
		{
			MoveTo(dst_pt, range, flag, point, disengage: false, defeated_move: true);
		}
		recalc_path = false;
	}

	private void CheckChasingNeedsTeleport()
	{
		if (stance == Stance.Aggressive && movement?.path?.dst_obj is Squad squad && squad?.formation != null && formation != null)
		{
			PPos pPos = VisualPosition();
			PPos pPos2 = position;
			PPos pt = squad.position;
			float num = pPos.Dist(pt);
			float num2 = pPos2.Dist(pt);
			float aggressive_mode_ground_control_zone = formation.def.aggressive_mode_ground_control_zone;
			float num3 = ((squad.stance != Stance.Aggressive) ? squad.formation.def.defensive_stance_control_zone : squad.formation.def.aggressive_mode_ground_control_zone);
			if (num < num2 && num <= aggressive_mode_ground_control_zone + num3)
			{
				AdvanceTo(pPos, 0f, based_on_speed: false);
			}
		}
	}

	public override void OnUpdate()
	{
		if (recalc_path)
		{
			RecalcPath();
		}
		if (fighting_target_status_changed)
		{
			OnFightingTargetStatusChanged();
		}
		if (fighting_status_changed)
		{
			OnFightingStatusChanged();
		}
		UpdateAfter(0.5f);
		CheckChasingNeedsTeleport();
		CheckUnderFire();
		CheckFlee();
		CheckNeedsPack();
		CheckCharge();
		CheckDirtyBuffs();
		UpdateStamina();
		UpdateCombat();
		UpdateCommandQueue();
		if (battle != null && battle.winner != -1 && IsMoving())
		{
			Stop();
		}
		UpdateThreat();
		UpdateLastMoveTime();
		if (simulation.state != BattleSimulation.Squad.State.Retreating && simulation.state != BattleSimulation.Squad.State.Fled)
		{
			FindAndResolveSquadCollisions(4f, def.is_cavalry ? 2f : 3.5f);
			UpdateDisorganize();
		}
		if (!is_main_squad)
		{
			UpdatePathToMainSquad();
		}
		UpdateShootingSquads();
		ResetMeleeSquads();
	}

	public override void OnStopMoving()
	{
		base.OnStopMoving();
		if (ShootingWhileMoving())
		{
			SetRangedEnemy(null);
		}
	}

	private void UpdateDisorganize()
	{
		if (def.is_siege_eq)
		{
			return;
		}
		if (IsMoving())
		{
			simulation.disorganize = 0f;
			return;
		}
		List<Squad> list = battle.squads.Get(battle_side);
		float num = 0f;
		PPos pPos = position;
		if (position.Dist(avg_troops_pos) < formation.cur_radius * 2f)
		{
			pPos = avg_troops_pos;
		}
		float num2 = formation.cur_radius * 0.8f;
		foreach (Squad item in list)
		{
			if (item != this && simulation.state != BattleSimulation.Squad.State.Retreating && simulation.state != BattleSimulation.Squad.State.Fled && simulation.state != BattleSimulation.Squad.State.Dead && !item.def.is_siege_eq)
			{
				PPos pt = item.position;
				if (item.position.Dist(item.avg_troops_pos) < item.formation.cur_radius * 2f)
				{
					pt = item.avg_troops_pos;
				}
				float num3 = num2 + item.formation.cur_radius * 0.8f;
				float num4 = pPos.Dist(pt);
				if (!(num4 >= num3))
				{
					float num5 = num2 - (num3 - num4) * 0.5f;
					float num6 = 1f - num5 / num2;
					num += num6 * (float)item.simulation.NumTroops();
				}
			}
		}
		simulation.disorganize = num / (float)simulation.NumTroops();
	}

	public override bool ForceRefreshFollowPath()
	{
		if (movement?.path?.dst_obj != null && movement.path.dst_obj is Squad squad && squad.climbing != last_target_was_climbing)
		{
			last_target_was_climbing = squad.climbing;
			return true;
		}
		return base.ForceRefreshFollowPath();
	}

	private void UpdateLastMoveTime()
	{
		if (IsMoving())
		{
			time_from_last_move = 0f;
		}
		else
		{
			time_from_last_move += GetElapsedTime();
		}
	}

	private void UpdatePathToMainSquad()
	{
		if ((movement.path == null && movement.pf_path == null) || command == Command.Fight)
		{
			sub_squad_path_recalc_cd += 1f;
			if (sub_squad_path_recalc_cd >= sub_squad_path_recalc_max_cd_ticks)
			{
				sub_squad_path_recalc_cd = 0f;
				RecalcPathToMainSquad();
			}
		}
		else
		{
			sub_squad_path_recalc_cd = 0f;
		}
	}

	private void RecalcPathToMainSquad()
	{
		if (main_squad != null)
		{
			Squad squad = main_squad;
			Point point = main_squad.direction;
			Stop();
			PathData.DataPointers pointers = game.path_finding.data.pointers;
			pointers.BurstedTrace(position, squad.position, out var result);
			if (position.SqrDist(result) < 0.1f && position.paID != result.paID && pointers.PointInArea(position, result.paID))
			{
				position.paID = result.paID;
			}
			bool flag = main_squad.command == Command.Disengage;
			MoveTo(squad, 0f, double_time: true, point, is_fighting || flag);
		}
	}

	private void OnFightingTargetStatusChanged()
	{
		fighting_target_status_changed = false;
		if (target == null)
		{
			is_fighting_target = false;
		}
		else if (is_fighting_target)
		{
			fighting_status_changed = false;
			if (command != Command.Hold && command != Command.Disengage)
			{
				SetCommand(Command.Fight, target);
			}
		}
		else if (target != null)
		{
			fighting_status_changed = false;
			SetEnemy(null);
			Attack(target, double_time, command: false, force_melee);
		}
	}

	private void OnFightingStatusChanged()
	{
		fighting_status_changed = false;
		if (is_fighting)
		{
			if (command != Command.Disengage)
			{
				if (!is_main_squad && (command == Command.Charge || command == Command.Move))
				{
					SetCommand(Command.Disengage, target);
				}
				else if (target == null && command != Command.Hold)
				{
					SetCommand(Command.Fight);
				}
			}
		}
		else
		{
			if (target != null || command != Command.Fight)
			{
				return;
			}
			if (movement.path != null)
			{
				if (double_time)
				{
					SetCommand(Command.Charge);
				}
				else
				{
					SetCommand(Command.Move);
				}
			}
			else
			{
				SetCommand(Command.Hold);
			}
		}
	}

	public void AddMeleeSquad(Squad squad)
	{
		if (!melee_squads.Contains(squad))
		{
			melee_squads.Add(squad);
		}
	}

	private void ResetMeleeSquads()
	{
		previous_melee_squads.Clear();
		previous_melee_squads.AddRange(melee_squads);
		melee_squads.Clear();
	}

	public void AddShootingSquad(Squad squad)
	{
		if (enemy_shooting_squads.Contains(squad))
		{
			enemy_shooting_cooldowns[squad] = squad.def.shoot_interval + game.time.seconds;
			return;
		}
		enemy_shooting_squads.Add(squad);
		enemy_shooting_cooldowns.Add(squad, squad.def.shoot_interval + game.time.seconds);
	}

	private void UpdateShootingSquads()
	{
		for (int num = enemy_shooting_squads.Count - 1; num >= 0; num--)
		{
			Squad key = enemy_shooting_squads[num];
			if (game.time.seconds > enemy_shooting_cooldowns[key])
			{
				enemy_shooting_squads.RemoveAt(num);
				enemy_shooting_cooldowns.Remove(key);
			}
		}
	}

	private float CalcThreat(Squad squad, float multiplier)
	{
		return (float)squad.NumTroops() * multiplier;
	}

	private bool IsFlanked()
	{
		if (!(rear_threat > 30f) && !(left_threat > 30f))
		{
			return right_threat > 30f;
		}
		return true;
	}

	private void UpdateThreat()
	{
		if (IsDefeated() || battle.stage != Battle.Stage.Ongoing || battle.battle_map_finished)
		{
			return;
		}
		float threat_additional_side_dist = simulation.simulation.def.threat_additional_side_dist;
		float threat_additional_rear_dist = simulation.simulation.def.threat_additional_rear_dist;
		bool flag = IsFlanked();
		rear_threat = 0f;
		left_threat = 0f;
		right_threat = 0f;
		List<Squad> list = battle.squads.Get(1 - battle_side);
		List<Squad> list2 = battle.squads.Get(battle_side);
		float num = direction.Heading();
		PPos rotated = new PPos(formation.cur_height * 0.5f, formation.cur_width * 0.5f).GetRotated(0f - num);
		PPos rotated2 = new PPos((0f - formation.cur_height) * 0.5f, formation.cur_width * 0.5f).GetRotated(0f - num);
		float num2 = direction.GetNormalized().Dot(rotated.GetNormalized());
		float num3 = direction.GetNormalized().Dot(rotated2.GetNormalized());
		if (game?.path_finding == null || !game.path_finding.CheckValid())
		{
			return;
		}
		PathData.DataPointers pointers = game.path_finding.data.pointers;
		PPos result;
		foreach (Squad item in list)
		{
			if (!item.is_main_squad || item.is_fleeing || item.IsDefeated())
			{
				continue;
			}
			PPos pPos = item.position - position;
			float num4 = direction.GetNormalized().Dot(pPos.GetNormalized());
			if (!(num4 < num2))
			{
				continue;
			}
			PPos rotated3 = pPos.GetRotated(num);
			if (num4 < num3)
			{
				if (rotated3.x > 0f - formation.cur_radius - item.formation.cur_radius - threat_additional_rear_dist && pointers.BurstedTrace(position, item.position, out result, ignore_impassable_terrain: false, check_in_area: true, water_is_passable: false, movement.allowed_area_types, battle_side, is_inside_walls_or_on_walls))
				{
					float multiplier = ((rotated3.x > 0f - formation.cur_height - threat_additional_rear_dist * 0.5f) ? 1f : 0.5f);
					rear_threat += CalcThreat(item, multiplier);
				}
			}
			else if (rotated3.y < 0f)
			{
				if (rotated3.y > 0f - formation.cur_radius - item.formation.cur_radius - threat_additional_side_dist && pointers.BurstedTrace(position, item.position, out result, ignore_impassable_terrain: false, check_in_area: true, water_is_passable: false, movement.allowed_area_types, battle_side, is_inside_walls_or_on_walls))
				{
					float multiplier2 = ((rotated3.y > 0f - formation.cur_width - threat_additional_side_dist * 0.5f) ? 1f : 0.5f);
					right_threat += CalcThreat(item, multiplier2);
				}
			}
			else if (rotated3.y < formation.cur_radius + item.formation.cur_radius + threat_additional_side_dist && pointers.BurstedTrace(position, item.position, out result, ignore_impassable_terrain: false, check_in_area: true, water_is_passable: false, movement.allowed_area_types, battle_side, is_inside_walls_or_on_walls))
			{
				float multiplier3 = ((rotated3.y < formation.cur_width + threat_additional_side_dist * 0.5f) ? 1f : 0.5f);
				left_threat += CalcThreat(item, multiplier3);
			}
		}
		foreach (Squad item2 in list2)
		{
			if (item2 == this || !item2.is_main_squad || item2.is_fleeing || item2.IsDefeated())
			{
				continue;
			}
			PPos pPos2 = item2.position - position;
			float num5 = direction.GetNormalized().Dot(pPos2.GetNormalized());
			if (!(num5 < num2))
			{
				continue;
			}
			PPos rotated4 = pPos2.GetRotated(num);
			if (num5 < num3)
			{
				if (rotated4.x > 0f - formation.cur_radius - item2.formation.cur_radius - threat_additional_rear_dist && pointers.BurstedTrace(position, item2.position, out result, ignore_impassable_terrain: false, check_in_area: true, water_is_passable: false, movement.allowed_area_types, battle_side, is_inside_walls_or_on_walls))
				{
					float multiplier4 = ((rotated4.y > 0f - formation.cur_height - threat_additional_rear_dist * 0.5f) ? 1f : 0.5f);
					rear_threat -= CalcThreat(item2, multiplier4);
				}
			}
			else if (rotated4.y < 0f)
			{
				if (rotated4.y > 0f - formation.cur_radius - item2.formation.cur_radius - threat_additional_side_dist && pointers.BurstedTrace(position, item2.position, out result, ignore_impassable_terrain: false, check_in_area: true, water_is_passable: false, movement.allowed_area_types, battle_side, is_inside_walls_or_on_walls))
				{
					float multiplier5 = ((rotated4.x > 0f - formation.cur_width - threat_additional_side_dist * 0.5f) ? 1f : 0.5f);
					right_threat -= CalcThreat(item2, multiplier5);
				}
			}
			else if (rotated4.y < formation.cur_radius + item2.formation.cur_radius + threat_additional_side_dist && pointers.BurstedTrace(position, item2.position, out result, ignore_impassable_terrain: false, check_in_area: true, water_is_passable: false, movement.allowed_area_types, battle_side, is_inside_walls_or_on_walls))
			{
				float multiplier6 = ((rotated4.y < formation.cur_width + threat_additional_side_dist * 0.5f) ? 1f : 0.5f);
				left_threat -= CalcThreat(item2, multiplier6);
			}
		}
		if (IsFlanked() && !flag)
		{
			NotifyListeners("flanked");
		}
	}

	private void FindAndResolveSquadCollisions(float t = 4f, float t_mov = 2f)
	{
		if (!is_main_squad)
		{
			return;
		}
		float num = t;
		bool ignore_moving = true;
		bool ignore_fighting = true;
		if (def.is_siege_eq && movement.IsMoving())
		{
			num = t_mov;
			ignore_fighting = false;
		}
		CalcPos(out var pos, out var dir, num, num, based_on_speed: true);
		if (pos.paID > 0)
		{
			List<Squad> friendlySquadsOnPath = GetFriendlySquadsOnPath(pos, dir, 0f, ignore_moving: true, ignore_fighting: true, only_moving: false, ignore_PA: false);
			overlap_with_ally = friendlySquadsOnPath.Count > 0;
			return;
		}
		List<Squad> friendlySquadsOnPath2 = GetFriendlySquadsOnPath(pos, dir, 0f, ignore_moving, ignore_fighting);
		overlap_with_ally = friendlySquadsOnPath2.Count > 0;
		if (just_moved || !CanAvoid())
		{
			return;
		}
		foreach (Squad item in friendlySquadsOnPath2)
		{
			PPos shift = default(PPos);
			if (CalcAvoid(item, this, item.position, item.direction, position, direction, out shift, check_collisions: true))
			{
				PPos target_pos = position + shift;
				Avoid(target_pos, check_collisions: false);
			}
		}
	}

	private List<Squad> GetFriendlySquadsOnPath(PPos pos, PPos dir, float t = 0f, bool ignore_moving = true, bool ignore_fighting = true, bool only_moving = false, bool ignore_PA = true, bool only_fighting = false)
	{
		List<Squad> list = battle.squads.Get(battle_side);
		List<Squad> list2 = new List<Squad>();
		foreach (Squad item in list)
		{
			if (item == this || !item.is_main_squad || item.climbing || (item.position.paID > 0 && ignore_PA) || (item.is_fighting && ignore_fighting) || (!item.is_fighting && only_fighting) || item.target != null || ((item.movement.pf_path != null || item.movement.path != null) && ignore_moving))
			{
				continue;
			}
			if (t <= 0f)
			{
				if (IsColliding(this, item, pos, dir, item.position, item.direction))
				{
					list2.Add(item);
				}
				continue;
			}
			item.CalcPos(out var pos2, out var dir2, t, t, based_on_speed: true);
			if (IsColliding(this, item, pos, dir, pos2, dir2))
			{
				list2.Add(item);
			}
		}
		return list2;
	}

	private bool CalcAvoid(Squad squad, Squad other, PPos pos, PPos dir, PPos pos_other, PPos dir_other, out PPos shift, bool check_collisions = false, bool force_left = false, bool force_right = false, int iteration = 0)
	{
		float num = dir.Heading();
		float cur_width = other.formation.cur_width;
		float cur_height = other.formation.cur_height;
		float num2 = dir_other.Heading();
		PPos[] array = new PPos[4];
		PPos rotated = (pos_other - pos).GetRotated(num);
		array[0] = (new Point(cur_height * 0.5f, cur_width * 0.5f).GetRotated(0f - num2) + pos_other - pos).GetRotated(num);
		array[1] = (new Point((0f - cur_height) * 0.5f, cur_width * 0.5f).GetRotated(0f - num2) + pos_other - pos).GetRotated(num);
		array[2] = (new Point((0f - cur_height) * 0.5f, (0f - cur_width) * 0.5f).GetRotated(0f - num2) + pos_other - pos).GetRotated(num);
		array[3] = (new Point(cur_height * 0.5f, (0f - cur_width) * 0.5f).GetRotated(0f - num2) + pos_other - pos).GetRotated(num);
		float cur_width2 = squad.formation.cur_width;
		float cur_height2 = squad.formation.cur_height;
		PPos[] squad_verts = new PPos[4]
		{
			new Point(cur_height2 * 0.5f, cur_width2 * 0.5f),
			new Point((0f - cur_height2) * 0.5f, cur_width2 * 0.5f),
			new Point((0f - cur_height2) * 0.5f, (0f - cur_width2) * 0.5f),
			new Point(cur_height2 * 0.5f, (0f - cur_width2) * 0.5f)
		};
		shift = new PPos(0f, 0f, 0);
		List<Squad> squads = null;
		List<Squad> squads2 = null;
		List<Squad> squads3 = null;
		List<Squad> squads4 = null;
		PPos pPos = new PPos(0f, 0f, 0);
		PPos pPos2 = new PPos(0f, 0f, 0);
		PPos pPos3 = new PPos(0f, 0f, 0);
		PPos pPos4 = new PPos(0f, 0f, 0);
		if ((rotated.y > 0f && !force_left) || force_right)
		{
			PPos pPos5 = CalcAvoidRight(squad_verts, array, num);
			pPos = pPos5;
			if (!check_collisions)
			{
				shift = pPos5;
				return true;
			}
			if (!WillCollideAfterShift(pPos5, pos_other, dir_other, out squads))
			{
				shift = pPos5;
				return true;
			}
			pPos5 = CalcAvoidLeft(squad_verts, array, num);
			pPos2 = pPos5;
			if (!WillCollideAfterShift(pPos5, pos_other, dir_other, out squads2))
			{
				shift = pPos5;
				return true;
			}
		}
		else
		{
			PPos pPos6 = CalcAvoidLeft(squad_verts, array, num);
			pPos2 = pPos6;
			if (!check_collisions)
			{
				shift = pPos6;
				return true;
			}
			if (!WillCollideAfterShift(pPos6, pos_other, dir_other, out squads2))
			{
				shift = pPos6;
				return true;
			}
			pPos6 = CalcAvoidRight(squad_verts, array, num);
			pPos = pPos6;
			if (!WillCollideAfterShift(pPos6, pos_other, dir_other, out squads))
			{
				shift = pPos6;
				return true;
			}
		}
		if (rotated.x > 0f)
		{
			PPos pPos7 = CalcAvoidFront(squad_verts, array, num);
			pPos3 = pPos7;
			if (!check_collisions)
			{
				shift = pPos7;
				return true;
			}
			if (!WillCollideAfterShift(pPos7, pos_other, dir_other, out squads3))
			{
				shift = pPos7;
				return true;
			}
			pPos7 = CalcAvoidBack(squad_verts, array, num);
			pPos4 = pPos7;
			if (!WillCollideAfterShift(pPos7, pos_other, dir_other, out squads4))
			{
				shift = pPos7;
				return true;
			}
		}
		else
		{
			PPos pPos8 = CalcAvoidBack(squad_verts, array, num);
			pPos4 = pPos8;
			if (!check_collisions)
			{
				shift = pPos8;
				return true;
			}
			if (!WillCollideAfterShift(pPos8, pos_other, dir_other, out squads4))
			{
				shift = pPos8;
				return true;
			}
			pPos8 = CalcAvoidFront(squad_verts, array, num);
			pPos3 = pPos8;
			if (!WillCollideAfterShift(pPos8, pos_other, dir_other, out squads3))
			{
				shift = pPos8;
				return true;
			}
		}
		if (++iteration < 3)
		{
			if ((rotated.y > 0f && !force_left) || force_right)
			{
				bool flag = true;
				for (int i = 0; i < squads.Count; i++)
				{
					Squad squad2 = squads[i];
					if (!CalcAvoid(other, squad2, other.position, other.direction, squad2.position, squad2.direction, out var _, check_collisions: true, force_left: false, force_right: false, iteration))
					{
						flag = false;
						break;
					}
				}
				if (flag)
				{
					shift = pPos;
					return true;
				}
				flag = true;
				for (int j = 0; j < squads2.Count; j++)
				{
					Squad squad3 = squads2[j];
					if (!CalcAvoid(other, squad3, other.position, other.direction, squad3.position, squad3.direction, out var _, check_collisions: true, force_left: false, force_right: false, iteration))
					{
						flag = false;
						break;
					}
				}
				if (flag)
				{
					shift = pPos2;
					return true;
				}
			}
			else
			{
				bool flag2 = true;
				for (int k = 0; k < squads2.Count; k++)
				{
					Squad squad4 = squads2[k];
					if (!CalcAvoid(other, squad4, other.position, other.direction, squad4.position, squad4.direction, out var _, check_collisions: true, force_left: false, force_right: false, iteration))
					{
						flag2 = false;
						break;
					}
				}
				if (flag2)
				{
					shift = pPos2;
					return true;
				}
				flag2 = true;
				for (int l = 0; l < squads.Count; l++)
				{
					Squad squad5 = squads[l];
					if (!CalcAvoid(other, squad5, other.position, other.direction, squad5.position, squad5.direction, out var _, check_collisions: true, force_left: false, force_right: false, iteration))
					{
						flag2 = false;
						break;
					}
				}
				if (flag2)
				{
					shift = pPos;
					return true;
				}
			}
			if (rotated.x > 0f)
			{
				bool flag3 = true;
				for (int m = 0; m < squads3.Count; m++)
				{
					Squad squad6 = squads3[m];
					if (!CalcAvoid(other, squad6, other.position, other.direction, squad6.position, squad6.direction, out var _, check_collisions: true, force_left: false, force_right: false, iteration))
					{
						flag3 = false;
						break;
					}
				}
				if (flag3)
				{
					shift = pPos3;
					return true;
				}
				flag3 = true;
				for (int n = 0; n < squads4.Count; n++)
				{
					Squad squad7 = squads4[n];
					if (!CalcAvoid(other, squad7, other.position, other.direction, squad7.position, squad7.direction, out var _, check_collisions: true, force_left: false, force_right: false, iteration))
					{
						flag3 = false;
						break;
					}
				}
				if (flag3)
				{
					shift = pPos4;
					return true;
				}
			}
			else
			{
				bool flag4 = true;
				for (int num3 = 0; num3 < squads4.Count; num3++)
				{
					Squad squad8 = squads4[num3];
					if (!CalcAvoid(other, squad8, other.position, other.direction, squad8.position, squad8.direction, out var _, check_collisions: true, force_left: false, force_right: false, iteration))
					{
						flag4 = false;
						break;
					}
				}
				if (flag4)
				{
					shift = pPos4;
					return true;
				}
				flag4 = true;
				for (int num4 = 0; num4 < squads3.Count; num4++)
				{
					Squad squad9 = squads3[num4];
					if (!CalcAvoid(other, squad9, other.position, other.direction, squad9.position, squad9.direction, out var _, check_collisions: true, force_left: false, force_right: false, iteration))
					{
						flag4 = false;
						break;
					}
				}
				if (flag4)
				{
					shift = pPos3;
					return true;
				}
			}
		}
		return false;
	}

	private PPos CalcAvoidRight(PPos[] squad_verts, PPos[] other_verts, float squad_angle)
	{
		float y = other_verts[0].y;
		for (int i = 1; i < 4; i++)
		{
			if (other_verts[i].y < y)
			{
				y = other_verts[i].y;
			}
		}
		float y2 = squad_verts[0].y;
		for (int j = 1; j < 4; j++)
		{
			if (squad_verts[j].y > y2)
			{
				y2 = squad_verts[j].y;
			}
		}
		return new PPos(0f, y2 - y).GetRotated(0f - squad_angle);
	}

	private PPos CalcAvoidLeft(PPos[] squad_verts, PPos[] other_verts, float squad_angle)
	{
		float y = other_verts[0].y;
		for (int i = 1; i < 4; i++)
		{
			if (other_verts[i].y > y)
			{
				y = other_verts[i].y;
			}
		}
		float y2 = squad_verts[0].y;
		for (int j = 1; j < 4; j++)
		{
			if (squad_verts[j].y < y2)
			{
				y2 = squad_verts[j].y;
			}
		}
		return new PPos(0f, y2 - y).GetRotated(0f - squad_angle);
	}

	private PPos CalcAvoidFront(PPos[] squad_verts, PPos[] other_verts, float squad_angle)
	{
		float x = other_verts[0].x;
		for (int i = 1; i < 4; i++)
		{
			if (other_verts[i].x < x)
			{
				x = other_verts[i].x;
			}
		}
		float x2 = squad_verts[0].x;
		for (int j = 1; j < 4; j++)
		{
			if (squad_verts[j].x > x2)
			{
				x2 = squad_verts[j].x;
			}
		}
		return new PPos(x2 - x).GetRotated(0f - squad_angle);
	}

	private PPos CalcAvoidBack(PPos[] squad_verts, PPos[] other_verts, float squad_angle)
	{
		float x = other_verts[0].x;
		for (int i = 1; i < 4; i++)
		{
			if (other_verts[i].x > x)
			{
				x = other_verts[i].x;
			}
		}
		float x2 = squad_verts[0].x;
		for (int j = 1; j < 4; j++)
		{
			if (squad_verts[j].x < x2)
			{
				x2 = squad_verts[j].x;
			}
		}
		return new PPos(x2 - x).GetRotated(0f - squad_angle);
	}

	private bool WillCollideAfterShift(PPos shift, PPos pos, PPos dir, out List<Squad> squads)
	{
		squads = null;
		if (game?.path_finding == null || !game.path_finding.CheckValid())
		{
			return false;
		}
		PPos result = pos + shift;
		PathData.DataPointers pointers = game.path_finding.data.pointers;
		squads = new List<Squad>();
		if (pointers.BurstedTrace(position, result, out result))
		{
			squads = GetFriendlySquadsOnPath(pos + shift, dir);
			return squads.Count > 0;
		}
		return true;
	}

	private void Avoid(PPos target_pos, bool check_collisions = true)
	{
		if (movement.pf_path != null)
		{
			return;
		}
		if (check_collisions)
		{
			if (game?.path_finding == null || !game.path_finding.CheckValid())
			{
				return;
			}
			PathData.DataPointers pointers = game.path_finding.data.pointers;
			if (!pointers.BurstedTrace(position, target_pos, out target_pos, ignore_impassable_terrain: false, check_in_area: true, water_is_passable: false, movement.allowed_non_ladder_areas, battle_side, is_inside_walls_or_on_walls))
			{
				PPos pPos = target_pos - position;
				target_pos -= pPos.GetNormalized() * formation.cur_radius * 0.5f;
				if (pPos.Dot(target_pos - position) < 0f)
				{
					return;
				}
			}
		}
		MoveTo(target_pos, 0f, double_time: false, direction, disengage: false, defeated_move: false, avoiding_move: true);
	}

	private void AvoidWhileMoving(PPos shift_vec, float offset, float max_dist, int length = 20)
	{
		if (movement == null || movement.path == null || climbing || original_path_segments == null)
		{
			return;
		}
		int num = (int)(offset - (float)length * 0.5f);
		int num2 = (int)(offset + (float)length * 0.5f);
		int num3 = movement.path.segment_idx + (int)offset - length / 2;
		bool flag = false;
		if (movement.path.segment_idx + num2 > movement.path.segments.Count - 1)
		{
			return;
		}
		List<PPos> list = new List<PPos>();
		if (num3 <= movement.path.segment_idx)
		{
			num = 1;
		}
		int num4 = num2 - num;
		for (int i = num; i <= num2; i++)
		{
			PPos pt = movement.path.segments[movement.path.segment_idx + i].pt;
			float num5 = (0.5f - Math.Abs(0.5f * (float)num4 - (float)(i - num)) / (float)num4) * 2f;
			if (num5 > 1f)
			{
				num5 = 1f;
			}
			if (num5 < -1f)
			{
				num5 = -1f;
			}
			PPos pPos = pt + shift_vec * num5;
			PPos pt2 = original_path_segments[movement.path.segment_idx + i].pt;
			PPos pPos2 = pPos - pt2;
			if (pPos2.Length() > max_dist)
			{
				pPos = pt2 + pPos2.GetNormalized() * max_dist;
			}
			list.Add(pPos);
		}
		float r = formation.cur_width / 2f;
		if (!AvoidancePathValid(list, num + movement.path.segment_idx, r))
		{
			return;
		}
		for (int j = num; j < num2; j++)
		{
			PPos pPos3 = list[j - num];
			if (!(movement.path.segments[movement.path.segment_idx + j].pt == pPos3))
			{
				if (!movement.path.ModifyPathAt(movement.path.segment_idx + j, pPos3, r, check_traces: false))
				{
					break;
				}
				flag = true;
			}
		}
		if (flag)
		{
			NotifyListeners("path_changed");
		}
	}

	private bool AvoidancePathValid(List<PPos> segments_pos, int offset, float r)
	{
		if (game == null || game.path_finding == null || movement.path.segments.Count <= offset + segments_pos.Count)
		{
			return false;
		}
		PathData data = game.path_finding.data;
		for (int i = 0; i < segments_pos.Count; i++)
		{
			if (movement.path.segments[offset + i].pt.paID != 0)
			{
				return false;
			}
		}
		if (data.pointers.Trace(movement.path.segments[offset - 1].pt, segments_pos[0], out var result))
		{
			if (result.paID != 0)
			{
				return false;
			}
			if (!data.Trace(movement.path.segments[offset - 1].pt, segments_pos[0], r))
			{
				return false;
			}
			for (int j = 0; j < segments_pos.Count - 1; j++)
			{
				if (data.pointers.Trace(segments_pos[j], segments_pos[j + 1], out result) && result.paID != 0)
				{
					return false;
				}
				if (!data.Trace(segments_pos[j], segments_pos[j + 1], r))
				{
					return false;
				}
			}
			if (offset + segments_pos.Count < movement.path.segments.Count && !data.Trace(segments_pos[segments_pos.Count - 1], movement.path.segments[offset + segments_pos.Count].pt, r))
			{
				return false;
			}
			return true;
		}
		return false;
	}

	public static bool IsColliding(Squad squad, Squad other, PPos pos, PPos dir, PPos pos_other, PPos dir_other)
	{
		float num = squad.formation.cur_radius + other.formation.cur_radius;
		if (pos.SqrDist(pos_other) >= num * num)
		{
			return false;
		}
		if (SquadContainsPoint(other, pos, pos_other, dir_other) || SquadContainsPoint(squad, pos_other, pos, dir))
		{
			return true;
		}
		float num2 = squad.formation.cur_width - 4f * squad.def.radius;
		float num3 = squad.formation.cur_height - 4f * squad.def.radius;
		if (squad.def.is_siege_eq && other.def.is_siege_eq && squad.NumTroops() == 1)
		{
			num2 = squad.def.radius;
			num3 = squad.def.radius;
		}
		float num4 = dir.Heading();
		Point[] array = new Point[4]
		{
			new Point(num3 * 0.5f, num2 * 0.5f).GetRotated(0f - num4) + pos,
			new Point((0f - num3) * 0.5f, num2 * 0.5f).GetRotated(0f - num4) + pos,
			new Point((0f - num3) * 0.5f, (0f - num2) * 0.5f).GetRotated(0f - num4) + pos,
			new Point(num3 * 0.5f, (0f - num2) * 0.5f).GetRotated(0f - num4) + pos
		};
		float num5 = other.formation.cur_width - 4f * other.def.radius;
		float num6 = other.formation.cur_height - 4f * other.def.radius;
		if (squad.def.is_siege_eq && other.def.is_siege_eq && other.NumTroops() == 1)
		{
			num5 = other.def.radius;
			num6 = other.def.radius;
		}
		float num7 = dir_other.Heading();
		Point[] array2 = new Point[4]
		{
			new Point(num6 * 0.5f, num5 * 0.5f).GetRotated(0f - num7) + pos_other,
			new Point((0f - num6) * 0.5f, num5 * 0.5f).GetRotated(0f - num7) + pos_other,
			new Point((0f - num6) * 0.5f, (0f - num5) * 0.5f).GetRotated(0f - num7) + pos_other,
			new Point(num6 * 0.5f, (0f - num5) * 0.5f).GetRotated(0f - num7) + pos_other
		};
		for (int i = 0; i < 4; i++)
		{
			int num8 = (i + 1) % 4;
			Point point = array[num8] - array[i];
			Point point2 = new Point(0f - point.y, point.x);
			bool flag = point2.Dot(array2[0] - array[i]) > 0f;
			bool flag2 = false;
			for (int j = 1; j < 4; j++)
			{
				if (flag2 = point2.Dot(array2[j] - array[i]) > 0f != flag)
				{
					break;
				}
			}
			if (!flag2)
			{
				int num9 = (i + 2) % 4;
				int num10 = (i + 3) % 4;
				if (point2.Dot(array[num9] - array[i]) > 0f != flag && point2.Dot(array[num10] - array[i]) > 0f != flag)
				{
					return false;
				}
			}
		}
		for (int k = 0; k < 4; k++)
		{
			int num11 = (k + 1) % 4;
			Point point3 = array2[num11] - array2[k];
			Point point4 = new Point(0f - point3.y, point3.x);
			bool flag3 = point4.Dot(array[0] - array2[k]) > 0f;
			bool flag4 = false;
			for (int l = 1; l < 4; l++)
			{
				if (flag4 = point4.Dot(array[l] - array2[k]) > 0f != flag3)
				{
					break;
				}
			}
			if (!flag4)
			{
				int num12 = (k + 2) % 4;
				int num13 = (k + 3) % 4;
				if (point4.Dot(array2[num12] - array2[k]) > 0f != flag3 && point4.Dot(array2[num13] - array2[k]) > 0f != flag3)
				{
					return false;
				}
			}
		}
		return true;
	}

	public static bool SquadContainsPoint(Squad squad, PPos p, PPos pos, Point dir)
	{
		PPos rotated = pos.GetRotated(dir.Heading());
		p = p.GetRotated(dir.Heading());
		float cur_height = squad.formation.cur_height;
		float cur_width = squad.formation.cur_width;
		if (p.x < rotated.x + 0.5f * cur_height && p.x > rotated.x - 0.5f * cur_height && p.y < rotated.y + 0.5f * cur_width)
		{
			return p.y > rotated.y - 0.5f * cur_width;
		}
		return false;
	}

	public override bool CanReserve()
	{
		bool flag = is_main_squad;
		if (_can_reserve && !flag)
		{
			ClearReserved();
		}
		_can_reserve = flag;
		return flag;
	}

	public override float GetRadius()
	{
		Formation formation = this.formation;
		if (formation == null)
		{
			return 0f;
		}
		return formation.cur_width / 2f;
	}

	public override PPos VisualPosition()
	{
		return avg_troops_pos;
	}

	public override bool AvoidAlreadyReservedPosition(MapObject other)
	{
		if (!(other is Squad squad))
		{
			return base.AvoidAlreadyReservedPosition(other);
		}
		if (squad.fighting)
		{
			return true;
		}
		return base.AvoidAlreadyReservedPosition(other);
	}

	public override bool ForceReserveCheck(MapObject other)
	{
		if (!(other is Squad squad))
		{
			return false;
		}
		if (!fighting || squad.fighting)
		{
			return other.ForceReservePush(this);
		}
		return true;
	}

	public override bool ForceReservePush(MapObject other)
	{
		if (!(other is Squad squad))
		{
			return false;
		}
		if (movement.IsMoving() && !squad.IsMoving())
		{
			return !squad.fighting;
		}
		return false;
	}

	public override bool IgnoreCollision(MapObject other)
	{
		if (base.IgnoreCollision(other))
		{
			return true;
		}
		if (!(other is Squad squad))
		{
			return true;
		}
		if (squad.battle_side != battle_side)
		{
			return true;
		}
		if (squad.main_squad != null)
		{
			return true;
		}
		if (!squad.fighting && squad.target == null)
		{
			return true;
		}
		return false;
	}

	private static List<int> CheckSquadNeedsLadder(Squad squad)
	{
		if (squad == null)
		{
			return null;
		}
		List<int> list = new List<int>();
		if (squad.climbing && squad.cur_ladder.Area_valid())
		{
			list.Add(squad.cur_ladder_paid);
		}
		if (squad?.movement?.path?.segments == null)
		{
			return list;
		}
		int num = -1;
		for (int i = squad.movement.path.segment_idx; i < squad.movement.path.segments.Count; i++)
		{
			Path.Segment segment = squad.movement.path.segments[i];
			if (segment.pt.paID > 0 && segment.pt.paID != num)
			{
				num = segment.pt.paID;
				PathData.PassableArea pA = squad.game.path_finding.data.pointers.GetPA(segment.pt.paID - 1);
				if (pA.enabled && pA.type == PathData.PassableArea.Type.Ladder)
				{
					list.Add(segment.pt.paID);
				}
			}
		}
		return list;
	}

	public void StopClimb()
	{
		if (cur_ladder != null)
		{
			cur_ladder = null;
			NotifyVisuals("stop_climb");
		}
	}

	public void CheckNeedsLadder()
	{
		List<Ladder> list = new List<Ladder>();
		List<int> list2 = CheckSquadNeedsLadder(this);
		foreach (KeyValuePair<int, Ladder> ladder in ladders)
		{
			if (!list.Contains(ladder.Value) && (list2 == null || !list2.Contains(ladder.Key)))
			{
				list.Add(ladder.Value);
			}
		}
		for (int i = 0; i < list.Count; i++)
		{
			list[i].DelSquad(this);
		}
		if (list2 == null || list2.Count == 0)
		{
			return;
		}
		for (int j = 0; j < list2.Count; j++)
		{
			int num = list2[j];
			if (battle.ladders == null || !battle.ladders.TryGetValue(num, out var value))
			{
				PathData.PassableArea pA = battle.batte_view_game.path_finding.data.pointers.GetPA(num - 1);
				value = new Ladder(battle, num, pA.Center().xz);
			}
			value.AddSquad(this);
		}
	}

	public void StartPack()
	{
		SetEnemy(null);
		SetRangedEnemy(null);
		CheckNeedsPack(manual: true);
		SetCommand(Command.Hold);
	}

	public void CheckNeedsPack(bool manual = false)
	{
		if (!def.is_siege_eq || def.unpack_time <= 0f)
		{
			return;
		}
		bool flag = is_packed;
		float num = 0f;
		if (((manual && !flag) || pack_progress.GetRate() == -1f || (movement.IsMoving() && (ranged_enemy == null || !CanShoot(ranged_enemy, max_dist: true)))) && !is_packed)
		{
			num = -1f;
		}
		if ((manual && flag) || pack_progress.GetRate() == 1f || (is_packed && ranged_enemy != null && CanShoot(ranged_enemy, max_dist: true)))
		{
			num = 1f;
			Stop();
		}
		if (pack_progress.Get() >= def.unpack_time && num > 0f)
		{
			is_packed = false;
			num = 0f;
		}
		else if (pack_progress.Get() <= 0f && num < 0f)
		{
			is_packed = true;
			num = 0f;
		}
		if (num != pack_progress.GetRate())
		{
			pack_progress.SetRate(num);
			if (num != 0f)
			{
				NotifyVisuals("started_packing");
				NotifyListeners("started_packing");
			}
			else
			{
				NotifyVisuals("finished_packing");
				NotifyListeners("finished_packing");
			}
		}
		if (flag != is_packed)
		{
			NotifyVisuals("refresh_drawers");
		}
	}

	public bool CanBePacked()
	{
		return def.unpack_time >= 0f;
	}

	public bool IsPacking()
	{
		return PackProgressNormalized() >= 0f;
	}

	public float PackProgressNormalized()
	{
		if (pack_progress == null || pack_progress.GetRate() == 0f)
		{
			return -1f;
		}
		if (is_packed)
		{
			return pack_progress.Get() / pack_progress.GetMax();
		}
		return 1f - pack_progress.Get() / pack_progress.GetMax();
	}

	public void CheckCharge()
	{
		bool flag = IsMoving() && double_time && command != Command.Disengage;
		bool num = charge_progress.Get() >= charge_progress.GetMax();
		float rate = -1f;
		if (flag)
		{
			rate = 1f;
		}
		charge_progress.SetRate(rate);
		float rate2 = 0f;
		if (num)
		{
			charge_remainder.Set(charge_remainder.GetMax());
		}
		if (HasCharge() && !flag)
		{
			rate2 = -1f;
			charge_progress.Set(0f);
		}
		charge_remainder.SetRate(rate2);
	}

	public bool HasCharge()
	{
		return charge_remainder.Get() > 0f;
	}

	public bool CanAvoid()
	{
		if (can_avoid && command != Command.Shoot)
		{
			return !def.is_siege_eq;
		}
		return false;
	}

	public bool CanShootInTrees()
	{
		if (trees_buff != null && def.is_siege_eq && def.is_ranged)
		{
			return !trees_buff.enabled;
		}
		return true;
	}

	public void ClearCommandQueue()
	{
		command_queue.Clear();
		cur_command = null;
	}

	public void AddToCommandQueue(PPos target, Point direction, float range = 0f, bool defeated_move = false, bool force_no_threat = false, bool disengage = false, bool double_time = false, bool use_current_setting = false)
	{
		command_queue.Add(new MoveCommand
		{
			target_pos = target,
			direction = direction,
			defeated_move = defeated_move,
			force_no_threat = force_no_threat,
			disengage = disengage,
			double_time = double_time,
			use_current_setting = use_current_setting
		});
		UpdateCommandQueue();
	}

	public void AddToCommandQueue(MapObject target, bool attack, Point direction, float range = 0f, bool force_no_threat = false, bool force_melee = false, bool double_time = false, bool use_current_setting = false)
	{
		if (target == null || cur_command?.target_obj == null || target != cur_command.target_obj || command_queue.Count != 0)
		{
			command_queue.Add(new MoveCommand
			{
				target_obj = target,
				attack = attack,
				direction = direction,
				force_no_threat = force_no_threat,
				force_melee = force_melee,
				double_time = double_time,
				use_current_setting = use_current_setting
			});
			UpdateCommandQueue();
		}
	}

	private void FinishCurCommand()
	{
		cur_command = null;
		last_shot_target = null;
	}

	public void UpdateCommandQueue()
	{
		if (command_queue.Count == 0)
		{
			return;
		}
		if (cur_command != null)
		{
			bool flag = false;
			if (cur_command.attack)
			{
				bool flag2 = false;
				if (cur_command.target_obj is Squad squad)
				{
					flag2 = squad.IsDefeated();
				}
				if (cur_command.target_obj is Fortification fortification)
				{
					flag2 = fortification.IsDefeated();
				}
				if (flag2 || (is_fighting_target && enemy == cur_command.target_obj) || last_shot_target == cur_command.target_obj)
				{
					flag = true;
				}
			}
			else
			{
				Path path = movement.path;
				if (path == null)
				{
					PPos pPos = cur_command.target_pos;
					if (cur_command.target_obj != null)
					{
						pPos = cur_command.target_obj.VisualPosition();
					}
					if (position.InRange(pPos, cur_command.range) && PathFinding.CheckReachable(game.path_finding.data, position, pPos))
					{
						flag = true;
					}
				}
				else if (path.t > path.path_len - 10f)
				{
					flag = true;
				}
			}
			if (!flag)
			{
				if (!movement.IsMoving())
				{
					ExecuteCommand();
				}
				return;
			}
			FinishCurCommand();
		}
		MoveCommand moveCommand = command_queue[0];
		command_queue.RemoveAt(0);
		cur_command = moveCommand;
		ExecuteCommand();
	}

	private void ExecuteCommand()
	{
		bool flag = (cur_command.use_current_setting ? double_time : cur_command.double_time);
		bool disengage = (cur_command.use_current_setting ? is_fighting : cur_command.disengage);
		bool flag2 = command_queue.Count == 0;
		if (cur_command.target_obj != null)
		{
			if (cur_command.attack)
			{
				SetTarget(cur_command.target_obj);
				Attack(cur_command.target_obj, flag, command: true, cur_command.force_melee, 0f, important: true, add_to_command_queue: false, from_command_queue: true, cur_command.force_no_threat);
			}
			else
			{
				MoveTo(cur_command.target_obj, cur_command.range, flag, flag2 ? cur_command.direction : (cur_command.target_obj.VisualPosition() - position).pos.GetNormalized(), disengage, defeated_move: false, ignore_reserve: false, important: true, add_to_command_queue: false, from_command_queue: true, cur_command.force_no_threat);
			}
		}
		else
		{
			MoveTo(cur_command.target_pos, cur_command.range, flag, flag2 ? cur_command.direction : (cur_command.target_pos - position).pos.GetNormalized(), disengage, cur_command.defeated_move, avoiding_move: false, returning_move: false, ignore_reserve: false, -1f, important: true, add_to_command_queue: false, from_command_queue: true, cur_command.force_no_threat);
		}
	}
}
