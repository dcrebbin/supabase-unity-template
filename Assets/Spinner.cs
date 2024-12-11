using UnityEngine;
public class Spinner : MonoBehaviour
{
    public float rotationSpeed = 500f;
    void Update()
    {
        transform.Rotate(0f, 0f, -rotationSpeed * Time.deltaTime);
    }
}