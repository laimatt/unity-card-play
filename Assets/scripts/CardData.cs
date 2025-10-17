using UnityEngine;

[CreateAssetMenu(menuName = "Card/CardData")]
public class CardData : ScriptableObject {
    public string cardName;
    public Sprite artwork;
    public int power;
    public int element; 
    public Sprite power_art;
    public Sprite element_art;
    // add more fields as needed
}