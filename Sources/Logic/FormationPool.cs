using System;
using System.Collections.Generic;
using UnityEngine;

namespace Logic;

public static class FormationPool
{
	private static Stack<TriangleFormation> triangle_formations_pool = new Stack<TriangleFormation>();

	private static Stack<RectFormation> rect_formations_pool = new Stack<RectFormation>();

	private static Stack<CheckerboardFormation> checkerboard_formations_pool = new Stack<CheckerboardFormation>();

	private static List<TriangleFormation> used_triangle_formations = new List<TriangleFormation>();

	private static List<RectFormation> used_rect_formations = new List<RectFormation>();

	private static List<CheckerboardFormation> used_checker_formations = new List<CheckerboardFormation>();

	public const int POOL_SIZE = 500;

	private static bool initialized = false;

	public static int TrianglePoolCount => triangle_formations_pool.Count;

	public static int RectPoolCount => rect_formations_pool.Count;

	public static int CheckerPoolCount => checkerboard_formations_pool.Count;

	public static int TriangleUsageCount => used_triangle_formations.Count;

	public static int RectUsageCount => used_rect_formations.Count;

	public static int CheckerUsageCount => used_checker_formations.Count;

	public static void Initialize()
	{
		if (!initialized)
		{
			for (int i = 0; i < 500; i++)
			{
				triangle_formations_pool.Push(new TriangleFormation());
				rect_formations_pool.Push(new RectFormation());
				checkerboard_formations_pool.Push(new CheckerboardFormation());
			}
			initialized = true;
		}
	}

	public static void Deinitialize()
	{
		if (TriangleUsageCount > 0 || RectUsageCount > 0 || CheckerUsageCount > 0)
		{
			Debug.LogError($"Trying to deinitialize FormationPool when some of the objects were not returned... \nTriangle: {TriangleUsageCount}, Rect: {RectUsageCount}, Checker: {CheckerUsageCount}");
		}
		foreach (RectFormation item in rect_formations_pool)
		{
			item.Dispose();
		}
		foreach (CheckerboardFormation item2 in checkerboard_formations_pool)
		{
			item2.Dispose();
		}
		foreach (TriangleFormation item3 in triangle_formations_pool)
		{
			item3.Dispose();
		}
		rect_formations_pool.Clear();
		checkerboard_formations_pool.Clear();
		triangle_formations_pool.Clear();
		initialized = false;
		Debug.Log("Formations deinitialized");
	}

	private static void DeinitializeIfPossible()
	{
		if (CheckerUsageCount == 0 && RectUsageCount == 0 && TriangleUsageCount == 0)
		{
			Deinitialize();
		}
	}

	public static Formation Get(Formation.Def def, Squad owner)
	{
		return Formation.Create(def, owner);
	}

	public static TriangleFormation GetTriangle(Formation.Def def, Squad owner)
	{
		Initialize();
		TriangleFormation triangleFormation = triangle_formations_pool.Pop();
		used_triangle_formations.Add(triangleFormation);
		triangleFormation.Setup(def, owner);
		RegisterDebugGetFormation(triangleFormation);
		return triangleFormation;
	}

	public static RectFormation GetRect(Formation.Def def, Squad owner)
	{
		Initialize();
		RectFormation rectFormation = rect_formations_pool.Pop();
		rectFormation.Setup(def, owner);
		used_rect_formations.Add(rectFormation);
		RegisterDebugGetFormation(rectFormation);
		return rectFormation;
	}

	public static CheckerboardFormation GetCheckerboard(Formation.Def def, Squad owner)
	{
		Initialize();
		CheckerboardFormation checkerboardFormation = checkerboard_formations_pool.Pop();
		checkerboardFormation.Setup(def, owner);
		used_checker_formations.Add(checkerboardFormation);
		RegisterDebugGetFormation(checkerboardFormation);
		return checkerboardFormation;
	}

	public static void Return(ref Formation formation)
	{
		if (formation == null)
		{
			return;
		}
		Formation formation2 = formation;
		if (formation2 != null)
		{
			if (!(formation2 is TriangleFormation triangleFormation))
			{
				if (!(formation2 is CheckerboardFormation checkerboardFormation))
				{
					if (!(formation2 is RectFormation rectFormation))
					{
						goto IL_006d;
					}
					RectFormation formation3 = rectFormation;
					ReturnFormation(rect_formations_pool, used_rect_formations, formation3);
				}
				else
				{
					CheckerboardFormation formation4 = checkerboardFormation;
					ReturnFormation(checkerboard_formations_pool, used_checker_formations, formation4);
				}
			}
			else
			{
				TriangleFormation formation5 = triangleFormation;
				ReturnFormation(triangle_formations_pool, used_triangle_formations, formation5);
			}
			DeinitializeIfPossible();
			formation = null;
			return;
		}
		goto IL_006d;
		IL_006d:
		throw new Exception("Not known formation type.");
	}

	private static void ReturnFormation<T>(Stack<T> pool, List<T> usages, T formation) where T : Formation
	{
		bool flag = usages.Contains(formation);
		bool flag2 = pool.Contains(formation);
		if (flag && !flag2)
		{
			pool.Push(formation);
			usages.Remove(formation);
			RegisterReturnFormation(formation);
		}
		else if (flag && flag2)
		{
			Debug.LogError($"{formation.GetType()} formation is in the pool and used at the same time!. \n\n Get stack trace: \n{GetStackTraceFor(formation)}, \n\n last returned: \n{ReturnStackTraceFor(formation)}");
		}
		else if (!flag && flag2)
		{
			Debug.LogError($"Trying to return {formation.GetType()} formation that is present in the pool!. \n\n Get stack trace: \n{GetStackTraceFor(formation)}, \n\n last returned: \n{ReturnStackTraceFor(formation)}");
		}
		else if (!flag && !flag2)
		{
			Debug.LogError($"Trying to return {formation.GetType()} formation that was not created by the pool!. \n\n Get stack trace: \n{GetStackTraceFor(formation)}, \n\n last returned: \n{ReturnStackTraceFor(formation)}");
		}
	}

	private static void RegisterDebugGetFormation(object formation)
	{
	}

	private static void RegisterReturnFormation(object obj)
	{
	}

	private static string GetStackTraceFor(object formation)
	{
		return "-";
	}

	private static string ReturnStackTraceFor(object formation)
	{
		return "-";
	}
}
