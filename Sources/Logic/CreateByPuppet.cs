namespace Logic;

public class HasPuppetStatus : Status
{
	public delegate HasPuppetStatus CreateByPuppet(Character puppet);

	public new class FullData : Status.FullData
	{
		public NID puppet;

		public new static FullData Create()
		{
			return new FullData();
		}

		public override bool InitFrom(object obj)
		{
			if (!base.InitFrom(obj))
			{
				return false;
			}
			if (!(obj is HasPuppetStatus hasPuppetStatus))
			{
				return false;
			}
			puppet = hasPuppetStatus.puppet;
			return true;
		}

		public override void Save(Serialization.IWriter ser)
		{
			base.Save(ser);
			ser.WriteNID<Character>(puppet, "puppet");
		}

		public override void Load(Serialization.IReader ser)
		{
			base.Load(ser);
			puppet = ser.ReadNID<Character>("puppet");
		}

		public override bool ApplyTo(object obj, Game game)
		{
			if (!base.ApplyTo(obj, game))
			{
				return false;
			}
			if (!(obj is HasPuppetStatus hasPuppetStatus))
			{
				return false;
			}
			hasPuppetStatus.puppet = puppet.Get<Character>(game);
			return true;
		}
	}

	public Character puppet;

	public HasPuppetStatus(Def def)
		: base(def)
	{
	}

	public new static Status Create(Def def)
	{
		return new HasPuppetStatus(def);
	}

	public HasPuppetStatus(Character puppet)
		: base(null)
	{
		this.puppet = puppet;
		def = puppet.game.defs.Get<Def>(rtti.name);
	}

	public static HasPuppetStatus Create(Character puppet)
	{
		switch (puppet.class_name)
		{
		case "Marshal":
			return new HasMarshalPuppetStatus(puppet);
		case "Merchant":
			return new HasMerchantPuppetStatus(puppet);
		case "Diplomat":
			return new HasDiplomatPuppetStatus(puppet);
		case "Spy":
			return new HasSpyPuppetStatus(puppet);
		case "Cleric":
			return new HasClericPuppetStatus(puppet);
		default:
			Game.Log("Unknown puppet class: " + puppet.class_name, Game.LogType.Error);
			return null;
		}
	}

	public static HasPuppetStatus Find(Character master, Character puppet)
	{
		if (master.statuses == null || master.statuses.additional == null)
		{
			return null;
		}
		for (int i = 0; i < master.statuses.additional.Count; i++)
		{
			if (master.statuses.additional[i] is HasPuppetStatus hasPuppetStatus && hasPuppetStatus.puppet == puppet)
			{
				return hasPuppetStatus;
			}
		}
		return null;
	}

	public override Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		if (key == "puppet")
		{
			return puppet;
		}
		return base.GetVar(key, vars, as_value);
	}

	public override string ToString()
	{
		return base.ToString() + "(" + (puppet?.ToString() ?? "null") + ")";
	}

	public override bool AllowMultiple()
	{
		return true;
	}
}
