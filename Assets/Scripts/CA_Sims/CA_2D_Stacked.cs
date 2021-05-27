using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Writing to files
using System.IO;

public class CA_2D_Stacked : CA
{
    public const int m_gridSize = 100;
    Cell_Cube2D[,,] m_cells = new Cell_Cube2D[m_gridSize, m_gridSize, m_gridSize];

    private bool m_neighbourhood = false;    // False = Von Neumann, True = Moore
    string neighbourhood = "Von Neumann";

    // Looping bounds
    int m_xMin = m_gridSize / 2;
    int m_xMax = m_gridSize / 2;
    int m_zMin = m_gridSize / 2;
    int m_zMax = m_gridSize / 2;
    int m_yLevel = 0;

    // Start is called before the first frame update
    void Start()
    {
        m_seedSize = 90;
        m_camera.transform.position = new Vector3(m_gridSize / 2.0f, m_gridSize + (m_gridSize * 0.3f), -m_gridSize);

        CreateCells();
        CreateRandomSeed(m_seedSize);
        //CreateSymmetricSeed(3, m_seedSize);
        CalculateBounds();
        SaveSeed();
        InitialiseNeighbours(m_neighbourhood);
        CalculateActiveNeighbours(m_neighbourhood);

        instanceCount = m_activeCells;
        m_yLevel++;

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
            if (m_yLevel != m_gridSize - 1)
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
                    m_yLevel++;
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
            GUI.Label(new Rect(10, 10, 100, 20), "Growth Step: " + m_growthStep);
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
            if (GUI.Button(new Rect(10, 340, 100, 20), "Save Seed"))
            {
                //WriteConfigToFile();
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
                    Cell_Cube2D cell = new Cell_Cube2D();
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
        m_zMin = m_gridSize / 2;
        m_zMax = m_gridSize / 2;
        m_yLevel = 0;

        // Create new seed
        CreateRandomSeed(m_seedSize);
        CalculateBounds();
        InitialiseNeighbours(m_neighbourhood);
        CalculateActiveNeighbours(m_neighbourhood);
        instanceCount = m_activeCells;
        m_yLevel++;
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
            for (int x = centre - (_seedSize / 2); x < centre + (_seedSize / 2); ++x)
            {
                if (Random.Range(1, 3) % 2 == 0)
                {
                    m_cells[x, 0, z].SetAlive(true);
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
                    for (int z = gridCentre - (_seedSize / 2); z < gridCentre + (_seedSize / 2); ++z)
                    {
                        for (int x = gridCentre - (_seedSize / 2); x < gridCentre; ++x)
                        {
                            if (Random.Range(1, 3) % 2 == 0)
                            {
                                int symX = ((gridCentre + seedCentre) - x) + gridCentre - seedCentre - 1;
                                m_cells[x, 0, z].SetAlive(true);
                                m_cells[symX, 0, z].SetAlive(true);
                                m_activeCells += 2;
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
                        for (int x = gridCentre - (_seedSize / 2); x < gridCentre; ++x)
                        {
                            if (Random.Range(1, 3) % 2 == 0)
                            {
                                int symX = ((gridCentre + seedCentre) - x) + gridCentre - seedCentre - 1;
                                int symZ = ((gridCentre + seedCentre) - z) + gridCentre - seedCentre - 1;
                                m_cells[x, 0, z].SetAlive(true);
                                m_cells[symX, 0, z].SetAlive(true);
                                m_cells[x, 0, symZ].SetAlive(true);
                                m_cells[symX, 0, symZ].SetAlive(true);
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
            for (int x = m_xMin; x <= m_xMax; ++x)
            {
                if (m_cells[x, m_yLevel, z].GetAlive() && !m_cells[x, m_yLevel, z].GetDrawn())
                {
                    m_cells[x, m_yLevel, z].SetMesh(Instantiate(m_cellMesh, new Vector3(x, m_yLevel, z), Quaternion.identity));
                    m_cells[x, m_yLevel, z].SetDrawn(true);
                }
            }
        }
    }

    void UpdateCells()
    {
        List<Cell_Cube2D> newCells = new List<Cell_Cube2D>();
        for (int z = m_zMin; z <= m_zMax; ++z)
        {
            for (int x = m_xMin; x <= m_xMax; ++x)
            {
                // Apply growth rule to 'dead' cells only
                if (!m_cells[x, m_yLevel, z].GetAlive())
                {
                    int corners = m_cells[x, m_yLevel - 1, z].GetActiveCorners();
                    int edges = m_cells[x, m_yLevel - 1, z].GetActiveEdges();
                    int totalNeighbours = corners + edges;
                    if (totalNeighbours == 4 ||
                        
                        totalNeighbours == 3)
                    {
                        newCells.Add(m_cells[x, m_yLevel, z]);
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

            // Re-calculate number of 'alive' edge and corner neighbours
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
            for (int y = 0; y < m_gridSize; ++y)
            {
                for (int x = 1; x < m_gridSize - 1; ++x)
                {
                    // Edges
                    m_cells[x, y, z].m_edges[0] = m_cells[x - 1, y, z];
                    m_cells[x, y, z].m_edges[1] = m_cells[x + 1, y, z];
                    m_cells[x, y, z].m_edges[2] = m_cells[x, y, z - 1];
                    m_cells[x, y, z].m_edges[3] = m_cells[x, y, z + 1];

                    // Moore Neighbourhood
                    if (_type)
                    {
                        // Corners
                        m_cells[x, y, z].m_corners[0] = m_cells[x - 1, y, z - 1];
                        m_cells[x, y, z].m_corners[1] = m_cells[x - 1, y, z + 1];
                        m_cells[x, y, z].m_corners[2] = m_cells[x + 1, y, z - 1];
                        m_cells[x, y, z].m_corners[3] = m_cells[x + 1, y, z + 1];
                    }
                }
            }
        }
    }

    void CalculateActiveNeighbours(bool _type)
    {
        // _type = 0 (Von Neumann), _type = 1 (Moore)
        // Count number of 'alive' edge and corner neighbours
        for (int z = m_zMin; z <= m_zMax; ++z)
        {
            for (int y = 0; y < m_gridSize; ++y)
            {
                for (int x = m_xMin; x <= m_xMax; ++x)
                {
                    int faceCount = 0;
                    int cornerCount = 0;

                    // Edges
                    for (int face = 0; face < m_cells[x, y, z].m_edges.Length; ++face)
                    {
                        if (m_cells[x, y, z].m_edges[face].GetAlive())
                        {
                            faceCount++;
                        }
                    }
                    m_cells[x, y, z].SetActiveEdges(faceCount);

                    // Moore Neighbourhood
                    if (_type)
                    {
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
            for (int y = 0; y < m_gridSize; ++y)
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
        for (int z = 0; z < m_gridSize; ++z)
        {
            for (int y = 0; y < m_yLevel; ++y)
            {
                for (int x = 0; x < m_gridSize; ++x)
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
        for (int z = m_zMin + 1; z < m_zMax; ++z)
        {
            for (int y = 0; y < 1; ++y)
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
}
