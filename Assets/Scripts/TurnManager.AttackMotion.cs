using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public partial class TurnManager
{
    AttackMotionPresentation attackMotion;
    public IEnumerator PlayAttackBattleMotion(CardController attacker,CardController defender)
    {
        if(attacker==null||defender==null)yield break;
        int outcome=CompareBattleCards(attacker,defender);
        var victims=new List<CardController>();if(outcome<=0)victims.Add(attacker);if(outcome>=0)victims.Add(defender);
        yield return PlayAttackMotion(attacker,new List<CardController>{defender},victims,null);
    }
    public IEnumerator PlayAttackMotion(CardController attacker,IList<CardController> related,IList<CardController> victims,Transform zone)
    {
        if(attacker==null||IsBattleEnded)yield break;
        if(attackMotion==null)attackMotion=gameObject.AddComponent<AttackMotionPresentation>();
        bool enemy=enemyBattleArea!=null&&attacker.transform.IsChildOf(enemyBattleArea);
        yield return attackMotion.Play(this,attacker,related,victims,zone,enemy,IsMonarchCard(attacker.data),()=>{if(seSource!=null&&slashSE!=null)seSource.PlayOneShot(slashSE);});
    }
    IEnumerator PlayerWallAttackMotion(CardController attacker,List<GameObject> walls,CardController guard)
    {
        resolvingPlayerCardAttack=true;
        try{
            if(guard!=null){yield return PlayAttackBattleMotion(attacker,guard);if(!IsBattleEnded&&attacker!=null&&guard!=null)ResolveCardBattle(attacker,guard);yield break;}
            var targets=new List<CardController>();foreach(var wall in walls)if(wall!=null)targets.Add(wall.GetComponent<CardController>());
            yield return PlayAttackMotion(attacker,targets,targets,handDealer.enemyWallArea);
            if(IsBattleEnded||attacker==null)yield break;
            handDealer.SuppressAttackWallSlash=true;
            try{foreach(var wall in walls)if(wall!=null)handDealer.DamageEnemyWallFromAttack(wall);while(handDealer.IsResolvingEnemyWallAttack)yield return null;}
            finally{handDealer.SuppressAttackWallSlash=false;}
        }finally{resolvingPlayerCardAttack=false;}
    }
    IEnumerator EnemyWallAttackMotion(CardController attacker,List<GameObject> walls)
    {
        var targets=new List<CardController>();foreach(var wall in walls)if(wall!=null)targets.Add(wall.GetComponent<CardController>());
        yield return PlayAttackMotion(attacker,targets,targets,handDealer.wallArea);
        if(IsBattleEnded||attacker==null)yield break;
        handDealer.SuppressAttackWallSlash=true;
        try{yield return handDealer.DamagePlayerWallsAndWait(walls);}finally{handDealer.SuppressAttackWallSlash=false;}
    }
}
