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

    [Header("AI Settings")]
    [SerializeField]
    private AICardContainerController aiController;

    [SerializeField]
    private AIDifficulty aiDifficulty = AIDifficulty.Medium;

    [SerializeField]
    private GameMode gameMode = GameMode.PlayerVsAI;

    // 1 = player, 2 = ai/opponent; default player leads first
    private int nextLeader = 1;

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
            boardContainer.RoundWinner += OnRoundWinner;
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
            boardContainer.RoundWinner -= OnRoundWinner;
        }
    }

    private void Start() {
        nextLeader = 1;
        ApplyAIDifficulty();
        BeginNextTurn();
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

    private void BeginNextTurn() {
        if (IsMatchComplete()) {
            playerContainer?.SetPreventCardInteraction(true);
            aiContainer?.SetPreventCardInteraction(true);
            return;
        }

        if (gameMode == GameMode.PlayerVsAI) {
            if (nextLeader == 1) {
                BeginPlayerTurn();
            } else {
                BeginAITurn();
            }
        } else { // PvP
            if (nextLeader == 1) {
                playerContainer?.SetPreventCardInteraction(false);
                aiContainer?.SetPreventCardInteraction(true);
            } else {
                playerContainer?.SetPreventCardInteraction(true);
                aiContainer?.SetPreventCardInteraction(false);
            }
        }
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
        // After AI leads, hand turn control to player (player vs AI only)
        if (gameMode == GameMode.PlayerVsAI && !IsMatchComplete()) {
            playerContainer?.SetPreventCardInteraction(false);
            aiContainer?.SetPreventCardInteraction(true);
        }
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
        BeginNextTurn();
    }

    private bool IsMatchComplete() {
        return boardContainer != null && boardContainer.IsMatchComplete;
    }

    private void OnRoundWinner(int winnerIndex) {
        // 1 = player, 2 = ai/opponent
        nextLeader = winnerIndex == 1 || winnerIndex == 2 ? winnerIndex : 1;
    }

    private void ApplyAIDifficulty() {
        if (aiController != null) {
            aiController.Difficulty = aiDifficulty;
        }
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

        var options = new[] { view.representation, view.vowelharmony, view.firstlast, view.disharmonic };
        if (options.Length == 0) {
            return;
        }

        var randomIndex = Random.Range(0, options.Length);
        view.representation = options[randomIndex];
    }
}
