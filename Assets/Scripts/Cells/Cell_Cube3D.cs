using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cell_Cube3D : Cell
{
    public Cell_Cube3D[] m_faces = new Cell_Cube3D[6];
    public Cell_Cube3D[] m_edges = new Cell_Cube3D[12];
    public Cell_Cube3D[] m_corners = new Cell_Cube3D[8];

    int m_activeFaces = 0;
    int m_activeEdges = 0;
    int m_activeCorners = 0;

    public int GetActiveFaces() { return m_activeFaces; }
    public void SetActiveFaces(int _faces) { m_activeFaces = _faces; }
    public int GetActiveEdges() { return m_activeEdges; }
    public void SetActiveEdges(int _edges) { m_activeEdges = _edges; }
    public int GetActiveCorners() { return m_activeCorners; }
    public void SetActiveCorners(int _corners) { m_activeCorners = _corners; }

    override public void Reset()
    {
        DestroyMesh();
        ResetNeighbours();
        SetAlive(false);
        SetDrawn(false);
    }
    public void ResetNeighbours()
    {
        m_faces = new Cell_Cube3D[6];
        m_edges = new Cell_Cube3D[12];
        m_corners = new Cell_Cube3D[8];
        m_activeFaces = 0;
        m_activeEdges = 0;
        m_activeCorners = 0;
    }
};
