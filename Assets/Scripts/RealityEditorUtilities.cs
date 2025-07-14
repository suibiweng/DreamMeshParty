using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Net;


namespace RealityEditor
{
    public enum GenerateType
    {
        Add,
        Reconstruction,

        Instruction,


        None

    }



    public enum MeshType
    {
        Generated,
        TargetObject,
        BackGround,
        Modified

    }



    public class ModelIformation
    {
        public GameObject gameobjectWarp;
        public string ModelURL;

        public MeshType meshType;


        public MeshType modelType()
        {

            if (ModelURL.Contains("scaned"))
            {
                if (ModelURL.Contains("target"))
                {

                    meshType = MeshType.TargetObject;
                }

                if (ModelURL.Contains("background"))
                {


                    meshType = MeshType.BackGround;

                }

                if (ModelURL.Contains("Instruction"))
                {

                    meshType = MeshType.Modified;

                }

            }
            else if (ModelURL.Contains("generated"))
            {


                meshType = MeshType.Generated;


            }


            return meshType;
        }





    }

    public class WebSocketmessage
    {
        public string ID;
        public string prompt;

        public string getMsg()
        {

            return "" + ID + "," + prompt;

        }

        public void setMsg(string msg)
        {
            string[] data = msg.Split(",");

            ID = data[0];
            prompt = data[1];


        }




    }





    public static class TimestampGenerator
    {
        public static string GetTimestamp()
        {
            // Get the current date and time
            DateTime now = DateTime.Now;

            // Format the date and time as a timestamp string
            string timestamp = now.ToString("yyyyMMddHHmmss");

            return timestamp;
        }
    }


    public static class URLChecker
    {
        public static bool CheckURLConnection(string url)
        {
            try
            {
                // Create a web request to the specified URL
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);

                // Set the request method to HEAD to only get headers without downloading the content
                request.Method = "HEAD";

                // Get the response
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                // Check if the response status is OK (200)
                bool connectionExists = response.StatusCode == HttpStatusCode.OK;

                // Close the response
                response.Close();

                return connectionExists;
            }
            catch (Exception e)
            {
                // An exception occurred, indicating that the URL connection does not exist
                //  Debug.LogError("Error checking URL connection: " + e.Message);
                return false;
            }
        }
    }



    

public class ColliderUtils : MonoBehaviour
{
    /// <summary>
    /// Adds a MeshCollider to the first MeshFilter found under the given GameObject.
    /// </summary>
    /// <param name="parentObject">The GameObject to search (e.g., "TargetObject").</param>
    /// <param name="convex">Whether the collider should be convex (true) or not (false).</param>
    /// <returns>The MeshCollider that was added (or already existed), or null if none found.</returns>
    public static MeshCollider AddMeshCollider(GameObject parentObject, bool convex = true)
    {
        if (parentObject == null)
        {
            Debug.LogWarning("AddMeshCollider: parentObject is null.");
            return null;
        }

        // Look for a MeshFilter on the parent or its children
        MeshFilter meshFilter = parentObject.GetComponentInChildren<MeshFilter>(includeInactive: true);
        if (meshFilter == null || meshFilter.sharedMesh == null)
        {
            Debug.LogWarning($"AddMeshCollider: No MeshFilter with mesh found under \"{parentObject.name}\".");
            return null;
        }

        // Get or add MeshCollider to the object with the mesh
        GameObject target = meshFilter.gameObject;
        MeshCollider meshCollider = target.GetComponent<MeshCollider>();
        if (meshCollider == null)
            meshCollider = target.AddComponent<MeshCollider>();

        meshCollider.sharedMesh = meshFilter.sharedMesh;
        meshCollider.convex = convex;

        return meshCollider;
    }
}









}
