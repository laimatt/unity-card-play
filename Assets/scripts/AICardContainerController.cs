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

    [SerializeField]
    private AIDifficulty difficulty = AIDifficulty.Medium;

    public AIDifficulty Difficulty {
        get => difficulty;
        set => difficulty = value;
    }

    private CardWrapper lastLeaderCard;

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

        lastLeaderCard = evt.card;
        StopPendingPlay();
        playRoutine = StartCoroutine(PlayAfterDelay());
    }

    private void OnRoundReset() {
        StopPendingPlay();
        lastLeaderCard = null;
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

        var leaderView = lastLeaderCard != null ? lastLeaderCard.GetComponent<CardView>() : null;
        var scored = new List<(CardWrapper card, int score)>();

        foreach (var card in availableCards) {
            var score = ScoreCard(card, leaderView);
            scored.Add((card, score));
        }

        // Prefer valid moves (score > 0); if none exist, fall back to any card.
        var valid = scored.FindAll(s => s.score > 0);
        var pool = valid.Count > 0 ? valid : scored;

        // easy sometimes picks suboptimal, medium picks near top, hard picks optimal.
        pool.Sort((a, b) => b.score.CompareTo(a.score));

        if (pool.Count == 0) {
            return null;
        }

        int index;
        switch (difficulty) {
            case AIDifficulty.Easy:
                // 50% chance to pick non-best option when possible
                if (pool.Count > 1 && Random.value < 0.5f) {
                    index = Random.Range(1, pool.Count);
                } else {
                    index = 0;
                }
                break;
            case AIDifficulty.Medium:
                var topRange = Mathf.Min(2, pool.Count);
                index = Random.Range(0, topRange);
                break;
            case AIDifficulty.Hard:
            default:
                index = 0;
                break;
        }

        return pool[index].card;
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

    private int ScoreCard(CardWrapper candidate, CardView leaderView) {
        var view = candidate != null ? candidate.GetComponent<CardView>() : null;
        if (view == null) {
            return 0;
        }

        if (leaderView == null) {
            // No info about leader; pick by power as a fallback
            return view.powerValue;
        }

        var matchesSuit = MatchesSuit(leaderView, view);
        if (!matchesSuit) {
            return 0; // illegal move per rules
        }

        int score = 10; // base for being legal

        var elementResult = CompareElements(view.elementValue, leaderView.elementValue); // >0 means candidate wins element
        if (elementResult > 0) score += 6;
        else if (elementResult == 0) score += 2;

        if (view.powerValue > leaderView.powerValue) score += 4;
        else if (view.powerValue == leaderView.powerValue) score += 1;

        return score;
    }

    private bool MatchesSuit(CardView leader, CardView follower) {
        if (leader == null || follower == null) return false;

        return string.Equals(leader.vowelharmony, follower.vowelharmony, System.StringComparison.OrdinalIgnoreCase)
               || string.Equals(leader.firstlast, follower.firstlast, System.StringComparison.OrdinalIgnoreCase)
               || string.Equals(leader.disharmonic, follower.disharmonic, System.StringComparison.OrdinalIgnoreCase);
    }

    private int CompareElements(int followerElement, int leaderElement) {
        // element beats logic mirrors BoardContainer: Red > Green, Green > Blue, Blue > Red
        if (followerElement == leaderElement) {
            return 0;
        }

        var followerWins =
            (followerElement == 0 && leaderElement == 2) ||
            (followerElement == 2 && leaderElement == 1) ||
            (followerElement == 1 && leaderElement == 0);

        if (followerWins) {
            return 1;
        }

        var leaderWins =
            (leaderElement == 0 && followerElement == 2) ||
            (leaderElement == 2 && followerElement == 1) ||
            (leaderElement == 1 && followerElement == 0);

        if (leaderWins) {
            return -1;
        }

        return 0;
    }
}

public enum AIDifficulty {
    Easy,
    Medium,
    Hard
}
