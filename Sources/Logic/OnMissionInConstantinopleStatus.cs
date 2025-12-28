namespace Logic;

public class OnMissionInConstantinopleStatus : Status, IListener
{
	public OnMissionInConstantinopleStatus(Def def)
		: base(def)
	{
	}

	public new static Status Create(Def def)
	{
		return new OnMissionInConstantinopleStatus(def);
	}

	protected override void OnStart()
	{
		base.OnStart();
		base.own_kingdom?.AddListener(this);
		game?.religions?.AddListener(this);
	}

	protected override void OnDestroy()
	{
		base.own_kingdom?.DelListener(this);
		game?.religions?.DelListener(this);
		base.OnDestroy();
	}

	public override void OnUpdate()
	{
		base.OnUpdate();
		ImproveRelations();
	}

	public void OnMessage(object obj, string message, object param)
	{
		if (IsAuthority())
		{
			if (obj == base.own_kingdom && message == "religion_changed" && (!base.own_kingdom.religion.def.christian || base.own_kingdom.HasEcumenicalPatriarch()))
			{
				base.own_character.FireEvent("MissionInConstantinopleCancelledBecauseWeChangedReligionMessage", null);
				base.own_character.Recall();
			}
			if (obj == game?.religions && (message == "orthodox_restored" || message == "orthodox_destroyed"))
			{
				base.own_character.SetMissionKingdom(game?.religions.orthodox.head_kingdom);
			}
		}
	}

	private void ImproveRelations()
	{
		Kingdom kingdom = base.own_kingdom;
		Kingdom kingdom2 = base.own_character?.mission_kingdom;
		if (kingdom != null && kingdom2 != null && kingdom != kingdom2)
		{
			float num = def.field.GetFloat("rel_change_amount", this);
			if (!(num <= 0f))
			{
				kingdom.AddRelationship(kingdom2, num, this);
				Vars vars = new Vars();
				vars.Set("cleric", base.own_character);
				vars.Set("amount", num);
				kingdom.NotifyListeners("rel_changed_cleric", vars);
			}
		}
	}
}
