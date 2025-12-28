namespace Logic;

public class ResilienceDrop : Component
{
	private Settlement settlement;

	private float refresh_interval = 15f;

	private Battle battle;

	public ResilienceDrop(Battle battle)
		: base(battle)
	{
		this.battle = battle;
		settlement = battle.settlement;
	}

	public void Begin(float random_offset = 15f)
	{
		if (settlement != null)
		{
			UpdateAfter(random_offset);
		}
	}

	public void Reset()
	{
		if (settlement != null)
		{
			StopUpdating();
			UpdateAfter(refresh_interval);
		}
	}

	public void Stop()
	{
		StopUpdating();
	}

	public override void OnUpdate()
	{
		if (settlement == null)
		{
			return;
		}
		if (settlement.IsAuthority())
		{
			float num = battle.ArmyStrength() * ((base.game.Random(0f, 2f) + base.game.Random(0f, 2f)) * (battle.def.resilience_damage_mod / (100f + battle.siege_defense)));
			if (settlement.keep_effects.CanBeTakenOver() && settlement.type == "Keep")
			{
				num *= battle.def.keep_resil_damage_mod;
			}
			battle.resilience -= num;
			if (settlement is Castle && battle.settlement_food_copy.Get() <= 0f)
			{
				battle.resilience -= 2f;
			}
			if (battle.resilience < 0f)
			{
				battle.resilience = 0f;
			}
			battle.SendState<Battle.SiegeStatsState>();
		}
		UpdateAfter(refresh_interval);
	}
}
