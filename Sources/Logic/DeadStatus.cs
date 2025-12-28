namespace Logic;

public class DeadStatus : Status
{
	public new class FullData : Status.FullData
	{
		public string reason;

		public Data vars;

		public new static FullData Create()
		{
			return new FullData();
		}

		public bool InitAs<T>(object obj) where T : DeadStatus
		{
			if (!base.InitFrom(obj))
			{
				return false;
			}
			if (!(obj is T val))
			{
				return false;
			}
			reason = val.reason;
			vars = Data.Create(val.vars);
			return true;
		}

		public override bool InitFrom(object obj)
		{
			return InitAs<DeadStatus>(obj);
		}

		public override void Save(Serialization.IWriter ser)
		{
			base.Save(ser);
			ser.WriteStr(reason, "reason");
			ser.WriteData(vars, "vars");
		}

		public override void Load(Serialization.IReader ser)
		{
			base.Load(ser);
			reason = ser.ReadStr("reason");
			vars = ser.ReadData("vars");
		}

		public bool ApplyAs<T>(object obj, Game game) where T : DeadStatus
		{
			if (!base.ApplyTo(obj, game))
			{
				return false;
			}
			if (!(obj is T val))
			{
				return false;
			}
			val.reason = reason;
			val.vars = Data.RestoreObject<Vars>(vars, game);
			return true;
		}

		public override bool ApplyTo(object obj, Game game)
		{
			return ApplyAs<DeadStatus>(obj, game);
		}
	}

	public string reason = "";

	public Vars vars;

	public DeadStatus(Def def = null)
		: base(def)
	{
	}

	public DeadStatus(string reason, Vars vars)
		: base(null)
	{
		this.reason = reason;
		this.vars = vars;
	}

	public DeadStatus(string reason, Character character)
		: base(null)
	{
		this.reason = reason;
		vars = new Vars();
		character.FillDeadVars(vars, is_owner: true);
	}

	public new static Status Create(Def def)
	{
		return new DeadStatus(def);
	}

	public override bool IsDead()
	{
		return true;
	}

	public override Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		if (!(key == "status_text"))
		{
			if (key == "reason")
			{
				return reason;
			}
			if (this.vars != null)
			{
				this.vars.GetVar(key, vars);
			}
			return base.GetVar(key, vars, as_value);
		}
		return string.IsNullOrEmpty(reason) ? "DeadStatus.status_texts.default" : ("DeadStatus.status_texts." + reason);
	}
}
