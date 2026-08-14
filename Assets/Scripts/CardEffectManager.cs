using UnityEngine;
using System.Collections;

public class CardEffectManager :
    MonoBehaviour
{
    public static CardEffectManager I;

    void Awake()
    {
        I = this;
    }

    public void ActivateOnSummon(
        CardController card,
        bool isShieldTrigger = false,
        bool isEnemy = false
    )
    {
        if (card == null)
        {
            Debug.LogWarning(
                "召喚時効果：" +
                "CardControllerがnullです"
            );
            return;
        }

        if (card.data == null)
        {
            Debug.LogWarning(
                "召喚時効果：" +
                "CardDataがnullです"
            );
            return;
        }

        if (card.data.effectTypes == null)
        {
            Debug.LogWarning(
                "召喚時効果：" +
                card.data.name +
                "のeffectTypesがnullです"
            );
            return;
        }

        HandDealer handDealer =
            FindFirstObjectByType<HandDealer>();

        TurnManager turnManager =
            FindFirstObjectByType<TurnManager>();

        bool instantDestroySelf = false;

        Debug.Log(
            "召喚時効果チェック：" +
            card.data.name +
            " / ShieldTrigger=" +
            isShieldTrigger +
            " / Enemy=" +
            isEnemy
        );

        foreach (
            EffectType effect
            in card.data.effectTypes
        )
        {
            Debug.Log(
                "召喚時効果を処理：" +
                card.data.name +
                " / " +
                effect
            );

            switch (effect)
            {
                case EffectType.None:
                    break;

                case EffectType.ChargeTopDeck:
                    if (handDealer != null)
                    {
                        Debug.Log(
                            "効果発動：" +
                            "山札上をリソースへ"
                        );

                        handDealer
                            .ChargeTopDeckToResource();
                    }
                    else
                    {
                        Debug.LogWarning(
                            "ChargeTopDeck：" +
                            "HandDealerが見つかりません"
                        );
                    }
                    break;

                case EffectType.Draw1:
                    if (handDealer != null)
                    {
                        Debug.Log(
                            "効果発動：1ドロー"
                        );

                        handDealer.DrawOneCard();
                    }
                    else
                    {
                        Debug.LogWarning(
                            "Draw1：" +
                            "HandDealerが見つかりません"
                        );
                    }
                    break;

                case EffectType.DiscardEnemyHand:
                    if (handDealer != null)
                    {
                        Debug.Log(
                            "効果発動：" +
                            "敵手札ランダム墓地"
                        );

                        handDealer
                            .DiscardRandomEnemyHand();
                    }
                    else
                    {
                        Debug.LogWarning(
                            "DiscardEnemyHand：" +
                            "HandDealerが見つかりません"
                        );
                    }
                    break;

                case EffectType.RecoverWall:
                    if (handDealer != null)
                    {
                        Debug.Log(
                            "効果発動：" +
                            "Wall回復 最大9枚対応"
                        );

                        handDealer.RecoverWallByKing();
                    }
                    else
                    {
                        Debug.LogWarning(
                            "RecoverWall：" +
                            "HandDealerが見つかりません"
                        );
                    }
                    break;

                case EffectType.JokerClearBattleArea:
                    if (turnManager == null)
                    {
                        Debug.LogWarning(
                            "JokerClearBattleArea：" +
                            "TurnManagerが見つかりません"
                        );
                        break;
                    }

                    /*
                     * シールドトリガーJokerは
                     * 選択なしで場を一掃する。
                     *
                     * Joker自身の墓地移動は
                     * HandDealerが担当する。
                     */
                    if (isShieldTrigger)
                    {
                        Debug.Log(
                            "シールドトリガーJOKER：" +
                            "選択なしで一掃"
                        );

                        turnManager
                            .JokerClearBattleArea(card);

                        break;
                    }

                    /*
                     * 上級ルールで敵がJokerを召喚した場合、
                     * AIが一掃か追加ターンを選択する。
                     */
                    if (
                        isEnemy &&
                        GameSettings.IsAdvancedRule &&
                        HasEffect(
                            card,
                            EffectType.JokerExtraTurn
                        )
                    )
                    {
                        TCardEnemyAIBrain brain =
                            FindFirstObjectByType
                                <TCardEnemyAIBrain>();

                        bool chooseClear = true;

                        if (brain != null)
                        {
                            chooseClear =
                                brain.ChooseJokerClear(
                                    TCardAIUnityBridge
                                        .GetCards(
                                            turnManager
                                                .playerBattleArea
                                        ),
                                    TCardAIUnityBridge
                                        .GetCards(
                                            turnManager
                                                .enemyBattleArea
                                        )
                                );
                        }

                        if (chooseClear)
                        {
                            Debug.Log(
                                "敵JOKER：" +
                                "AI選択 → 一掃"
                            );

                            turnManager
                                .JokerClearBattleArea(card);
                        }
                        else
                        {
                            Debug.Log(
                                "敵JOKER：" +
                                "AI選択 → 追加ターン"
                            );

                            turnManager
                                .RequestEnemyExtraTurn();

                            turnManager
                                .SendCardToOwnGraveyard(
                                    card
                                );
                        }

                        break;
                    }

                    /*
                     * プレイヤーの通常召喚、
                     * または通常ルールの敵召喚。
                     */
                    Debug.Log(
                        "JOKER通常召喚：" +
                        "バトルエリアを一掃"
                    );

                    turnManager
                        .JokerClearBattleArea(card);

                    break;

                case EffectType.JokerExtraTurn:
                    /*
                     * 敵の選択は
                     * JokerClearBattleArea側で処理する。
                     *
                     * プレイヤーの追加ターンは
                     * Joker選択パネル側で処理する。
                     */
                    break;

                case EffectType.TapAllEnemyBattle:
                    if (turnManager != null)
                    {
                        Debug.Log(
                            isEnemy
                                ? "敵9の効果発動：" +
                                  "プレイヤー全体タップ"
                                : "9の効果発動：" +
                                  "敵全体タップ"
                        );

                        /*
                         * 9が場にいる間に効果を発動する。
                         *
                         * TurnManagerはカードの親から
                         * 所属陣営と対象エリアを判定する。
                         */
                        turnManager
                            .TapAllEnemyBattle(card);

                        /*
                         * 通常プレイされた敵9は
                         * BattleDropZoneを通らないため、
                         * 効果解決後にここで墓地へ送る。
                         *
                         * プレイヤー9：
                         * BattleDropZoneが墓地へ送る。
                         *
                         * シールドトリガー9：
                         * HandDealerが墓地へ送る。
                         */
                        if (
                            isEnemy &&
                            !isShieldTrigger &&
                            turnManager.enemyBattleArea != null &&
                            card.transform.IsChildOf(
                                turnManager.enemyBattleArea
                            )
                        )
                        {
                            Debug.Log(
                                "敵9の効果解決完了：" +
                                "9を敵墓地へ移動"
                            );

                            turnManager
                                .SendCardToOwnGraveyard(
                                    card
                                );
                        }
                    }
                    else
                    {
                        Debug.LogWarning(
                            "TapAllEnemyBattle：" +
                            "TurnManagerが見つかりません"
                        );
                    }
                    break;

                case EffectType.DestroyOneEnemyBattle:
                    if (turnManager != null)
                    {
                        Debug.Log(
                            isEnemy
                                ? "敵Aの効果発動：" +
                                  "プレイヤーカード破壊開始"
                                : "Aの効果発動：" +
                                  "敵1体破壊選択開始"
                        );

                        /*
                         * プレイヤーA：
                         * 敵カードを選択する。
                         *
                         * 敵A：
                         * TurnManager内でAIが対象を選択する。
                         *
                         * シールドトリガーA：
                         * HandDealerが選択完了まで待機する。
                         *
                         * A自身の墓地移動は
                         * TurnManager.TrySelectDestroyTarget()
                         * が担当する。
                         */
                        turnManager
                            .StartSelectEnemyBattleToDestroy(
                                card
                            );
                    }
                    else
                    {
                        Debug.LogWarning(
                            "DestroyOneEnemyBattle：" +
                            "TurnManagerが見つかりません"
                        );
                    }
                    break;

                case EffectType.InstantDestroySelf:
                    /*
                     * A・9・Jokerには設定しない。
                     *
                     * それぞれ専用処理で
                     * 墓地移動を管理する。
                     */
                    instantDestroySelf = true;
                    break;

                default:
                    Debug.LogWarning(
                        "未対応の召喚時効果：" +
                        effect
                    );
                    break;
            }
        }

        /*
         * 専用の即時消滅効果を持つカードだけ
         * 最後に墓地へ送る。
         */
        if (instantDestroySelf)
        {
            if (turnManager != null)
            {
                Debug.Log(
                    "効果発動：" +
                    "インスタンスカード消滅"
                );

                turnManager
                    .SendCardToOwnGraveyard(card);
            }
            else
            {
                Debug.LogWarning(
                    "InstantDestroySelf：" +
                    "TurnManagerが見つかりません"
                );
            }
        }
    }

    bool HasEffect(
        CardController card,
        EffectType effectType
    )
    {
        if (
            card == null ||
            card.data == null ||
            card.data.effectTypes == null
        )
        {
            return false;
        }

        return System.Array.Exists(
            card.data.effectTypes,
            effect => effect == effectType
        );
    }

    public void ActivateOnAttack(
        CardController card
    )
    {
        if (
            card == null ||
            card.data == null
        )
        {
            return;
        }

        Debug.Log(
            "攻撃時効果チェック：" +
            card.data.name
        );
    }

    public void ActivateOnDestroy(
        CardController card
    )
    {
        if (
            card == null ||
            card.data == null
        )
        {
            return;
        }

        Debug.Log(
            "破壊時効果チェック：" +
            card.data.name
        );
    }

    public void ActivateOnBlock(
        CardController card
    )
    {
        if (
            card == null ||
            card.data == null
        )
        {
            return;
        }

        Debug.Log(
            "ブロック時効果チェック：" +
            card.data.name
        );
    }

    public IEnumerator ActivateOnSummonRoutine(
        CardController card,
        bool isShieldTrigger = false,
        bool isEnemy = false
    )
    {
        if (card == null)
            yield break;

        ActivateOnSummon(
            card,
            isShieldTrigger,
            isEnemy
        );

        yield return null;
    }
}