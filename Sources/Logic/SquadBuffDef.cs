using System;
using System.Collections.Generic;

namespace Logic;

public class SquadBuff : Component, IVars
{
	public class SquadBuffDef
	{
		public DT.Field validate_field;

		public float base_cth;

		public float base_defense;

		public float base_cth_shoot;

		public float base_resilience_flat;

		public float base_cth_cavalry;

		public float base_defense_against_ranged_mod;

		public float base_cth_against_me;

		public float base_movement_speed;

		public float base_stamina_recovery_rate;

		public string[] triggers;

		public DT.Field chance_field;

		public float duration;

		public float priority = -1f;
	}

	public delegate SquadBuff CreateAction(Squad owner, Def def, DT.Field field = null);

	public Squad squad;

	private bool _enabled;

	public Time start_time;

	public static Dictionary<string, SquadBuffDef> defs = new Dictionary<string, SquadBuffDef>();

	public bool enabled
	{
		get
		{
			return _enabled;
		}
		private set
		{
			if (_enabled != value)
			{
				_enabled = value;
				if (_enabled)
				{
					OnEnable();
				}
				else
				{
					OnDisable();
				}
			}
		}
	}

	public Def buffDef { get; private set; }

	public DT.Field field { get; private set; }

	public SquadBuffDef def
	{
		get
		{
			if (field == null)
			{
				return null;
			}
			if (!defs.TryGetValue(field.key, out var value))
			{
				value = new SquadBuffDef();
				defs[field.key] = value;
			}
			return value;
		}
	}

	public SquadBuff(Squad owner, Def def, DT.Field field = null)
		: base(owner)
	{
		Init(owner, def, field);
	}

	public static SquadBuff Create(Squad owner, Def def, DT.Field field = null)
	{
		if (def == null)
		{
			return new SquadBuff(owner, null, field);
		}
		Reflection.TypeInfo obj_type = def.obj_type;
		if (obj_type == null)
		{
			Game.Log("Action def has no C# type: " + def.ToString(), Game.LogType.Error);
			return null;
		}
		if (obj_type.type == typeof(SquadBuff))
		{
			return new SquadBuff(owner, def, field);
		}
		CreateAction createAction = obj_type.FindCreateMethod(typeof(SquadBuff), typeof(Squad), typeof(Def), typeof(DT.Field))?.func as CreateAction;
		try
		{
			if (createAction != null)
			{
				return createAction(owner, def, field);
			}
			return Reflection.CreateObjectViaReflection<SquadBuff>(obj_type.type, new object[2] { owner, def });
		}
		catch (Exception ex)
		{
			owner.game.Error("Error creating " + obj_type.name + ": " + ex);
			return null;
		}
	}

	protected virtual void OnEnable()
	{
		squad.stats_dirty = true;
		if (def.base_movement_speed != 0f)
		{
			squad.RecalcMoveSpeed();
			squad.SetDoubleTime(squad.double_time);
		}
		if (!(def.priority >= 0f))
		{
			return;
		}
		for (int i = 0; i < squad.buffs.Count; i++)
		{
			SquadBuff squadBuff = squad.buffs[i];
			if (squadBuff != this && squadBuff.enabled && squadBuff.def.priority >= 0f && squadBuff.def.priority < def.priority)
			{
				squadBuff.enabled = false;
			}
		}
	}

	protected virtual void OnDisable()
	{
		squad.stats_dirty = true;
		if (def.base_movement_speed != 0f)
		{
			squad.RecalcMoveSpeed();
			squad.SetDoubleTime(squad.double_time);
		}
	}

	public virtual bool Validate()
	{
		if (def.validate_field != null && !def.validate_field.Bool(squad.simulation))
		{
			return false;
		}
		if (!ValidPriority())
		{
			return false;
		}
		return true;
	}

	public bool ValidPriority()
	{
		if (def.priority >= 0f)
		{
			for (int i = 0; i < squad.buffs.Count; i++)
			{
				SquadBuff squadBuff = squad.buffs[i];
				if (squadBuff != this && squadBuff.enabled && squadBuff.def.priority >= 0f && squadBuff.def.priority > def.priority)
				{
					return false;
				}
			}
		}
		return true;
	}

	public virtual void Init(Squad squad, Def def, DT.Field field = null)
	{
		this.squad = squad;
		this.field = ((field != null) ? field : def?.field);
		buffDef = def;
		LoadDefOnlyFields(field);
		UpdateInBatch(base.game.update_1sec);
	}

	private void LoadDefOnlyFields(DT.Field field)
	{
		def.base_cth = field.GetFloat("base_cth", null, def.base_cth);
		def.base_cth_shoot = field.GetFloat("base_cth_shoot", null, def.base_cth_shoot);
		def.base_defense = field.GetFloat("base_defense", null, def.base_defense);
		def.base_resilience_flat = field.GetFloat("base_resilience_flat", null, def.base_resilience_flat);
		def.base_cth_cavalry = field.GetFloat("base_cth_cavalry", null, def.base_cth_cavalry);
		def.base_defense_against_ranged_mod = field.GetFloat("base_defense_against_ranged_mod", null, def.base_defense_against_ranged_mod);
		def.base_cth_against_me = field.GetFloat("base_cth_against_me", squad.simulation, def.base_cth_against_me);
		def.base_movement_speed = field.GetFloat("base_movement_speed", null, def.base_movement_speed);
		def.base_stamina_recovery_rate = field.GetFloat("base_stamina_recovery_rate", null, def.base_stamina_recovery_rate);
		DT.Field field2 = field.FindChild("trigger");
		if (field2 != null)
		{
			def.triggers = new string[field2.NumValues()];
			for (int i = 0; i < def.triggers.Length; i++)
			{
				def.triggers[i] = field2.String(i);
			}
		}
		else
		{
			def.triggers = null;
		}
		def.chance_field = field?.FindChild("chance");
		def.validate_field = field?.FindChild("validate");
		def.duration = field.GetFloat("duration", null, def.duration);
		def.priority = field.GetFloat("priority", null, def.priority);
	}

	public void OnTrigger(string trigger)
	{
		bool flag = false;
		if (def.triggers != null)
		{
			for (int i = 0; i < def.triggers.Length; i++)
			{
				if (def.triggers[i] == trigger)
				{
					flag = true;
					break;
				}
			}
		}
		if (flag && (def.chance_field == null || !((float)base.game.Random(0, 100) >= def.chance_field.Float(squad.simulation))) && Validate())
		{
			OnTrigger();
		}
	}

	private void OnTrigger()
	{
		start_time = base.game.time;
		enabled = Validate();
	}

	public override void OnUpdate()
	{
		if (def.duration <= 0f)
		{
			enabled = Validate();
		}
		else if (enabled && start_time + def.duration < base.game.time)
		{
			enabled = false;
		}
	}

	public virtual float GetCTH()
	{
		if (enabled)
		{
			return def.base_cth;
		}
		return 0f;
	}

	public virtual float GetCTHShootMod()
	{
		if (!squad.def.is_ranged)
		{
			return 0f;
		}
		if (enabled)
		{
			return def.base_cth_shoot;
		}
		return 0f;
	}

	public virtual float getCTHCavalry()
	{
		if (!squad.def.is_cavalry)
		{
			return 0f;
		}
		if (enabled)
		{
			return def.base_cth_cavalry;
		}
		return 0f;
	}

	public virtual float getCTHAgainstMe()
	{
		if (enabled)
		{
			return def.base_cth_against_me;
		}
		return 0f;
	}

	public virtual float GetDefense()
	{
		if (enabled)
		{
			return def.base_defense;
		}
		return 0f;
	}

	public virtual float GetResilienceFlat()
	{
		if (enabled)
		{
			return def.base_resilience_flat;
		}
		return 0f;
	}

	public virtual float GetDefenseAgainstRanged()
	{
		if (enabled)
		{
			return def.base_defense_against_ranged_mod;
		}
		return 0f;
	}

	public virtual float GetMoveSpeed()
	{
		if (enabled)
		{
			return def.base_movement_speed;
		}
		return 0f;
	}

	public virtual float GetStaminaRecovery()
	{
		if (enabled)
		{
			return def.base_stamina_recovery_rate;
		}
		return 0f;
	}

	public virtual string DebugUIText()
	{
		return "";
	}

	public override void OnDestroy()
	{
		base.OnDestroy();
		StopUpdating();
	}

	public Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		if (key == "enabled")
		{
			return enabled;
		}
		return Value.Unknown;
	}
}
