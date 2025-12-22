using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Base class for pluggable AI card selection strategies.
/// </summary>
public abstract class AICardPlayStrategy : ScriptableObject {
	/// <param name="hand">Current AI hand cards.</param>
	/// <param name="board">Board context (can be null).</param>
	/// <param name="isFollower">True if AI is playing second this trick.</param>
	public abstract CardWrapper SelectCard(List<CardWrapper> hand, BoardContainer board, bool isFollower);
}
