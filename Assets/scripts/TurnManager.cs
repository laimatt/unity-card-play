using System.Collections;
using events;
using UnityEngine;

public class TurnManager : MonoBehaviour {
    [SerializeField]
    private CardContainer playerContainer;

    [SerializeField]
    private CardContainer aiContainer;

    [SerializeField]
    private BoardContainer boardContainer;

    [SerializeField]
    private float aiTurnDelay = 0.75f;

    private Coroutine aiRoutine;

    private void OnEnable() {
        if (playerContainer != null && playerContainer.eventsConfig != null && playerContainer.eventsConfig.OnCardPlayed != null) {
            playerContainer.eventsConfig.OnCardPlayed.AddListener(OnPlayerCardPlayed);
        }

        if (boardContainer != null) {
            boardContainer.RoundReset += OnRoundReset;
        }
    }

    private void OnDisable() {
        if (playerContainer != null && playerContainer.eventsConfig != null && playerContainer.eventsConfig.OnCardPlayed != null) {
            playerContainer.eventsConfig.OnCardPlayed.RemoveListener(OnPlayerCardPlayed);
        }

        if (boardContainer != null) {
            boardContainer.RoundReset -= OnRoundReset;
        }
    }

    private void Start() {
        BeginPlayerTurn();
    }

    private void BeginPlayerTurn() {
        if (aiRoutine != null) {
            StopCoroutine(aiRoutine);
            aiRoutine = null;
        }

        if (IsMatchComplete()) {
            playerContainer?.SetPreventCardInteraction(true);
            aiContainer?.SetPreventCardInteraction(true);
            return;
        }

        playerContainer?.SetPreventCardInteraction(false);
        aiContainer?.SetPreventCardInteraction(true);
    }

    private void BeginAITurn() {
        if (IsMatchComplete()) {
            return;
        }

        playerContainer?.SetPreventCardInteraction(true);
        aiContainer?.SetPreventCardInteraction(true);

        if (aiRoutine != null) {
            StopCoroutine(aiRoutine);
        }
        aiRoutine = StartCoroutine(AITurnRoutine());
    }

    private IEnumerator AITurnRoutine() {
        yield return new WaitForSeconds(aiTurnDelay);
        var card = SelectAICard();
        if (card == null || aiContainer == null || IsMatchComplete()) {
            aiRoutine = null;
            yield break;
        }
        aiContainer.PlayCard(card);
        aiRoutine = null;
    }

    private CardWrapper SelectAICard() {
        if (aiContainer == null) {
            return null;
        }

        foreach (var card in aiContainer.Cards) {
            if (card != null) {
                return card;
            }
        }

        return null;
    }

    private void OnPlayerCardPlayed(CardPlayed evt) {
        if (IsMatchComplete()) {
            return;
        }
        BeginAITurn();
    }

    private void OnRoundReset() {
        if (IsMatchComplete()) {
            playerContainer?.SetPreventCardInteraction(true);
            aiContainer?.SetPreventCardInteraction(true);
            return;
        }
        BeginPlayerTurn();
    }

    private bool IsMatchComplete() {
        return boardContainer != null && boardContainer.IsMatchComplete;
    }
}
