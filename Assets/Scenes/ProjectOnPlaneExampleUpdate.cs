// This example rotates a cube above a tilted plane. As the cube rotates, the cube's position vector is projected onto the plane and rendered as a line. 

using UnityEngine;

public class ProjectOnPlaneExampleUpdate : MonoBehaviour
{
    GameObject groundPlane;
    GameObject rotObject;
    LineRenderer line;


    void Start()
    {
        // Create the plane
        groundPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        groundPlane.transform.Rotate(-30, 10, 0);

        // Create the item to rotate    
        rotObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rotObject.transform.position = new Vector3(5, 5, 0);
        line = rotObject.AddComponent<LineRenderer>();
    }

    void Update()
    {
        // Set the rotation origin
        Vector3 origin = Vector3.zero;

        // Rotate the object above the plane
        rotObject.transform.RotateAround(origin, Vector3.up, 20 * Time.deltaTime);

        // Project the location of the cube onto the plane
        Vector3 projected = Vector3.ProjectOnPlane(rotObject.transform.position, groundPlane.transform.up);

        // Draw the projected vector as a line
        line.SetPosition(0, origin);
        line.SetPosition(1, projected);
        line.startWidth = 0.1f;

    }
}
