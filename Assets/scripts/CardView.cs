using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CardView : MonoBehaviour {
    [SerializeField] public string representation;

    [SerializeField] public string vowelharmony;
    [SerializeField] public string firstlast;
    [SerializeField] public string disharmonic;
    [SerializeField] public int powerValue;
    [SerializeField] public int elementValue;
    [SerializeField] private Image powerImage;
    [SerializeField] private Image elementImage;

    public void Initialize(CardData data) {
        representation = data.cardName;
        vowelharmony = data.vowelharmony; // or another field
        firstlast = data.firstlast; // or another field
        disharmonic = data.disharmonic;
        if (powerImage != null) powerImage.sprite = data.power_art;
        if (elementImage != null) elementImage.sprite = data.element_art;
        powerValue = data.power;
        elementValue = data.element;
        gameObject.name = data.cardName; // helpful for debugging/events
    }

    // Convenience initializer when you only have sprite + title (used by DeckManager)
    public void Initialize(string title, string vh, string fl, string dh, int power, int element) {
        representation = title;
        vowelharmony = vh; // or another field
        firstlast = fl; // or another field
        disharmonic = dh;
        powerValue = power; 
        elementValue = element;
        
        Debug.LogWarning($"{power}, {element}");
        // Note: prefer direct sprite references passed from DeckManager instead of Resources.Load
        if (powerImage != null) powerImage.sprite = null;
        if (elementImage != null) elementImage.sprite = null;

        gameObject.name = title;
        
    }

    // Overload accepting direct sprites for power and element icons
    public void Initialize(string title, string vh, string fl, string dh, int power, int element, Sprite powerSprite, Sprite elementSprite) {
        representation = title;
        vowelharmony = vh; // or another field
        firstlast = fl; // or another field
        disharmonic = dh;
        powerValue = power;
        elementValue = element;
        if (powerImage != null) powerImage.sprite = powerSprite;
        if (elementImage != null) elementImage.sprite = elementSprite;
        gameObject.name = title;
    }
}