namespace Logic;

public class SettlementUpgrade : Component
{
	public Time last_time;

	public bool upgrading { get; private set; }

	public float progress { get; protected set; }

	private Settlement settlement => obj as Settlement;

	private Realm realm => settlement?.GetRealm();

	private Castle castle => realm?.castle;

	public float total_hammers => settlement.def.repair_hammers;

	public float total_time => total_hammers / HammersPerSecond();

	public SettlementUpgrade(Object obj)
		: base(obj)
	{
		if (!(obj is Settlement))
		{
			Log(obj.ToString() + " must be of type Logic.Settlement");
		}
	}

	public void Begin()
	{
		if (settlement != null && settlement.IsAuthority())
		{
			Set(upgrading: true);
			obj.NotifyListeners("upgrade_started");
		}
	}

	private void CompleteUpgrade()
	{
		if (settlement != null && settlement.IsAuthority() && upgrading)
		{
			settlement.SetStateDestroyed(isRazed: false);
			Set(upgrading: false);
			obj.NotifyListeners("upgrade_completed");
		}
	}

	public float HammersPerSecond()
	{
		float stat = realm.GetStat(Stats.rs_settlement_recovery);
		float num = 1f + stat / 100f;
		float num2 = 1f - settlement.razedPenaltyPerc / 100f;
		return realm.income.Get(ResourceType.Hammers) * num2 * num;
	}

	public void Set(bool upgrading, float progress = 0f, float delta_time = 0f, bool send_state = true)
	{
		this.upgrading = upgrading;
		this.progress = progress;
		last_time = obj.game.time - delta_time;
		if (send_state)
		{
			settlement?.SendState<Village.UpgradeProgressState>();
		}
	}

	public void Update()
	{
		if (settlement == null || !settlement.IsAuthority() || !settlement.razed)
		{
			return;
		}
		if (castle?.battle != null)
		{
			last_time = obj.game.time;
			return;
		}
		if (!upgrading)
		{
			Begin();
			return;
		}
		float num = obj.game.time - last_time;
		if (num < 10f)
		{
			return;
		}
		last_time = obj.game.time;
		float num2 = HammersPerSecond() / total_hammers;
		if (!(num2 <= 0f))
		{
			progress += num * num2;
			if (progress >= 1f)
			{
				CompleteUpgrade();
			}
			else
			{
				settlement.SendState<Village.UpgradeProgressState>();
			}
		}
	}
}
