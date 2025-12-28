namespace Logic;

public class KingdomBankruptcy : Component
{
	public class Def : Logic.Def
	{
		public float minTime = 60f;

		public float maxTime = 300f;

		public float penalties_cooldown = 1800f;

		public override bool Load(Game game)
		{
			DT.Field field = base.field;
			minTime = field.GetFloat("min_time", null, minTime);
			maxTime = field.GetFloat("max_time", null, maxTime);
			penalties_cooldown = field.GetFloat("penalties_cooldown", null, penalties_cooldown);
			return true;
		}
	}

	private Kingdom k;

	private Def def;

	public Time lastBankruptcy = Time.Zero;

	public KingdomBankruptcy(Kingdom k)
		: base(k)
	{
		def = k.game.defs.GetBase<Def>();
		this.k = k;
		lastBankruptcy = new Time((long)(def.penalties_cooldown * -1000f));
	}

	public Time GetExpireTime()
	{
		return tmNextUpdate;
	}

	public bool IsBankrupt()
	{
		float num = k.resources[ResourceType.Gold];
		float num2 = k.income[ResourceType.Gold] - k.expenses[ResourceType.Gold];
		if (num <= 0f)
		{
			return num2 < 0f;
		}
		return false;
	}

	private float GetNextUpdateTime()
	{
		return base.game.Random(def.minTime, def.maxTime);
	}

	public void Refresh()
	{
		if (IsBankrupt())
		{
			if (!IsRegisteredForUpdate())
			{
				UpdateAfter(GetNextUpdateTime());
			}
			if (base.game.session_time - lastBankruptcy > def.penalties_cooldown)
			{
				k.NotifyListeners("getting_bankrupt");
				k.GetCrownAuthority().AddModifier("bankruptcy");
				lastBankruptcy = base.game.session_time;
			}
		}
		else
		{
			StopUpdating();
		}
	}

	public override void OnUpdate()
	{
		base.OnUpdate();
		if (k.occupiedRealms.Count > 0 && base.game.IsAuthority())
		{
			Realm realm = k.occupiedRealms[base.game.Random(0, k.occupiedRealms.Count)];
			(realm.controller as Rebellion)?.GetComponent<RebellionIndependence>()?.DeclareIndependence();
			k.FireEvent("occupied_realm_lost_bankrupcy", realm);
		}
		UpdateAfter(GetNextUpdateTime());
	}
}
