using System.Collections;
using System.Collections.Generic;
using DefaultNamespace;
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

    [SerializeField]
    private GameMode gameMode = GameMode.PlayerVsAI;

    private Coroutine aiRoutine;

    public GameMode CurrentMode => gameMode;

    private void OnEnable() {
        if (playerContainer != null && playerContainer.eventsConfig != null && playerContainer.eventsConfig.OnCardPlayed != null) {
            playerContainer.eventsConfig.OnCardPlayed.AddListener(OnPlayerCardPlayed);
        }

        if (aiContainer != null && aiContainer.eventsConfig != null && aiContainer.eventsConfig.OnCardPlayed != null) {
            aiContainer.eventsConfig.OnCardPlayed.AddListener(OnOpponentCardPlayed);
        }

        if (boardContainer != null) {
            boardContainer.RoundReset += OnRoundReset;
        }
    }

    private void OnDisable() {
        if (playerContainer != null && playerContainer.eventsConfig != null && playerContainer.eventsConfig.OnCardPlayed != null) {
            playerContainer.eventsConfig.OnCardPlayed.RemoveListener(OnPlayerCardPlayed);
        }

        if (aiContainer != null && aiContainer.eventsConfig != null && aiContainer.eventsConfig.OnCardPlayed != null) {
            aiContainer.eventsConfig.OnCardPlayed.RemoveListener(OnOpponentCardPlayed);
        }

        if (boardContainer != null) {
            boardContainer.RoundReset -= OnRoundReset;
        }
    }

    private void Start() {
        BeginPlayerTurn();
    }

    public void SetGameMode(GameMode mode) {
        if (gameMode == mode) {
            return;
        }

        gameMode = mode;
        ApplyModeLocks();
    }

    private void ApplyModeLocks() {
        if (IsMatchComplete()) {
            playerContainer?.SetPreventCardInteraction(true);
            aiContainer?.SetPreventCardInteraction(true);
            return;
        }

        switch (gameMode) {
            case GameMode.PlayerVsAI:
                playerContainer?.SetPreventCardInteraction(false);
                aiContainer?.SetPreventCardInteraction(true);
                break;
            case GameMode.PlayerVsPlayer:
                playerContainer?.SetPreventCardInteraction(false);
                aiContainer?.SetPreventCardInteraction(true);
                break;
        }
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
        if (gameMode != GameMode.PlayerVsAI) {
            return;
        }

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
        AssignRandomRepresentation(card);
        aiContainer.PlayCard(card);
        aiRoutine = null;
    }

    private CardWrapper SelectAICard() {
        if (aiContainer == null) {
            return null;
        }

        var availableCards = new List<CardWrapper>();
        foreach (var card in aiContainer.Cards) {
            if (card != null) {
                availableCards.Add(card);
            }
        }

        if (availableCards.Count == 0) {
            return null;
        }

        var randomIndex = Random.Range(0, availableCards.Count);
        return availableCards[randomIndex];
    }

    private void OnPlayerCardPlayed(CardPlayed evt) {
        if (IsMatchComplete()) {
            return;
        }

        if (gameMode == GameMode.PlayerVsAI) {
            BeginAITurn();
        } else {
            BeginOpponentTurn();
        }
    }

    private void BeginOpponentTurn() {
        if (gameMode != GameMode.PlayerVsPlayer) {
            return;
        }

        playerContainer?.SetPreventCardInteraction(true);
        aiContainer?.SetPreventCardInteraction(false);
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

    private void OnOpponentCardPlayed(CardPlayed evt) {
        if (gameMode != GameMode.PlayerVsPlayer) {
            return;
        }

        aiContainer?.SetPreventCardInteraction(true);
    }

    private void AssignRandomRepresentation(CardWrapper card) {
        if (card == null) {
            return;
        }

        var view = card.GetComponent<CardView>();
        if (view == null) {
            return;
        }

        var options = new[] { view.representation, view.vowelharmony, view.firstlast };
        if (options.Length == 0) {
            return;
        }

        var randomIndex = Random.Range(0, options.Length);
        view.representation = options[randomIndex];
    }
}
