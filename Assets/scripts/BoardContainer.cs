using System.Collections;
using System.Linq;
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
        if (lastCardPlayed_1 != null && lastCardPlayed_2 != null) {

            int powerValue_1 = lastCardPlayed_1?.card?.gameObject?.GetComponent<CardView>()?.powerValue ?? -1;
            int powerValue_2 = lastCardPlayed_2?.card?.gameObject?.GetComponent<CardView>()?.powerValue ?? -1;

            int elementValue_1 = lastCardPlayed_1?.card?.gameObject?.GetComponent<CardView>()?.elementValue ?? -1;
            int elementValue_2 = lastCardPlayed_2?.card?.gameObject?.GetComponent<CardView>()?.elementValue ?? -1;

            if (elementValue_1 + 1 == elementValue_2 || (elementValue_1 == 2 && elementValue_2 == 0)) { //element triangle
                player_2_score += 1;
                Debug.LogWarning("BoardContainer: Player 2 wins the round!");
            } else if (elementValue_2 + 1 == elementValue_1 || (elementValue_2 == 2 && elementValue_1 == 0)) {
                Debug.LogWarning("BoardContainer: Player 1 wins the round!");
                player_1_score += 1;
            } else{
                //elements are the same, compare power
                if (powerValue_1 < powerValue_2) { // power comparison
                    Debug.LogWarning("BoardContainer: Player 2 wins the round!");
                    player_2_score += 1;
                } else if (powerValue_1 > powerValue_2) {
                    Debug.LogWarning("BoardContainer: Player 1 wins the round!");
                    player_1_score += 1;
                } else {
                    Debug.LogWarning("BoardContainer: It's a tie!");
                }
            }

            messageText.text = $"Player 1 score: {player_1_score} \nPlayer 2 score: {player_2_score}";
        


            // int powerValue = lastCardPlayed_1?.card?.gameObject?.CardData?.powerValue ?? -1;
            // Debug.LogWarning(powerValue_1);



            StartCoroutine(ResetBoard());
            Debug.LogWarning("BoardContainer: Both cards played!");

            // container_1.DestroyCard(lastCardPlayed_1.card);
            // container_2.DestroyCard(lastCardPlayed_2.card);
            // lastCardPlayed_1 = null;
            // lastCardPlayed_2 = null;
            // container_1.setCardPlayed(false);
            // container_2.setCardPlayed(false);


            // Do something when both cards have been played
        }
    }

    IEnumerator ResetBoard() {
        yield return new WaitForSeconds(0.5f);
        lastCardPlayed_1.card.transform.SetParent(discard_container_1.transform, true);
        
        lastCardPlayed_2.card.transform.SetParent(discard_container_2.transform, true);
        discard_container_1.SpawnCard(lastCardPlayed_1.card);
        discard_container_2.SpawnCard(lastCardPlayed_2.card);
        
        lastCardPlayed_1 = null;
        lastCardPlayed_2 = null;
        container_1.setCardPlayed(false);
        container_2.setCardPlayed(false);
    }

    
}