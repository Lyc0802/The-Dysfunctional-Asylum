using UnityEngine;

public class Rope_Grab : MonoBehaviour
{

    public GameObject rock_rope;
    public GameObject Rock;
    public GameObject This_rope;
    public Vector3 initial_position;


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

        rock_rope.transform.position = new Vector3(rock_rope.transform.position.x, rock_rope.transform.position.y+delta/8, rock_rope.transform.position.z);
        rock_rope.transform.localScale = new Vector3(rock_rope.transform.localScale.x, rock_rope.transform.localScale.y - delta / 4, rock_rope.transform.localScale.z);
        Rock.transform.position = new Vector3(Rock.transform.position.x, Rock.transform.position.y + delta / 4, Rock.transform.position.z);

        lastPosition = position;
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
