using UnityEngine;

public class ShellPlacement : MonoBehaviour
{
    public Material red;
    public Material green;
    private MeshRenderer selfRenderer;
    public EntityType type;

    public bool placeable = false;

    public int collisionCount = 0;


    // Start is called before the first frame update
    void Start()
    {
        selfRenderer = this.GetComponent<MeshRenderer>();
    }

    void OnEnable() {
        collisionCount = 0;
    }

    // Update is called once per frame
    void Update()
    {
        //checks for placeability
        if (collisionCount == 0) {
            placeable = true;
            selfRenderer.material = green;
        }

        //Get new position based on mouse position
        transform.position = new Vector3(RTSInput.InputManager.Instance.staticPosition.x, RTSInput.InputManager.Instance.staticPosition.y + transform.localScale.y, RTSInput.InputManager.Instance.staticPosition.z);

    }

    private void OnTriggerEnter(Collider collision)
    {
        collisionCount++;
        selfRenderer.material = red;
        placeable = false;
    }

    public void OnTriggerExit(Collider collision)
    {
        collisionCount--;
    }
}
