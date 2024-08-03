using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class Potion : MonoBehaviour, IPointerClickHandler
{
    public static Action<OnAnyPotionClickEventArgs> OnAnyPotionClick;

    public class OnAnyPotionClickEventArgs : EventArgs
    {
        public int x = 0;
        public int y = 0;
    }

    [Header("Attributes")]
    [SerializeField] public PotionType potionType;

    public int xCoordinate;
    public int yCoordinate;

    public bool isMatched;

    public bool isMoving;

    public Potion(int xCoordinate, int yCoordinate)
    {
        this.xCoordinate = xCoordinate;
        this.yCoordinate = yCoordinate;
    }

    public void SetCoordinates(int xCoordinate, int yCoordinate)
    {
        this.xCoordinate = xCoordinate;
        this.yCoordinate = yCoordinate;
    }
    public void MoveToTarget(Transform potionStandPos)
    {
        StartCoroutine(MoveCoroutine(potionStandPos));
    }

    private IEnumerator MoveCoroutine(Transform potionStandPos)
    {
        isMoving = true;

        float elapsed = 0f;
        float duration = 0.2f;
        float t = elapsed / duration;

        Vector2 startPos = transform.position;

        while (elapsed < duration)
        {

            t = elapsed / duration;

            transform.position = Vector2.Lerp(startPos, potionStandPos.position, t);

            elapsed += Time.deltaTime;

            yield return null;
        }

        transform.position = potionStandPos.position;
        transform.SetParent(potionStandPos);
        isMoving = false;
    }
    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log(potionType);
        OnAnyPotionClick?.Invoke(new OnAnyPotionClickEventArgs { x = xCoordinate, y = yCoordinate });
    }

}

public enum PotionType
{
    Red,
    Blue,
    Purple,
    Green,
    White
}
