using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Writing to files
using System.IO;

public class CA : MonoBehaviour
{
    public GameObject m_camera;
    public GameObject m_cellMesh;
    public bool m_GPUInstancing = true;
    public Mesh instanceMesh;
    public Material instanceMaterial;

    // Rules
    protected int[,,] m_rulesMoore = new int[7, 13, 9];
    protected int[] m_rulesVN = new int[7];

    // Growth
    protected int m_growthStep = 0;
    protected int m_activeCells = 0;
    protected float m_cellUpdate = 1.0f;
    protected float m_cellUpdateDelay = 1.0f;

    // Debug
    protected bool debugCube = true;
    protected bool m_start = false;
    protected bool m_stop = false;
    protected bool m_spinCamera = false;
    protected bool m_uiToggle = true;

    // Seed
    protected List<int> m_seed = new List<int>();
    public int m_seedSize = 8;

    // GPU Instancing
    protected int subMeshIndex = 0;
    protected int instanceCount;
    protected int cachedInstanceCount = -1;
    protected int cachedSubMeshIndex = -1;
    protected ComputeBuffer positionBuffer;
    protected ComputeBuffer argsBuffer;
    protected uint[] args = new uint[5] { 0, 0, 0, 0, 0 };

    public void CameraZoom()
    {
        if (Input.GetAxis("Mouse ScrollWheel") < 0)
        {
            Camera.main.fieldOfView += 1.0f;
        }
        if (Input.GetAxis("Mouse ScrollWheel") > 0)
        {
            Camera.main.fieldOfView -= 1.0f;
        }
        Camera.main.fieldOfView = Mathf.Clamp(Camera.main.fieldOfView, 30.0f, 80.0f);
    }

    public void WriteConfigToFile(int[,,] _moore, int[] _vn)
    {
        string file = "Assets/Exports/saved_configs.txt";
        StreamWriter sw;

        if (!File.Exists(file))
        {
            sw = File.CreateText(file);
        }
        else
        {
            sw = File.AppendText(file);
        }

        // Seed
        sw.WriteLine("Seed: ");
        for (int i = 0; i < m_seed.Count; ++i)
        {
            sw.Write(m_seed[i]);
        }
        sw.WriteLine("");

        // Moore
        sw.WriteLine("");
        sw.WriteLine("Moore Rules:");
        for (int face = 0; face <= 6; ++face)
        {
            for (int edge = 0; edge <= 12; ++edge)
            {
                for (int corner = 0; corner <= 8; ++corner)
                {
                    if (_moore[face, edge, corner] == 1)
                    {
                        sw.WriteLine("Faces: " + face + "   Edges: " + edge + "   Corners: " + corner);
                    }
                }
            }
        }

        // VN
        sw.WriteLine("");
        sw.WriteLine("VN Rules:");
        for (int face = 1; face <= 6; ++face)
        {
            if (_vn[face] == 1)
            {
                sw.WriteLine("Faces: " + face);
            }
        }
        sw.WriteLine("");
        sw.Close();
        Debug.Log("Config saved to file!");
    }
}
