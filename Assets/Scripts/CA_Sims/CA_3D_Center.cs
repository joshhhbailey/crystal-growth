using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Writing to files
using System.IO;

public class CA_3D_Center : CA
{
    public bool m_obstaclesOn = false;
    public List<GameObject> m_obstacles = new List<GameObject>();

    public const int m_gridSize = 100;
    Cell_Cube3D[,,] m_cells = new Cell_Cube3D[m_gridSize, m_gridSize, m_gridSize];

    private bool m_neighbourhood = false;    // False = Von Neumann, True = Moore
    string neighbourhood = "Von Neumann";

    // Looping bounds
    int m_xMin = m_gridSize / 2;
    int m_xMax = m_gridSize / 2;
    int m_yMin = m_gridSize / 2;
    int m_yMax = m_gridSize / 2;
    int m_zMin = m_gridSize / 2;
    int m_zMax = m_gridSize / 2;

    // Start is called before the first frame update
    void Start()
    {
        m_seedSize = 8;
        m_camera.transform.position = new Vector3(m_gridSize / 2.0f, m_gridSize + (m_gridSize * 0.3f), -m_gridSize);

        CreateCells();
        if (m_obstaclesOn)
        {
            CalculateObstacles();
        }
        //CreateRandomSeed(m_seedSize);
        CreateSymmetricSeed(3, m_seedSize);
        CalculateBounds();
        SaveSeed();
        InitialiseNeighbours(m_neighbourhood);
        CalculateActiveNeighbours(m_neighbourhood);
        GenerateRules();

        instanceCount = m_activeCells;

        if (m_GPUInstancing)
        {
            argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
            UpdateBuffers();
        }
        else
        {
            DrawCells();
        }
    }

    void CalculateObstacles()
    {
        for (int i = 0; i < m_obstacles.Count; ++i)
        {
            Vector3 position = m_obstacles[i].transform.position;
            Vector3 scale = m_obstacles[i].transform.localScale;

            int xMin = (int)(position.x - (scale.x / 2.0f)) + 1;
            int yMin = (int)(position.y - (scale.y / 2.0f)) + 1;
            int zMin = (int)(position.z - (scale.z / 2.0f)) + 1;
            int xMax = (int)(position.x + (scale.x / 2.0f)) - 1;
            int yMax = (int)(position.y + (scale.y / 2.0f)) - 1;
            int zMax = (int)(position.z + (scale.z / 2.0f)) - 1;

            for (int z = zMin; z < zMax; ++z)
            {
                for (int y = yMin; y < yMax; ++y)
                {
                    for (int x = xMin; x < xMax; ++x)
                    {
                        m_cells[x, y, z].SetChecked(true);
                    }
                }
            }
        }
    }

    void GenerateRules()
    {
        for (int face = 0; face <= 6; ++face)
        {
            if (Random.Range(1, 3) % 2 == 0 && face != 0)   // 50% chance
            {
                m_rulesVN[face] = 1;
                //print("Faces: " + face);
            }
            else
            {
                m_rulesVN[face] = 0;
            }
            for (int edge = 0; edge <= 12; ++edge)
            {
                for (int corner = 0; corner <= 8; ++corner)
                {
                    if (Random.Range(1, 11) <= 2)   // 20% chance
                    {
                        m_rulesMoore[face, edge, corner] = 1;
                        //print("Faces: " + face + "   Edges: " + edge + "   Corners: " + corner);
                    }
                    else
                    {
                        m_rulesMoore[face, edge, corner] = 0;
                    }
                }
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (m_start && !m_stop)
        {
            // When cell reaches boundary, stop simulation
            if (m_xMin != 1 && m_yMin != 1 && m_zMin != 1 && m_xMax != m_gridSize - 1 && m_yMax != m_gridSize - 1 && m_zMax != m_gridSize - 1)
            {
                m_cellUpdate -= Time.deltaTime;
                if (m_cellUpdate < 0.0f)
                {
                    UpdateCells();
                    instanceCount = m_activeCells;
                    // Update starting position buffer
                    if ((cachedInstanceCount != instanceCount || cachedSubMeshIndex != subMeshIndex) && m_GPUInstancing)
                    {
                        UpdateBuffers();
                    }
                    if (!m_GPUInstancing)
                    {
                        DrawCells();
                    }
                    m_growthStep++;
                    m_cellUpdate = m_cellUpdateDelay;
                }
            }
        }

        if (m_GPUInstancing)
        {
            // Render
            Graphics.DrawMeshInstancedIndirect(instanceMesh, subMeshIndex, instanceMaterial,
                new Bounds(new Vector3(m_gridSize / 2.0f, m_gridSize / 2.0f, m_gridSize / 2.0f),
                new Vector3(m_gridSize, m_gridSize, m_gridSize)), argsBuffer);
        }

        if (m_spinCamera)
        {
            // Spin the object around the target at 20 degrees/second.
            m_camera.transform.RotateAround(new Vector3(m_gridSize / 2, m_gridSize / 2, m_gridSize / 2), Vector3.up, 20 * Time.deltaTime);
        }
        
        if (debugCube)
        {
            DrawDebugCube();
        }
        CameraZoom();

        if (Input.GetKeyDown(KeyCode.Space))
        {
            m_uiToggle = !m_uiToggle;
        }
    }

    public void DrawDebugCube()
    {
        Color color = new Color(1.0f, 1.0f, 1.0f);
        Debug.DrawLine(new Vector3(0.0f, 0.0f, 0.0f), new Vector3(m_gridSize, 0.0f, 0.0f), color);
        Debug.DrawLine(new Vector3(0.0f, 0.0f, 0.0f), new Vector3(0.0f, m_gridSize, 0.0f), color);
        Debug.DrawLine(new Vector3(0.0f, 0.0f, 0.0f), new Vector3(0.0f, 0.0f, m_gridSize), color);
        Debug.DrawLine(new Vector3(m_gridSize, 0.0f, m_gridSize), new Vector3(0.0f, 0.0f, m_gridSize), color);
        Debug.DrawLine(new Vector3(m_gridSize, 0.0f, m_gridSize), new Vector3(m_gridSize, 0.0f, 0.0f), color);
        Debug.DrawLine(new Vector3(m_gridSize, 0.0f, m_gridSize), new Vector3(m_gridSize, m_gridSize, m_gridSize), color);
        Debug.DrawLine(new Vector3(m_gridSize, m_gridSize, 0.0f), new Vector3(0.0f, m_gridSize, 0.0f), color);
        Debug.DrawLine(new Vector3(m_gridSize, m_gridSize, 0.0f), new Vector3(m_gridSize, 0.0f, 0.0f), color);
        Debug.DrawLine(new Vector3(m_gridSize, m_gridSize, 0.0f), new Vector3(m_gridSize, m_gridSize, m_gridSize), color);
        Debug.DrawLine(new Vector3(0.0f, m_gridSize, m_gridSize), new Vector3(0.0f, m_gridSize, 0.0f), color);
        Debug.DrawLine(new Vector3(0.0f, m_gridSize, m_gridSize), new Vector3(m_gridSize, m_gridSize, m_gridSize), color);
        Debug.DrawLine(new Vector3(0.0f, m_gridSize, m_gridSize), new Vector3(0.0f, 0.0f, m_gridSize), color);
    }

    void OnGUI()
    {
        if (m_uiToggle)
        {
            GUI.Label(new Rect(10, 10, 150, 20), "Growth Step: " + m_growthStep);
            GUI.Label(new Rect(10, 40, 150, 20), "Active Cells: " + m_activeCells);
            GUI.Label(new Rect(10, 70, 200, 20), "Neighbourhood: " + neighbourhood);
            if (GUI.Button(new Rect(10, 100, 150, 20), "Toggle Neighbourhood"))
            {
                if (m_neighbourhood)
                {
                    m_neighbourhood = false;
                    neighbourhood = "Von Neumann";
                }
                else
                {
                    m_neighbourhood = true;
                    neighbourhood = "Moore";
                }
                ChangeNeighbours();
            }
            if (GUI.Button(new Rect(10, 130, 150, 20), "Toggle Debug Cube"))
            {
                if (debugCube)
                {
                    debugCube = false;
                }
                else
                {
                    debugCube = true;
                }
            }
            if (GUI.Button(new Rect(10, 160, 100, 20), "Start"))
            {
                m_start = true;
            }
            if (GUI.Button(new Rect(10, 190, 100, 20), "Stop"))
            {
                m_start = false;
            }
            if (GUI.Button(new Rect(10, 220, 100, 20), "Reset"))
            {
                m_start = false;
                m_stop = false;
                ResetCells(m_seedSize);
            }
            if (GUI.Button(new Rect(10, 250, 30, 20), "1x"))
            {
                m_cellUpdateDelay = 1.0f;
            }
            if (GUI.Button(new Rect(45, 250, 30, 20), "2x"))
            {
                m_cellUpdateDelay = 0.5f;
            }
            if (GUI.Button(new Rect(80, 250, 30, 20), "4x"))
            {
                m_cellUpdateDelay = 0.25f;
            }
            if (GUI.Button(new Rect(10, 280, 100, 20), "Spin Camera"))
            {
                m_spinCamera = true;
            }
            if (GUI.Button(new Rect(10, 310, 100, 20), "Stop Camera"))
            {
                m_spinCamera = false;
            }
            if (GUI.Button(new Rect(10, 340, 150, 20), "Regenerate Rules"))
            {
                GenerateRules();
            }
            if (GUI.Button(new Rect(10, 370, 150, 20), "Save Seed / Rules"))
            {
                WriteConfigToFile(m_rulesMoore, m_rulesVN);
            }
            if (GUI.Button(new Rect(10, 400, 150, 20), "Export to .obj"))
            {
                CalculateVertexData("obj");
            }
            if (GUI.Button(new Rect(10, 430, 150, 20), "Export to .stl"))
            {
                CalculateVertexData("stl");
            }
        }
    }

    void CreateCells()
    {
        // Initialise 3D data structure
        for (int z = 0; z < m_gridSize; ++z)
        {
            for (int y = 0; y < m_gridSize; ++y)
            {
                for (int x = 0; x < m_gridSize; ++x)
                {
                    Cell_Cube3D cell = new Cell_Cube3D();
                    cell.SetPosition(new Vector3(x, y, z));
                    m_cells[x, y, z] = cell;
                }
            }
        }
    }

    void ResetCells(int _seedSize)
    {
        for (int z = 0; z < m_gridSize; ++z)
        {
            for (int y = 0; y < m_gridSize; ++y)
            {
                for (int x = 0; x < m_gridSize; ++x)
                {
                    m_cells[x, y, z].Reset();
                }
            }
        }

        // Reset variables
        m_activeCells = 0;
        m_growthStep = 0;
        m_seed.Clear();
        m_xMin = m_gridSize / 2;
        m_xMax = m_gridSize / 2;
        m_yMin = m_gridSize / 2;
        m_yMax = m_gridSize / 2;
        m_zMin = m_gridSize / 2;
        m_zMax = m_gridSize / 2;

        if (m_obstaclesOn)
        {
            CalculateObstacles();
        }
        CreateRandomSeed(m_seedSize);
        CalculateBounds();
        SaveSeed();
        InitialiseNeighbours(m_neighbourhood);
        CalculateActiveNeighbours(m_neighbourhood);
        instanceCount = m_activeCells;
        if (m_GPUInstancing)
        {
            UpdateBuffers();
        }
        else
        {
            DrawCells();
        }
    }

    void CreateRandomSeed(int _seedSize)
    {
        // Seed random starting geometry at centre of data structure
        int centre = m_gridSize / 2;

        for (int z = centre - (_seedSize / 2); z < centre + (_seedSize / 2); ++z)
        {
            for (int y = centre - (_seedSize / 2); y < centre + (_seedSize / 2); ++y)
            {
                for (int x = centre - (_seedSize / 2); x < centre + (_seedSize / 2); ++x)
                {
                    if (Random.Range(1, 3) % 2 == 0)
                    {
                        m_cells[x, y, z].SetAlive(true);
                        m_activeCells++;
                    }
                }
            }
        }
    }

    void CreateSymmetricSeed(int _type, int _seedSize)
    {
        // Seed symmetrical starting geometry at centre of data structure
        int gridCentre = m_gridSize / 2;
        int seedCentre = _seedSize / 2;

        switch (_type)
        {
            // One mirror line
            case 1:
                {
                    for (int z = gridCentre - (_seedSize / 2); z < gridCentre + (_seedSize / 2); ++z)
                    {
                        for (int y = gridCentre - (_seedSize / 2); y < gridCentre + (_seedSize / 2); ++y)
                        {
                            for (int x = gridCentre - (_seedSize / 2); x < gridCentre; ++x)
                            {
                                if (Random.Range(1, 3) % 2 == 0)
                                {
                                    int symX = ((gridCentre + seedCentre) - x) + gridCentre - seedCentre - 1;
                                    m_cells[x, y, z].SetAlive(true);
                                    m_cells[symX, y, z].SetAlive(true);
                                    m_activeCells += 2;
                                }
                            }
                        }
                    }
                    break;
                }
            // Two mirror lines
            case 2:
                {
                    for (int z = gridCentre - (_seedSize / 2); z < gridCentre + (_seedSize / 2); ++z)
                    {
                        for (int y = gridCentre - (_seedSize / 2); y < gridCentre; ++y)
                        {
                            for (int x = gridCentre - (_seedSize / 2); x < gridCentre; ++x)
                            {
                                if (Random.Range(1, 3) % 2 == 0)
                                {
                                    int symX = ((gridCentre + seedCentre) - x) + gridCentre - seedCentre - 1;
                                    int symY = ((gridCentre + seedCentre) - y) + gridCentre - seedCentre - 1;
                                    m_cells[x, y, z].SetAlive(true);
                                    m_cells[symX, y, z].SetAlive(true);
                                    m_cells[x, symY, z].SetAlive(true);
                                    m_cells[symX, symY, z].SetAlive(true);
                                    m_activeCells += 4;
                                }
                            }
                        }
                    }
                    break;
                }
            // Three mirror lines
            case 3:
                {
                    for (int z = gridCentre - (_seedSize / 2); z < gridCentre; ++z)
                    {
                        for (int y = gridCentre - (_seedSize / 2); y < gridCentre; ++y)
                        {
                            for (int x = gridCentre - (_seedSize / 2); x < gridCentre; ++x)
                            {
                                if (Random.Range(1, 3) % 2 == 0)
                                {
                                    int symX = ((gridCentre + seedCentre) - x) + gridCentre - seedCentre - 1;
                                    int symY = ((gridCentre + seedCentre) - y) + gridCentre - seedCentre - 1;
                                    int symZ = ((gridCentre + seedCentre) - z) + gridCentre - seedCentre - 1;
                                    m_cells[x, y, z].SetAlive(true);
                                    m_cells[symX, y, z].SetAlive(true);
                                    m_cells[x, symY, z].SetAlive(true);
                                    m_cells[x, y, symZ].SetAlive(true);
                                    m_cells[symX, symY, z].SetAlive(true);
                                    m_cells[symX, y, symZ].SetAlive(true);
                                    m_cells[x, symY, symZ].SetAlive(true);
                                    m_cells[symX, symY, symZ].SetAlive(true);
                                    m_activeCells += 8;
                                }
                            }
                        }
                    }
                    break;
                }
            default:
                {
                    Debug.LogError("Symmetric Seed Error: Can only generate 1, 2 or 3 mirror lines.");
                    break;
                }
        }
    }

    void CalculateBounds()
    {
        // Limit loop range to farthest blocks +/- 1 to stop...
        // ...unnecessary looping of cells that can never birth
        for (int z = 0; z < m_gridSize; ++z)
        {
            for (int y = 0; y < m_gridSize; ++y)
            {
                for (int x = 0; x < m_gridSize; ++x)
                {
                    if (m_cells[x, y, z].GetAlive())
                    {
                        // X bounds
                        if ((int)m_cells[x, y, z].GetPosition().x <= m_xMin && m_xMin != 1)
                        {
                            m_xMin = (int)m_cells[x, y, z].GetPosition().x - 1;
                        }
                        if ((int)m_cells[x, y, z].GetPosition().x >= m_xMax && m_xMax != m_gridSize - 2)
                        {
                            m_xMax = (int)m_cells[x, y, z].GetPosition().x + 1;
                        }
                        // Y bounds
                        if ((int)m_cells[x, y, z].GetPosition().y <= m_yMin && m_yMin != 1)
                        {
                            m_yMin = (int)m_cells[x, y, z].GetPosition().y - 1;
                        }
                        if ((int)m_cells[x, y, z].GetPosition().y >= m_yMax && m_yMax != m_gridSize - 2)
                        {
                            m_yMax = (int)m_cells[x, y, z].GetPosition().y + 1;
                        }
                        // Z bounds
                        if ((int)m_cells[x, y, z].GetPosition().z <= m_zMin && m_zMin != 1)
                        {
                            m_zMin = (int)m_cells[x, y, z].GetPosition().z - 1;
                        }
                        if ((int)m_cells[x, y, z].GetPosition().z >= m_zMax && m_zMax != m_gridSize - 2)
                        {
                            m_zMax = (int)m_cells[x, y, z].GetPosition().z + 1;
                        }
                    }
                }
            }
        }
    }

    void DrawCells()
    {
        // Draw 'alive' cells
        for (int z = m_zMin; z <= m_zMax; ++z)
        {
            for (int y = m_yMin; y <= m_yMax; ++y)
            {
                for (int x = m_xMin; x <= m_xMax; ++x)
                {
                    if (m_cells[x, y, z].GetAlive() && !m_cells[x, y, z].GetDrawn())
                    {
                        m_cells[x, y, z].SetMesh(Instantiate(m_cellMesh, new Vector3(x, y, z), Quaternion.identity));
                        m_cells[x, y, z].SetDrawn(true);
                    }
                }
            }
        }
    }

    void UpdateCells()
    {
        List<Cell_Cube3D> newCells = new List<Cell_Cube3D>();
        for (int z = m_zMin; z <= m_zMax; ++z)
        {
            for (int y = m_yMin; y <= m_yMax; ++y)
            {
                for (int x = m_xMin; x <= m_xMax; ++x)
                {
                    // Destroy living cells that are hidden (not 3D printable)
                    if (!m_GPUInstancing && m_cells[x, y, z].GetActiveFaces() == 6)
                    {
                        m_cells[x, y, z].DestroyMesh();
                    }

                    // Apply growth rule to 'dead' cells only
                    if (!m_cells[x, y, z].GetAlive() && !m_cells[x, y, z].GetChecked())
                    {
                        if (!m_neighbourhood)
                        {
                            if (m_rulesMoore[m_cells[x, y, z].GetActiveFaces(), m_cells[x, y, z].GetActiveEdges(), m_cells[x, y, z].GetActiveCorners()] == 1)
                            {
                                newCells.Add(m_cells[x, y, z]);
                            }
                        }
                        else
                        {
                            if (m_rulesVN[m_cells[x, y, z].GetActiveFaces()] == 1)
                            {
                                newCells.Add(m_cells[x, y, z]);
                            }
                        }
                    }
                }
            }
        }

        if (newCells.Count > 0)
        {
            // Birth new cells
            for (int i = 0; i < newCells.Count; ++i)
            {
                newCells[i].SetAlive(true);
                m_activeCells++;
            }

            // Re-calculate min/max loop bounds
            CalculateBounds();

            // Re-calculate number of 'alive' face, edge and corner neighbours
            CalculateActiveNeighbours(m_neighbourhood);
        }
        else
        {
            m_stop = true;
        }
    }

    void InitialiseNeighbours(bool _type)
    {
        // _type = 0 (Von Neumann), _type = 1 (Moore)
        // Assign all cell neighbours, except boundary cells
        for (int z = 1; z < m_gridSize - 1; ++z)
        {
            for (int y = 1; y < m_gridSize - 1; ++y)
            {
                for (int x = 1; x < m_gridSize - 1; ++x)
                {
                    // Faces
                    m_cells[x, y, z].m_faces[0] = m_cells[x - 1, y, z];
                    m_cells[x, y, z].m_faces[1] = m_cells[x + 1, y, z];
                    m_cells[x, y, z].m_faces[2] = m_cells[x, y - 1, z];
                    m_cells[x, y, z].m_faces[3] = m_cells[x, y + 1, z];
                    m_cells[x, y, z].m_faces[4] = m_cells[x, y, z - 1];
                    m_cells[x, y, z].m_faces[5] = m_cells[x, y, z + 1];

                    // Moore Neighbourhood
                    if (_type)
                    {
                        // Edges
                        m_cells[x, y, z].m_edges[0] = m_cells[x - 1, y + 1, z];
                        m_cells[x, y, z].m_edges[1] = m_cells[x + 1, y + 1, z];
                        m_cells[x, y, z].m_edges[2] = m_cells[x, y + 1, z - 1];
                        m_cells[x, y, z].m_edges[3] = m_cells[x, y + 1, z + 1];

                        m_cells[x, y, z].m_edges[4] = m_cells[x - 1, y, z - 1];
                        m_cells[x, y, z].m_edges[5] = m_cells[x - 1, y, z + 1];
                        m_cells[x, y, z].m_edges[6] = m_cells[x + 1, y, z - 1];
                        m_cells[x, y, z].m_edges[7] = m_cells[x + 1, y, z + 1];

                        m_cells[x, y, z].m_edges[8] = m_cells[x - 1, y - 1, z];
                        m_cells[x, y, z].m_edges[9] = m_cells[x + 1, y - 1, z];
                        m_cells[x, y, z].m_edges[10] = m_cells[x, y - 1, z - 1];
                        m_cells[x, y, z].m_edges[11] = m_cells[x, y - 1, z + 1];

                        // Corners
                        m_cells[x, y, z].m_corners[0] = m_cells[x - 1, y + 1, z - 1];
                        m_cells[x, y, z].m_corners[1] = m_cells[x - 1, y + 1, z + 1];
                        m_cells[x, y, z].m_corners[2] = m_cells[x + 1, y + 1, z - 1];
                        m_cells[x, y, z].m_corners[3] = m_cells[x + 1, y + 1, z + 1];

                        m_cells[x, y, z].m_corners[4] = m_cells[x - 1, y - 1, z - 1];
                        m_cells[x, y, z].m_corners[5] = m_cells[x - 1, y - 1, z + 1];
                        m_cells[x, y, z].m_corners[6] = m_cells[x + 1, y - 1, z - 1];
                        m_cells[x, y, z].m_corners[7] = m_cells[x + 1, y - 1, z + 1];
                    }
                }
            }
        }
    }

    void CalculateActiveNeighbours(bool _type)
    {
        // _type = 0 (Von Neumann), _type = 1 (Moore)
        // Count number of 'alive' face, edge and corner neighbours
        for (int z = m_zMin; z <= m_zMax; ++z)
        {
            for (int y = m_yMin; y <= m_yMax; ++y)
            {
                for (int x = m_xMin; x <= m_xMax; ++x)
                {
                    int faceCount = 0;
                    int edgeCount = 0;
                    int cornerCount = 0;

                    // Faces
                    for (int face = 0; face < m_cells[x, y, z].m_faces.Length; ++face)
                    {
                        if (m_cells[x, y, z].m_faces[face].GetAlive())
                        {
                            faceCount++;
                        }
                    }
                    m_cells[x, y, z].SetActiveFaces(faceCount);

                    // Moore Neighbourhood
                    if (_type)
                    {
                        // Edges
                        for (int edge = 0; edge < m_cells[x, y, z].m_edges.Length; ++edge)
                        {
                            if (m_cells[x, y, z].m_edges[edge].GetAlive())
                            {
                                edgeCount++;
                            }
                        }
                        m_cells[x, y, z].SetActiveEdges(edgeCount);

                        // Corners
                        for (int corner = 0; corner < m_cells[x, y, z].m_corners.Length; ++corner)
                        {
                            if (m_cells[x, y, z].m_corners[corner].GetAlive())
                            {
                                cornerCount++;
                            }
                        }
                        m_cells[x, y, z].SetActiveCorners(cornerCount);
                    }
                }
            }
        }
    }

    void ChangeNeighbours()
    {
        for (int z = 1; z < m_gridSize - 1; ++z)
        {
            for (int y = 1; y < m_gridSize - 1; ++y)
            {
                for (int x = 1; x < m_gridSize - 1; ++x)
                {
                    m_cells[x, y, z].ResetNeighbours();
                }
            }
        }
        InitialiseNeighbours(m_neighbourhood);
        CalculateActiveNeighbours(m_neighbourhood);
    }

    void UpdateBuffers()
    {
        // Ensure submesh index is in range
        if (instanceMesh != null)
        {
            subMeshIndex = Mathf.Clamp(subMeshIndex, 0, instanceMesh.subMeshCount - 1);
        }

        // Positions
        if (positionBuffer != null)
        {
            positionBuffer.Release();
        }

        positionBuffer = new ComputeBuffer(instanceCount, 16);
        Vector4[] positions = new Vector4[instanceCount];

        int index = 0;

        // Draw 'alive' cells
        for (int z = m_zMin; z <= m_zMax; ++z)
        {
            for (int y = m_yMin; y <= m_yMax; ++y)
            {
                for (int x = m_xMin; x <= m_xMax; ++x)
                {
                    if (m_cells[x, y, z].GetAlive())
                    {
                        float xPos = m_cells[x, y, z].GetPosition().x;
                        float yPos = m_cells[x, y, z].GetPosition().y;
                        float zPos = m_cells[x, y, z].GetPosition().z;
                        float size = 1.0f;
                        positions[index] = new Vector4(xPos, yPos, zPos, size);
                        index++;
                    }

                }
            }
        }

        positionBuffer.SetData(positions);
        instanceMaterial.SetBuffer("positionBuffer", positionBuffer);

        // Indirect args
        if (instanceMesh != null)
        {
            args[0] = (uint)instanceMesh.GetIndexCount(subMeshIndex);
            args[1] = (uint)instanceCount;
            args[2] = (uint)instanceMesh.GetIndexStart(subMeshIndex);
            args[3] = (uint)instanceMesh.GetBaseVertex(subMeshIndex);
        }
        else
        {
            args[0] = args[1] = args[2] = args[3] = 0;
        }
        argsBuffer.SetData(args);

        cachedInstanceCount = instanceCount;
        cachedSubMeshIndex = subMeshIndex;
    }

    void OnDisable()
    {
        if (positionBuffer != null)
            positionBuffer.Release();
        positionBuffer = null;

        if (argsBuffer != null)
            argsBuffer.Release();
        argsBuffer = null;
    }

    void SaveSeed()
    {
        m_seed.Clear();
        for (int z = m_zMin + 1; z < m_zMax; ++z)
        {
            for (int y = m_yMin + 1; y < m_yMax; ++y)
            {
                for (int x = m_xMin + 1; x < m_xMax; ++x)
                {
                    if (m_cells[x, y, z].GetAlive())
                    {
                        m_seed.Add(1);
                    }
                    else
                    {
                        m_seed.Add(0);
                    }
                }
            }
        }
    }

    void CalculateVertexData(string _extension)
    {
        // Can't use "[GameObject].GetComponent<MeshFilter>().mesh.GetVertices()"
        // because Unity Cube was evidently made by a child...
        // https://forum.unity.com/threads/cube-mesh-order-of-vertices-and-triangles.873901/
        Vector3[,] cubeVertices = { {new Vector3(-0.5f, -0.5f, -0.5f), new Vector3(0.5f, -0.5f, -0.5f), new Vector3(0.5f, 0.5f, -0.5f)},  // Front
                                    {new Vector3(-0.5f, -0.5f, -0.5f), new Vector3(0.5f, 0.5f, -0.5f), new Vector3(-0.5f, 0.5f, -0.5f)},
                                    {new Vector3(0.5f, -0.5f, -0.5f), new Vector3(0.5f, -0.5f, 0.5f), new Vector3(0.5f, 0.5f, 0.5f)},     // Right
                                    {new Vector3(0.5f, -0.5f, -0.5f), new Vector3(0.5f, 0.5f, 0.5f), new Vector3(0.5f, 0.5f, -0.5f)},
                                    {new Vector3(-0.5f, -0.5f, 0.5f), new Vector3(-0.5f, 0.5f, 0.5f), new Vector3(0.5f, 0.5f, 0.5f)},     // Back
                                    {new Vector3(0.5f, 0.5f, 0.5f), new Vector3(0.5f, -0.5f, 0.5f), new Vector3(-0.5f, -0.5f, 0.5f)},
                                    {new Vector3(-0.5f, 0.5f, 0.5f), new Vector3(-0.5f, -0.5f, 0.5f), new Vector3(-0.5f, -0.5f, -0.5f)},  // Left
                                    {new Vector3(-0.5f, -0.5f, -0.5f), new Vector3(-0.5f, 0.5f, -0.5f), new Vector3(-0.5f, 0.5f, 0.5f)},
                                    {new Vector3(0.5f, -0.5f, 0.5f), new Vector3(0.5f, -0.5f, -0.5f), new Vector3(-0.5f, -0.5f, -0.5f)},  // Bottom
                                    {new Vector3(-0.5f, -0.5f, -0.5f), new Vector3(-0.5f, -0.5f, 0.5f), new Vector3(0.5f, -0.5f, 0.5f)},
                                    {new Vector3(-0.5f, 0.5f, -0.5f), new Vector3(0.5f, 0.5f, -0.5f), new Vector3(0.5f, 0.5f, 0.5f)},     // Top
                                    {new Vector3(0.5f, 0.5f, 0.5f), new Vector3(-0.5f, 0.5f, 0.5f), new Vector3(-0.5f, 0.5f, -0.5f)}};
        List<Vector3> vertexData = new List<Vector3>();

        float offset = m_gridSize / 2.0f;

        // Loop all possible alive cells
        for (int z = m_zMin; z <= m_zMax; ++z)
        {
            for (int y = m_yMin; y <= m_yMax; ++y)
            {
                for (int x = m_xMin; x <= m_xMax; ++x)
                {
                    if (m_cells[x, y, z].GetAlive())
                    {
                        Vector3 cellPos = m_cells[x, y, z].GetPosition();
                        List<Vector3> cellVertices = new List<Vector3>();
                        for (int i = 0; i < cubeVertices.Length / 3; ++i)
                        {
                            vertexData.Add(new Vector3(cellPos.x + cubeVertices[i, 0].x - offset, cellPos.y + cubeVertices[i, 0].y - offset, cellPos.z + cubeVertices[i, 0].z - offset));
                            vertexData.Add(new Vector3(cellPos.x + cubeVertices[i, 1].x - offset, cellPos.y + cubeVertices[i, 1].y - offset, cellPos.z + cubeVertices[i, 1].z - offset));
                            vertexData.Add(new Vector3(cellPos.x + cubeVertices[i, 2].x - offset, cellPos.y + cubeVertices[i, 2].y - offset, cellPos.z + cubeVertices[i, 2].z - offset));
                        }
                    }
                }
            }
        }
        if (_extension == "obj")
        {
            ExportToObj("testCrystal", vertexData);
        }
        else if (_extension == "stl")
        {
            ExportToStl("testCrystal", vertexData);
        }
        else
        {
            print("Invalid file extension.");
        }
    }

    void ExportToObj(string _filename, List<Vector3> _vertexData)
    {
        string file = "Assets/Exports/Meshes/" + _filename + ".obj";
        StreamWriter sw;

        sw = File.CreateText(file);
        for (int i = 0; i < _vertexData.Count - 2; i += 3)
        {
            sw.Write("v " + _vertexData[i].x + " " + _vertexData[i].y + " " + _vertexData[i].z + " \n");
            sw.Write("v " + _vertexData[i + 1].x + " " + _vertexData[i + 1].y + " " + _vertexData[i + 1].z + " \n");
            sw.Write("v " + _vertexData[i + 2].x + " " + _vertexData[i + 2].y + " " + _vertexData[i + 2].z + " \n");
            sw.Write("f -1 -2 -3\n\n");
        }
        sw.Close();
        Debug.Log("Mesh saved to .obj");
    }

    void ExportToStl(string _filename, List<Vector3> _vertexData)
    {
        string file = "Assets/Exports/Meshes/" + _filename + ".stl";
        StreamWriter sw;

        sw = File.CreateText(file);
        sw.Write("solid\n");

        for (int i = 0; i < _vertexData.Count - 2; i += 3)
        {
            Vector3 normal = CalculateNormal(_vertexData[i], _vertexData[i + 1], _vertexData[i + 2]);
            sw.Write("facet normal " + normal.x + " " + normal.y + " " + normal.z + "\n");
            sw.Write("outer loop\n");
            sw.Write("vertex " + _vertexData[i].x + " " + _vertexData[i].y + " " + _vertexData[i].z + "\n");
            sw.Write("vertex " + _vertexData[i + 1].x + " " + _vertexData[i + 1].y + " " + _vertexData[i + 1].z + "\n");
            sw.Write("vertex " + _vertexData[i + 2].x + " " + _vertexData[i + 2].y + " " + _vertexData[i + 2].z + "\n");
            sw.Write("endloop\n");
            sw.Write("endfacet\n");
        }
        sw.Write("endsolid");
        sw.Close();
        Debug.Log("Mesh saved to .stl");
    }

    Vector3 CalculateNormal(Vector3 _a, Vector3 _b, Vector3 _c)
    {
        Vector3 side1 = _b - _a;
        Vector3 side2 = _c - _a;
        return Vector3.Cross(side1, side2).normalized;
    }
}
