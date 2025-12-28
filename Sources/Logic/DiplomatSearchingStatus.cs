using System.Collections.Generic;

namespace Logic;

public class DiplomatSearchingStatus : Status
{
	protected struct Distance
	{
		public Kingdom kingdom;

		public int distance;

		public Distance(Kingdom k, int distance)
		{
			kingdom = k;
			this.distance = distance;
		}
	}

	public new class FullData : Status.FullData
	{
		private List<NID> kingdoms = new List<NID>();

		private List<float> times = new List<float>();

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
			if (!(obj is DiplomatSearchingStatus diplomatSearchingStatus))
			{
				return false;
			}
			foreach (KeyValuePair<Kingdom, Time> item in diplomatSearchingStatus.recentlyProcessed)
			{
				kingdoms.Add(item.Key);
				times.Add(item.Value - diplomatSearchingStatus.owner.game.time);
			}
			return true;
		}

		public override void Save(Serialization.IWriter ser)
		{
			base.Save(ser);
			ser.Write7BitUInt(kingdoms.Count, "count");
			for (int i = 0; i < kingdoms.Count; i++)
			{
				ser.WriteNID<Kingdom>(kingdoms[i], "kingdom", i);
				ser.WriteFloat(times[i], "time", i);
			}
		}

		public override void Load(Serialization.IReader ser)
		{
			base.Load(ser);
			int num = ser.Read7BitUInt("count");
			for (int i = 0; i < num; i++)
			{
				kingdoms.Add(ser.ReadNID<Kingdom>("kingdom", i));
				times.Add(ser.ReadFloat("time", i));
			}
		}

		public override bool ApplyTo(object obj, Game game)
		{
			if (!base.ApplyTo(obj, game))
			{
				return false;
			}
			if (!(obj is DiplomatSearchingStatus diplomatSearchingStatus))
			{
				return false;
			}
			diplomatSearchingStatus.recentlyProcessed.Clear();
			for (int i = 0; i < kingdoms.Count; i++)
			{
				diplomatSearchingStatus.recentlyProcessed[kingdoms[i].Get<Kingdom>(game)] = game.time + times[i];
			}
			return true;
		}
	}

	private Dictionary<Kingdom, Time> recentlyProcessed = new Dictionary<Kingdom, Time>();

	private List<Distance> distances;

	private int numKingdomsPerTick = 5;

	private int timeoutPerKingdom = 1800;

	private bool defLoaded;

	public DiplomatSearchingStatus()
		: base(null)
	{
	}

	public DiplomatSearchingStatus(Def def)
		: base(def)
	{
	}

	public new static Status Create(Def def)
	{
		return new DiplomatSearchingStatus(def);
	}

	public void LoadDef()
	{
		if (!defLoaded)
		{
			numKingdomsPerTick = def.field.GetInt("numKingdomsPerTick");
			timeoutPerKingdom = def.field.GetInt("timeoutPerKingdom");
			distances = new List<Distance>(game.kingdoms.Count);
			defLoaded = true;
		}
	}

	public void FindBestKingdoms()
	{
		distances.Clear();
		for (int i = 0; i < game.kingdoms.Count; i++)
		{
			Kingdom kingdom = game.kingdoms[i];
			if (!kingdom.is_player && kingdom != base.own_kingdom)
			{
				int num = base.own_kingdom.DistanceToKingdom(kingdom);
				if (num != int.MaxValue && (!recentlyProcessed.TryGetValue(kingdom, out var value) || !(value + timeoutPerKingdom > game.time)))
				{
					distances.Add(new Distance(kingdom, num));
				}
			}
		}
		distances.Sort((Distance d1, Distance d2) => d1.distance.CompareTo(d2.distance));
	}

	public virtual Offer GetReversedOffer(Offer o)
	{
		return null;
	}

	public virtual bool TrySendOffer(Offer offer)
	{
		Offer counterOffer;
		string text = offer.DecideAIAnswer(out counterOffer);
		if (text == "counter_offer")
		{
			offer.answer = text;
			counterOffer?.Send();
			return true;
		}
		if (text == "accept")
		{
			Offer reversedOffer = GetReversedOffer(offer);
			if (reversedOffer.Validate() == "ok")
			{
				reversedOffer.Send();
				return true;
			}
		}
		return false;
	}

	public virtual bool ProcessKingdom(Kingdom k)
	{
		return false;
	}

	public override void OnUpdate()
	{
		if (!IsAuthority())
		{
			return;
		}
		base.OnUpdate();
		LoadDef();
		FindBestKingdoms();
		for (int i = 0; i < numKingdomsPerTick && i < distances.Count; i++)
		{
			Kingdom kingdom = distances[i].kingdom;
			bool num = ProcessKingdom(kingdom);
			recentlyProcessed[kingdom] = game.time;
			if (num)
			{
				break;
			}
		}
		base.statuses?.obj?.SendSubstate<StatusesState.StatusState>(base.statuses.FindIndex<DiplomatSearchingStatus>());
	}
}
