using System.Collections;
using events;
using UnityEngine;
using UnityEngine.UI;
using TMPro; 

// Attach this script to a GameObject with a UI Text (or other display) and wire it to
// the CardContainer -> Events -> OnCardPlayed event in the Inspector.
public class CardPlayedDisplay : MonoBehaviour {
    [SerializeField]
    private TextMeshProUGUI messageText;

    [SerializeField]
    private float showDuration = 2f;

    public void ShowCardPlayed(CardPlayed evt) {
        if (messageText == null) {
            Debug.LogWarning("CardPlayedDisplay: messageText is not assigned.");
            return;
        }

        var cardName = evt?.card?.gameObject?.name ?? "Card";
        messageText.text = $"Played: {cardName}";

        // Debug.LogWarning($"CardPlayedDisplay: {cardName}");
        StopAllCoroutines();
        StartCoroutine(HideAfterDelay());

    }

    private IEnumerator HideAfterDelay() {
        yield return new WaitForSeconds(showDuration);
        if (messageText != null) messageText.text = "";
    }
}