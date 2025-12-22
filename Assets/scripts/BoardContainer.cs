using System;
using System.Collections;
using config;
using DefaultNamespace;
using events;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BoardContainer : MonoBehaviour {
    [Header("Containers")]
    [SerializeField]
    private CardContainer container_1;
    [SerializeField]
    private CardContainer container_2;
    [Header("Discard Containers")]
    [SerializeField]
    private CardContainer discard_container_1;
    [SerializeField]
    private CardContainer discard_container_2;
    [Header("Config")]
    [SerializeField]
    private BoardConfig boardConfig;

    private CardPlayed lastCardPlayed_1;
    private CardPlayed lastCardPlayed_2;

    private int player_1_score = 0;
    private int player_2_score = 0;


    [SerializeField]
    private TextMeshProUGUI p1Text;

    [SerializeField]
    private TextMeshProUGUI p2Text;

    [SerializeField]
    private TextMeshProUGUI messageText;

    public event Action RoundReset;
    public event Action<int> RoundWinner; // 1 = player 1, 2 = player 2

    private int totalTricksExpected;
    private int tricksPlayed;
    private bool matchComplete;
    private bool tieBreakerApplied;
    private bool parityWarningIssued;
    private bool roundResolving;
    private int? currentLeaderContainer;

    private const int ElementRed = 0;
    private const int ElementGreen = 2;
    private const int ElementBlue = 1;

    public bool IsMatchComplete => matchComplete;

    private void OnEnable() {
        if (container_1 != null && container_1.eventsConfig != null && container_1.eventsConfig.OnCardPlayed != null) {
            container_1.eventsConfig.OnCardPlayed.AddListener(OnCardPlayedFromContainer1);
        }

        if (container_2 != null && container_2.eventsConfig != null && container_2.eventsConfig.OnCardPlayed != null) {
            container_2.eventsConfig.OnCardPlayed.AddListener(OnCardPlayedFromContainer2);
        }
    }

    private void OnDisable() {
        if (container_1 != null && container_1.eventsConfig != null && container_1.eventsConfig.OnCardPlayed != null) {
            container_1.eventsConfig.OnCardPlayed.RemoveListener(OnCardPlayedFromContainer1);
        }

        if (container_2 != null && container_2.eventsConfig != null && container_2.eventsConfig.OnCardPlayed != null) {
            container_2.eventsConfig.OnCardPlayed.RemoveListener(OnCardPlayedFromContainer2);
        }
    }

    private void Start() {
        InitializeMatchMetadata();
        UpdateScoreDisplay();
    }

    private void InitializeMatchMetadata() {
        totalTricksExpected = CalculateInitialTrickCount();
    tricksPlayed = 0;
    matchComplete = false;
    tieBreakerApplied = false;
    parityWarningIssued = false;
    roundResolving = false;
    currentLeaderContainer = null;

        if (totalTricksExpected == 0) {
            return;
        }

        WarnIfUnevenTricks();
    }

    private int CalculateInitialTrickCount() {
        var playerOneCount = container_1 != null ? container_1.transform.childCount : 0;
        var playerTwoCount = container_2 != null ? container_2.transform.childCount : 0;
        return Mathf.Min(playerOneCount, playerTwoCount);
    }

    private void OnCardPlayedFromContainer1(CardPlayed e) {
        if (currentLeaderContainer == null && lastCardPlayed_1 == null && lastCardPlayed_2 == null) {
            currentLeaderContainer = 1; // first card this trick becomes leader
        }

        lastCardPlayed_1 = e;
        BothCardsPlayed();
    }

    private void OnCardPlayedFromContainer2(CardPlayed e) {
        if (currentLeaderContainer == null && lastCardPlayed_1 == null && lastCardPlayed_2 == null) {
            currentLeaderContainer = 2; // first card this trick becomes leader
        }

        lastCardPlayed_2 = e;
        BothCardsPlayed();
    }

    public void BothCardsPlayed() {
        if (matchComplete || lastCardPlayed_1 == null || lastCardPlayed_2 == null || roundResolving) {
            return;
        }

        roundResolving = true;
        EnsureTotalTricksInitialized();

        var cardView1 = lastCardPlayed_1.card != null ? lastCardPlayed_1.card.GetComponent<CardView>() : null;
        var cardView2 = lastCardPlayed_2.card != null ? lastCardPlayed_2.card.GetComponent<CardView>() : null;

        var leaderIndex = currentLeaderContainer ?? 1;
        var followerIndex = leaderIndex == 1 ? 2 : 1;
        var leaderView = leaderIndex == 1 ? cardView1 : cardView2;
        var followerView = leaderIndex == 1 ? cardView2 : cardView1;

        var followerValid = SharesSuit(leaderView, followerView);
        var outcome = ResolveRoundOutcome(leaderView, followerView, leaderIndex, out bool followerTieBreakerWin, followerValid);

        string roundMessage;
        var leaderLabel = leaderIndex == 1 ? "Player 1" : "Player 2";
        var followerLabel = followerIndex == 1 ? "Player 1" : "Player 2";

        int winnerIndex = 0;
        switch (outcome) {
            case RoundOutcome.Player1:
                player_1_score += 1;
                winnerIndex = 1;
                roundMessage = followerValid
                    ? "Player 1 wins the round!"
                    : $"{followerLabel} mismatched suit; Player 1 wins the round!";
                break;
            case RoundOutcome.Player2:
                player_2_score += 1;
                winnerIndex = 2;
                roundMessage = followerValid
                    ? "Player 2 wins the round!"
                    : $"{followerLabel} mismatched suit; Player 2 wins the round!";
                break;
            default:
                roundMessage = followerTieBreakerWin
                    ? $"Perfect tie. {followerLabel} wins on follower advantage."
                    : "Round is a tie!";
                winnerIndex = followerTieBreakerWin ? followerIndex : 0;
                break;
        }

        if (followerTieBreakerWin) {
            roundMessage = $"Perfect tie. {followerLabel} wins on follower advantage.";
        }

        tricksPlayed += 1;
        if (winnerIndex != 0) {
            RoundWinner?.Invoke(winnerIndex);
        }

        var finalMessage = CheckForMatchEnd();
        UpdateScoreDisplay(roundMessage, finalMessage);

        Debug.LogWarning($"BoardContainer: roundmessage {roundMessage}");
        if (!string.IsNullOrEmpty(finalMessage)) {
            Debug.LogWarning($"BoardContainer: finalmessage {finalMessage}");
        }

        StartCoroutine(ResetBoard());
    }

    private void EnsureTotalTricksInitialized() {
        if (totalTricksExpected > 0) {
            return;
        }

        var playerOneTotal = CountPlayerCards(container_1, discard_container_1, lastCardPlayed_1);
        var playerTwoTotal = CountPlayerCards(container_2, discard_container_2, lastCardPlayed_2);

        totalTricksExpected = Mathf.Min(playerOneTotal, playerTwoTotal);
        if (totalTricksExpected == 0) {
            return;
        }

        WarnIfUnevenTricks();
    }

    private int CountPlayerCards(CardContainer handContainer, CardContainer discardContainer, CardPlayed currentCard) {
        var count = 0;
        if (handContainer != null) {
            count += handContainer.transform.childCount;
        }

        if (discardContainer != null) {
            count += discardContainer.transform.childCount;
        }

        if (currentCard?.card != null) {
            count += 1;
        }

        return count;
    }

    private void WarnIfUnevenTricks() {
        if (parityWarningIssued || totalTricksExpected % 2 == 0) {
            return;
        }

        // Debug.LogWarning("BoardContainer: Uneven trick count detected. Consider adjusting hand sizes to keep turns even.");
        parityWarningIssued = true;
    }

    private string CheckForMatchEnd() {
        if (matchComplete) {
            return null;
        }

        if (totalTricksExpected <= 0 || tricksPlayed < totalTricksExpected) {
            return null;
        }

        matchComplete = true;

        if (player_1_score > player_2_score) {
            return "Player 1 wins the match!";
        }

        if (player_2_score > player_1_score) {
            return "Player 2 wins the match!";
        }

        if (!tieBreakerApplied) {
            player_2_score += 1;
            tieBreakerApplied = true;
        }

        return "Match tied. Player 2 wins on the second-mover tiebreaker.";
    }

    private RoundOutcome ResolveRoundOutcome(CardView leaderView, CardView followerView, int leaderContainerIndex, out bool followerTieBreakerWin, bool followerValid) {
        followerTieBreakerWin = false;

        var leaderElement = leaderView != null ? leaderView.elementValue : -1;
        var followerElement = followerView != null ? followerView.elementValue : -1;
        var leaderPower = leaderView != null ? leaderView.powerValue : -1;
        var followerPower = followerView != null ? followerView.powerValue : -1;

        // If follower failed suit match, leader wins immediately
        if (!followerValid) {
            return leaderContainerIndex == 1 ? RoundOutcome.Player1 : RoundOutcome.Player2;
        }

        var elementComparison = CompareElements(leaderElement, followerElement); // >0 leader wins element

        if (elementComparison > 0) {
            return leaderContainerIndex == 1 ? RoundOutcome.Player1 : RoundOutcome.Player2;
        }

        if (elementComparison < 0) {
            return leaderContainerIndex == 1 ? RoundOutcome.Player2 : RoundOutcome.Player1;
        }

        if (leaderPower > followerPower) {
            return leaderContainerIndex == 1 ? RoundOutcome.Player1 : RoundOutcome.Player2;
        }

        if (followerPower > leaderPower) {
            return leaderContainerIndex == 1 ? RoundOutcome.Player2 : RoundOutcome.Player1;
        }

        // Perfect tie (element + power) => follower wins by rule
        followerTieBreakerWin = true;
        return leaderContainerIndex == 1 ? RoundOutcome.Player2 : RoundOutcome.Player1;
    }

    private int CompareElements(int elementOne, int elementTwo) {
        if (elementOne == elementTwo) {
            return 0;
        }

        var firstWins =
            (elementOne == ElementRed && elementTwo == ElementGreen) ||
            (elementOne == ElementGreen && elementTwo == ElementBlue) ||
            (elementOne == ElementBlue && elementTwo == ElementRed);

        if (firstWins) {
            return 1;
        }

        var secondWins =
            (elementTwo == ElementRed && elementOne == ElementGreen) ||
            (elementTwo == ElementGreen && elementOne == ElementBlue) ||
            (elementTwo == ElementBlue && elementOne == ElementRed);

        if (secondWins) {
            return -1;
        }

        return 0;
    }

    private bool SharesSuit(CardView leader, CardView follower) {
        if (leader == null || follower == null) {
            return false;
        }

        return string.Equals(leader.vowelharmony, follower.vowelharmony, StringComparison.OrdinalIgnoreCase)
               || string.Equals(leader.firstlast, follower.firstlast, StringComparison.OrdinalIgnoreCase)
               || string.Equals(leader.disharmonic, follower.disharmonic, StringComparison.OrdinalIgnoreCase);
    }

    private void UpdateScoreDisplay(string roundSummary = null, string finalSummary = null) {
        if (p1Text == null || p2Text == null || messageText == null) {
            return;
        }

        var text = string.Empty;
        var p1 = string.Empty;
        var p2 = string.Empty;

        if (!string.IsNullOrEmpty(roundSummary)) {
            text += roundSummary + "\n";
        }

        p1 += $"P1: {player_1_score}";
        p2 += $"P2: {player_2_score}";

        if (!string.IsNullOrEmpty(finalSummary)) {
            text += "\n" + finalSummary;
        }

        messageText.text = text;
        p1Text.text = p1;
        p2Text.text = p2;
    }

    IEnumerator ResetBoard() {
        yield return new WaitForSeconds(0.5f);
        if (lastCardPlayed_1?.card != null && discard_container_1 != null) {
            lastCardPlayed_1.card.transform.SetParent(discard_container_1.transform, true);
            discard_container_1.SpawnCard(lastCardPlayed_1.card);
        }

        if (lastCardPlayed_2?.card != null && discard_container_2 != null) {
            lastCardPlayed_2.card.transform.SetParent(discard_container_2.transform, true);
            discard_container_2.SpawnCard(lastCardPlayed_2.card);
        }
        
        lastCardPlayed_1 = null;
        lastCardPlayed_2 = null;
        container_1.setCardPlayed(false);
        container_2.setCardPlayed(false);
        currentLeaderContainer = null;
        roundResolving = false;
        RoundReset?.Invoke();
    }

    private enum RoundOutcome {
        Tie,
        Player1,
        Player2
    }
}