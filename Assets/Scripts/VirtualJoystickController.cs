using UnityEngine;

/// <summary>
/// Виртуальный джойстик для управления 3D-шариком без инерции.
/// Размер джойстика определяется долей экрана (JoystickSizeRatio) и не превышает MaxJoystickRadius.
/// Добавлена мёртвая зона (DeadZoneRatio).
/// Движение за счёт установки Rigidbody.velocity, без накопления силы.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class VirtualJoystickController : MonoBehaviour
{
    [Header("Rigidbody шарика")]
    public Rigidbody ballRigidbody;

    [Header("Скорость движения")]
    [Tooltip("Скорость шарика по осям XZ.")]
    public float moveSpeed = 5f;

    [Header("Размер джойстика")]
    [Range(0.1f, 0.5f), Tooltip("Доля ширины экрана для диаметра джойстика.")]
    public float joystickSizeRatio = 0.25f;
    [Tooltip("Максимальный радиус джойстика в пикселях.")]
    public float maxJoystickRadius = 150f;

    [Header("Мёртвая зона")]
    [Range(0f, 1f), Tooltip("Минимальный ввод как доля радиуса, ниже которого игнорируем движение.")]
    public float deadZoneRatio = 0.1f;

    // Текстуры
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
        // Создадим текстуры позже, когда будем знать размер
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
        // Рассчитываем радиус джойстика
        float desired = Screen.width * joystickSizeRatio * 0.5f;
        joystickRadius = Mathf.Min(desired, maxJoystickRadius);
        // Создаем текстуры круга
        int baseSize = Mathf.RoundToInt(joystickRadius * 2f);
        baseTex = CreateCircleTexture(baseSize, new Color(1, 1, 1, 0.3f));
        // Ручка — чуть меньшего радиуса (60% от базы)
        int knobSize = Mathf.RoundToInt(joystickRadius * 1.2f);
        knobTex = CreateCircleTexture(knobSize, Color.white);
        // Установим начальную позицию ручки
        knobPosition = joystickCenter;
    }

    void Update()
    {
        ProcessTouch();
    }

    void FixedUpdate()
    {
        // Управление скоростью без инерции, с плавным нарастанием
        if (inputVector.magnitude > 0f)
        {
            // Нормируем направление
            Vector2 dir = inputVector.normalized;
            // Интерполируем величину исходя из мёртвой зоны: от 0 при deadZone до 1 при границе джойстика
            float mag = inputVector.magnitude;
            float smooth = Mathf.InverseLerp(deadZoneRatio, 1f, mag);
            // Плавная кривая ускорения (можно заменить на Mathf.Pow или SmoothStep)
            float speedFactor = Mathf.SmoothStep(0f, 1f, smooth);
            Vector3 vel = new Vector3(dir.x, 0f, dir.y) * moveSpeed * speedFactor;
            ballRigidbody.velocity = vel;
        }
        else
        {
            // Останавливаем шарик
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
                // Мёртвая зона
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
        // База
        Rect baseRect = new Rect(
            joystickCenter.x - joystickRadius,
            Screen.height - joystickCenter.y - joystickRadius,
            joystickRadius * 2f,
            joystickRadius * 2f);
        GUI.DrawTexture(baseRect, baseTex);
        // Ручка (меньший диаметр)
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


