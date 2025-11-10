using DefaultNamespace;
using UnityEngine;

public class GameModeManager : MonoBehaviour {
    [SerializeField]
    private TurnManager turnManager;

    [SerializeField]
    private AICardContainerController aiController;

    [SerializeField]
    private GameMode defaultMode = GameMode.PlayerVsAI;

    [SerializeField]
    private bool applyDefaultOnAwake = true;

    private void Awake() {
        if (applyDefaultOnAwake) {
            ApplyMode(defaultMode);
        }
    }

    public void ApplyMode(GameMode mode) {
        if (turnManager == null) {
            Debug.LogWarning("GameModeManager: TurnManager reference not assigned.");
            return;
        }

        turnManager.SetGameMode(mode);

        if (aiController != null) {
            var shouldEnableAI = mode == GameMode.PlayerVsAI;
            aiController.enabled = shouldEnableAI;
        }
    }

    // These helpers allow hooking UI buttons or keyboard shortcuts in the inspector.
    public void SetPlayerVsAI() {
        ApplyMode(GameMode.PlayerVsAI);
    }

    public void SetPlayerVsPlayer() {
        ApplyMode(GameMode.PlayerVsPlayer);
    }
}
