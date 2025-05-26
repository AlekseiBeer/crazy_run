using UnityEngine;
using System.Collections.Generic;

public class TouchGestureHandler : MonoBehaviour
{
    [Header("��������� ����")]
    [SerializeField] private Rigidbody2D ball = null;
    [SerializeField] private float speed = 0.2f;

    [Header("��������� ������")]
    [Tooltip("���. ����� ������ (�������).")]
    [SerializeField] private float SwipeThreshold = 220f;
    [Tooltip("���������� ���������� �� ���������������� ��� (�������).")]
    [SerializeField] private float MaxDeviation = 150f;
    [Tooltip("���. ���-�� �������� ��� �������. �������� ������� �������� ���������.")]
    [SerializeField] private int MinSampleCount = 3;

    [Header("��������� ���������������")]
    [SerializeField] private bool SimulateSecondTouch = false;
    [SerializeField] private Vector2 SimulatedTouchPosition = new Vector2(150f, 150f);

    // ������� ����� ������
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
    /// ��������� ���� ���� �� �������� ����������� �����������,
    /// ���� ��������� ������� � �� ���� ������, � ����� ������� �����.
    /// </summary>
    private void AnalyzeSwipe()
    {
        var swipePoints = _swipePositions;
        int totalPoints = swipePoints.Count;
        if (totalPoints < MinSampleCount)
            return;

        int segmentStartIndex = 0;
        bool isHorizontalSegment = false;
        int segmentDirection = 0; // +1 = �������� ������/�����, -1 = �����/����

        // ������� ������ �������� ��������, ����� ������ ��������� ��������� ��������
        for (int i = 1; i < totalPoints; i++)
        {
            Vector2 delta = swipePoints[i] - swipePoints[i - 1];
            if (delta.sqrMagnitude < 1f)
                continue; // ���������� ���

            isHorizontalSegment = Mathf.Abs(delta.x) > Mathf.Abs(delta.y);
            segmentDirection = isHorizontalSegment
                                  ? (delta.x > 0 ? +1 : -1)
                                  : (delta.y > 0 ? +1 : -1);
            segmentStartIndex = i - 1;
            break;
        }

        if (segmentDirection == 0)
            return; // �� ���� ��������

        // �������� �� ������ � ���������� ������ �������� ��� ����� ����������� ��� ���
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
                // ������� � ��� �� ��������
            }
            else
            {
                // ����� �����������/��� � �������� ����� �������
                segmentStartIndex = i - 1;
                isHorizontalSegment = currentIsHor;
                segmentDirection = currentDir;
            }
        }

        // ����������� ��������� �������
        Vector2 segmentStartPoint = swipePoints[segmentStartIndex];
        Vector2 segmentEndPoint = swipePoints[totalPoints - 1];
        Vector2 segmentDelta = segmentEndPoint - segmentStartPoint;

        if (isHorizontalSegment)
        {
            if (segmentDelta.x > SwipeThreshold && Mathf.Abs(segmentDelta.y) <= MaxDeviation)
                Debug.Log("����� ������");
            else if (segmentDelta.x < -SwipeThreshold && Mathf.Abs(segmentDelta.y) <= MaxDeviation)
                Debug.Log("����� �����");
        }
        else
        {
            if (segmentDelta.y > SwipeThreshold && Mathf.Abs(segmentDelta.x) <= MaxDeviation)
                Debug.Log("����� �����");
            else if (segmentDelta.y < -SwipeThreshold && Mathf.Abs(segmentDelta.x) <= MaxDeviation)
                Debug.Log("����� ����");
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
                Debug.Log("���� ����������");
            else if (curDist < _initialPinchDistance)
                Debug.Log("���� ����������");

            _isPinching = false;
        }
    }
}