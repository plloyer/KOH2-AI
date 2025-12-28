namespace Logic;

public class OnStudyStatus : Status, IListener
{
	public OnStudyStatus(Def def)
		: base(def)
	{
	}

	public new static Status Create(Def def)
	{
		return new OnStudyStatus(def);
	}

	protected override void OnStart()
	{
		base.OnStart();
		base.own_kingdom?.AddListener(this);
		base.own_character?.mission_kingdom?.AddListener(this);
	}

	protected override void OnDestroy()
	{
		base.own_kingdom?.DelListener(this);
		base.OnDestroy();
	}

	public void OnMessage(object obj, string message, object param)
	{
		if (IsAuthority())
		{
			if (obj == base.own_kingdom && message == "religion_changed" && !base.own_kingdom.is_pagan)
			{
				base.own_character.FireEvent("StudyCancelledBecauseWeChangedReligionMessage", null);
				base.own_character?.mission_kingdom?.DelListener(this);
				base.own_character.Recall();
			}
			if (obj == base.own_character?.mission_kingdom && message == "religion_changed" && base.own_character.mission_kingdom.is_pagan)
			{
				base.own_character.FireEvent("StudyCancelledBecauseTheyChangedReligionMessage", null);
				base.own_character?.mission_kingdom?.DelListener(this);
				base.own_character.Recall();
			}
		}
	}
}
