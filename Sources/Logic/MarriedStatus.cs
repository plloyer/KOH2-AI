namespace Logic;

public class MarriedStatus : Status
{
	public delegate MarriedStatus CreateByMarriage(Marriage marriage);

	public new class FullData : Status.FullData
	{
		public NID marriage;

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
			if (!(obj is MarriedStatus marriedStatus))
			{
				return false;
			}
			marriage = marriedStatus.marriage;
			return true;
		}

		public override void Save(Serialization.IWriter ser)
		{
			base.Save(ser);
			ser.WriteNID<Marriage>(marriage, "marriage");
		}

		public override void Load(Serialization.IReader ser)
		{
			base.Load(ser);
			marriage = ser.ReadNID<Marriage>("marriage");
		}

		public override bool ApplyTo(object obj, Game game)
		{
			if (!base.ApplyTo(obj, game))
			{
				return false;
			}
			if (!(obj is MarriedStatus marriedStatus))
			{
				return false;
			}
			marriedStatus.marriage = marriage.Get<Marriage>(game);
			if (!(obj is WidowedStatus))
			{
				marriedStatus.own_character.marriage = marriedStatus.marriage;
			}
			return true;
		}
	}

	public Marriage marriage;

	public MarriedStatus(Def def)
		: base(def)
	{
	}

	public MarriedStatus(Marriage marriage)
		: base(null)
	{
		this.marriage = marriage;
		def = marriage.wife.game.defs.Get<Def>(rtti.name);
	}

	public new static Status Create(Def def)
	{
		return new MarriedStatus(def);
	}

	public static MarriedStatus Create(Marriage marriage)
	{
		return new MarriedStatus(marriage);
	}

	public override bool IsIdle()
	{
		return true;
	}

	public override Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		if (!(key == "marriage"))
		{
			if (key == "spouse")
			{
				return marriage?.GetSpouse(base.own_character);
			}
			return base.GetVar(key, vars, as_value);
		}
		return marriage;
	}
}
