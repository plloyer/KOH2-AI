using System.Collections.Generic;

namespace Logic;

public class DBGOffersData
{
	public class ArgData
	{
		public int total_times;

		public Dictionary<int, int> times_on_argument = new Dictionary<int, int>();
	}

	public static bool tracking_enabled;

	public int sent;

	public Dictionary<string, ArgData> argsData = new Dictionary<string, ArgData>();

	public float validateWithoutArgs_minTime = float.MaxValue;

	public float validateWithoutArgs_maxTime;

	public float validateWithoutArgs_totalTime;

	public long validateWithoutArgs_count;

	public float fillOfferArgs_minTime = float.MaxValue;

	public float fillOfferArgs_maxTime;

	public float fillOfferArgs_totalTime;

	public long fillOfferArgs_count;

	public void RecordSending(Offer offer)
	{
		sent++;
		if (offer.args == null)
		{
			return;
		}
		for (int i = 0; i < offer.args.Count; i++)
		{
			if (offer.args[i].obj_val is Offer offer2)
			{
				ArgData value = null;
				if (!argsData.TryGetValue(offer2.def.field.key, out value))
				{
					value = new ArgData();
					argsData.Add(offer2.def.field.key, value);
				}
				value.total_times++;
				int value2 = 0;
				value.times_on_argument.TryGetValue(i, out value2);
				value.times_on_argument[i] = value2 + 1;
			}
		}
	}
}
