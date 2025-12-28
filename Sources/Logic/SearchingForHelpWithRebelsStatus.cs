namespace Logic;

public class SearchingForHelpWithRebelsStatus : DiplomatSearchingStatus
{
	public SearchingForHelpWithRebelsStatus()
	{
	}

	public SearchingForHelpWithRebelsStatus(Def def)
		: base(def)
	{
	}

	public new static Status Create(Def def)
	{
		return new SearchingForHelpWithRebelsStatus(def);
	}

	public override bool ProcessKingdom(Kingdom k)
	{
		if (k == null)
		{
			return false;
		}
		DemandHelpWithRebels demandHelpWithRebels = new DemandHelpWithRebels(base.own_kingdom, k);
		if (demandHelpWithRebels.Validate() == "ok" && TrySendOffer(demandHelpWithRebels))
		{
			return true;
		}
		return false;
	}

	public override bool TrySendOffer(Offer offer)
	{
		offer.GetSourceObj();
		Kingdom kingdom = offer.GetTargetObj() as Kingdom;
		float num = 0f;
		for (int i = 0; i < kingdom.rebellions.Count; i++)
		{
			Rebellion rebellion = kingdom.rebellions[i];
			for (int j = 0; j < rebellion.rebels.Count; j++)
			{
				Army army = rebellion.rebels[j].army;
				for (int k = 0; k < army.units.Count; k++)
				{
					Resource resource = army?.units[k]?.def?.cost;
					if (!(resource == null))
					{
						num += resource[ResourceType.Gold];
					}
				}
			}
		}
		Offer counterOffer;
		string text = offer.DecideAIAnswer(out counterOffer);
		if (text == "counter_offer")
		{
			offer.answer = text;
			bool flag = false;
			for (int l = 1; l < counterOffer.args.Count; l++)
			{
				if (counterOffer.args[l].obj_val is DemandGold { args: var args })
				{
					args[0] = (float)args[0] + num;
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				DemandGold demandGold2 = new DemandGold(offer.to as Kingdom, offer.from as Kingdom, (int)num);
				demandGold2.SetParent(counterOffer);
				counterOffer.SetArg(counterOffer.args.Count, demandGold2);
			}
			counterOffer.vars.Set("diplomat", base.own_character);
			counterOffer.Send();
			return true;
		}
		if (text == "accept")
		{
			counterOffer = Offer.GetCachedOffer("CounterOffer", offer.to, offer.from);
			offer.SetParent(counterOffer);
			counterOffer.SetArg(0, offer);
			DemandGold demandGold3 = new DemandGold(offer.to as Kingdom, offer.from as Kingdom, (int)num);
			demandGold3.SetParent(counterOffer);
			counterOffer.SetArg(1, demandGold3);
			if (counterOffer.Validate() == "ok")
			{
				offer.answer = text;
				counterOffer.vars.Set("diplomat", base.own_character);
				counterOffer.Send();
				return true;
			}
		}
		return false;
	}
}
