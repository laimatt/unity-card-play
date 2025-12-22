using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "AI/WeightedOptimalStrategy")]
public class WeightedOptimalStrategy : AICardPlayStrategy {
    public override CardWrapper SelectCard(List<CardWrapper> hand, BoardContainer board, bool isFollower) {
        if (hand == null || hand.Count == 0) {
            return null;
        }

        CardWrapper best = hand[0];
        var bestScore = ScoreCard(best);

        for (int i = 1; i < hand.Count; i++) {
            var candidate = hand[i];
            var score = ScoreCard(candidate);
            if (score > bestScore) {
                best = candidate;
                bestScore = score;
            }
        }

        return best;
    }

    private int ScoreCard(CardWrapper card) {
        if (card == null) return int.MinValue;
        var view = card.GetComponent<CardView>();
        if (view == null) return 0;

        // Simple heuristic: prefer higher power; break ties by element value for determinism.
        return (view.powerValue * 10) + view.elementValue;
    }
}
