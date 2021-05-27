using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GreedyMeshing : MonoBehaviour
{
    private const int m_gridSize = 5;
    public Cell_Cube2D[] m_cells1D = new Cell_Cube2D[m_gridSize];
    public Cell_Cube2D[,] m_cells2D = new Cell_Cube2D[m_gridSize, m_gridSize];


    public GameObject m_cellMesh;
    // Start is called before the first frame update
    void Start()
    {
        /*CreateCells1D();
        for (int x = 0; x < m_gridSize; ++x)
        {
            if (Random.Range(1, 3) % 2 == 0)
            {
                m_cells1D[x].SetAlive(true);
                m_cells1D[x].SetMesh(Instantiate(m_cellMesh, new Vector3(x, 0, 0), Quaternion.identity));
            }
        }
        Greedy1D();*/
        CreateCells2D();
        for (int z = 0; z < m_gridSize; ++z)
        {
            for (int x = 0; x < m_gridSize; ++x)
            {
                //if (Random.Range(1, 3) % 2 == 0)
                if (z < 3)
                {
                    m_cells2D[x, z].SetAlive(true);
                    m_cells2D[x, z].SetChecked(false);
                    m_cells2D[x, z].SetMesh(Instantiate(m_cellMesh, new Vector3(x, 0, z), Quaternion.identity));
                }
                else
                {
                    m_cells2D[x, z].SetChecked(true);
                }
            }
        }
        Greedy2D();
    }

    // Update is called once per frame
    void Update()
    {

    }

    void CreateCells1D()
    {
        for (int x = 0; x < m_gridSize; ++x)
        {
            Cell_Cube2D cell = new Cell_Cube2D();
            cell.SetPosition(new Vector3(x, 0, 0));
            m_cells1D[x] = cell;
        }
    }

    void CreateCells2D()
    {
        for (int z = 0; z < m_gridSize; ++z)
        {
            for (int x = 0; x < m_gridSize; ++x)
            {
                Cell_Cube2D cell = new Cell_Cube2D();
                cell.SetPosition(new Vector3(x, 0, z));
                m_cells2D[x, z] = cell;
            }
        }
    }

    void Greedy1D()
    {
        int startPos = 0;
        int restartPos = 0;

        bool reachedEnd = false;
        int objectLength;

        while (!reachedEnd)
        {
            int currentPos;
            objectLength = 0;
            for (currentPos = restartPos; currentPos < m_gridSize; ++currentPos)
            {
                if (!m_cells1D[currentPos].GetAlive())
                {
                    break;
                }
                else
                {
                    objectLength++;
                }
                // Reached final cell, which is alive
                if (currentPos == m_gridSize - 1)
                {
                    reachedEnd = true;
                }
                restartPos++;
            }

            // Spawn new mesh
            if (objectLength > 0)
            {
                GameObject mesh = Instantiate(m_cellMesh, new Vector3(startPos, 1, 0), Quaternion.identity);
                mesh.gameObject.transform.localScale = new Vector3(objectLength, 1, 1);
                mesh.gameObject.transform.localPosition += new Vector3((objectLength - 1) / 2.0f, 0, 0);
            }

            // Reached final cell, which is dead
            if (currentPos == m_gridSize - 1)
            {
                reachedEnd = true;
            }

            restartPos = currentPos + 1;
            startPos = restartPos;
        }
    }

    void Greedy2D()
    {
        bool meshing = true;
        int count = 0;

        while (meshing)
        {
            count++;

            bool initialiseStart = false;
            int startPosX = -1;
            int startPosZ = -1;
            int endPosX = -1;
            int endPosZ = -1;

            int meshX = 0;
            int meshZ = 0;
            bool endXfound = false;
            bool drawMesh = false;
            bool endOfData = false;

            for (int z = 0; z < m_gridSize; ++z)
            {
                for (int x = 0; x < m_gridSize; ++x)
                {
                    // Iterate X from mesh start position
                    if (initialiseStart && x < startPosX)
                    {
                        x = startPosX;
                    }
                    // Cell is alive and has not yet been checked
                    if (m_cells2D[x, z].GetAlive() && !m_cells2D[x, z].GetChecked())
                    {
                        // Mesh already initialised AND haven't found end
                        if (initialiseStart && !endXfound)
                        {
                            meshX++;
                            endPosX = meshX;
                        }
                        // Mesh NOT already initialised
                        if (!initialiseStart)
                        {
                            startPosX = x;
                            startPosZ = z;
                            endPosX = startPosX;
                            endPosZ = startPosZ;
                            meshX = startPosX;
                            meshZ = startPosZ;
                            initialiseStart = true;
                        }
                        if (meshX == m_gridSize - 1)
                        {
                            endXfound = true;
                        }
                        // Reached the length of mesh
                        if (x == meshX && endXfound)
                        {
                            meshZ++;
                            endPosZ = meshZ;
                            break;
                        }
                        // Reached end of data
                        if (x == m_gridSize - 1 && z == m_gridSize - 1)
                        {
                            endOfData = true;
                            break;
                        }
                    }
                    // Cell is dead
                    else if (!m_cells2D[x, z].GetAlive())
                    {
                        // Reached end of mesh
                        if (initialiseStart && !endXfound)
                        {
                            endXfound = true;
                            break;
                        }
                        // Row Z contains a dead cell, stop meshing
                        if (endXfound && x != endPosX)
                        {
                            drawMesh = true;
                            if (x == 0)
                            {
                                endPosZ--;
                            }
                            break;
                        }
                        // Reached end of Z
                        if (endXfound && x == endPosX && !m_cells2D[x, z].GetAlive())
                        {
                            drawMesh = true;
                            if (endPosZ == z)
                            {
                                endPosZ--;
                            }
                            break;
                        }
                        continue;
                    }
                }
                // Draw mesh, mark cells as checked, then restart meshing
                if (drawMesh || endOfData)
                {
                    GameObject mesh = Instantiate(m_cellMesh, new Vector3(startPosX, 1, startPosZ), Quaternion.identity);
                    mesh.gameObject.transform.localScale = new Vector3((endPosX - startPosX) + 1, 1, (endPosZ - startPosZ) + 1);
                    mesh.gameObject.transform.localPosition += new Vector3((endPosX - startPosX) / 2.0f, 0, (endPosZ - startPosZ) / 2.0f);

                    for (int zz = startPosZ; zz <= endPosZ; ++zz)
                    {
                        for (int xx = startPosX; xx <= endPosX; ++xx)
                        {
                            m_cells2D[xx, zz].SetChecked(true);
                        }
                    }
                    break;
                }
            }
            // See if all cells have been checked
            meshing = false;
            for (int z = 0; z < m_gridSize; ++z)
            {
                for (int x = 0; x < m_gridSize; ++x)
                {
                    // Cell not been checked
                    if (!m_cells2D[x, z].GetChecked())
                    {
                        meshing = true;
                        break;
                    }
                }
                if (meshing)
                {
                    break;
                }
            }
            // DEBUG: Break out of infinite loop
            if (count > 1000)
            {
                break;
            }
        }
    }
}
