using UnityEngine;

namespace Logic;

public class TradeExpeditionStatus : Status
{
	public new class FullData : Status.FullData
	{
		public string random_name_key;

		public new static FullData Create()
		{
			return new FullData();
		}

		public override bool InitFrom(object obj)
		{
			if (!base.InitFrom(obj))
			{
				return false;
			}
			if (!(obj is TradeExpeditionStatus tradeExpeditionStatus))
			{
				return false;
			}
			random_name_key = tradeExpeditionStatus.random_name_key;
			return true;
		}

		public override void Save(Serialization.IWriter ser)
		{
			base.Save(ser);
			ser.WriteStr(random_name_key, "random_name_key");
		}

		public override void Load(Serialization.IReader ser)
		{
			base.Load(ser);
			random_name_key = ser.ReadStr("random_name_key");
		}

		public override bool ApplyTo(object obj, Game game)
		{
			if (!base.ApplyTo(obj, game))
			{
				return false;
			}
			if (!(obj is TradeExpeditionStatus tradeExpeditionStatus))
			{
				return false;
			}
			tradeExpeditionStatus.random_name_key = random_name_key;
			return true;
		}
	}

	private string random_name_key = "";

	private DT.Field profit_field;

	private float kill_chance;

	public TradeExpeditionStatus(Def def)
		: base(def)
	{
	}

	public new static Status Create(Def def)
	{
		return new TradeExpeditionStatus(def);
	}

	public override bool AllowMultiple()
	{
		return true;
	}

	public override bool IsIdle()
	{
		return true;
	}

	public override void OnInit()
	{
		DT.Field field = def.field.FindChild("fantasyNames");
		random_name_key = field.children[game.Random(0, field.children.Count)].key;
		kill_chance = def.field.GetFloat("chance_to_kill_perc");
		profit_field = def.field.FindChild("profit");
		OnStart();
	}

	public override Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		if (key == "random_fantasy_name")
		{
			return "TradeExpeditionStatus.fantasyNames." + random_name_key;
		}
		return base.GetVar(key, vars, as_value);
	}

	public override void OnUpdate()
	{
		if (IsAuthority() && (float)game.Random(0, 100) < kill_chance && base.own_character.IsAlive())
		{
			base.own_character.Die(new DeadStatus("killed_in_trade_colony", base.own_character));
		}
		base.OnUpdate();
	}

	public float GetProfit()
	{
		if (profit_field == null)
		{
			return 0f;
		}
		return Mathf.Floor(profit_field.Float(new Vars(this)));
	}
}
