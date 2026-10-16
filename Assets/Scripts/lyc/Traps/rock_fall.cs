using UnityEngine;

public class rock_fall : MonoBehaviour
{

    public float Fall_timer = -1;
    public float Fall_timer_seted = -1;
    public float Y_pos_start;

    public GameObject rope;


    public void Set_timer()
    {
        if (transform.position.y - Y_pos_start >= 2)
        { 
            Fall_timer = (transform.position.y - Y_pos_start) * 5;
            Fall_timer_seted = Time.time;
        }

    }

    public void Interrupted()
    {
        Fall_timer = -1;
        Fall_timer_seted = -1;
    }


    public bool Falling()
    {
        transform.position = new Vector3(transform.position.x, transform.position.y-0.2f, transform.position.z);
        rope.transform.position = new Vector3(rope.transform.position.x, rope.transform.position.y-0.1f, rope.transform.position.z);
        rope.transform.localScale = new Vector3(rope.transform.localScale.x, rope.transform.localScale.y+0.2f, rope.transform.localScale.z);

        if (transform.position.y<=Y_pos_start)
        {
            transform.position = new Vector3(transform.position.x, Y_pos_start, transform.position.z);
            return false;
        }
        return true;
    }



    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Y_pos_start = transform.position.y;
    }

    // Update is called once per frame
    void Update()
    {
        if(Fall_timer_seted!=-1 && Fall_timer_seted+ Fall_timer<Time.time)
        {
            if(!Falling())
            {
                Fall_timer = -1;
                Fall_timer_seted = -1;
            }
        }




    }
}
