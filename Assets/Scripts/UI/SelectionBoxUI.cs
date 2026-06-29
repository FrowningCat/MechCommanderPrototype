using UnityEngine;
using UnityEngine.UI;

public class SelectionBoxUI : MonoBehaviour
{
    [SerializeField] private RectTransform boxRect;

    private Vector2 startPos;

    public void BeginSelection(Vector2 mousePos)
    {
        boxRect.gameObject.SetActive(true);

        startPos = mousePos;

        boxRect.anchoredPosition = startPos;
        boxRect.sizeDelta = Vector2.zero;
    }

    public void UpdateSelection(Vector2 mousePos)
    {
        Vector2 size = mousePos - startPos;

        boxRect.anchoredPosition =
            startPos + size * 0.5f;

        boxRect.sizeDelta = new Vector2(
            Mathf.Abs(size.x),
            Mathf.Abs(size.y)
        );
    }

    public void EndSelection()
    {
        boxRect.gameObject.SetActive(false);
    }
}