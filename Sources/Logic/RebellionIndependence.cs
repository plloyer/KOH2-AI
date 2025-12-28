using System.Collections.Generic;

namespace Logic;

public class RebellionIndependence : Component
{
	public class Def : Logic.Def
	{
		public float time_interval = 300f;

		public float chance_per_occupied_province = 5f;

		public float chance_when_fully_occupied_zone = 100f;

		public override bool Load(Game game)
		{
			DT.Field field = base.field;
			time_interval = field.GetFloat("time_interval", null, time_interval);
			chance_per_occupied_province = field.GetFloat("chance_per_occupied_province", null, chance_per_occupied_province);
			chance_when_fully_occupied_zone = field.GetFloat("chance_when_fully_occupied_zone", null, chance_when_fully_occupied_zone);
			return true;
		}
	}

	private Rebellion rebellion;

	private Def def;

	private bool canDeclareIndependence;

	public bool isDeclaringIndependence;

	public RebellionIndependence(Rebellion r)
		: base(r)
	{
		rebellion = r;
		def = base.game.defs.GetBase<Def>();
	}

	public List<Realm> GetIndependenceRealms()
	{
		return new List<Realm>(rebellion.occupiedRealms);
	}

	public Kingdom GetBaseIndependenceKingdom()
	{
		return base.game.GetIndependenceKingdom(rebellion.occupiedRealms, rebellion.IsReligious() ? rebellion.religion : null, rebellion.enemies);
	}

	public Kingdom GetIndependenceKingdom()
	{
		if (!rebellion.IsLoyalist())
		{
			return GetBaseIndependenceKingdom();
		}
		Kingdom hq_kingdom = base.game.religions.catholic.hq_kingdom;
		if (rebellion.loyal_to == hq_kingdom.id && hq_kingdom.IsDefeated())
		{
			return null;
		}
		Kingdom loyalTo = rebellion.GetLoyalTo();
		if (loyalTo.IsDefeated())
		{
			return loyalTo;
		}
		for (int i = 0; i < rebellion.zone.Count; i++)
		{
			if (loyalTo.externalBorderRealms.Contains(rebellion.zone[i]))
			{
				return loyalTo;
			}
		}
		Kingdom baseIndependenceKingdom = GetBaseIndependenceKingdom();
		if (baseIndependenceKingdom != null && baseIndependenceKingdom.IsDefeated())
		{
			return baseIndependenceKingdom;
		}
		return loyalTo;
	}

	private void HandleRebel(Rebel rebel, Kingdom independenceKingdom, List<Army> armiesToKeep)
	{
		Army army = rebel.army;
		if (army.battle != null)
		{
			army.battle.Cancel(Battle.VictoryReason.Retreat);
		}
		if (independenceKingdom.court == null || independenceKingdom.court.Count == 0)
		{
			independenceKingdom.InitCourt();
		}
		if (independenceKingdom.GetFreeCourtSlotIndex() == -1)
		{
			army.Destroy();
			return;
		}
		if ((float)base.game.Random(0, 100) >= rebel.def.chance_join_court_on_independence)
		{
			army.Destroy();
			return;
		}
		armiesToKeep.Add(army);
		army.Stop();
		rebel?.GetSpawnKingdom()?.DelSpecialCourtMember(rebel.character);
		rebel.SetArmy(null);
		army.SetRebel(null);
		rebel.Destroy();
		army.leader.FireEvent("declared_independence", null);
	}

	public static bool IsInNonRDIBattle(Army army)
	{
		Battle battle = army.battle;
		if (battle == null)
		{
			return false;
		}
		return !battle.is_plunder;
	}

	public bool DeclareIndependence(bool forced = false)
	{
		if (!rebellion.IsAuthority())
		{
			return false;
		}
		if (!forced)
		{
			if (IsInNonRDIBattle(rebellion.leader.army))
			{
				return false;
			}
			for (int i = 0; i < rebellion.occupiedRealms.Count; i++)
			{
				if (rebellion.occupiedRealms[i].castle?.battle != null)
				{
					return false;
				}
			}
			for (int j = 0; j < rebellion.rebels.Count; j++)
			{
				if (IsInNonRDIBattle(rebellion.rebels[j].army))
				{
					return false;
				}
			}
		}
		isDeclaringIndependence = true;
		List<Kingdom> list = new List<Kingdom>();
		List<Realm> independenceRealms = GetIndependenceRealms();
		Kingdom independenceKingdom = GetIndependenceKingdom();
		Kingdom loyalTo = rebellion.GetLoyalTo();
		if (independenceRealms.Count == 0 || independenceKingdom == null)
		{
			return false;
		}
		List<Army> list2 = new List<Army>(rebellion.rebels.Count);
		while (rebellion.rebels.Count > 1)
		{
			Rebel rebel = ((rebellion.rebels[0] == rebellion.leader) ? rebellion.rebels[1] : rebellion.rebels[0]);
			HandleRebel(rebel, independenceKingdom, list2);
		}
		HandleRebel(rebellion.leader, independenceKingdom, list2);
		for (int k = 0; k < independenceRealms.Count; k++)
		{
			Kingdom kingdom = independenceRealms[k].GetKingdom();
			if (!list.Contains(kingdom))
			{
				list.Add(kingdom);
			}
		}
		bool flag = independenceKingdom.IsDefeated();
		Religion religion = (rebellion.IsReligious() ? rebellion.religion : null);
		List<Character> courtChars = null;
		Character character = rebellion.leader.character;
		if (!character.IsValid())
		{
			character = null;
		}
		if (character != null)
		{
			courtChars = new List<Character> { rebellion.leader.character };
		}
		independenceKingdom.DeclareIndependenceOrJoin(independenceRealms, character, courtChars, list2, religion, !forced);
		independenceKingdom.AddResources(KingdomAI.Expense.Category.Military, ResourceType.Gold, rebellion.GetWealth());
		if (flag && rebellion.IsLoyalist() && loyalTo != independenceKingdom && !loyalTo.IsDefeated() && !loyalTo.IsVassal())
		{
			independenceKingdom.SetSovereignState(loyalTo);
		}
		List<Kingdom> list3 = null;
		for (int l = 0; l < list.Count; l++)
		{
			if (list[l].IsDefeated())
			{
				Kingdom item = list[l];
				if (list3 == null)
				{
					list3 = new List<Kingdom>();
				}
				list3.Add(item);
				list.Remove(item);
				l--;
			}
		}
		Vars vars = new Vars();
		vars.Set("realms", independenceRealms);
		vars.Set("independence_kingdom", independenceKingdom);
		if (list.Count != 0)
		{
			vars.Set("affected_kingdoms", list);
		}
		if (list3 != null)
		{
			vars.Set("defeated_kingdoms", list3);
		}
		vars.Set("formNewKingdom", flag);
		for (int m = 0; m < list.Count; m++)
		{
			list[m].FireEvent("rebellion_independence_rebels", vars, list[m].id);
		}
		independenceKingdom.FireEvent("rebellion_independence_loyalists", vars, independenceKingdom.id);
		if (flag && rebellion.IsLoyalist() && independenceKingdom.sovereignState == loyalTo)
		{
			loyalTo.FireEvent("rebellion_independce_newKingdom_loyalists", vars, loyalTo.id);
		}
		return true;
	}

	private bool RebellionFullyOccupiedZone()
	{
		foreach (Realm item in rebellion.zone)
		{
			if (!rebellion.occupiedRealms.Contains(item))
			{
				return false;
			}
		}
		return true;
	}

	private bool CheckIndependence()
	{
		if (!canDeclareIndependence)
		{
			return false;
		}
		if ((float)base.game.Random(0, 100) < (float)rebellion.occupiedRealms.Count * def.chance_per_occupied_province + (RebellionFullyOccupiedZone() ? def.chance_when_fully_occupied_zone : 0f))
		{
			return DeclareIndependence();
		}
		return false;
	}

	public void Refresh(Realm r)
	{
		if (!rebellion.marked_for_destroy)
		{
			if (rebellion.occupiedRealms.Count == 0)
			{
				StopUpdating();
				canDeclareIndependence = false;
			}
			else if (!CheckIndependence() && !IsRegisteredForUpdate())
			{
				UpdateAfter(r.GetKingdom().GetCrownAuthority().GetRebelIndependenceTimeInitialInterval());
			}
		}
	}

	public override void OnUpdate()
	{
		canDeclareIndependence = true;
		if (!CheckIndependence())
		{
			UpdateAfter(def.time_interval);
		}
		base.OnUpdate();
	}
}
