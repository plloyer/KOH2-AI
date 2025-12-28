using System;
using System.Collections.Generic;

namespace Logic;

public class Offers : Component
{
	public int last_uoid;

	public List<Offer> outgoing;

	public List<Offer> incoming;

	public Offers(Object obj)
		: base(obj)
	{
	}

	public bool Empty()
	{
		if (outgoing == null || outgoing.Count == 0)
		{
			if (incoming != null)
			{
				return incoming.Count == 0;
			}
			return true;
		}
		return false;
	}

	public static Offers Get(Object obj, bool create)
	{
		if (obj == null)
		{
			return null;
		}
		Offers offers = obj.GetComponent<Offers>();
		if (offers == null && create)
		{
			offers = new Offers(obj);
		}
		return offers;
	}

	public Offer FindIncoming(Object from)
	{
		if (incoming == null)
		{
			return null;
		}
		for (int i = 0; i < incoming.Count; i++)
		{
			Offer offer = incoming[i];
			if (offer.from == from)
			{
				return offer;
			}
		}
		return null;
	}

	public Offer FindIncoming(int id)
	{
		if (incoming != null)
		{
			for (int i = 0; i < incoming.Count; i++)
			{
				Offer offer = incoming[i];
				if (offer.uoid == id)
				{
					return offer;
				}
			}
		}
		return null;
	}

	public Offer FindOutgoing(Object to)
	{
		if (outgoing == null)
		{
			return null;
		}
		for (int i = 0; i < outgoing.Count; i++)
		{
			Offer offer = outgoing[i];
			if (offer.to == to)
			{
				return offer;
			}
		}
		return null;
	}

	public Offer FindOutgoing(int id)
	{
		if (outgoing == null)
		{
			return null;
		}
		for (int i = 0; i < outgoing.Count; i++)
		{
			Offer offer = outgoing[i];
			if (offer.uoid == id)
			{
				return offer;
			}
		}
		return null;
	}

	public static Offer Find(Object from, Object to)
	{
		return Get(to, create: false)?.FindIncoming(from);
	}

	public void Add(Offer offer)
	{
		bool flag = false;
		if (!obj.IsValid())
		{
			return;
		}
		if (offer.from == obj)
		{
			if (outgoing == null)
			{
				outgoing = new List<Offer>();
			}
			outgoing.Add(offer);
			flag = true;
		}
		if (offer.to == obj)
		{
			if (incoming == null)
			{
				incoming = new List<Offer>();
			}
			incoming.Add(offer);
			flag = true;
		}
		if (flag)
		{
			obj.NotifyListeners("offer_added", offer);
			CheckNeedsUpdate();
			SendState();
		}
	}

	public static void Add(Object obj, Offer offer)
	{
		Get(obj, create: true)?.Add(offer);
	}

	public void Del(Offer offer)
	{
		bool flag = false;
		if (offer.from == obj && outgoing != null)
		{
			flag |= outgoing.Remove(offer);
		}
		if (offer.to == obj && incoming != null)
		{
			flag |= incoming.Remove(offer);
		}
		if (flag)
		{
			obj.NotifyListeners("offer_removed", offer);
			CheckNeedsUpdate();
			SendState();
		}
	}

	public static void Del(Object obj, Offer offer)
	{
		Get(obj, create: false)?.Del(offer);
	}

	public bool RemoveInvalid()
	{
		bool result = false;
		if (outgoing != null)
		{
			for (int i = 0; i < outgoing.Count; i++)
			{
				if (!outgoing[i].Update())
				{
					result = true;
					i--;
				}
			}
		}
		if (incoming != null)
		{
			for (int j = 0; j < incoming.Count; j++)
			{
				if (!incoming[j].Update())
				{
					result = true;
					j--;
				}
			}
		}
		return result;
	}

	public static bool RemoveInvalid(Object obj)
	{
		return Get(obj, create: false)?.RemoveInvalid() ?? false;
	}

	private void CheckNeedsUpdate()
	{
		if (Empty())
		{
			StopUpdating();
		}
		else if (!IsRegisteredForUpdate())
		{
			UpdateAfter(0.5f);
		}
	}

	public override void OnUpdate()
	{
		if (obj.IsAuthority() && obj.components != null && !RemoveInvalid())
		{
			CheckNeedsUpdate();
		}
	}

	public void SendState()
	{
		obj.SendState<Object.OffersState>();
	}

	public override void OnDestroy()
	{
		base.OnDestroy();
		incoming?.Clear();
		outgoing?.Clear();
		last_uoid = 0;
	}

	public static string TestSend(string offer_type, Kingdom plr_kingdom, Object sel_obj, string dump_treshold = null)
	{
		if (plr_kingdom == null)
		{
			return "no player kingdom";
		}
		if (sel_obj == null)
		{
			return "no selected object";
		}
		Kingdom kingdom = sel_obj.GetKingdom();
		if (kingdom == null)
		{
			return "no selected kingdom";
		}
		Defs.Registry registry = kingdom.game.defs.Get(typeof(Offer.Def));
		if (registry == null)
		{
			return "no offer defs";
		}
		Offer.Def def = null;
		foreach (KeyValuePair<string, Def> def2 in registry.defs)
		{
			if (def2.Key.IndexOf(offer_type, StringComparison.OrdinalIgnoreCase) >= 0)
			{
				def = def2.Value as Offer.Def;
				break;
			}
		}
		if (def == null)
		{
			return "unknown offer type";
		}
		Offer offer = Offer.Create(def, plr_kingdom, kingdom);
		if (offer == null)
		{
			return "Offer.Create() failed";
		}
		offer.AI = plr_kingdom.ai.Enabled(KingdomAI.EnableFlags.Diplomacy);
		if (!OfferGenerator.instance.FillOfferArgs("propose", offer))
		{
			if (offer.Validate() != "ok")
			{
				return offer.ToString() + ": Could not be validated - " + offer.Validate();
			}
			if (!string.IsNullOrEmpty(dump_treshold))
			{
				ProsAndCons prosAndCons = ProsAndCons.Get(offer, dump_treshold);
				if (prosAndCons != null)
				{
					Game.Log(prosAndCons.Dump(), Game.LogType.Message);
				}
			}
			if (!offer.CheckThreshold("propose"))
			{
				offer.Send();
				return offer.ToString() + ": Could not cover the 'propose' treshhold. But sending anyway.";
			}
			return offer.ToString() + ": Could not fill its arguments";
		}
		if (offer.Validate() != "ok")
		{
			return offer.ToString() + " - " + offer.Validate();
		}
		if (!string.IsNullOrEmpty(dump_treshold))
		{
			ProsAndCons prosAndCons2 = ProsAndCons.Get(offer, dump_treshold);
			if (prosAndCons2 != null)
			{
				Game.Log(prosAndCons2.Dump(), Game.LogType.Message);
			}
		}
		offer.Send();
		if (!offer.CheckThreshold("propose"))
		{
			return offer.ToString() + ": Could not cover the 'propose' treshhold. But sending anyway.";
		}
		return "Sending " + offer;
	}

	public static string TestSendCounterOffer(string offer_type_original, Kingdom plr_kingdom, Object sel_obj, params string[] additional)
	{
		if (plr_kingdom == null)
		{
			return "no player kingdom";
		}
		if (sel_obj == null)
		{
			return "no selected object";
		}
		Kingdom kingdom = sel_obj.GetKingdom();
		if (kingdom == null)
		{
			return "no selected kingdom";
		}
		Defs.Registry registry = kingdom.game.defs.Get(typeof(Offer.Def));
		if (registry == null)
		{
			return "no offer defs";
		}
		Offer.Def def = null;
		foreach (KeyValuePair<string, Def> def2 in registry.defs)
		{
			if (def2.Key.IndexOf(offer_type_original, StringComparison.OrdinalIgnoreCase) >= 0)
			{
				def = def2.Value as Offer.Def;
				break;
			}
		}
		if (def == null)
		{
			return "unknown offer type";
		}
		Offer offer = Offer.Create(def, plr_kingdom, kingdom);
		if (offer == null)
		{
			return "Offer.Create() failed";
		}
		if (!OfferGenerator.instance.FillOfferArgs("propose", offer))
		{
			if (offer.Validate() != "ok")
			{
				return offer.ToString() + ": Could not be validated - " + offer.Validate();
			}
			if (!offer.CheckThreshold("propose"))
			{
				offer.Send();
				return offer.ToString() + ": Could not cover the 'propose' treshhold. But sending anyway.";
			}
			return offer.ToString() + ": Could not fill its arguments";
		}
		if (offer.Validate() != "ok")
		{
			return offer.ToString() + " - " + offer.Validate();
		}
		Game.Log("Sending " + offer, Game.LogType.Message);
		offer.Send();
		return null;
	}
}
