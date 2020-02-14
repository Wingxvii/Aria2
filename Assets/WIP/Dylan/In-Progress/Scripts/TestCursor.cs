using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestCursor : MonoBehaviour
{
    public Material m_goodMat, m_badMat;

    private MeshRenderer m_renderer;

    private void Awake() {
        m_renderer = GetComponent<MeshRenderer>();
        m_renderer.material = m_badMat;
    }

    private void Update()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        RaycastHit hit;
        if (Physics.Raycast(ray, out hit)) {
            PlacementGrid pg;
            if (hit.collider.gameObject.TryGetComponent(out pg)) {
                print("Good");
                transform.position = pg.Snap(hit.point);
                transform.rotation = pg.gameObject.transform.rotation;
                m_renderer.material = m_goodMat;
            } else {
                transform.position = hit.point;
                transform.rotation = Quaternion.Euler(0, 0, 0);
                m_renderer.material = m_badMat;

            }
        }
    }
}
