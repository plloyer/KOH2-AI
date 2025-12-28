namespace Logic;

public class OnMissionInRomeStatus : Status, IListener
{
	public OnMissionInRomeStatus(Def def)
		: base(def)
	{
	}

	public new static Status Create(Def def)
	{
		return new OnMissionInRomeStatus(def);
	}

	protected override void OnStart()
	{
		base.OnStart();
		base.own_kingdom?.AddListener(this);
	}

	protected override void OnDestroy()
	{
		base.own_kingdom?.DelListener(this);
		base.OnDestroy();
	}

	public override void OnUpdate()
	{
		base.OnUpdate();
		ImproveRelations();
	}

	public void OnMessage(object obj, string message, object param)
	{
		if (obj == base.own_kingdom && message == "religion_changed" && !base.own_kingdom.is_christian)
		{
			base.own_character.FireEvent("MissionInRomeCancelledBecauseWeChangedReligionMessage", null);
			base.own_character.Recall();
		}
		if (obj == base.own_kingdom && message == "excommunicated")
		{
			base.own_character.FireEvent("MissionInRomeCancelledBecauseWeWereExcommunicatedMessage", null);
			base.own_character.Recall();
		}
	}

	private void ImproveRelations()
	{
		Kingdom kingdom = base.own_kingdom;
		Kingdom kingdom2 = base.own_character?.mission_kingdom;
		if (kingdom != null && kingdom2 != null)
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
