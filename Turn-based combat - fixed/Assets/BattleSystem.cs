using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum BattleState { START, PLAYERCHOOSE, RESOLVING, WON, LOST }

public class BattleSystem : MonoBehaviour
{
    public GameObject playerPrefab;
    public GameObject enemyPrefab;

    public Transform playerBattleStation;
    public Transform enemyBattleStation;

    Unit playerUnit;
    Unit enemyUnit;

    public Text dialogueText;

    public BattleHUD playerHUD;
    public BattleHUD enemyHUD;

    public BattleState state;

    int snipingCount = 7;

    void Start()
    {
        state = BattleState.START;
        StartCoroutine(SetupBattle());
    }

    IEnumerator SetupBattle()
    {
        GameObject playerGO = Instantiate(playerPrefab, playerBattleStation);
        playerUnit = playerGO.GetComponent<Unit>();

        GameObject enemyGO = Instantiate(enemyPrefab, enemyBattleStation);
        enemyUnit = enemyGO.GetComponent<Unit>();

        dialogueText.text = "A wild " + enemyUnit.unitName + " approaches...";

        playerHUD.SetHUD(playerUnit);
        enemyHUD.SetHUD(enemyUnit);

        yield return new WaitForSeconds(2f);

        state = BattleState.PLAYERCHOOSE;
        PlayerChoose();
    }

    IEnumerator ResolveTurn(List<Action> actions)
    {
        actions.Sort((a, b) => b.priority.CompareTo(a.priority));

        foreach (Action action in actions)
        {
            if (state == BattleState.WON || state == BattleState.LOST)
                yield break;

            yield return StartCoroutine(action.PerformAction());
        }

        if (playerUnit.currentHP <= 0)
        {
            state = BattleState.LOST;
            EndBattle();
        }
        else if (enemyUnit.currentHP <= 0)
        {
            state = BattleState.WON;
            EndBattle();
        }
        else
        {
            state = BattleState.PLAYERCHOOSE;
            PlayerChoose();
        }
    }

    void EndBattle()
    {
        if (state == BattleState.WON)
        {
            dialogueText.text = "You won the battle!";
        }
        else if (state == BattleState.LOST)
        {
            dialogueText.text = "You were defeated.";
        }
    }

    void PlayerChoose()
    {
        dialogueText.text = "Choose an action:";
    }

    IEnumerator PlayerAttack()
    {
        bool isDead = enemyUnit.TakeDamage(playerUnit.damage);

        enemyHUD.SetHP(enemyUnit.currentHP);
        dialogueText.text = "The attack is successful!";

        yield return new WaitForSeconds(2f);
    }

    IEnumerator PlayerProtect()
    {
        playerUnit.isProtected = true;
        dialogueText.text = "You brace yourself for the next attack!";

        yield return new WaitForSeconds(2f);
    }

    IEnumerator EnemySniping()
    {
        dialogueText.text = enemyUnit.unitName + " uses sniping!";

        yield return new WaitForSeconds(1f);

        if (!playerUnit.isProtected)
        {
            bool isDead = playerUnit.TakeDamage(enemyUnit.damage);
            playerHUD.SetHP(playerUnit.currentHP);
            if (isDead)
            {
                state = BattleState.LOST;
                EndBattle();
            }
        }
        else
        {
            dialogueText.text = "You protected yourself from the attack!";
            yield return new WaitForSeconds(1f);
        }

        snipingCount--;
    }

    IEnumerator EnemyBombing()
    {
        dialogueText.text = enemyUnit.unitName + " uses bombing!";

        yield return new WaitForSeconds(1f);

        bool isDead = playerUnit.TakeDamage(enemyUnit.damage);
        playerHUD.SetHP(playerUnit.currentHP);

        if (isDead)
        {
            state = BattleState.LOST;
            EndBattle();
        }
    }

    public void OnAttackButton()
    {
        if (state != BattleState.PLAYERCHOOSE)
            return;

        List<Action> actions = new List<Action>
        {
            new Action(PlayerAttack, 0),
            GetEnemyAction()
        };

        state = BattleState.RESOLVING;
        StartCoroutine(ResolveTurn(actions));
    }

    public void OnProtectButton()
    {
        if (state != BattleState.PLAYERCHOOSE)
            return;

        List<Action> actions = new List<Action>
        {
            new Action(PlayerProtect, 2),
            GetEnemyAction()
        };

        state = BattleState.RESOLVING;
        StartCoroutine(ResolveTurn(actions));
    }

    Action GetEnemyAction()
    {
        if (snipingCount > 0 && Random.value > 0.5f)
        {
            return new Action(EnemySniping, 1);
        }
        else
        {
            return new Action(EnemyBombing, -4);
        }
    }

    class Action
    {
        public System.Func<IEnumerator> PerformAction;
        public int priority;

        public Action(System.Func<IEnumerator> action, int priority)
        {
            PerformAction = action;
            this.priority = priority;
        }
    }
}
