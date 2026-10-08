using UnityEngine;
using UnityEngine.EventSystems;

public class Joystick : MonoBehaviour,
    IDragHandler,
    IPointerDownHandler,
    IPointerUpHandler
{
    public RectTransform background;
    public RectTransform handle;

    private Vector2 input;

    public Vector2 Input => input;

    public void OnPointerDown(PointerEventData eventData)
    {
        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 position;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            background,
            eventData.position,
            eventData.pressEventCamera,
            out position
        );

        float radius = background.sizeDelta.x / 2f;

        input = position / radius;

        input = Vector2.ClampMagnitude(input, 1f);

        handle.anchoredPosition = input * radius;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        input = Vector2.zero;
        handle.anchoredPosition = Vector2.zero;
    }
}