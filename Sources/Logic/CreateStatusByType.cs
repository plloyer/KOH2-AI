using System;
using System.Collections.Generic;

namespace Logic;

public class Status : Object
{
	public class Def : Logic.Def
	{
		public string Name;

		public float update_interval_min = -1f;

		public float update_interval_max = -1f;

		public List<Action.Def> actions;

		public bool invalidate_incomes;

		public DT.Field show_in_portrait;

		public bool show_in_window = true;

		public override bool Load(Game game)
		{
			DT.Field field = base.field;
			Name = field.GetString("name", null, dt_def.path);
			DT.Field field2 = field.FindChild("update_interval");
			if (field2 != null)
			{
				update_interval_min = field2.Float(0, null, -1f);
				update_interval_max = field2.Float(1, null, update_interval_min);
			}
			invalidate_incomes = field.GetBool("invalidate_incomes", null, invalidate_incomes);
			actions = null;
			show_in_portrait = field.FindChild("show_in_portrait");
			show_in_window = field.GetBool("show_in_window", null, def_val: true);
			return true;
		}

		public float UpdateInterval(Game game)
		{
			if (update_interval_min < 0f)
			{
				return -1f;
			}
			if (update_interval_max <= update_interval_min)
			{
				return update_interval_min;
			}
			return game.Random(update_interval_min, update_interval_max);
		}
	}

	public delegate Status CreateStatus(Type type, Def def);

	public delegate Status CreateStatusByTypeInfo(Reflection.TypeInfo rtti, Def def);

	public delegate Status CreateStatusByDef(Def def);

	public delegate Status CreateStatusByType(Game game, Type type);

	public class RefData : Data
	{
		public NID owner_nid;

		public int usid;

		public static RefData Create()
		{
			return new RefData();
		}

		public override string ToString()
		{
			return base.ToString() + "(Status " + usid + " of " + owner_nid.ToString() + ")";
		}

		public override bool InitFrom(object obj)
		{
			if (!(obj is Status status))
			{
				return false;
			}
			owner_nid = status.owner;
			usid = status.usid;
			return true;
		}

		public override void Save(Serialization.IWriter ser)
		{
			ser.WriteNID(owner_nid, "owner");
			ser.Write7BitUInt(usid, "usid");
		}

		public override void Load(Serialization.IReader ser)
		{
			owner_nid = ser.ReadNID("owner");
			usid = ser.Read7BitUInt("usid");
		}

		public override object GetObject(Game game)
		{
			return owner_nid.GetObj(game)?.GetComponent<Statuses>()?.Find(usid);
		}

		public override bool ApplyTo(object obj, Game game)
		{
			if (!(obj is Status status))
			{
				return false;
			}
			if (status.usid != usid)
			{
				return false;
			}
			return true;
		}
	}

	public class FullData : RefData
	{
		public string status_def_id;

		public new static FullData Create()
		{
			return new FullData();
		}

		public override bool InitFrom(object obj)
		{
			if (!(obj is Status status))
			{
				return false;
			}
			base.InitFrom(obj);
			status_def_id = status.def.id;
			return true;
		}

		public override void Save(Serialization.IWriter ser)
		{
			base.Save(ser);
			ser.WriteStr(status_def_id, "def");
		}

		public override void Load(Serialization.IReader ser)
		{
			base.Load(ser);
			status_def_id = ser.ReadStr("def");
		}

		public override object GetObject(Game game)
		{
			Object obj = owner_nid.GetObj(game);
			if (obj == null)
			{
				return null;
			}
			Statuses statuses = obj.GetComponent<Statuses>();
			if (statuses == null)
			{
				statuses = new Statuses(obj);
			}
			Status status = statuses.Find(usid);
			if (status != null)
			{
				return status;
			}
			Def def = game.defs.Find<Def>(status_def_id);
			if (def == null)
			{
				return null;
			}
			status = Status.Create(def);
			status.usid = usid;
			return status;
		}

		public override bool ApplyTo(object obj, Game game)
		{
			if (!(obj is Status status))
			{
				return false;
			}
			if (status.usid != usid)
			{
				return false;
			}
			if (status.def.id != status_def_id)
			{
				Game.Log("Attempting to apply " + status_def_id + " data to " + status.ToString(), Game.LogType.Error);
				return false;
			}
			return true;
		}
	}

	public Def def;

	public Object owner;

	public int usid;

	private float update_interval;

	public Character own_character => owner as Character;

	public Kingdom own_kingdom => owner?.GetKingdom();

	public Status(Def def)
		: base((Game)null)
	{
		this.def = def;
	}

	public void SetOwner(Object owner)
	{
		if (this.owner == owner)
		{
			return;
		}
		if (this.owner != null && owner != null)
		{
			Error("Attempting to change status owner");
			return;
		}
		this.owner = owner;
		if (owner == null)
		{
			usid = 0;
		}
		if (game == null && owner != null && owner.game != null)
		{
			game = owner.game;
			if (def == null)
			{
				def = game.defs.Find<Def>(rtti.name);
			}
			Init();
			if (usid == 0 && !IsAutomatic() && owner.IsAuthority())
			{
				usid = ++owner.statuses.last_usid;
			}
			if (base.obj_state == ObjState.Created)
			{
				game.starts.Add(this);
			}
		}
		if (def != null && def.invalidate_incomes)
		{
			owner?.GetKingdom()?.InvalidateIncomes();
		}
	}

	protected override void OnStart()
	{
		base.OnStart();
		update_interval = def.UpdateInterval(game);
		if (update_interval > 0f)
		{
			UpdateAfter(update_interval);
		}
	}

	public override void OnUpdate()
	{
		_ = update_interval;
		update_interval = def.UpdateInterval(game);
		if (update_interval > 0f)
		{
			UpdateAfter(update_interval);
		}
	}

	public static Status Create(Reflection.TypeInfo rtti, Def def)
	{
		if (rtti.type == typeof(Status))
		{
			return new Status(def);
		}
		CreateStatusByDef createStatusByDef = rtti.FindCreateMethod(typeof(Status), typeof(Def))?.func as CreateStatusByDef;
		try
		{
			if (createStatusByDef != null)
			{
				return createStatusByDef(def);
			}
			return Reflection.CreateObjectViaReflection<Status>(rtti.type, new object[1] { def });
		}
		catch (Exception ex)
		{
			Game.Log("Error creating " + rtti.name + ": " + ex, Game.LogType.Error);
			return null;
		}
	}

	public static Status Create(Type type, Def def)
	{
		return Create(Reflection.GetTypeInfo(type), def);
	}

	public static Status Create(Def def)
	{
		if (def == null)
		{
			return null;
		}
		return Create(def.obj_type, def);
	}

	public static Status Create(Game game, Type type)
	{
		Def def = game.defs.Get<Def>(type.Name);
		if (def == null)
		{
			game.Error("Status def not found: " + type.Name);
			return null;
		}
		return Create(type, def);
	}

	public override string ToString()
	{
		return Object.ToString(owner) + "." + base.ToString() + " - " + def?.id;
	}

	public override Kingdom GetKingdom()
	{
		return owner?.GetKingdom();
	}

	public virtual void GetProgress(out float cur, out float max)
	{
		cur = (max = 0f);
	}

	public virtual bool IsIdle()
	{
		return false;
	}

	public virtual bool IsDead()
	{
		return false;
	}

	public virtual bool IsAutomatic()
	{
		return false;
	}

	public virtual bool AllowMultiple()
	{
		return false;
	}

	public override Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		switch (key)
		{
		case "owner":
			return owner;
		case "own_character":
			return own_character;
		case "own_kingdom":
			return own_kingdom;
		case "dbg_info":
			return GetDebugInfo();
		case "name":
			return base.GetVar(key, vars, as_value);
		case "is_valid":
			return IsValid();
		default:
			if (owner != null)
			{
				Value var = owner.GetVar(key, vars, as_value);
				if (!var.is_unknown)
				{
					return var;
				}
			}
			return base.GetVar(key, vars, as_value);
		}
	}

	public virtual string GetDebugInfo()
	{
		return def.Name;
	}

	protected override void OnDestroy()
	{
		if (def != null && def.invalidate_incomes)
		{
			owner?.GetKingdom()?.InvalidateIncomes();
		}
		if (owner != null)
		{
			owner.DelStatus(this);
		}
		base.OnDestroy();
	}

	public DT.Field GetButtonField(string btn_id)
	{
		return def.field.FindChild("buttons." + btn_id);
	}

	public Action GetButtonAction(string btn_id)
	{
		if (owner == null)
		{
			return null;
		}
		DT.Field buttonField = GetButtonField(btn_id);
		if (buttonField == null)
		{
			return null;
		}
		DT.Field field = buttonField.GetRef("action");
		if (field == null || field.def == null || field.def.def == null)
		{
			return null;
		}
		if (!(field.def.def is Action.Def def))
		{
			return null;
		}
		return Action.Find(owner, def);
	}

	public virtual void OnButton(string btn_id)
	{
	}

	public void GetActions(List<Action.Def> result)
	{
		AddActions(result, def);
	}

	private void AddActions(List<Action.Def> result, Def def)
	{
		if (def == null)
		{
			return;
		}
		Def def2 = def.BasedOn<Def>();
		AddActions(result, def2);
		if (def.actions != null)
		{
			for (int i = 0; i < def.actions.Count; i++)
			{
				result.Add(def.actions[i]);
			}
		}
	}

	public override bool IsRefSerializable()
	{
		if (owner != null && owner.IsRefSerializable())
		{
			return usid > 0;
		}
		return false;
	}
}
