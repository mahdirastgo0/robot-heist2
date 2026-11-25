using UnityEngine;
using System.Collections.Generic;

public partial class GameManager
{
    // Rounded boxes: use authored dimensions instead of scaling a hard-edged Unity cube.
    readonly Dictionary<Vector3,Mesh> roundedMeshes=new Dictionary<Vector3,Mesh>();
    Mesh RoundedBox(Vector3 size)
    {
        if(roundedMeshes.TryGetValue(size,out Mesh mesh))return mesh;
        float radius=Mathf.Min(size.x,Mathf.Min(size.y,size.z))*.21f;
        Vector3 half=size*.5f, inner=half-Vector3.one*radius;
        var v=new List<Vector3>(); var normals=new List<Vector3>(); var uv=new List<Vector2>(); var tri=new List<int>();
        float[] Samples(float h){return new[]{-h,-h+radius*.25f,-h+radius,h-radius,h-radius*.25f,h};}
        void Face(Vector3 n,Vector3 a,Vector3 b,float hn,float ha,float hb)
        {
            int start=v.Count; var u=Samples(ha);var w=Samples(hb);
            for(int y=0;y<6;y++)for(int x=0;x<6;x++)
            {
                Vector3 p=n*hn+a*u[x]+b*w[y];
                Vector3 q=new Vector3(Mathf.Clamp(p.x,-inner.x,inner.x),Mathf.Clamp(p.y,-inner.y,inner.y),Mathf.Clamp(p.z,-inner.z,inner.z));
                Vector3 norm=(p-q).normalized;v.Add(q+norm*radius);normals.Add(norm);uv.Add(new Vector2((u[x]+ha)/(ha*2),(w[y]+hb)/(hb*2)));
            }
            bool positive=Vector3.Dot(Vector3.Cross(a,b),n)>0;
            for(int y=0;y<5;y++)for(int x=0;x<5;x++)
            {int i=start+y*6+x;if(positive){tri.Add(i);tri.Add(i+1);tri.Add(i+7);tri.Add(i);tri.Add(i+7);tri.Add(i+6);}else{tri.Add(i);tri.Add(i+7);tri.Add(i+1);tri.Add(i);tri.Add(i+6);tri.Add(i+7);}}
        }
        Face(Vector3.right,Vector3.up,Vector3.forward,half.x,half.y,half.z);Face(Vector3.left,Vector3.up,Vector3.forward,half.x,half.y,half.z);
        Face(Vector3.up,Vector3.right,Vector3.forward,half.y,half.x,half.z);Face(Vector3.down,Vector3.right,Vector3.forward,half.y,half.x,half.z);
        Face(Vector3.forward,Vector3.right,Vector3.up,half.z,half.x,half.y);Face(Vector3.back,Vector3.right,Vector3.up,half.z,half.x,half.y);
        mesh=new Mesh{name="Rounded art mesh"};mesh.SetVertices(v);mesh.SetNormals(normals);mesh.SetUVs(0,uv);mesh.SetTriangles(tri,0);mesh.RecalculateBounds();mesh.RecalculateTangents();roundedMeshes[size]=mesh;return mesh;
    }
    GameObject ArtBox(string name,Transform parent,Vector3 position,Vector3 size,Material material)
    {
        var g=new GameObject(name);g.transform.SetParent(parent,false);g.transform.localPosition=position;
        g.AddComponent<MeshFilter>().sharedMesh=RoundedBox(size);g.AddComponent<MeshRenderer>().sharedMaterial=material;return g;
    }
    Material CarPaint(Color c)
    {return TexMat("vehicle_paint",c,.78f,.45f);}
}
