using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class PlacementGrid : MonoBehaviour
{
    [Header("Grid configuration.")]
    public int m_width = 1;
    public int m_height = 1;
    public float m_gridSize = 1;
    public Vector3 m_offset = Vector3.zero;

    [Header("Debug")]
    public bool m_drawHitbox = false;
    public Material m_hitboxMaterial = null;    

    private Rigidbody m_rigidbody = null;
    private MeshCollider m_meshCollider = null;
    private Mesh m_hitMesh = null;

    private MeshFilter m_meshFilter = null;
    private MeshRenderer m_renderer = null;

    public Vector3 Snap(Vector3 _in) {
        Vector3 temp = _in;
        Quaternion inverse = Quaternion.Inverse(transform.rotation);

            //Grid snap

            //Get relative position
            temp.x -= transform.position.x + m_offset.x;
            temp.z -= transform.position.z + m_offset.z;
            
            //Invert rotation of input.
            temp = inverse * temp;

            //Snap horizontal
            temp.x -= temp.x % m_gridSize;
            temp.z -= temp.z % m_gridSize;

            temp.y = transform.position.y + m_offset.y;

            //Offset
            temp += new Vector3(m_gridSize * 0.5f, 0.0f, m_gridSize * 0.5f);

            //re-apply rotation.
            temp = transform.rotation * temp;

            //Put back in world space
            temp.x += transform.position.x + m_offset.x;
            temp.z += transform.position.z + m_offset.z;


        return temp;
    }

    private void Start() {
        m_rigidbody = gameObject.AddComponent<Rigidbody>();
        m_rigidbody.isKinematic = true;
        m_rigidbody.useGravity = false;
        m_rigidbody.constraints = RigidbodyConstraints.FreezeAll;

        m_hitMesh = new Mesh();
        m_hitMesh.name = "GridMesh";
        GenerateMesh();

        m_meshCollider = gameObject.AddComponent<MeshCollider>();
        m_meshCollider.sharedMesh = m_hitMesh;

        if (GameSceneController.Instance.type == PlayerType.RTS) {
            m_meshFilter = gameObject.AddComponent<MeshFilter>();
            m_meshFilter.mesh = m_hitMesh;
            m_meshFilter.sharedMesh = m_hitMesh;

            m_renderer = gameObject.AddComponent<MeshRenderer>();

            m_renderer.material = m_hitboxMaterial;
            m_renderer.material.SetTextureScale("_BaseColorMap", new Vector2(m_width, m_height));
        }
    }

    private Vector3 oldPos = Vector3.zero;
    private Quaternion oldRot = Quaternion.Euler(0,0,0);
    private void FixedUpdate() {
        if (oldPos != transform.localPosition || oldRot != transform.localRotation) {
            GenerateMesh();

            m_meshCollider.sharedMesh = m_hitMesh;
            if (m_drawHitbox) {
                m_meshFilter.mesh = m_hitMesh;
                m_meshFilter.sharedMesh = m_hitMesh;
            }
        }
        oldPos = transform.localPosition;
        oldRot = transform.localRotation;
    }

    private void GenerateMesh() {
        Vector3[] verts = {
            new Vector3(0, 0, 0)+m_offset,
            new Vector3(m_width*m_gridSize, 0, 0)+m_offset,
            new Vector3(m_width*m_gridSize, 0, m_height*m_gridSize)+m_offset,
            
            new Vector3(0, 0, 0)+m_offset,
            new Vector3(m_width*m_gridSize, 0, m_height*m_gridSize)+m_offset,
            new Vector3(0, 0, m_height*m_gridSize)+m_offset
        };
        Vector3[] norms = {
            new Vector3(0,1,0),
            new Vector3(0,1,0),
            new Vector3(0,1,0),

            new Vector3(0,1,0),
            new Vector3(0,1,0),
            new Vector3(0,1,0)
        };
        Vector2[] uvs = {
            new Vector2(0,0),
            new Vector2(1,0),
            new Vector2(1,1),

            new Vector2(0,0),
            new Vector2(1,1),
            new Vector2(0,1)
        };
        int[] tris = {
            2,1,0,
            5,4,3
        };


        m_hitMesh.SetVertices(verts);
        m_hitMesh.SetNormals(norms);
        m_hitMesh.SetUVs(0, uvs);
        m_hitMesh.SetTriangles(tris, 0);

        m_hitMesh.UploadMeshData(false);
    }

    private void OnDrawGizmos() {
        if (transform.lossyScale.x == 1 && transform.lossyScale.y == 1 && transform.lossyScale.z == 1)
            Gizmos.color = Color.green;
        else
            Gizmos.color = Color.red;
        
        //Render Grid
        for (int x = 0; x < m_width; ++x) {
            for (int y = 0; y < m_height; ++y) {
                Gizmos.DrawLine(transform.rotation * new Vector3(x * m_gridSize * transform.lossyScale.x, 0, y * m_gridSize * transform.lossyScale.z) + transform.position + m_offset, transform.rotation * new Vector3(x * m_gridSize * transform.lossyScale.x + m_gridSize * transform.lossyScale.x, 0, y * m_gridSize * transform.lossyScale.z) + transform.position + m_offset);
                Gizmos.DrawLine(transform.rotation * new Vector3(x * m_gridSize * transform.lossyScale.x, 0, y * m_gridSize * transform.lossyScale.z) + transform.position + m_offset, transform.rotation * new Vector3(x * m_gridSize * transform.lossyScale.x, 0, y * m_gridSize * transform.lossyScale.z + m_gridSize* transform.lossyScale.z) + transform.position + m_offset);
                Gizmos.DrawLine(transform.rotation * new Vector3(x * m_gridSize * transform.lossyScale.x + m_gridSize * transform.lossyScale.x, 0, y * m_gridSize * transform.lossyScale.z + m_gridSize * transform.lossyScale.z) + transform.position + m_offset, transform.rotation * new Vector3(x * m_gridSize * transform.lossyScale.x + m_gridSize * transform.lossyScale.x, 0, y * m_gridSize * transform.lossyScale.z) + transform.position + m_offset);
                Gizmos.DrawLine(transform.rotation * new Vector3(x * m_gridSize * transform.lossyScale.x + m_gridSize * transform.lossyScale.x, 0, y * m_gridSize * transform.lossyScale.z + m_gridSize * transform.lossyScale.z) + transform.position + m_offset, transform.rotation * new Vector3(x * m_gridSize * transform.lossyScale.x, 0, y * m_gridSize * transform.lossyScale.z + m_gridSize * transform.lossyScale.z) + transform.position + m_offset);
            }
        }
    }
}
