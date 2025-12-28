namespace Logic;

public class PeaceTalksStatus : Status, IListener
{
	public new class FullData : Status.FullData
	{
		private NID kingdom;

		private float time;

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
			if (!(obj is PeaceTalksStatus peaceTalksStatus))
			{
				return false;
			}
			kingdom = peaceTalksStatus.kingdom;
			time = peaceTalksStatus.time - peaceTalksStatus.owner.game.time;
			return true;
		}

		public override void Save(Serialization.IWriter ser)
		{
			base.Save(ser);
			ser.WriteNID<Kingdom>(kingdom, "kingdom");
			ser.WriteFloat(time, "time");
		}

		public override void Load(Serialization.IReader ser)
		{
			base.Load(ser);
			kingdom = ser.ReadNID<Kingdom>("kingdom");
			time = ser.ReadFloat("time");
		}

		public override bool ApplyTo(object obj, Game game)
		{
			if (!base.ApplyTo(obj, game))
			{
				return false;
			}
			if (!(obj is PeaceTalksStatus peaceTalksStatus))
			{
				return false;
			}
			if (peaceTalksStatus.kingdom != null)
			{
				peaceTalksStatus.kingdom.DelListener(peaceTalksStatus);
			}
			peaceTalksStatus.kingdom = kingdom.Get<Kingdom>(game);
			peaceTalksStatus.kingdom.AddListener(peaceTalksStatus);
			peaceTalksStatus.time = game.time + time;
			return true;
		}
	}

	public Kingdom kingdom;

	public Time time;

	public PeaceTalksStatus(Kingdom kingdom)
		: base(null)
	{
		if (this.kingdom != null)
		{
			this.kingdom.DelListener(this);
		}
		this.kingdom = kingdom;
		kingdom.AddListener(this);
		time = kingdom.game.time;
	}

	public PeaceTalksStatus(Def def)
		: base(def)
	{
	}

	public new static Status Create(Def def)
	{
		return new PeaceTalksStatus(def);
	}

	public override Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		if (key == "target_kingdom")
		{
			return kingdom;
		}
		return base.GetVar(key, vars, as_value);
	}

	public void OnMessage(object obj, string message, object param)
	{
		if (IsAuthority() && message == "stance_changed" && !kingdom.IsEnemy(base.own_kingdom) && base.own_character?.cur_action is DiplomacyPeaceTalksAction)
		{
			base.own_character.cur_action.Cancel();
		}
	}
}
