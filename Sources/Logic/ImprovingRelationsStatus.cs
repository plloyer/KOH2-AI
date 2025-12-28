namespace Logic;

public class ImprovingRelationsStatus : Status
{
	public ImprovingRelationsStatus(Def def)
		: base(def)
	{
	}

	public new static Status Create(Def def)
	{
		return new ImprovingRelationsStatus(def);
	}

	public override void OnUpdate()
	{
		base.OnUpdate();
		ImproveRelations();
		CheckAICancel();
	}

	private void ImproveRelations()
	{
		Kingdom kingdom = base.own_kingdom;
		Kingdom kingdom2 = base.own_character?.mission_kingdom;
		if (kingdom == null || kingdom2 == null)
		{
			return;
		}
		float num = def.field.GetFloat("rel_change_amount", this);
		if (!(num <= 0f))
		{
			float relationship = kingdom.GetRelationship(kingdom2);
			if (relationship < RelationUtils.Def.maxRelationship)
			{
				kingdom.AddRelationship(kingdom2, num, this);
			}
			Vars vars = new Vars();
			vars.Set("diplomat", base.own_character);
			vars.Set("amount", num);
			vars.Set("already_maxed_out", relationship >= RelationUtils.Def.maxRelationship);
			vars.Set("maxed_out", relationship + num >= RelationUtils.Def.maxRelationship);
			kingdom.NotifyListeners("rel_changed_diplomat", vars);
		}
	}

	private void CheckAICancel()
	{
		if (ShouldAICancel())
		{
			base.own_character.ExecuteAction("Recall");
		}
	}

	private bool ShouldAICancel()
	{
		Kingdom kingdom = base.own_kingdom;
		Kingdom kingdom2 = base.own_character?.mission_kingdom;
		if (kingdom?.ai == null || kingdom2 == null)
		{
			return true;
		}
		if (!kingdom.ai.Enabled(KingdomAI.EnableFlags.Characters))
		{
			return false;
		}
		if (kingdom.GetRelationship(kingdom2) >= 1000f)
		{
			return true;
		}
		return false;
	}
}
