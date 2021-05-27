using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Writing to files
using System.IO;

public class CA_2D_Snowflakes : CA
{
    private float m_meshWidth = 2 * 1.0f;
    private float m_meshHeight = Mathf.Sqrt(3) * 1.0f;

    public const int m_gridSize = 800;
    Cell_Hex2D[,] m_cells = new Cell_Hex2D[m_gridSize, m_gridSize];

    // Looping bounds
    int m_xMin = m_gridSize / 2;
    int m_xMax = m_gridSize / 2;
    int m_zMin = m_gridSize / 2;
    int m_zMax = m_gridSize / 2;

    // Start is called before the first frame update
    void Start()
    {
        m_seedSize = 8;
        m_camera.transform.position = new Vector3(m_gridSize * (m_meshWidth * 0.75f) / 2.0f, m_gridSize * 1.75f, m_gridSize * m_meshHeight / 2.0f);
        m_camera.transform.rotation = Quaternion.Euler(90, 0, 0);

        CreateCells();
        CreateRandomSeed(m_seedSize);
        //CreateSymmetricSeed(3, m_seedSize);
        CalculateBounds();
        SaveSeed();
        InitialiseNeighbours();
        CalculateActiveNeighbours();

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

    // Update is called once per frame
    void Update()
    {
        if (m_start && !m_stop)
        {
            // When cell reaches boundary, stop simulation
            if (m_xMin != 1 && m_zMin != 1 && m_xMax != m_gridSize - 1 && m_zMax != m_gridSize - 1)
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
                new Bounds(new Vector3(m_gridSize * (m_meshWidth * 0.75f) / 2.0f, m_gridSize / 2.0f, m_gridSize * m_meshHeight / 2.0f),
                new Vector3(m_gridSize * (m_meshWidth * 0.75f), 1, m_gridSize * m_meshHeight)), argsBuffer);
        }

        // Spin the object around the target at 20 degrees/second.
        //m_camera.transform.RotateAround(new Vector3(m_gridSize * (m_meshWidth * 0.75f) / 2.0f, m_gridSize / 2, m_gridSize * m_meshHeight / 2.0f), Vector3.up, 20 * Time.deltaTime);

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

    void CreateCells()
    {
        for (int x = 0; x < m_gridSize; ++x)
        {
            for (int z = 0; z < m_gridSize; ++z)
            {
                Cell_Hex2D cell = new Cell_Hex2D();
                if (x % 2 == 0)
                {
                    cell.SetPosition(new Vector3(x * (m_meshWidth * 0.75f), 0, z * m_meshHeight));
                }
                else
                {
                    cell.SetPosition(new Vector3(x * (m_meshWidth * 0.75f), 0, (z * m_meshHeight) + (m_meshHeight / 2.0f)));
                }
                m_cells[x, z] = cell;
            }
        }
    }

    public void DrawDebugCube()
    {
        Color color = new Color(1.0f, 1.0f, 1.0f);
        Debug.DrawLine(new Vector3(0.0f, 0.0f, 0.0f), new Vector3(m_gridSize * (m_meshWidth * 0.75f), 0.0f, 0.0f), color);
        Debug.DrawLine(new Vector3(0.0f, 0.0f, 0.0f), new Vector3(0.0f, 0.0f, m_gridSize * m_meshHeight), color);
        Debug.DrawLine(new Vector3(m_gridSize * (m_meshWidth * 0.75f), 0.0f, m_gridSize * m_meshHeight), new Vector3(0.0f, 0.0f, m_gridSize * m_meshHeight), color);
        Debug.DrawLine(new Vector3(m_gridSize * (m_meshWidth * 0.75f), 0.0f, m_gridSize * m_meshHeight), new Vector3(m_gridSize * (m_meshWidth * 0.75f), 0.0f, 0.0f), color);
    }

    void OnGUI()
    {
        if (m_uiToggle)
        {
            GUI.Label(new Rect(10, 10, 150, 20), "Growth Step: " + m_growthStep);
            GUI.Label(new Rect(10, 40, 150, 20), "Active Cells: " + m_activeCells);
            if (GUI.Button(new Rect(10, 70, 150, 20), "Toggle Debug Cube"))
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
            if (GUI.Button(new Rect(10, 100, 100, 20), "Start"))
            {
                m_start = true;
            }
            if (GUI.Button(new Rect(10, 130, 100, 20), "Stop"))
            {
                m_start = false;
            }
            if (GUI.Button(new Rect(10, 160, 100, 20), "Reset"))
            {
                m_start = false;
                m_stop = false;
                ResetCells(m_seedSize);
            }
            if (GUI.Button(new Rect(10, 190, 30, 20), "1x"))
            {
                m_cellUpdateDelay = 1.0f;
            }
            if (GUI.Button(new Rect(50, 190, 30, 20), "2x"))
            {
                m_cellUpdateDelay = 0.5f;
            }
            if (GUI.Button(new Rect(90, 190, 30, 20), "4x"))
            {
                m_cellUpdateDelay = 0.25f;
            }
            if (GUI.Button(new Rect(10, 220, 100, 20), "Save Seed"))
            {
                //WriteConfigToFile();
            }
        }
    }

    void ResetCells(int _seedSize)
    {
        for (int x = 0; x < m_gridSize; ++x)
        {
            for (int z = 0; z < m_gridSize; ++z)
            {
                m_cells[x, z].Reset();
            }
        }

        // Reset variables
        m_activeCells = 0;
        m_growthStep = 0;
        m_seed.Clear();
        m_xMin = m_gridSize / 2;
        m_xMax = m_gridSize / 2;
        m_zMin = m_gridSize / 2;
        m_zMax = m_gridSize / 2;

        // Create new seed
        CreateRandomSeed(m_seedSize);
        CalculateBounds();
        InitialiseNeighbours();
        CalculateActiveNeighbours();
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

        for (int x = centre - (_seedSize / 2); x < centre + (_seedSize / 2); ++x)
        {
            for (int z = centre - (_seedSize / 2); z < centre + (_seedSize / 2); ++z)
            {
                if (Random.Range(1, 3) % 2 == 0)
                {
                    m_cells[x, z].SetAlive(true);
                    m_activeCells++;
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
                    for (int x = gridCentre - (_seedSize / 2); x < gridCentre + (_seedSize / 2); ++x)
                    {
                        for (int z = gridCentre - (_seedSize / 2); z < gridCentre; ++z)
                        {
                            if (Random.Range(1, 3) % 2 == 0)
                            {
                                int symX = ((gridCentre + seedCentre) - x) + gridCentre - seedCentre - 1;
                                m_cells[x, z].SetAlive(true);
                                m_cells[symX, z].SetAlive(true);
                                m_activeCells += 2;
                            }
                        }
                    }
                    break;
                }
            // Two mirror lines
            case 2:
                {
                    for (int x = gridCentre - (_seedSize / 2); x < gridCentre + (_seedSize / 2); ++x)
                    {
                        for (int z = gridCentre - (_seedSize / 2); z < gridCentre; ++z)
                        {
                            if (Random.Range(1, 3) % 2 == 0)
                            {
                                int symX = ((gridCentre + seedCentre) - x) + gridCentre - seedCentre - 1;
                                int symZ = ((gridCentre + seedCentre) - z) + gridCentre - seedCentre - 1;
                                m_cells[x, z].SetAlive(true);
                                m_cells[symX, z].SetAlive(true);
                                m_cells[x, symZ].SetAlive(true);
                                m_cells[symX, symZ].SetAlive(true);
                                m_activeCells += 4;
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
        for (int x = 0; x < m_gridSize; ++x)
        {
            for (int z = 0; z < m_gridSize; ++z)
            {
                if (m_cells[x, z].GetAlive())
                {
                    int convertedZ;
                    if (x % 2 == 0)
                    {
                        convertedZ = (int)(m_cells[x, z].GetPosition().z / m_meshHeight);
                    }
                    else
                    {
                        convertedZ = (int)Mathf.Ceil(((m_cells[x, z].GetPosition().z / m_meshHeight) - (m_meshHeight / 2.0f)));
                    }
                    // X bounds
                    if ((int)(m_cells[x, z].GetPosition().x / (m_meshWidth * 0.75f)) <= m_xMin && m_xMin != 1)
                    {
                        m_xMin = (int)(m_cells[x, z].GetPosition().x / (m_meshWidth * 0.75f)) - 1;
                    }
                    if ((int)(m_cells[x, z].GetPosition().x / (m_meshWidth * 0.75f)) >= m_xMax && m_xMax != m_gridSize - 2)
                    {
                        m_xMax = (int)(m_cells[x, z].GetPosition().x / (m_meshWidth * 0.75f)) + 1;
                    }
                    // Z bounds
                    if (convertedZ <= m_zMin && m_zMin != 1)
                    {
                        m_zMin = convertedZ - 1;
                    }
                    if (convertedZ >= m_zMax && m_zMax != m_gridSize - 2)
                    {
                        m_zMax = convertedZ + 1;
                    }
                }
            }
        }
    }

    void DrawCells()
    {
        // Draw 'alive' cells
        for (int x = m_xMin; x <= m_xMax; ++x)
        {
            for (int z = m_zMin; z <= m_zMax; ++z)
            {
                if (m_cells[x, z].GetAlive() && !m_cells[x, z].GetDrawn())
                {
                    if (x % 2 == 0)
                    {
                        m_cells[x, z].SetMesh(Instantiate(m_cellMesh, new Vector3(x * (m_meshWidth * 0.75f), 0, z * m_meshHeight), Quaternion.identity));
                    }
                    else
                    {
                        m_cells[x, z].SetMesh(Instantiate(m_cellMesh, new Vector3(x * (m_meshWidth * 0.75f), 0, (z * m_meshHeight) + (m_meshHeight / 2.0f)), Quaternion.identity));
                    }
                    m_cells[x, z].SetDrawn(true);
                }
            }
        }
    }

    void UpdateCells()
    {
        List<Cell_Hex2D> newCells = new List<Cell_Hex2D>();
        for (int x = m_xMin; x <= m_xMax; ++x)
        {
            for (int z = m_zMin; z <= m_zMax; ++z)
            {
                // Apply growth rule to 'dead' cells only
                if (!m_cells[x, z].GetAlive())
                {
                    if (m_cells[x, z].GetActiveNeighbours() == 5 || m_cells[x, z].GetActiveNeighbours() == 1 || m_cells[x, z].GetActiveNeighbours() == 3)
                    {
                        newCells.Add(m_cells[x, z]);
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

            // Re-calculate number of 'alive' neighbours
            CalculateActiveNeighbours();
        }
        else
        {
            m_stop = true;
        }
    }

    void InitialiseNeighbours()
    {
        // Assign all cell neighbours, except boundary cells
        for (int x = 1; x < m_gridSize - 1; ++x)
        {
            for (int z = 1; z < m_gridSize - 1; ++z)
            {
                m_cells[x, z].m_neighbours[0] = m_cells[x - 1, z];
                m_cells[x, z].m_neighbours[1] = m_cells[x + 1, z];
                m_cells[x, z].m_neighbours[2] = m_cells[x, z - 1];
                m_cells[x, z].m_neighbours[3] = m_cells[x, z + 1];

                if (x % 2 == 0)
                {
                    m_cells[x, z].m_neighbours[4] = m_cells[x - 1, z - 1];
                    m_cells[x, z].m_neighbours[5] = m_cells[x + 1, z - 1];
                }
                else
                {
                    m_cells[x, z].m_neighbours[4] = m_cells[x - 1, z + 1];
                    m_cells[x, z].m_neighbours[5] = m_cells[x + 1, z + 1];
                }
            }
        }
    }

    void CalculateActiveNeighbours()
    {
        // Count number of 'alive' neighbours
        for (int x = 1; x < m_gridSize - 1; ++x)
        {
            for (int z = 1; z < m_gridSize - 1; ++z)
            {
                int neighbourCount = 0;

                for (int neighbour = 0; neighbour < m_cells[x, z].m_neighbours.Length; ++neighbour)
                {
                    if (m_cells[x, z].m_neighbours[neighbour].GetAlive())
                    {
                        neighbourCount++;
                    }
                }
                m_cells[x, z].SetActiveNeighbours(neighbourCount);
            }
        }
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
        for (int x = 0; x < m_gridSize; ++x)
        {
            for (int z = 0; z < m_gridSize; ++z)
            {
                if (m_cells[x, z].GetAlive())
                {
                    float xPos = m_cells[x, z].GetPosition().x;
                    float zPos = m_cells[x, z].GetPosition().z;
                    float size = 1.0f;
                    positions[index] = new Vector4(xPos, 0.0f, zPos, size);
                    index++;
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
        for (int x = m_xMin; x <= m_xMax; ++x)
        {
            for (int z = m_zMin; z <= m_zMax; ++z)
            {
                if (m_cells[x, z].GetAlive())
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
