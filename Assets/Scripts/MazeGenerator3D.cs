using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class MazeGenerator3D : MonoBehaviour
{
    [Header("Размер лабиринта")]
    [SerializeField] private int width = 5;
    [SerializeField] private int height = 5;

    [Header("Увеличение размера")]
    [SerializeField] private int sizeIncrement = 1;

    [Header("Генерация")]
    [SerializeField, Range(0f, 1f), Tooltip("Вероятность выбора случайной клетки из списка вместо последней при росте.")]
    private float branchingProbability = 0.5f;
    [SerializeField, Tooltip("Количество петель (циклов) добавить после генерации.")]
    private int loopsToAdd = 2;

    [Header("Префабы")]
    [SerializeField] private GameObject cubePrefab;
    [SerializeField] private GameObject ballPrefab;

    [Header("Параметры стен/пола")]
    [SerializeField] private float wallHeight = 1f;
    [SerializeField] private float wallThickness = 0.1f;
    [SerializeField] private float floorThickness = 0.1f;

    [Header("Камера")]
    [SerializeField] private CinemachineVirtualCamera vcam;
    [Tooltip("Отступ камеры (в клетках)")]
    [SerializeField] private float cameraMargin = 0.5f;

    private Cell[,] grid;
    private GameObject mazeParent, markersParent, cameraTarget, ballInstance;

    void Start() => GenerateLevel();

    public void GenerateLevel()
    {
        // Очистка
        if (mazeParent != null) Destroy(mazeParent);
        if (markersParent != null) Destroy(markersParent);
        if (cameraTarget != null) Destroy(cameraTarget);
        if (ballInstance != null) Destroy(ballInstance);

        mazeParent = new GameObject("MazeParent");
        markersParent = new GameObject("MarkersParent");

        // Инициализируем сетку
        grid = new Cell[width, height];
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                grid[x, y] = new Cell(x, y);

        // Генерация лабиринта
        GrowingTree();
        AddLoops(loopsToAdd);

        // Визуализация
        BuildVisuals();

        // Камера и шарик
        CreateCameraTarget();
        SetupOrthographicCamera();
        SpawnBallAtStart();
    }

    void GrowingTree()
    {
        var rand = new System.Random();
        var list = new List<Cell>();
        // стартовая клетка
        var start = grid[0, 0];
        list.Add(start);
        start.InMaze = true;

        while (list.Count > 0)
        {
            // выбор клетки: либо последняя, либо случайная
            int idx = (rand.NextDouble() < branchingProbability) ? rand.Next(list.Count) : list.Count - 1;
            var cell = list[idx];

            // получаем соседей не включённых
            var neighbors = GetUnvisitedNeighbors(cell);
            if (neighbors.Count > 0)
            {
                // выбираем случайного соседа
                var next = neighbors[rand.Next(neighbors.Count)];
                // удаляем стену
                RemoveWall(cell, next);
                next.InMaze = true;
                list.Add(next);
            }
            else
            {
                list.RemoveAt(idx);
            }
        }
    }

    List<Cell> GetUnvisitedNeighbors(Cell c)
    {
        var list = new List<Cell>();
        int x = c.X, y = c.Y;
        // вверх
        if (y + 1 < height && !grid[x, y + 1].InMaze) list.Add(grid[x, y + 1]);
        // право
        if (x + 1 < width && !grid[x + 1, y].InMaze) list.Add(grid[x + 1, y]);
        // вниз
        if (y - 1 >= 0 && !grid[x, y - 1].InMaze) list.Add(grid[x, y - 1]);
        // влево
        if (x - 1 >= 0 && !grid[x - 1, y].InMaze) list.Add(grid[x - 1, y]);
        return list;
    }

    void RemoveWall(Cell a, Cell b)
    {
        int dx = b.X - a.X, dy = b.Y - a.Y;
        if (dx == 1) { a.Walls[1] = false; b.Walls[3] = false; }
        if (dx == -1) { a.Walls[3] = false; b.Walls[1] = false; }
        if (dy == 1) { a.Walls[0] = false; b.Walls[2] = false; }
        if (dy == -1) { a.Walls[2] = false; b.Walls[0] = false; }
    }

    void AddLoops(int count)
    {
        var allWalls = new List<(Cell, Cell)>();
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                var c = grid[x, y];
                if (c.Walls[0] && y + 1 < height) allWalls.Add((c, grid[x, y + 1]));
                if (c.Walls[1] && x + 1 < width) allWalls.Add((c, grid[x + 1, y]));
            }
        var rand = new System.Random();
        for (int i = 0; i < count && allWalls.Count > 0; i++)
        {
            var pair = allWalls[rand.Next(allWalls.Count)];
            RemoveWall(pair.Item1, pair.Item2);
        }
    }

    void BuildVisuals()
    {
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                // Пол
                float fy = -0.5f * floorThickness;
                var floor = Instantiate(cubePrefab, new Vector3(x, fy, y), Quaternion.identity, mazeParent.transform);
                floor.transform.localScale = new Vector3(1f, floorThickness, 1f);
                var rend = floor.GetComponent<Renderer>();
                if (rend != null)
                {
                    if (x == 0 && y == 0) rend.material.color = Color.green;
                    else if (x == width - 1 && y == height - 1) rend.material.color = Color.red;
                    else rend.material.color = Color.gray;
                }
                // Стены
                var c = grid[x, y]; var ctr = new Vector3(x, 0, y);
                float wl = 1f + wallThickness;
                if (c.Walls[0]) CreateWall(ctr + Vector3.forward * 0.5f + Vector3.up * (wallHeight / 2), Quaternion.identity, new Vector3(wl, wallHeight, wallThickness));
                if (c.Walls[1]) CreateWall(ctr + Vector3.right * 0.5f + Vector3.up * (wallHeight / 2), Quaternion.Euler(0, 90, 0), new Vector3(wl, wallHeight, wallThickness));
                if (c.Walls[2]) CreateWall(ctr + Vector3.back * 0.5f + Vector3.up * (wallHeight / 2), Quaternion.identity, new Vector3(wl, wallHeight, wallThickness));
                if (c.Walls[3]) CreateWall(ctr + Vector3.left * 0.5f + Vector3.up * (wallHeight / 2), Quaternion.Euler(0, 90, 0), new Vector3(wl, wallHeight, wallThickness));
            }
        // Триггер финиша
        int fx = width - 1, fy1 = height - 1;
        var ftObj = new GameObject("FinishTrigger");
        ftObj.transform.parent = markersParent.transform;
        ftObj.transform.position = new Vector3(fx, 0, fy1);
        var bc = ftObj.AddComponent<BoxCollider>(); bc.isTrigger = true;
        bc.size = new Vector3(1f, wallHeight, 1f);
        bc.center = new Vector3(0f, wallHeight / 2, 0f);
        var ft = ftObj.AddComponent<FinishTrigger>(); ft.mazeGenerator = this;
    }

    void CreateWall(Vector3 pos, Quaternion rot, Vector3 scale)
    {
        var w = Instantiate(cubePrefab, pos, rot, mazeParent.transform);
        w.transform.localScale = scale;
    }

    void CreateCameraTarget()
    {
        var center = new Vector3((width - 1) * 0.5f, 0, (height - 1) * 0.5f);
        cameraTarget = new GameObject("CameraTarget");
        cameraTarget.transform.position = center;
        cameraTarget.transform.parent = mazeParent.transform;
        vcam.LookAt = null; vcam.Follow = null;
    }

    void SetupOrthographicCamera()
    {
        if (vcam == null) return;
        var lens = vcam.m_Lens; lens.Orthographic = true;
        float halfW = width * 0.5f + cameraMargin;
        float halfH = height * 0.5f + cameraMargin;
        var cam = Camera.main ?? vcam.VirtualCameraGameObject.GetComponent<Camera>();
        float aspect = cam != null ? cam.aspect : 1f;
        lens.OrthographicSize = Mathf.Max(halfH, halfW / aspect);
        vcam.m_Lens = lens;
        var c = cameraTarget.transform.position;
        vcam.transform.position = new Vector3(c.x, c.y + 10f, c.z);
        vcam.transform.rotation = Quaternion.Euler(90f, 0, 0);
    }

    void SpawnBallAtStart()
    {
        if (ballPrefab == null) return;
        var pos = new Vector3(0, floorThickness * 0.5f + ballPrefab.transform.localScale.y * 0.5f, 0);
        ballInstance = Instantiate(ballPrefab, pos, Quaternion.identity);
    }

    public void NextLevel()
    {
        width += sizeIncrement;
        height += sizeIncrement;
        GenerateLevel();
    }
}

public class Cell
{
    public int X, Y;
    public bool InMaze;
    public bool[] Walls = new bool[4] { true, true, true, true };
    public Cell(int x, int y) { X = x; Y = y; InMaze = false; }
}

public class FinishTrigger : MonoBehaviour
{
    [HideInInspector] public MazeGenerator3D mazeGenerator;
    void OnTriggerEnter(Collider other) { if (other.CompareTag("Player")) mazeGenerator.NextLevel(); }
}