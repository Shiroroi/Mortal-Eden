using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Simple UI for turn management
/// Attach to a Canvas
/// </summary>
public class TurnUI : MonoBehaviour
{
    [Header("UI References")]
    public Button endTurnButton;
    public TextMeshProUGUI turnText;

    void Start()
    {
        if (endTurnButton != null)
        {
            endTurnButton.onClick.AddListener(OnEndTurnClicked);
        }

        UpdateUI();

        // Subscribe to turn events
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.OnTurnStart.AddListener(UpdateUI);
        }
    }

    void OnEndTurnClicked()
    {
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.EndTurn();
        }
    }

    void UpdateUI()
    {
        if (TurnManager.Instance != null && turnText != null)
        {
            turnText.text = $"Turn {TurnManager.Instance.currentTurn}";
        }
    }
}