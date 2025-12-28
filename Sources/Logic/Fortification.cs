using System;
using System.Collections.Generic;

namespace Logic;

public class Fortification : MapObject
{
	public enum Type
	{
		Wall,
		Gate,
		Bridge,
		Tower,
		COUNT
	}

	public class Def : Logic.Def
	{
		public Type type;

		public DT.Field max_health;

		public DT.Field max_secondary_health;

		public DT.Field starting_health;

		public DT.Field CTH;

		public float shoot_interval = 10f;

		public string salvo_def;

		public DT.Field num_arrows;

		public string AttackingMeleeSound;

		public string DestroyedWoodSound;

		public string DestroyedStoneSound;

		public string DestroyedCombinedSound;

		public string GateDestroyedWoodSound;

		public string GateDestroyedMetalSound;

		public float chance_target_closest_archer;

		public float chance_target_closest_cavalry;

		public float chance_ignore_marshal;

		public float chance_target_already_targetted_squad = 50f;

		public float chance_ignore_cavalry;

		public bool can_be_attacked = true;

		public override bool Load(Game game)
		{
			DT.Field field = base.field;
			if (!Enum.TryParse<Type>(field.GetString("type"), out type))
			{
				type = Type.COUNT;
			}
			max_health = field.FindChild("max_health");
			max_secondary_health = field.FindChild("max_secondary_health");
			starting_health = field.FindChild("starting_health");
			CTH = base.field.FindChild("CTH");
			shoot_interval = base.field.GetFloat("shoot_interval", null, shoot_interval);
			salvo_def = base.field.GetString("salvo_def", null, salvo_def);
			num_arrows = base.field.FindChild("num_arrows");
			can_be_attacked = base.field.GetBool("can_be_attacked", null, can_be_attacked);
			AttackingMeleeSound = field.GetString("attacking_melee_sound");
			DestroyedWoodSound = field.GetString("destroyed_wood_sound");
			DestroyedStoneSound = field.GetString("destroyed_stone_sound");
			DestroyedCombinedSound = field.GetString("destroyed_combined_sound");
			GateDestroyedWoodSound = field.GetString("gate_destroyed_wood_sound");
			GateDestroyedMetalSound = field.GetString("gate_destroyed_metal_sound");
			chance_target_closest_archer = field.GetFloat("chance_target_closest_archer", null, chance_target_closest_archer);
			chance_target_closest_cavalry = field.GetFloat("chance_target_closest_cavalry", null, chance_target_closest_cavalry);
			chance_ignore_marshal = field.GetFloat("chance_ignore_marshal", null, chance_ignore_marshal);
			chance_target_already_targetted_squad = field.GetFloat("chance_target_already_targetted_squad", null, chance_target_already_targetted_squad);
			chance_ignore_cavalry = field.GetFloat("chance_ignore_cavalry", null, chance_ignore_cavalry);
			return true;
		}
	}

	public class ShootComponent : Component
	{
		public SalvoData.Def salvo_def;

		public bool is_shooting;

		public Fortification fortification => obj as Fortification;

		public ShootComponent(Fortification fortification)
			: base(fortification)
		{
			salvo_def = base.game.defs.Get<SalvoData.Def>(fortification.def.salvo_def);
		}

		public override void OnStart()
		{
			base.OnStart();
			UpdateAfter(fortification.def.shoot_interval);
		}

		public Squad PickTarget()
		{
			List<Squad> list = fortification.battle.squads.Get(1 - fortification.battle_side);
			Squad result = null;
			float num = fortification.shoot_comp.salvo_def.max_shoot_range;
			for (int i = 0; i < list.Count; i++)
			{
				Squad squad = list[i];
				if (squad.IsDefeated())
				{
					continue;
				}
				float num2 = squad.avg_troops_pos.Dist(fortification.position);
				if (num2 > salvo_def.min_shoot_range && num2 < num)
				{
					obj.NotifyListeners("check_salvo_passability", squad);
					if (fortification.arrow_path_is_clear)
					{
						num = num2;
						result = squad;
					}
				}
			}
			return result;
		}

		public void Shoot(Squad target)
		{
			obj.NotifyListeners("shoot", target);
		}

		public override void OnUpdate()
		{
			base.OnUpdate();
			if (!fortification.IsDefeated())
			{
				Squad squad = PickTarget();
				if (squad != null)
				{
					Shoot(squad);
				}
				is_shooting = squad != null;
				UpdateAfter(fortification.def.shoot_interval);
			}
		}
	}

	public float max_health;

	public float max_gate_health;

	public float health = 2000f;

	public MapObject owner;

	public Def def;

	public List<Squad> cur_attackers = new List<Squad>();

	public Squad last_attacker;

	public ShootComponent shoot_comp;

	public int battle_side;

	public Battle battle;

	public PassableGate gate;

	public CapturePoint capture_point;

	private int original_battle_side;

	public float CTH_modified;

	public int num_arrows;

	public List<int> paids = new List<int>();

	public PPos attack_position_inside_wall;

	public PPos attack_position_outside_wall;

	public bool arrow_path_is_clear;

	public Fortification(Battle battle, PPos pos, MapObject owner, int battle_side, Def def)
		: base(battle.batte_view_game, pos, owner.GetKingdom().id)
	{
		this.battle = battle;
		this.def = def;
		this.owner = owner;
		original_battle_side = battle_side;
		if (battle.fortifications == null)
		{
			battle.fortifications = new List<Fortification>();
		}
		if (battle.towers == null)
		{
			battle.towers = new List<Fortification>();
		}
		battle.fortifications.Add(this);
		if (def.type == Type.Tower)
		{
			battle.towers.Add(this);
		}
		if (def.CTH != null)
		{
			CTH_modified = def.CTH.Float(this);
		}
		if (def.num_arrows != null)
		{
			num_arrows = def.num_arrows.Int(this);
		}
		if (def.type == Type.Tower)
		{
			shoot_comp = new ShootComponent(this);
		}
		Reset();
	}

	public override IRelationCheck GetStanceObj()
	{
		return GetKingdom();
	}

	public bool IsDefeated()
	{
		return health <= 0f;
	}

	public void Reset()
	{
		max_health = def.max_health.Float(this);
		max_gate_health = def.max_secondary_health.Float(this);
		health = def.starting_health.Float(this);
		if (gate != null)
		{
			gate.health = max_gate_health;
		}
		battle_side = original_battle_side;
		if (capture_point != null)
		{
			capture_point.SetBattleSide(battle_side);
		}
		NotifyVisuals("reset");
	}

	public void SetGateHealth(float val)
	{
		gate?.SetHealth(val);
		NotifyVisuals("reset");
	}

	protected override void OnStart()
	{
		base.OnStart();
	}

	public float Threat()
	{
		if (shoot_comp == null)
		{
			return 0f;
		}
		return CTH_modified / 60f + def.shoot_interval / 10f;
	}

	public override Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		switch (key)
		{
		case "realm":
			return battle?.settlement?.GetRealm();
		case "siege_defense":
			return battle.siege_defense;
		case "max_siege_defense":
			if (battle.settlement?.GetRealm() == null)
			{
				return 0;
			}
			return battle.settlement.GetRealm().GetStat(Stats.rs_siege_defense);
		default:
			return base.GetVar(key, vars, as_value);
		}
	}
}
