using UnityEngine;
using System.Collections.Generic;

public class TouchGestureHandler : MonoBehaviour
{
    [Header("Настройки шара")]
    [SerializeField] private Rigidbody2D ball = null;
    [SerializeField] private float speed = 0.2f;

    [Header("Настройки свайпа")]
    [Tooltip("Мин. длина свайпа (пиксели).")]
    [SerializeField] private float SwipeThreshold = 220f;
    [Tooltip("Допустимое отклонение по перпендикулярной оси (пиксели).")]
    [SerializeField] private float MaxDeviation = 150f;
    [Tooltip("Мин. кол-во отсчётов для анализа. Помогает отсеять короткие «дребезги».")]
    [SerializeField] private int MinSampleCount = 3;

    [Header("Настройки масштабирования")]
    [SerializeField] private bool SimulateSecondTouch = false;
    [SerializeField] private Vector2 SimulatedTouchPosition = new Vector2(150f, 150f);

    // Трекинг точек свайпа
    private List<Vector2> _swipePositions = new List<Vector2>();
    private bool _isSwiping;

    private bool _isPinching;
    private float _initialPinchDistance;

    private void Start()
    {
        if (!ball)
        {
            Debug.LogError($"TouchGestureHandler: ball is not find");
            enabled = false;
            return;
        }
    }

    private void Update()
    {
        AddForce();
        TrackSwipe();
        DetectPinch();
    }

    private void AddForce()
    {
        if (Input.touchCount == 0)
            return;

        Touch t = Input.GetTouch(0);
        if (t.phase == TouchPhase.Moved)
        {
            Vector2 diffTouchMoveForce = t.deltaPosition;
            ball.AddForce(diffTouchMoveForce * speed);
        }
    }

    private void TrackSwipe()
    {
        if (Input.touchCount == 0)
        {
            _isSwiping = false;
            _swipePositions.Clear();
            return;
        }

        Touch t = Input.GetTouch(0);
        switch (t.phase)
        {
            case TouchPhase.Began:
                _isSwiping = true;
                _swipePositions.Clear();
                _swipePositions.Add(t.position);
                break;

            case TouchPhase.Moved or TouchPhase.Stationary when _isSwiping:
                _swipePositions.Add(t.position);
                break;

            case TouchPhase.Ended when _isSwiping:
                _swipePositions.Add(t.position);
                AnalyzeSwipe();
                _isSwiping = false;
                _swipePositions.Clear();
                break;
        }
    }

    /// <summary>
    /// Разбивает весь путь на сегменты однородного направления,
    /// берёт последний сегмент и по нему решает, в какую сторону свайп.
    /// </summary>
    private void AnalyzeSwipe()
    {
        var swipePoints = _swipePositions;
        int totalPoints = swipePoints.Count;
        if (totalPoints < MinSampleCount)
            return;

        int segmentStartIndex = 0;
        bool isHorizontalSegment = false;
        int segmentDirection = 0; // +1 = движение вправо/вверх, -1 = влево/вниз

        // Находим первое заметное движение, чтобы задать начальные параметры сегмента
        for (int i = 1; i < totalPoints; i++)
        {
            Vector2 delta = swipePoints[i] - swipePoints[i - 1];
            if (delta.sqrMagnitude < 1f)
                continue; // игнорируем шум

            isHorizontalSegment = Mathf.Abs(delta.x) > Mathf.Abs(delta.y);
            segmentDirection = isHorizontalSegment
                                  ? (delta.x > 0 ? +1 : -1)
                                  : (delta.y > 0 ? +1 : -1);
            segmentStartIndex = i - 1;
            break;
        }

        if (segmentDirection == 0)
            return; // не было движения

        // Проходим по точкам и сбрасываем начало сегмента при смене направления или оси
        for (int i = segmentStartIndex + 1; i < totalPoints; i++)
        {
            Vector2 delta = swipePoints[i] - swipePoints[i - 1];
            if (delta.sqrMagnitude < 1f)
                continue;

            bool currentIsHor = Mathf.Abs(delta.x) > Mathf.Abs(delta.y);
            int currentDir = currentIsHor
                                   ? (delta.x > 0 ? +1 : -1)
                                   : (delta.y > 0 ? +1 : -1);

            if (currentIsHor == isHorizontalSegment && currentDir == segmentDirection)
            {
                // остаёмся в том же сегменте
            }
            else
            {
                // смена направления/оси — стартуем новый сегмент
                segmentStartIndex = i - 1;
                isHorizontalSegment = currentIsHor;
                segmentDirection = currentDir;
            }
        }

        // Анализируем последний сегмент
        Vector2 segmentStartPoint = swipePoints[segmentStartIndex];
        Vector2 segmentEndPoint = swipePoints[totalPoints - 1];
        Vector2 segmentDelta = segmentEndPoint - segmentStartPoint;

        if (isHorizontalSegment)
        {
            if (segmentDelta.x > SwipeThreshold && Mathf.Abs(segmentDelta.y) <= MaxDeviation)
                Debug.Log("Свайп вправо");
            else if (segmentDelta.x < -SwipeThreshold && Mathf.Abs(segmentDelta.y) <= MaxDeviation)
                Debug.Log("Свайп влево");
        }
        else
        {
            if (segmentDelta.y > SwipeThreshold && Mathf.Abs(segmentDelta.x) <= MaxDeviation)
                Debug.Log("Свайп вверх");
            else if (segmentDelta.y < -SwipeThreshold && Mathf.Abs(segmentDelta.x) <= MaxDeviation)
                Debug.Log("Свайп вниз");
        }
    }


    private void DetectPinch()
    {
        int cnt = Input.touchCount;
        Vector2 p1, p2;

        if (cnt >= 2)
        {
            p1 = Input.GetTouch(0).position;
            p2 = Input.GetTouch(1).position;
        }
        else if (SimulateSecondTouch && cnt == 1)
        {
            p1 = Input.GetTouch(0).position;
            p2 = SimulatedTouchPosition;
        }
        else
        {
            _isPinching = false;
            return;
        }

        float curDist = Vector2.Distance(p1, p2);

        if (!_isPinching)
        {
            _initialPinchDistance = curDist;
            _isPinching = true;
        }
        else
        {
            if (curDist > _initialPinchDistance)
                Debug.Log("Жест увеличение");
            else if (curDist < _initialPinchDistance)
                Debug.Log("Жест уменьшение");

            _isPinching = false;
        }
    }
}