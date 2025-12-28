namespace Logic;

public class MissionInRomeAction : Action
{
	public MissionInRomeAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new MissionInRomeAction(owner as Character, def);
	}

	public override void Run()
	{
		Character character = base.own_character;
		if (character != null)
		{
			Kingdom hq_kingdom = base.game.religions.catholic.hq_kingdom;
			if (!hq_kingdom.IsDefeated())
			{
				character.SetMissionKingdom(hq_kingdom);
				character.SetDefaultStatus<OnMissionInRomeStatus>();
				base.Run();
			}
		}
	}
}
