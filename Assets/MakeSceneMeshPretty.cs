using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MakeSceneMeshPretty : MonoBehaviour
{
    // public List<Rigidbody> balls = new List<Rigidbody>();
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    //I just want to remove the global meshes because I think they are ugly. 
    public void makeSceneMeshPretty()
    {
        GameObject MeshVolumeNoCollider =  GameObject.Find("MeshVolumeNoCollider");
        if (MeshVolumeNoCollider!= null)
        {
            MeshVolumeNoCollider.SetActive(false);
        }
        // GameObject GLOBAL_MESH =  GameObject.Find("GLOBAL_MESH");
        // if (GLOBAL_MESH!= null)
        // {
        //     GLOBAL_MESH.SetActive(false);
        // }
        // GameObject.Find("MeshVolumeNoCollider").SetActive(false);
        StartCoroutine("removeGloabalMesh"); 

        // MeshVolumeNoCollider
        // GLOBAL_MESH
    }

    IEnumerator removeGloabalMesh()
    {
        while (GameObject.Find("GLOBAL_MESH") == null)
        {
            Debug.Log("waiting to find the GLOBAL_MESH");
            yield return new WaitForEndOfFrame();
        }
        GameObject.Find("GLOBAL_MESH").SetActive(false);
        yield return new WaitForSeconds(1);

        // foreach (var ball in balls)
        // {
        //     ball.isKinematic = false;
        // }

    }
}
