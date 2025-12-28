using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Logic;

public class NetworkProfiler
{
	public struct Sample
	{
		public int val;

		public long time;

		public Sample(int val, long time)
		{
			this.val = val;
			this.time = time;
		}

		public override string ToString()
		{
			return time + ": " + val;
		}
	}

	public class Sampler
	{
		public const int size = 100;

		public Sample[] samples = new Sample[100];

		public int count;

		public long sum;

		private int cur;

		public void Reset()
		{
			count = 0;
			sum = 0L;
			cur = 0;
		}

		public void Add(int val, long time)
		{
			if (count <= 0)
			{
				samples[0] = new Sample(val, time);
				count = 1;
				sum = val;
				cur = 0;
			}
			else if (count < 100)
			{
				cur = (cur + 1) % 100;
				samples[cur] = new Sample(val, time);
				count++;
				sum += val;
			}
			else
			{
				cur = (cur + 1) % 100;
				sum += val - samples[cur].val;
				samples[cur] = new Sample(val, time);
			}
		}

		public bool Calc(long time, out int BPS, out int CPS)
		{
			if (count <= 0)
			{
				BPS = (CPS = 0);
				return false;
			}
			int num = ((count >= 100) ? ((cur + 1) % 100) : 0);
			long num2 = time - samples[num].time;
			if (num2 <= 0)
			{
				BPS = (int)sum;
				CPS = count;
				return false;
			}
			BPS = (int)(sum * 1000 / num2);
			CPS = (int)(count * 1000 / num2);
			return true;
		}
	}

	public class Stat
	{
		public int count;

		public int min = int.MaxValue;

		public int max = int.MinValue;

		public long sum;

		public int avg
		{
			get
			{
				if (count > 0)
				{
					return (int)(sum / count);
				}
				return 0;
			}
		}

		public override string ToString()
		{
			if (count <= 0)
			{
				return "---";
			}
			if (min >= max)
			{
				return count + " x " + min + " = " + ((float)sum / 1000f).ToString("F3") + "K";
			}
			return count + " x " + avg + " (" + min + " - " + max + ") = " + ((float)sum / 1000f).ToString("F3") + "K";
		}

		public string ToCSV(string d = ";")
		{
			return $"{count}{d}{avg}{d}{min}{d}{max}{d}{sum}";
		}

		public void Reset()
		{
			count = 0;
			min = int.MaxValue;
			max = int.MinValue;
			sum = 0L;
		}

		public void Add(int val)
		{
			count++;
			if (val < min)
			{
				min = val;
			}
			if (val > max)
			{
				max = val;
			}
			sum += val;
		}
	}

	public struct MessageId
	{
		public int val;

		public Multiplayer.MessageId msgid => (Multiplayer.MessageId)(byte)val;

		public Serialization.ObjectType tid => (Serialization.ObjectType)(byte)(val >> 8);

		public Serialization.ObjectTypeInfo ti => Serialization.ObjectTypeInfo.Get(tid);

		public byte omid => (byte)(val >> 16);

		public byte ssid => (byte)(val >> 24);

		public MessageId(Multiplayer.MessageId msgid, Serialization.ObjectType tid = Serialization.ObjectType.COUNT, byte omid = 0, byte ssid = 0)
		{
			val = (ssid << 24) | (omid << 16) | ((int)tid << 8) | (int)msgid;
		}

		public override string ToString()
		{
			Multiplayer.MessageId messageId = msgid;
			switch (messageId)
			{
			case Multiplayer.MessageId.OBJ_EVENT:
			{
				Serialization.Event eventAttr = Serialization.GetEventAttr(omid, ti);
				return tid.ToString() + "." + (eventAttr?.name ?? ("<" + omid + ">"));
			}
			case Multiplayer.MessageId.OBJ_STATE:
			{
				Serialization.State stateAttr2 = Serialization.GetStateAttr(omid, ti);
				return tid.ToString() + "." + (stateAttr2?.name ?? ("<" + omid + ">"));
			}
			case Multiplayer.MessageId.OBJ_SUBSTATE:
			{
				Serialization.Substate substateAttr = Serialization.GetSubstateAttr(omid, ssid, ti);
				if (substateAttr == null)
				{
					Serialization.State stateAttr = Serialization.GetStateAttr(omid, ti);
					return tid.ToString() + ".<" + (stateAttr?.name ?? omid.ToString()) + "." + ssid + ">";
				}
				return tid.ToString() + "." + substateAttr.state_attr.name + "." + substateAttr.name;
			}
			default:
				return messageId.ToString();
			}
		}
	}

	public class MessageInfo
	{
		public string name;

		public Stat stat = new Stat();

		public override string ToString()
		{
			return name + ": " + stat.ToString();
		}

		public string ToCSV(string d = ";")
		{
			return name + d + stat.ToCSV(d);
		}
	}

	public bool verbose = true;

	private BinarySerializeReader reader;

	private bool debug_headers;

	public object Lock = new object();

	public Stopwatch timer;

	public Stat packets = new Stat();

	public Stat messages = new Stat();

	public Sampler bps = new Sampler();

	public Dictionary<int, MessageInfo> messages_info = new Dictionary<int, MessageInfo>();

	private static List<MessageInfo> tmp_list = new List<MessageInfo>(512);

	public NetworkProfiler(bool useDebugHeaders)
	{
		reader = new BinarySerializeReader(null, null);
		debug_headers = useDebugHeaders;
	}

	public override string ToString()
	{
		lock (Lock)
		{
			CalcRates(out var BPS, out var PPS);
			CalcTotalRates(out var BPS2, out var _);
			return "Messages: " + messages.ToString() + ", Packets: " + packets.ToString() + ", PPS: " + PPS + ", BPS: " + BPS + " / " + BPS2;
		}
	}

	public string ToCSV()
	{
		tmp_list.Clear();
		foreach (KeyValuePair<int, MessageInfo> item in messages_info)
		{
			MessageInfo value = item.Value;
			tmp_list.Add(value);
		}
		tmp_list.Sort((MessageInfo a, MessageInfo b) => b.stat.sum.CompareTo(a.stat.sum));
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("name;count;avg;min;max;total");
		for (int num = 0; num < tmp_list.Count; num++)
		{
			MessageInfo messageInfo = tmp_list[num];
			stringBuilder.AppendLine(messageInfo.ToCSV());
		}
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("TOTAL;" + messages.ToCSV());
		return stringBuilder.ToString();
	}

	public void Reset()
	{
		lock (Lock)
		{
			timer = null;
			packets.Reset();
			messages.Reset();
			bps.Reset();
		}
	}

	private void StartTimer()
	{
		if (timer == null)
		{
			timer = Stopwatch.StartNew();
		}
	}

	public long Now()
	{
		if (timer == null)
		{
			return 0L;
		}
		return timer.ElapsedMilliseconds;
	}

	public void AddPacket(int len)
	{
		lock (Lock)
		{
			StartTimer();
			packets.Add(len);
			bps.Add(len, Now());
		}
	}

	public void AddMessage(byte[] buf, int len)
	{
		lock (Lock)
		{
			StartTimer();
			messages.Add(len);
			if (verbose)
			{
				MessageId messageId = ReadMessageId(buf, len);
				if (!messages_info.TryGetValue(messageId.val, out var value))
				{
					value = new MessageInfo();
					value.name = messageId.ToString();
					messages_info.Add(messageId.val, value);
				}
				value.stat.Add(len);
			}
		}
	}

	private MessageId ReadMessageId(byte[] buf, int len)
	{
		Serialization.ObjectType tid = Serialization.ObjectType.COUNT;
		byte id = 0;
		byte substate_id = 0;
		Serialization.ObjectTypeInfo ti = null;
		reader.Init(new MemStream(buf, 0, len), debug_headers);
		Multiplayer.MessageId messageId = (Multiplayer.MessageId)reader.ReadByte(null);
		int nid;
		switch (messageId)
		{
		case Multiplayer.MessageId.OBJ_EVENT:
		case Multiplayer.MessageId.OBJ_STATE:
			reader.ReadMessageHeader(out id, out tid, out ti, out nid);
			break;
		case Multiplayer.MessageId.OBJ_SUBSTATE:
		{
			reader.ReadMessageHeader(out id, out tid, out ti, out nid, out substate_id, out var _);
			break;
		}
		}
		return new MessageId(messageId, tid, id, substate_id);
	}

	public void CalcRates(out int BPS, out int PPS)
	{
		long time = Now();
		lock (Lock)
		{
			bps.Calc(time, out BPS, out PPS);
		}
	}

	public void CalcTotalRates(out int BPS, out int PPS)
	{
		long num = Now();
		if (num <= 0)
		{
			BPS = 0;
			PPS = 0;
			return;
		}
		lock (Lock)
		{
			BPS = (int)(packets.sum * 1000 / num);
			PPS = (int)(packets.count * 1000 / num);
		}
	}
}
