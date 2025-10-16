using UnityEngine;

[CreateAssetMenu(menuName = "Card/CardData")]
public class CardData : ScriptableObject {
    public string cardName;
    public Sprite artwork;
    public int power;
    // add more fields as needed
}