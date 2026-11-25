using UnityEngine;
// Cosmetic pedestrian on the beach promenade; no traffic or combat AI.
public class CoastalWalker : MonoBehaviour
{
    Vector3 start;float length,travel;const float Speed=1.65f;
    public void Init(Vector3 p,float metres){start=p;length=Mathf.Max(1,metres);}
    void Update(){travel+=Time.deltaTime*Speed;float phase=travel%(length*2);bool forward=phase<length;transform.position=start+Vector3.right*(forward?phase:length*2-phase);transform.rotation=Quaternion.RotateTowards(transform.rotation,Quaternion.Euler(0,forward?90:270,0),Time.deltaTime*360);}
}
