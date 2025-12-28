namespace Logic;

public class DeadPatriarchStatus : DeadStatus
{
	public new class FullData : DeadStatus.FullData
	{
		public new static FullData Create()
		{
			return new FullData();
		}

		public override bool InitFrom(object obj)
		{
			return InitAs<DeadPatriarchStatus>(obj);
		}

		public override bool ApplyTo(object obj, Game game)
		{
			return ApplyAs<DeadPatriarchStatus>(obj, game);
		}
	}

	public DeadPatriarchStatus(Def def = null)
		: base(def)
	{
	}

	public new static Status Create(Def def)
	{
		return new DeadPatriarchStatus(def);
	}

	public DeadPatriarchStatus(string reason, Vars vars)
		: base(reason, vars)
	{
	}

	public DeadPatriarchStatus(string reason, Character character)
		: base(reason, character)
	{
	}
}
