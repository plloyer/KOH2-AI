namespace Logic;

public class SearchingForSpouseStatus : DiplomatSearchingStatus, IListener
{
	public new class FullData : DiplomatSearchingStatus.FullData
	{
		public NID character;

		public new static FullData Create()
		{
			return new FullData();
		}

		public override bool InitFrom(object obj)
		{
			if (!base.InitFrom(obj))
			{
				return false;
			}
			if (!(obj is SearchingForSpouseStatus searchingForSpouseStatus))
			{
				return false;
			}
			character = searchingForSpouseStatus.character;
			return true;
		}

		public override void Save(Serialization.IWriter ser)
		{
			base.Save(ser);
			ser.WriteNID<Character>(character, "character");
		}

		public override void Load(Serialization.IReader ser)
		{
			base.Load(ser);
			character = ser.ReadNID<Character>("character");
		}

		public override bool ApplyTo(object obj, Game game)
		{
			if (!base.ApplyTo(obj, game))
			{
				return false;
			}
			if (!(obj is SearchingForSpouseStatus searchingForSpouseStatus))
			{
				return false;
			}
			if (searchingForSpouseStatus.character != null)
			{
				searchingForSpouseStatus.character.DelListener(searchingForSpouseStatus);
			}
			searchingForSpouseStatus.character = character.Get<Character>(game);
			searchingForSpouseStatus.character.AddListener(searchingForSpouseStatus);
			return true;
		}
	}

	public Character character;

	public SearchingForSpouseStatus(Character c)
	{
		if (character != null)
		{
			character.DelListener(this);
		}
		character = c;
		character.AddListener(this);
	}

	public SearchingForSpouseStatus(Def def)
		: base(def)
	{
	}

	public new static Status Create(Def def)
	{
		return new SearchingForSpouseStatus(def);
	}

	public override Offer GetReversedOffer(Offer o)
	{
		return new MarriageOffer(o.to as Kingdom, o.from as Kingdom, o.GetArg<Character>(0), o.GetArg<Character>(1));
	}

	public override bool ProcessKingdom(Kingdom k)
	{
		if (k == null)
		{
			return false;
		}
		MarriageOffer marriageOffer = new MarriageOffer(base.own_kingdom, k, this.character, null);
		for (int i = 0; i < k.royalFamily.Children.Count; i++)
		{
			Character character = k.royalFamily.Children[i];
			marriageOffer.SetArg(1, character);
			if (marriageOffer.Validate() == "ok" && TrySendOffer(marriageOffer))
			{
				return true;
			}
		}
		marriageOffer.SetArg(1, k.royalFamily.Sovereign);
		if (marriageOffer.Validate() == "ok" && TrySendOffer(marriageOffer))
		{
			return true;
		}
		return false;
	}

	protected override void OnDestroy()
	{
		character.DelListener(this);
		base.OnDestroy();
	}

	public override Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		if (key == "character")
		{
			return character;
		}
		return base.GetVar(key, vars, as_value);
	}

	public void OnMessage(object obj, string message, object param)
	{
		if (!IsAuthority())
		{
			return;
		}
		switch (message)
		{
		case "prison_changed":
			using (Game.Profile("SearchingForSpouseStatus OnMessage"))
			{
				if (character.IsPrisoner())
				{
					base.own_character?.cur_action?.Cancel(manual: false, notify: false);
				}
				break;
			}
		case "statuses_changed":
			if (character.IsMarried())
			{
				base.own_character?.cur_action?.Cancel();
			}
			break;
		case "dying":
		case "turned_into_rebel":
			base.own_character?.cur_action?.Cancel(manual: false, notify: false);
			break;
		case "no_longer_in_royal_family":
			base.own_character?.cur_action?.Cancel();
			break;
		}
	}
}
