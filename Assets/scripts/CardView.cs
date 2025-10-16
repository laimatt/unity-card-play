using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CardView : MonoBehaviour {
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private Image artworkImage;
    [SerializeField] private TMP_Text powerText;

    public void Initialize(CardData data) {
        if (titleText != null) titleText.text = data.cardName;
        if (artworkImage != null) artworkImage.sprite = data.artwork;
        if (powerText != null) powerText.text = data.power.ToString();
        gameObject.name = data.cardName; // helpful for debugging/events
    }

    // Convenience initializer when you only have sprite + title (used by DeckManager)
    public void Initialize(Sprite artwork, string title) {
        if (artworkImage != null) artworkImage.sprite = artwork;
        if (titleText != null) titleText.text = title;
        if (powerText != null) powerText.text = "";
        gameObject.name = title;
    }
}