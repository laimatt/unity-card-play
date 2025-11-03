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
    private TextMeshProUGUI messageText;

    public event Action RoundReset;

    private int totalTricksExpected;
    private int tricksPlayed;
    private bool matchComplete;
    private bool tieBreakerApplied;
    private bool parityWarningIssued;

    private const int ElementRed = 0;
    private const int ElementGreen = 1;
    private const int ElementBlue = 2;

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
        lastCardPlayed_1 = e;
        // you can access the card via e.card if needed
        BothCardsPlayed();
    }

    private void OnCardPlayedFromContainer2(CardPlayed e) {
        lastCardPlayed_2 = e;
        BothCardsPlayed();
    }

    public void BothCardsPlayed() {
        if (matchComplete || lastCardPlayed_1 == null || lastCardPlayed_2 == null) {
            return;
        }

        EnsureTotalTricksInitialized();

        var cardView1 = lastCardPlayed_1.card != null ? lastCardPlayed_1.card.GetComponent<CardView>() : null;
        var cardView2 = lastCardPlayed_2.card != null ? lastCardPlayed_2.card.GetComponent<CardView>() : null;

        int powerValue_1 = cardView1 != null ? cardView1.powerValue : -1;
        int powerValue_2 = cardView2 != null ? cardView2.powerValue : -1;

        int elementValue_1 = cardView1 != null ? cardView1.elementValue : -1;
        int elementValue_2 = cardView2 != null ? cardView2.elementValue : -1;

        var outcome = ResolveRoundOutcome(elementValue_1, powerValue_1, elementValue_2, powerValue_2);

        string roundMessage;
        switch (outcome) {
            case RoundOutcome.Player1:
                player_1_score += 1;
                roundMessage = "Player 1 wins the round!";
                break;
            case RoundOutcome.Player2:
                player_2_score += 1;
                roundMessage = "Player 2 wins the round!";
                break;
            default:
                roundMessage = "Round is a tie!";
                break;
        }

        tricksPlayed += 1;
        var finalMessage = CheckForMatchEnd();
        UpdateScoreDisplay(roundMessage, finalMessage);

        Debug.LogWarning($"BoardContainer: {roundMessage}");
        if (!string.IsNullOrEmpty(finalMessage)) {
            Debug.LogWarning($"BoardContainer: {finalMessage}");
        }

        StartCoroutine(ResetBoard());
        Debug.LogWarning("BoardContainer: Both cards played!");
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

        Debug.LogWarning("BoardContainer: Uneven trick count detected. Consider adjusting hand sizes to keep turns even.");
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

    private RoundOutcome ResolveRoundOutcome(int elementOne, int powerOne, int elementTwo, int powerTwo) {
        var elementComparison = CompareElements(elementOne, elementTwo);
        if (elementComparison > 0) {
            return RoundOutcome.Player1;
        }

        if (elementComparison < 0) {
            return RoundOutcome.Player2;
        }

        if (powerOne > powerTwo) {
            return RoundOutcome.Player1;
        }

        if (powerTwo > powerOne) {
            return RoundOutcome.Player2;
        }

        return RoundOutcome.Tie;
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

    private void UpdateScoreDisplay(string roundSummary = null, string finalSummary = null) {
        if (messageText == null) {
            return;
        }

        var text = string.Empty;
        if (!string.IsNullOrEmpty(roundSummary)) {
            text += roundSummary + "\n";
        }

        text += $"Player 1 score: {player_1_score}\n";
        text += $"Player 2 score: {player_2_score}";

        if (!string.IsNullOrEmpty(finalSummary)) {
            text += "\n" + finalSummary;
        }

        messageText.text = text;
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
        RoundReset?.Invoke();
    }

    private enum RoundOutcome {
        Tie,
        Player1,
        Player2
    }
}