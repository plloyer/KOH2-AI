namespace Logic;

public static class Angle
{
	public static float Normalize360(float a)
	{
		a %= 360f;
		if (a < 0f)
		{
			a += 360f;
		}
		return a;
	}

	public static float Normalize180(float a)
	{
		a %= 360f;
		if (a < -180f)
		{
			a += 360f;
		}
		else if (a > 180f)
		{
			a -= 360f;
		}
		return a;
	}

	public static float Diff(float from, float to)
	{
		return Normalize180(to - from);
	}

	public static float Lerp(float from, float to, float a)
	{
		float num = Diff(from, to);
		return from + num * a;
	}

	public static float Lerp360(float from, float to, float a)
	{
		float num = Diff(from, to);
		return Normalize360(from + num * a);
	}
}
