namespace Logic;

public class IndependenceWarOffer : Offer
{
	public IndependenceWarOffer(Def def, Object from, Object to)
		: base(def, from, to)
	{
	}

	public IndependenceWarOffer(Kingdom from, Kingdom to)
		: base(from, to)
	{
	}

	public new static Offer Create(Def def, Object from, Object to)
	{
		return new IndependenceWarOffer(def, from, to);
	}

	public override Object GetSourceObj()
	{
		return to;
	}

	public override Object GetTargetObj()
	{
		return from;
	}

	public override string ValidateWithoutArgs()
	{
		string text = base.ValidateWithoutArgs();
		if (ShouldReturn(text))
		{
			return text;
		}
		Kingdom kingdom = GetSourceObj() as Kingdom;
		Kingdom kingdom2 = GetTargetObj() as Kingdom;
		if (kingdom.sovereignState == null || kingdom.sovereignState != kingdom2)
		{
			return "not_a_vassal";
		}
		if (!ShouldSkip(text))
		{
			text = War.ValidateWarAllowed(kingdom, kingdom2);
			ShouldReturn(text);
			return text;
		}
		return text;
	}

	public override string Validate()
	{
		string text = base.Validate();
		if (ShouldReturn(text))
		{
			return text;
		}
		Kingdom kingdom = GetSourceObj() as Kingdom;
		Kingdom kingdom2 = GetTargetObj() as Kingdom;
		if (kingdom.sovereignState == null || kingdom.sovereignState != kingdom2)
		{
			return "not_a_vassal";
		}
		if (!ShouldSkip(text))
		{
			text = War.ValidateWarAllowed(kingdom, kingdom2);
			ShouldReturn(text);
			return text;
		}
		return text;
	}

	public override void OnAccept()
	{
		base.OnAccept();
		Kingdom kingdom = GetSourceObj() as Kingdom;
		kingdom?.NotifyListeners("failed_independence");
		int num = def.field.GetInt("fail_penalty_chance", null, 50);
		if ((float)base.game.Random(0, 100) < (float)num)
		{
			kingdom.GetCrownAuthority().AddModifier("ClaimIndependenceFail");
		}
	}

	public override void OnDecline()
	{
		using (Game.Profile("Independence War"))
		{
			using (Game.Profile("Base"))
			{
				base.OnDecline();
			}
			Kingdom kingdom = GetSourceObj() as Kingdom;
			Kingdom kingdom2 = GetTargetObj() as Kingdom;
			using (Game.Profile("Start War with"))
			{
				War war = kingdom.StartWarWith(kingdom2, War.InvolvementReason.VassalIndependenceRefuseReconsider, "WarDeclaredMessage");
				if (war != null)
				{
					war.SetType("IndependenceWar");
					kingdom.NotifyListeners("independence", kingdom2);
				}
			}
			using (Game.Profile("Close audience"))
			{
				kingdom2.FireEvent("close_audience", kingdom, kingdom2.id);
			}
		}
	}
}
