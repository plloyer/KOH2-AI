namespace Logic;

public interface IRelationCheck
{
	IRelationCheck GetStanceObj();

	bool IsEnemy(IRelationCheck obj);

	bool IsNeutral(IRelationCheck obj);

	bool IsAlly(IRelationCheck obj);

	bool IsAllyOrOwn(IRelationCheck obj);

	bool IsOwnStance(IRelationCheck obj);

	RelationUtils.Stance GetStance(IRelationCheck i);

	RelationUtils.Stance GetStance(Kingdom k);

	RelationUtils.Stance GetStance(Settlement s);

	RelationUtils.Stance GetStance(Rebellion r);

	RelationUtils.Stance GetStance(Crusade k);
}
