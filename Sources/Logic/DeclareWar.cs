namespace Logic;

public class DeclareWar : Offer
{
	public DeclareWar(Def def, Object from, Object to)
		: base(def, from, to)
	{
	}

	public DeclareWar(Kingdom from, Kingdom to)
		: base(from, to)
	{
	}

	public new static Offer Create(Def def, Object from, Object to)
	{
		return new DeclareWar(def, from, to);
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
		if (kingdom.sovereignState != null && kingdom.sovereignState == kingdom2)
		{
			return "we_are_their_vassal";
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
		if (kingdom.sovereignState != null && kingdom.sovereignState == kingdom2)
		{
			return "we_are_their_vassal";
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
		using (Game.Profile("Declare War"))
		{
			using (Game.Profile("Base"))
			{
				base.OnAccept();
			}
			Kingdom kingdom = GetSourceObj() as Kingdom;
			Kingdom kingdom2 = GetTargetObj() as Kingdom;
			Kingdom hq_kingdom = base.game.religions.catholic.hq_kingdom;
			if (kingdom2.HasPope() && !kingdom2.IsPapacy() && !hq_kingdom.IsDefeated())
			{
				kingdom.AddRelationModifier(hq_kingdom, "rel_declare_war_papacys_controller", null);
			}
			using (Game.Profile("Start War with"))
			{
				kingdom.StartWarWith(kingdom2, War.InvolvementReason.FormalDeclaration, "WarDeclaredMessage");
			}
			using (Game.Profile("Close audience"))
			{
				kingdom2.FireEvent("close_audience", kingdom, kingdom2.id);
			}
		}
	}

	public override bool IsWar(bool sender)
	{
		return true;
	}
}
