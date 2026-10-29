using UnityEngine;

public class Rope_Grab : MonoBehaviour
{

    public GameObject rock_rope;
    public GameObject Rock;
    public GameObject This_rope;
    public Vector3 initial_position;




    private Vector3 rock_last_position ;
    private Vector3 rock_rope_last_position ;
    private Vector3 rock_rope_last_scale ;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        initial_position = This_rope.transform.position;
    }

    
    private Vector3 lastPosition;

    // 抓取時呼叫
    public void Begin_Pull()
    {
        lastPosition = This_rope.transform.position;



        Rock.transform.position = rock_last_position;
        rock_rope.transform.position = rock_rope_last_position;
        rock_rope.transform.localScale = rock_rope_last_scale; 
    }

    // 抓取期間一直呼叫
    public void Update_Pull()
    {
        Vector3 position=This_rope.transform.position;
        Vector3 pullDirection = Vector3.down;

        float delta = Vector3.Dot(
            position - lastPosition,
            pullDirection
        );

        rock_rope.transform.position = new Vector3(rock_rope.transform.position.x, rock_rope.transform.position.y+delta/4, rock_rope.transform.position.z);
        rock_rope.transform.localScale = new Vector3(rock_rope.transform.localScale.x, rock_rope.transform.localScale.y - delta / 2, rock_rope.transform.localScale.z);
        Rock.transform.position = new Vector3(Rock.transform.position.x, Rock.transform.position.y + delta / 2, Rock.transform.position.z);

        lastPosition = position;

        rock_last_position = Rock.transform.position ;
        rock_rope_last_position = rock_rope.transform.position ;
        rock_rope_last_scale = rock_rope.transform.localScale ; 


    }

    //結束時呼叫
    public void End_Pull()
    {
        This_rope.transform.position = initial_position;
        
    }


    // Update is called once per frame
    void Update()
    {

    }

}
