using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CardView : MonoBehaviour {
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private Image artworkImage;
    [SerializeField] private int powerValue;
    [SerializeField] private int elementValue;
    [SerializeField] private Image powerImage;
    [SerializeField] private Image elementImage;

    public void Initialize(CardData data) {
        if (titleText != null) titleText.text = data.cardName;
        if (artworkImage != null) artworkImage.sprite = data.artwork;
        if (powerImage != null) powerImage.sprite = data.power_art;
        if (elementImage != null) elementImage.sprite = data.element_art;
        powerValue = data.power;
        elementValue = data.element;
        gameObject.name = data.cardName; // helpful for debugging/events
    }

    // Convenience initializer when you only have sprite + title (used by DeckManager)
    public void Initialize(Sprite artwork, string title, int power, int element) {
        if (artworkImage != null) artworkImage.sprite = artwork;
        if (titleText != null) titleText.text = title;
        powerValue = power; 
        elementValue = element;
        
        Debug.LogWarning($"{power}, {element}");
        // Note: prefer direct sprite references passed from DeckManager instead of Resources.Load
        if (powerImage != null) powerImage.sprite = null;
        if (elementImage != null) elementImage.sprite = null;

        gameObject.name = title;
        
    }

    // Overload accepting direct sprites for power and element icons
    public void Initialize(Sprite artwork, string title, int power, int element, Sprite powerSprite, Sprite elementSprite) {
        if (artworkImage != null) artworkImage.sprite = artwork;
        if (titleText != null) titleText.text = title;
        powerValue = power;
        elementValue = element;
        if (powerImage != null) powerImage.sprite = powerSprite;
        if (elementImage != null) elementImage.sprite = elementSprite;
        gameObject.name = title;
    }
}