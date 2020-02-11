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
        if (collisionCount == 0 && RTSInput.InputManager.Instance.staticTag == RTSInput.StaticTag.Buildable)
        {
            placeable = true;
            selfRenderer.material = green;
        }
        else {
            selfRenderer.material = red;
            placeable = false;
        }
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
