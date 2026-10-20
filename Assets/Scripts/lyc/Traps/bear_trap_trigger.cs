using Unity.Collections;
using UnityEngine;

public class bear_trap_trigger : MonoBehaviour
{
    public GameObject Trap_1;
    public GameObject Trap_2;

    private LayerMask targetLayers;

    private bool F = false;
    private float time_start = 0f;



    public void Trap_activate()
    {
        F = true;
        time_start = Time.time;
    }


    public void OnCollisionEnter(Collision collision)
    {
        
        if ((targetLayers.value &
             (1 << collision.gameObject.layer)) == 0)
        {
            return;
        }
        
        if (F)
        {
            return;
        }
        Trap_activate();
        //add kill script here
    }



    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(F==true && Time.time-time_start<=0.5)
        {
            Trap_1.transform.localRotation = Quaternion.Euler(0f, 90*Time.time-time_start/0.5f, 0f);
            Trap_2.transform.localRotation = Quaternion.Euler(0f, 90 * Time.time - time_start / 0.5f, 0f);

        }
        else if(F==true&&Time.time-time_start>0.5)
        {
            Trap_1.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            Trap_2.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
        }



    }
}
