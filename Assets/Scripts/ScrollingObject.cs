using UnityEngine;

public class ScrollingObject : MonoBehaviour
{
    [SerializeField] private float xOut = -0.5f;
    [SerializeField] private float xIn = 0.5f;
    [SerializeField] private float scrollSpeed = 0.5f;
    [SerializeField] private bool randomizeZ;
    [SerializeField] private Vector2 minMaxZ;

    private void Update()
    {
        var position = transform.position;
        
        position -= Vector3.right * scrollSpeed * Time.deltaTime;

        if (position.x < xOut)
        {
            position.x = xIn;

            if (randomizeZ)
                position.z = Random.Range(minMaxZ.x, minMaxZ.y);
        }
            

        transform.position = position;
    }
}
