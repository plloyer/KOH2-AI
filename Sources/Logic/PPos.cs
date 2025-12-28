using System;
using Unity.Mathematics;
using UnityEngine;

namespace Logic;

[Serializable]
public struct PPos
{
	public Point pos;

	public int paID;

	public static readonly PPos Invalid = default(PPos);

	public static readonly PPos Zero = new PPos(0f, 0f, 0);

	public static readonly PPos UnitUp = new PPos(0f, 1f);

	public static readonly PPos UnitRight = new PPos(1f);

	public float x
	{
		get
		{
			return pos.x;
		}
		set
		{
			pos.x = value;
		}
	}

	public float y
	{
		get
		{
			return pos.y;
		}
		set
		{
			pos.y = value;
		}
	}

	public PPos(float x = 0f, float y = 0f, int paID = 0)
	{
		pos = new Point(x, y);
		this.paID = paID;
	}

	public PPos(Point p, int paID = 0)
	{
		pos = p;
		this.paID = paID;
	}

	public override string ToString()
	{
		return pos.ToString() + ", " + paID;
	}

	public static bool TryParse(string s, out PPos pt)
	{
		pt = Point.Invalid;
		if (s == null)
		{
			return false;
		}
		int num = s.IndexOf(',');
		if (num <= 0)
		{
			return false;
		}
		string s2 = s.Substring(0, num).Trim();
		string text = s.Substring(num + 1).Trim();
		num = text.IndexOf(',');
		if (num <= 0)
		{
			return false;
		}
		string s3 = text.Substring(0, num).Trim();
		string s4 = text.Substring(num + 1).Trim();
		if (!DT.ParseFloat(s2, out var f))
		{
			return false;
		}
		if (!DT.ParseFloat(s3, out var f2))
		{
			return false;
		}
		if (!DT.ParseFloat(s4, out var f3))
		{
			return false;
		}
		pt.x = f;
		pt.y = f2;
		pt.paID = (int)f3;
		return true;
	}

	public static bool operator ==(PPos pt1, PPos pt2)
	{
		if (pt1.pos == pt2.pos)
		{
			return pt1.paID == pt2.paID;
		}
		return false;
	}

	public static bool operator ==(PPos pt1, Point pt2)
	{
		if (pt1.pos.x == pt2.x)
		{
			return pt1.pos.y == pt2.y;
		}
		return false;
	}

	public static bool operator ==(Point pt1, PPos pt2)
	{
		return pt2 == pt1;
	}

	public static bool operator !=(PPos pt1, PPos pt2)
	{
		if (!(pt1.pos != pt2.pos))
		{
			return pt1.paID != pt2.paID;
		}
		return true;
	}

	public static bool operator !=(PPos pt1, Point pt2)
	{
		if (pt1.pos.x == pt2.x)
		{
			return pt1.pos.y != pt2.y;
		}
		return true;
	}

	public static bool operator !=(Point pt1, PPos pt2)
	{
		return pt2 != pt1;
	}

	public override bool Equals(object obj)
	{
		if (obj is PPos)
		{
			return this == (PPos)obj;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return pos.GetHashCode() ^ paID.GetHashCode();
	}

	public static PPos operator +(PPos pt1, PPos pt2)
	{
		return new PPos(pt1.pos.x + pt2.pos.x, pt1.pos.y + pt2.pos.y, pt1.paID);
	}

	public static PPos operator +(PPos pt1, Point pt2)
	{
		Point point = pt1.pos + pt2;
		return new PPos(point.x, point.y, pt1.paID);
	}

	public static Point operator +(Point pt1, PPos pt2)
	{
		return pt2.pos + pt1;
	}

	public static PPos operator -(PPos pt1, PPos pt2)
	{
		return new PPos(pt1.pos.x - pt2.pos.x, pt1.pos.y - pt2.pos.y, pt1.paID);
	}

	public static PPos operator -(PPos pt1, Point pt2)
	{
		Point point = pt1.pos - pt2;
		return new PPos(point.x, point.y, pt1.paID);
	}

	public static Point operator -(Point pt1, PPos pt2)
	{
		return pt1 - pt2.pos;
	}

	public static PPos operator -(PPos pt)
	{
		return new PPos(-pt.pos, pt.paID);
	}

	public static PPos operator *(PPos pt, float f)
	{
		return new PPos(pt.pos * f, pt.paID);
	}

	public static PPos operator *(float f, PPos pt)
	{
		return new PPos(pt.pos * f, pt.paID);
	}

	public static PPos operator /(PPos pt, float f)
	{
		return new PPos(pt.pos / f, pt.paID);
	}

	public float SqrLength()
	{
		return pos.SqrLength();
	}

	public float Length()
	{
		return (float)Math.Sqrt(SqrLength());
	}

	public void SetLength(float len)
	{
		pos.SetLength(len);
	}

	public void SetPassableArea(int paID)
	{
		this.paID = paID;
	}

	public float Normalize()
	{
		return pos.Normalize();
	}

	public PPos GetNormalized()
	{
		PPos result = this;
		result.Normalize();
		return result;
	}

	public float SqrDist(PPos pt)
	{
		return (pt - this).SqrLength();
	}

	public float SqrDist(Point pt)
	{
		return (pt - this).SqrLength();
	}

	public float Dist(PPos pt)
	{
		return (pt - this).Length();
	}

	public float Dist(Point pt)
	{
		return (pt - this).Length();
	}

	public bool InRange(PPos pt, float range)
	{
		return SqrDist(pt) <= range * range;
	}

	public bool InRange(Point pt, float range)
	{
		return SqrDist(pt) <= range * range;
	}

	public PPos Right(float len = 0f)
	{
		PPos result = new PPos(pos.y, 0f - pos.x, paID);
		if (len != 0f)
		{
			result.SetLength(len);
		}
		return result;
	}

	public float Dot(PPos pt)
	{
		return pos.Dot(pt.pos);
	}

	public float Dot(Point pt)
	{
		return pos.Dot(pt);
	}

	public float ProjLen(PPos v)
	{
		return pos.ProjLen(v.pos);
	}

	public float ProjLen(Point v)
	{
		return pos.ProjLen(v);
	}

	public PPos Project(PPos v)
	{
		v.Normalize();
		float num = Dot(v);
		return v * num;
	}

	public PPos Project(Point v)
	{
		v.Normalize();
		float num = Dot(v);
		return new PPos(v * num, paID);
	}

	public float Heading()
	{
		return pos.Heading();
	}

	public float Heading(PPos to)
	{
		return (to - this).Heading();
	}

	public float Heading(Point to)
	{
		return (to - this).Heading();
	}

	public PPos GetRotated(float sin, float cos)
	{
		return new PPos(pos.GetRotated(sin, cos), paID);
	}

	public PPos GetRotated(float angle)
	{
		return new PPos(pos.GetRotated(angle), paID);
	}

	public static implicit operator PPos(Point v)
	{
		return new PPos(v.x, v.y);
	}

	public static implicit operator PPos(Point3 v)
	{
		return new PPos(v.x, v.z);
	}

	public unsafe float Height(Game game, float fallback = 0f, float offset = 0f)
	{
		if (!PathData.IsGroundPAid(paID) && game != null)
		{
			PathFinding path_finding = game.path_finding;
			if (path_finding != null)
			{
				PathData data = path_finding.data;
				if (data != null)
				{
					_ = data.pointers;
					if (0 == 0 && game.path_finding.data.pointers.Initted != null && *game.path_finding.data.pointers.Initted)
					{
						return Math.Max(fallback, game.path_finding.data.pointers.GetPA(paID - 1).GetHeight(pos) + offset);
					}
				}
			}
		}
		return fallback;
	}

	public Point3 Point3D(Game game, float fallback = 0f, float offset = 0f)
	{
		return new Point3(x, Height(game, fallback, offset), y);
	}

	public static implicit operator PPos(Vector3 v)
	{
		return new PPos(v.x, v.z);
	}

	public static implicit operator Vector3(PPos pt)
	{
		return new Vector3(pt.x, 0f, pt.y);
	}

	public static implicit operator PPos(Vector2 v)
	{
		return new PPos(v.x, v.y);
	}

	public static implicit operator Vector2(PPos pt)
	{
		return new PPos(pt.x, pt.y);
	}

	public static implicit operator PPos(float3 v)
	{
		return new PPos(v.x, v.z);
	}

	public static implicit operator float3(PPos pt)
	{
		return new float3(pt.x, 0f, pt.y);
	}

	public static implicit operator PPos(float2 v)
	{
		return new PPos(v.x, v.y);
	}

	public static implicit operator float2(PPos pt)
	{
		return new float2(pt.x, pt.y);
	}
}
