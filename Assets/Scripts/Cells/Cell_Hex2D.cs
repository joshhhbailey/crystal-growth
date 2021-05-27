using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cell_Hex2D : Cell
{
    public Cell_Hex2D[] m_neighbours = new Cell_Hex2D[6];

    int m_activeNeighbours;

    public int GetActiveNeighbours() { return m_activeNeighbours; }
    public void SetActiveNeighbours(int _neighbours) { m_activeNeighbours = _neighbours; }

    override public void Reset()
    {
        DestroyMesh();
        SetAlive(false);
        SetDrawn(false);
    }
}