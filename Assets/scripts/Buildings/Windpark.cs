using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class Windpark : MonoBehaviour
{
    // static variables
    static Vector3 windDirection = new Vector3(-0.7f, 0.0f, 0.4f);

    // public variables
    [SerializeField]
    public List<GameObject> windturbines;

    // Unity functions
    private void Start()
    {
        // move the windDirection back by 1000 units to use it as a distant point to look at
        Vector3 windPoint = windDirection * (-1000.0f);
        // rotate all turbines to face into the wind direction
        foreach (GameObject obj in windturbines)
        {
            // move this point to the same height to keep the turbine upright
            windPoint.y = obj.transform.localPosition.y;
            // look at this point
            obj.transform.LookAt(windPoint);
            
            // start the animation
            //Animator animator = obj.GetComponent<Animator>();
            //if (animator != null) animator.Play("Running");
        }

    }

}
