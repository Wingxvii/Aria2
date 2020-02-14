using UnityEngine;

public class ShellPlacement : MonoBehaviour
{
    public Material red;
    public Material green;
    private MeshRenderer selfRenderer;
    public EntityType type;

    public bool placeable = false;

    public int collisionCount = 0;

    public Transform offset = null;

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
        if (collisionCount == 0 && RTSInput.InputManager.Instance.staticTag == RTSInput.StaticTag.Buildable)
        {
            placeable = true;
            selfRenderer.material = green;
        }
<<<<<<< HEAD
        else {
            selfRenderer.material = red;
            placeable = false;
        }
    }
=======
>>>>>>> c7dfea265753eeacb5f982e2aad57251a33a9051

        //Get new position based on mouse position
        transform.position = new Vector3(RTSInput.InputManager.Instance.staticPosition.x, RTSInput.InputManager.Instance.staticPosition.y + transform.localScale.y, RTSInput.InputManager.Instance.staticPosition.z);

    }

    private void OnTriggerEnter(Collider collision)
    {
        collisionCount++;
    }

    public void OnTriggerExit(Collider collision)
    {
        collisionCount--;
    }
}
