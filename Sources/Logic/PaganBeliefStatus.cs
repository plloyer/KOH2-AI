namespace Logic;

public class PaganBeliefStatus : Status
{
	public delegate PaganBeliefStatus CreateByBelief(Religion.PaganBelief belief);

	public new class FullData : Status.FullData
	{
		public string beliefName;

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
			if (!(obj is PaganBeliefStatus paganBeliefStatus))
			{
				return false;
			}
			beliefName = paganBeliefStatus.belief.name;
			return true;
		}

		public override void Save(Serialization.IWriter ser)
		{
			base.Save(ser);
			ser.WriteStr(beliefName, "beliefName");
		}

		public override void Load(Serialization.IReader ser)
		{
			base.Load(ser);
			beliefName = ser.ReadStr("beliefName");
		}

		public override bool ApplyTo(object obj, Game game)
		{
			if (!base.ApplyTo(obj, game))
			{
				return false;
			}
			if (!(obj is PaganBeliefStatus paganBeliefStatus))
			{
				return false;
			}
			paganBeliefStatus.belief = game.religions.pagan.def.FindPaganBelief(beliefName);
			paganBeliefStatus.own_character.paganBelief = paganBeliefStatus.belief;
			return true;
		}
	}

	public Religion.PaganBelief belief;

	public PaganBeliefStatus(Def def)
		: base(def)
	{
	}

	public PaganBeliefStatus(Religion.PaganBelief belief)
		: base(null)
	{
		this.belief = belief;
	}

	public new static Status Create(Def def)
	{
		return new PaganBeliefStatus(def);
	}

	public static PaganBeliefStatus Create(Religion.PaganBelief belief)
	{
		return new PaganBeliefStatus(belief);
	}

	public override Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		switch (key)
		{
		case "cost":
			return base.own_character.actions.Find("ChangePaganBeliefAction").GetCost();
		case "current_upkeep":
		{
			float num = base.own_character.game.religions.pagan.def.CalcPaganBliefsUpkeep(base.own_kingdom);
			if (num != 0f)
			{
				return new Value(num);
			}
			return Value.Null;
		}
		case "next_upkeep":
			return base.own_character.game.religions.pagan.def.CalcPaganBliefsUpkeep(base.own_kingdom, base.own_kingdom.pagan_beliefs.Count + 1);
		case "can_promote":
			return base.own_character.actions.Find("ChangePaganBeliefAction").Validate() == "ok";
		case "belief":
			return belief.GetNameKey();
		case "beliefKey":
			return belief.name;
		case "is_preparing_to_promote":
			return base.own_character.actions.Find("PromotePaganBeliefAction").state == Action.State.Preparing;
		default:
			return base.GetVar(key, vars, as_value);
		}
	}

	public override void GetProgress(out float cur, out float max)
	{
		Action action = base.own_character.actions.Find("ChangePaganBeliefAction");
		if (action != null && action.is_active)
		{
			action.GetProgress(out cur, out max);
		}
		else
		{
			cur = (max = 0f);
		}
	}
}
