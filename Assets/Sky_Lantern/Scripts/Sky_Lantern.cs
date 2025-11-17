using UnityEngine;
using System.Collections;
 
public class Sky_Lantern : MonoBehaviour
{

    public GameObject lanternRigidBody;

    public float lanternFloatSpeed = 0.04f;
    public float lanternFloatTopSpeed = 0.3f;



    //initialize values 
    void Start() 
	{

        Physics.gravity = new Vector3(0, lanternFloatSpeed, 0);
        lanternRigidBody.GetComponent<Rigidbody>().maxAngularVelocity = lanternFloatTopSpeed;

    } 
 
	void Update() 
	{

        lanternFloatSpeed = lanternRigidBody.GetComponent<Rigidbody>().velocity.magnitude;

    }


}