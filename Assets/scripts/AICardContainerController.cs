using System.Collections;
using System.Collections.Generic;
using events;
using UnityEngine;

public class AICardContainerController : MonoBehaviour {
    [SerializeField]
    private CardContainer aiContainer;

    [SerializeField]
    private CardContainer opponentContainer;

    [SerializeField]
    private BoardContainer boardContainer;

    [SerializeField]
    private float playDelay = 0.75f;

    private Coroutine playRoutine;

    private void Awake() {
        if (aiContainer == null) {
            aiContainer = GetComponent<CardContainer>();
        }
    }

    private void OnEnable() {
        if (opponentContainer != null && opponentContainer.eventsConfig != null && opponentContainer.eventsConfig.OnCardPlayed != null) {
            opponentContainer.eventsConfig.OnCardPlayed.AddListener(OnOpponentCardPlayed);
        }

        if (boardContainer != null) {
            boardContainer.RoundReset += OnRoundReset;
        }

        ApplyInteractionLocks();
    }

    private void OnDisable() {
        if (opponentContainer != null && opponentContainer.eventsConfig != null && opponentContainer.eventsConfig.OnCardPlayed != null) {
            opponentContainer.eventsConfig.OnCardPlayed.RemoveListener(OnOpponentCardPlayed);
        }

        if (boardContainer != null) {
            boardContainer.RoundReset -= OnRoundReset;
        }

        StopPendingPlay();
    }

    private void ApplyInteractionLocks() {
        aiContainer?.SetPreventCardInteraction(true);
    }

    private void OnOpponentCardPlayed(CardPlayed evt) {
        if (!IsReadyToAct()) {
            return;
        }

        StopPendingPlay();
        playRoutine = StartCoroutine(PlayAfterDelay());
    }

    private void OnRoundReset() {
        StopPendingPlay();
    }

    private IEnumerator PlayAfterDelay() {
        yield return new WaitForSeconds(playDelay);
        if (!IsReadyToAct()) {
            playRoutine = null;
            yield break;
        }

        var card = SelectCard();
        if (card != null) {
            AssignRandomRepresentation(card);
            aiContainer.PlayCard(card);
        }
        playRoutine = null;
    }

    private CardWrapper SelectCard() {
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

    private bool IsReadyToAct() {
        if (aiContainer == null) {
            return false;
        }

        if (boardContainer != null && boardContainer.IsMatchComplete) {
            return false;
        }

        return true;
    }

    private void StopPendingPlay() {
        if (playRoutine != null) {
            StopCoroutine(playRoutine);
            playRoutine = null;
        }
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
