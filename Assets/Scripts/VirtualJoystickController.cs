using UnityEngine;

/// <summary>
/// ����������� �������� ��� ���������� 3D-������� ��� �������.
/// ������ ��������� ������������ ����� ������ (JoystickSizeRatio) � �� ��������� MaxJoystickRadius.
/// ��������� ������ ���� (DeadZoneRatio).
/// �������� �� ���� ��������� Rigidbody.velocity, ��� ���������� ����.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class VirtualJoystickController : MonoBehaviour
{
    [Header("Rigidbody ������")]
    public Rigidbody ballRigidbody;

    [Header("�������� ��������")]
    [Tooltip("�������� ������ �� ���� XZ.")]
    public float moveSpeed = 5f;

    [Header("������ ���������")]
    [Range(0.1f, 0.5f), Tooltip("���� ������ ������ ��� �������� ���������.")]
    public float joystickSizeRatio = 0.25f;
    [Tooltip("������������ ������ ��������� � ��������.")]
    public float maxJoystickRadius = 150f;

    [Header("̸����� ����")]
    [Range(0f, 1f), Tooltip("����������� ���� ��� ���� �������, ���� �������� ���������� ��������.")]
    public float deadZoneRatio = 0.1f;

    // ��������
    private Texture2D baseTex;
    private Texture2D knobTex;

    private Vector2 defaultCenter;
    private Vector2 joystickCenter;
    private Vector2 knobPosition;
    private Vector2 inputVector;
    private bool isTouching;

    private float joystickRadius;

    void Awake()
    {
        if (ballRigidbody == null)
            ballRigidbody = GetComponent<Rigidbody>();
        // �������� �������� �����, ����� ����� ����� ������
    }

    void Start()
    {
        defaultCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.2f);
        joystickCenter = defaultCenter;
        inputVector = Vector2.zero;
        InitializeJoystickTextures();
    }

    void InitializeJoystickTextures()
    {
        // ������������ ������ ���������
        float desired = Screen.width * joystickSizeRatio * 0.5f;
        joystickRadius = Mathf.Min(desired, maxJoystickRadius);
        // ������� �������� �����
        int baseSize = Mathf.RoundToInt(joystickRadius * 2f);
        baseTex = CreateCircleTexture(baseSize, new Color(1, 1, 1, 0.3f));
        // ����� � ���� �������� ������� (60% �� ����)
        int knobSize = Mathf.RoundToInt(joystickRadius * 1.2f);
        knobTex = CreateCircleTexture(knobSize, Color.white);
        // ��������� ��������� ������� �����
        knobPosition = joystickCenter;
    }

    void Update()
    {
        ProcessTouch();
    }

    void FixedUpdate()
    {
        // ���������� ��������� ��� �������, � ������� �����������
        if (inputVector.magnitude > 0f)
        {
            // ��������� �����������
            Vector2 dir = inputVector.normalized;
            // ������������� �������� ������ �� ������ ����: �� 0 ��� deadZone �� 1 ��� ������� ���������
            float mag = inputVector.magnitude;
            float smooth = Mathf.InverseLerp(deadZoneRatio, 1f, mag);
            // ������� ������ ��������� (����� �������� �� Mathf.Pow ��� SmoothStep)
            float speedFactor = Mathf.SmoothStep(0f, 1f, smooth);
            Vector3 vel = new Vector3(dir.x, 0f, dir.y) * moveSpeed * speedFactor;
            ballRigidbody.velocity = vel;
        }
        else
        {
            // ������������� �����
            ballRigidbody.velocity = Vector3.zero;
        }
    }

    void ProcessTouch()
    {
        if (Input.touchCount > 0)
        {
            Touch t = Input.GetTouch(0);
            if (t.phase == TouchPhase.Began)
            {
                isTouching = true;
                joystickCenter = t.position;
            }
            else if ((t.phase == TouchPhase.Moved || t.phase == TouchPhase.Stationary) && isTouching)
            {
                Vector2 delta = t.position - joystickCenter;
                float dist = delta.magnitude;
                if (dist > joystickRadius)
                    delta = delta.normalized * joystickRadius;
                knobPosition = joystickCenter + delta;
                inputVector = delta / joystickRadius;
                // ̸����� ����
                if (inputVector.magnitude < deadZoneRatio)
                {
                    inputVector = Vector2.zero;
                    knobPosition = joystickCenter;
                }
            }
            else if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
            {
                isTouching = false;
                ResetJoystick();
            }
        }
        else if (!isTouching)
        {
            ResetJoystick();
        }
    }

    void ResetJoystick()
    {
        joystickCenter = defaultCenter;
        knobPosition = defaultCenter;
        inputVector = Vector2.zero;
        isTouching = false;
    }

    void OnGUI()
    {
        if (baseTex == null) return;
        // ����
        Rect baseRect = new Rect(
            joystickCenter.x - joystickRadius,
            Screen.height - joystickCenter.y - joystickRadius,
            joystickRadius * 2f,
            joystickRadius * 2f);
        GUI.DrawTexture(baseRect, baseTex);
        // ����� (������� �������)
        float knobSize = joystickRadius * 0.8f;
        Rect knobRect = new Rect(
            knobPosition.x - knobSize * 0.5f,
            Screen.height - knobPosition.y - knobSize * 0.5f,
            knobSize, knobSize);
        GUI.DrawTexture(knobRect, knobTex);
    }

    Texture2D CreateCircleTexture(int diameter, Color color)
    {
        Texture2D tex = new Texture2D(diameter, diameter, TextureFormat.ARGB32, false);
        int r = diameter / 2;
        for (int y = 0; y < diameter; y++)
            for (int x = 0; x < diameter; x++)
            {
                float dx = x - r;
                float dy = y - r;
                tex.SetPixel(x, y, (dx * dx + dy * dy <= r * r) ? color : Color.clear);
            }
        tex.Apply();
        return tex;
    }
}


