using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "AI/RandomCardPlayStrategy")]
public class RandomCardPlayStrategy : AICardPlayStrategy {
	public override CardWrapper SelectCard(List<CardWrapper> hand, BoardContainer board, bool isFollower) {
		if (hand == null || hand.Count == 0) {
			return null;
		}

		var index = Random.Range(0, hand.Count);
		return hand[index];
	}
}
