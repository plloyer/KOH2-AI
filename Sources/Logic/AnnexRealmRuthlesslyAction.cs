namespace Logic;

public class AnnexRealmRuthlesslyAction : Action
{
	public AnnexRealmRuthlesslyAction(Realm owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new AnnexRealmRuthlesslyAction(owner as Realm, def);
	}

	public override string Validate(bool quick_out = false)
	{
		if (!(base.owner as Realm).IsOccupied())
		{
			return "realm_not_occupied";
		}
		return base.Validate(quick_out);
	}

	public override void Prepare()
	{
		Realm obj = base.owner as Realm;
		Kingdom kingdom = obj.controller.GetKingdom();
		Kingdom kingdom2 = obj.GetKingdom();
		kingdom.GetCrownAuthority().AddModifier("realm_annex");
		for (int i = 0; i < base.game.kingdoms.Count; i++)
		{
			Kingdom kingdom3 = base.game.kingdoms[i];
			if (kingdom3 != kingdom && kingdom3 != kingdom2 && !kingdom3.IsDefeated())
			{
				kingdom.AddRelationModifier(kingdom3, "rel_realm_annex_everyone_else", null);
			}
		}
		kingdom.AddRelationModifier(kingdom2, "rel_realm_annex_owner", null);
		base.Prepare();
	}

	public override void Run()
	{
		Realm realm = base.owner as Realm;
		float num = def.GetVar("annex_chance").Float();
		if ((float)base.game.Random(0, 100) < num)
		{
			realm.SetKingdom(realm.controller.GetKingdom().id);
			realm.NotifyListeners("realm_annex_success");
		}
		else
		{
			realm.NotifyListeners("realm_annex_failure");
		}
		base.Run();
	}
}
