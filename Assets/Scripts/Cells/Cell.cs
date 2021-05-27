using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cell
{
    Vector3 m_position;
    bool m_alive = false;
    bool m_drawn = false;
    bool m_checked = false;
    GameObject m_mesh;

    public Vector3 GetPosition() { return m_position; }
    public void SetPosition(Vector3 _position) { m_position = _position; }
    public bool GetAlive() { return m_alive; }
    public void SetAlive(bool _alive) { m_alive = _alive; if (_alive) { SetChecked(true); } else { SetChecked(false); } }
    public bool GetDrawn() { return m_drawn; }
    public void SetDrawn(bool _drawn) { m_drawn = _drawn; }
    public bool GetChecked() { return m_checked; }
    public void SetChecked(bool _checked) { m_checked = _checked; }
    public void SetMesh(GameObject _mesh) { m_mesh = _mesh; }
    public void DestroyMesh() { GameObject.Destroy(m_mesh); }
    public virtual void Reset()
    {
        // Call cell specific function
    }
}