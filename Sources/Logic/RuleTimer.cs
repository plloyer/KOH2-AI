using System.Collections.Generic;

namespace Logic;

public class RuleTimer
{
	public class Def
	{
		public Timer.Def def;

		public GameRule.Def rule;

		public List<GameRule.Def> child_rules;

		public bool trigger_parent;

		public bool stop_parent;

		public OutcomeDef outcomes;

		public void OnStart(Timer t)
		{
			ObjRules.Find(t.obj, rule)?.OnTimerStarted(t);
		}

		public void OnStop(Timer t)
		{
			ObjRules.Find(t.obj, rule)?.OnTimerStopped(t);
		}

		public void OnTick(Timer t)
		{
			ObjRules.Find(t.obj, rule)?.OnTimerTick(t);
		}

		public override string ToString()
		{
			return def.name;
		}
	}

	public Def def;

	public Timer timer;

	public List<GameRule> child_rules;

	public override string ToString()
	{
		return def.ToString();
	}
}
