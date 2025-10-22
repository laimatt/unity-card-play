using System.Collections.Generic;
using System.Linq;
using config;
using DefaultNamespace;
using events;
using UnityEngine;
using UnityEngine.UI;

public class BoardContainer : MonoBehaviour {
    [Header("Containers")]
    [SerializeField]
    private CardContainer container_1;
    [SerializeField]
    private CardContainer container_2;
    [Header("Config")]
    [SerializeField]
    private BoardConfig boardConfig;

    private bool container1Played = false;
    private bool container2Played = false;

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
        container1Played = true;
        // you can access the card via e.card if needed
        BothCardsPlayed();
    }

    private void OnCardPlayedFromContainer2(CardPlayed e) {
        container2Played = true;
        BothCardsPlayed();
    }

    public void BothCardsPlayed() {
        if (container1Played && container2Played) {
            Debug.LogWarning("BoardContainer: Both cards played!");
            // Do something when both cards have been played
        }
    }
}