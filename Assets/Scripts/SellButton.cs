using UnityEngine;
using UnityEngine.UI;

public class SellButton : MonoBehaviour
{
    private Button button;

    void Start()
    {
        button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(() => {
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.SellOre();
                }
                else
                {
                    Debug.LogError("GameManager.Instance не найден!");
                }
            });
        }
    }
}