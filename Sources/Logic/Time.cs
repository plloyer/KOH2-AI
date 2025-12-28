namespace Logic;

public struct Time
{
	public long milliseconds;

	public static Time Zero = new Time(0L);

	public float seconds => (float)milliseconds / 1000f;

	public float minutes => (float)milliseconds / 60000f;

	public float hours => seconds / 3600f;

	public Time(long ms = 0L)
	{
		milliseconds = ms;
	}

	public static bool operator ==(Time tm1, Time tm2)
	{
		return tm1.milliseconds == tm2.milliseconds;
	}

	public static bool operator !=(Time tm1, Time tm2)
	{
		return tm1.milliseconds != tm2.milliseconds;
	}

	public static bool operator >(Time tm1, Time tm2)
	{
		return tm1.milliseconds > tm2.milliseconds;
	}

	public static bool operator <(Time tm1, Time tm2)
	{
		return tm1.milliseconds < tm2.milliseconds;
	}

	public static bool operator >=(Time tm1, Time tm2)
	{
		return tm1.milliseconds >= tm2.milliseconds;
	}

	public static bool operator <=(Time tm1, Time tm2)
	{
		return tm1.milliseconds <= tm2.milliseconds;
	}

	public static Time operator +(Time tm, float sec)
	{
		long num = (long)(sec * 1000f + 0.5f);
		return new Time(tm.milliseconds + num);
	}

	public static Time operator -(Time tm, float sec)
	{
		long num = (long)(sec * 1000f + 0.5f);
		return new Time(tm.milliseconds - num);
	}

	public static float operator -(Time tm1, Time tm2)
	{
		return (float)(tm1.milliseconds - tm2.milliseconds) / 1000f;
	}

	public override int GetHashCode()
	{
		return (int)milliseconds;
	}

	public override bool Equals(object obj)
	{
		if (obj is Time)
		{
			return milliseconds == ((Time)obj).milliseconds;
		}
		return false;
	}

	public override string ToString()
	{
		bool num = milliseconds < 0;
		long num2 = (num ? (-milliseconds) : milliseconds);
		int num3 = (int)(num2 / 1000);
		int num4 = (int)(num2 % 1000);
		string arg = (num ? "-" : "");
		return $"{arg}{num3}.{num4:D3}";
	}

	public string ToHMSString()
	{
		bool num = milliseconds < 0;
		long num2 = (num ? (-milliseconds) : milliseconds);
		int num3 = (int)(num2 / 1000);
		int num4 = (int)(num2 % 1000);
		int num5 = num3 / 60;
		num3 %= 60;
		int num6 = num5 / 60;
		num5 %= 60;
		string text = (num ? "-" : "");
		return $"{text}{num6}:{num5:D2}:{num3:D2}.{num4:D3}";
	}
}
