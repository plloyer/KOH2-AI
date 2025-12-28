using System.Collections.Generic;

namespace Logic;

public class EmperorOftheWorldSupportNotificator : Component
{
	public class Def : Logic.Def
	{
		public float interval_time = 900f;

		public override bool Load(Game game)
		{
			DT.Field field = base.field;
			interval_time = field.GetFloat("interval_time");
			return true;
		}
	}

	public Def def;

	public EmperorOfTheWorld eow;

	public Dictionary<Kingdom, List<Kingdom>> previouslySupporting = new Dictionary<Kingdom, List<Kingdom>>();

	public List<Kingdom> currentlyNotified = new List<Kingdom>();

	private List<Kingdom> currentlySupportersToClear = new List<Kingdom>();

	public EmperorOftheWorldSupportNotificator(EmperorOfTheWorld eow)
		: base(eow)
	{
		this.eow = eow;
		def = base.game.defs.Get<Def>("EmperorOftheWorldSupportNotificator");
		UpdateAfter(def.interval_time);
	}

	private bool TryNotify(Kingdom gp, Kingdom plr)
	{
		if (eow.CalcVoteWeight(gp, plr) <= 0)
		{
			if (previouslySupporting.TryGetValue(gp, out var value) && value.Contains(plr))
			{
				plr.FireEvent("eow_lost_support", gp, plr.id);
				value.Remove(plr);
				currentlyNotified.Add(plr);
				return true;
			}
		}
		else
		{
			List<Kingdom> value2 = null;
			if (!previouslySupporting.TryGetValue(gp, out value2) || !value2.Contains(plr))
			{
				if (value2 == null)
				{
					value2 = new List<Kingdom>();
					previouslySupporting.Add(gp, value2);
				}
				plr.FireEvent("eow_gained_support", gp, plr.id);
				currentlyNotified.Add(plr);
				value2.Add(plr);
				return true;
			}
		}
		return false;
	}

	private bool ClearNonGreatPowers()
	{
		List<Kingdom> list = base.game.great_powers.TopKingdoms();
		foreach (KeyValuePair<Kingdom, List<Kingdom>> item in previouslySupporting)
		{
			Kingdom key = item.Key;
			if (list.Contains(key))
			{
				continue;
			}
			foreach (Kingdom item2 in item.Value)
			{
				if (!currentlyNotified.Contains(item2) && eow.ValidateVote(item2))
				{
					item2.FireEvent("eow_lost_support", key, item2.id);
				}
			}
			currentlySupportersToClear.Add(key);
		}
		foreach (Kingdom item3 in currentlySupportersToClear)
		{
			previouslySupporting.Remove(item3);
		}
		bool result = currentlySupportersToClear.Count != 0;
		currentlySupportersToClear.Clear();
		return result;
	}

	public override void OnUpdate()
	{
		UpdateAfter(def.interval_time);
		if (!eow.IsAuthority())
		{
			return;
		}
		List<Kingdom> list = base.game.great_powers.TopKingdoms();
		bool flag = false;
		for (int i = 0; i < list.Count; i++)
		{
			Kingdom kingdom = list[i];
			if (!kingdom.is_player || !eow.ValidateVote(kingdom))
			{
				continue;
			}
			int num = base.game.Random(0, list.Count);
			for (int j = 0; j < list.Count; j++)
			{
				Kingdom kingdom2 = list[(j + num) % list.Count];
				if (!kingdom2.is_player && kingdom2 != kingdom && TryNotify(kingdom2, kingdom))
				{
					flag = true;
					break;
				}
			}
		}
		if (ClearNonGreatPowers())
		{
			flag = true;
		}
		if (flag)
		{
			eow.SendState<EmperorOfTheWorld.NotificatorState>();
		}
		currentlyNotified.Clear();
		base.OnUpdate();
	}
}
