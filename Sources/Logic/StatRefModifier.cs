namespace Logic;

public class StatRefModifier : Stat.Modifier, IListener
{
	public Stat.GlobalModifier def;

	public Stat tgt_stat;

	public StatRefModifier(Stat.GlobalModifier def)
	{
		this.def = def;
		type = def.type;
	}

	public void SetKingdom(Kingdom k)
	{
		if (def.kingdom_stat == null)
		{
			return;
		}
		if (tgt_stat != null)
		{
			tgt_stat.DelListener(this);
			tgt_stat = null;
		}
		tgt_stat = k?.stats?.Find(def.kingdom_stat);
		Stat stat = base.stat;
		if (stat != null)
		{
			tgt_stat?.AddListener(this);
			if (!IsConst() && stat.var_mods != null && stat.var_mods.Contains(this))
			{
				stat.stats.NotifyChanged(stat);
				return;
			}
			stat.DelModifier(this, notify_changed: false);
			stat.AddModifier(this);
		}
	}

	public void ResolveTargetStat()
	{
		if (tgt_stat != null || stat == null)
		{
			return;
		}
		if (def.own_stat != null)
		{
			tgt_stat = stat.stats.Find(def.own_stat);
		}
		else if (def.kingdom_stat != null && base.owner is Object obj)
		{
			Kingdom kingdom = obj.GetKingdom();
			if (kingdom != null)
			{
				tgt_stat = kingdom.stats?.Find(def.kingdom_stat);
			}
		}
	}

	public override DT.Field GetField()
	{
		return def.field;
	}

	public override void OnActivate(Stats stats, Stat stat, bool from_state = false)
	{
		ResolveTargetStat();
		if (tgt_stat != null)
		{
			tgt_stat.AddListener(this);
		}
	}

	public override void OnDeactivate(Stats stats, Stat stat)
	{
		if (tgt_stat != null)
		{
			tgt_stat.DelListener(this);
		}
	}

	public override bool IsConst()
	{
		ResolveTargetStat();
		if (tgt_stat == null)
		{
			return true;
		}
		if (def.mul_field != null || def.condition_field != null)
		{
			return false;
		}
		if (tgt_stat.IsConst())
		{
			return true;
		}
		return false;
	}

	public override float CalcValue(Stats stats, Stat stat)
	{
		ResolveTargetStat();
		if (tgt_stat == null)
		{
			return 0f;
		}
		float num = def.CalcMultiplier(stats, stat);
		if (num == 0f)
		{
			return 0f;
		}
		return tgt_stat.CalcValue() * num;
	}

	public void OnMessage(object obj, string message, object param)
	{
		Stat stat = base.stat;
		if (stat != null)
		{
			if (!IsConst() && stat.var_mods != null && stat.var_mods.Contains(this))
			{
				stat.stats.NotifyChanged(stat);
				return;
			}
			stat.DelModifier(this, notify_changed: false);
			stat.AddModifier(this);
		}
	}
}
