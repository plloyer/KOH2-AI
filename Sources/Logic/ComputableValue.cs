namespace Logic;

public class ComputableValue : BaseObject
{
	public class FullData : Data
	{
		public float val;

		public float rate;

		public float maxVal;

		public float minVal;

		public static FullData Create()
		{
			return new FullData();
		}

		public override bool InitFrom(object obj)
		{
			ComputableValue computableValue = obj as ComputableValue;
			val = computableValue.Get();
			rate = computableValue.GetRate();
			maxVal = computableValue.maxVal;
			minVal = computableValue.minVal;
			return true;
		}

		public override void Save(Serialization.IWriter ser)
		{
			ser.WriteFloat(val, "value");
			ser.WriteFloat(rate, "rate");
			ser.WriteFloat(maxVal, "maxVal");
			ser.WriteFloat(minVal, "minVal");
		}

		public override void Load(Serialization.IReader ser)
		{
			val = ser.ReadFloat("value");
			rate = ser.ReadFloat("rate");
			maxVal = ser.ReadFloat("maxVal");
			minVal = ser.ReadFloat("minVal");
		}

		public override object GetObject(Game game)
		{
			return new ComputableValue(val, rate, game, minVal, maxVal);
		}

		public override bool ApplyTo(object obj, Game game)
		{
			ComputableValue obj2 = obj as ComputableValue;
			obj2.SetMinMax(minVal, maxVal);
			obj2.Set(val, rate);
			return true;
		}
	}

	private float curVal;

	private float maxVal = float.PositiveInfinity;

	private float minVal;

	private float rate;

	private Time lastTime = Time.Zero;

	public Game game { get; private set; }

	public ComputableValue(float val, float rate, Game game, float minVal = 0f, float maxVal = float.PositiveInfinity)
	{
		this.game = game;
		this.maxVal = maxVal;
		this.minVal = minVal;
		Set(val, rate);
	}

	public float Get()
	{
		Game game = this.game;
		if (game?.scheduler == null)
		{
			return 0f;
		}
		curVal += (game.time - lastTime) * rate;
		lastTime = game.time;
		Clamp();
		return curVal;
	}

	public void Set(float val, bool clamp = true)
	{
		Game game = this.game;
		if (game?.scheduler != null)
		{
			curVal = val;
			if (clamp)
			{
				Clamp();
			}
			lastTime = game.time;
		}
	}

	public void Set(float val, float rate)
	{
		Set(val);
		this.rate = rate;
	}

	public void Add(float add, bool clamp = true)
	{
		Set(Get() + add, clamp);
	}

	public float GetRate()
	{
		return rate;
	}

	public void SetRate(float rate)
	{
		curVal = Get();
		this.rate = rate;
	}

	public float GetMin()
	{
		return minVal;
	}

	public float GetMax()
	{
		return maxVal;
	}

	public void SetMinMax(float min, float max)
	{
		if (!(min > max))
		{
			minVal = min;
			maxVal = max;
			Clamp();
		}
	}

	private void Clamp()
	{
		if (curVal > maxVal)
		{
			curVal = maxVal;
		}
		if (curVal < minVal)
		{
			curVal = minVal;
		}
	}

	public override string ToString()
	{
		return "Computable value:  val = " + Get() + " rate = " + GetRate();
	}
}
