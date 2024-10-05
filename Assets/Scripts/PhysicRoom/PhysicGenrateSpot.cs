using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using Newtonsoft.Json;
using RealityEditor;

public class CombinedPhysicsScript : MonoBehaviour
{

    public SolarSystemSimulation solarSystem;



    public GenerateSpot generateSpot;
    public string jsonUrl = "http://localhost:8000/physicsProperties.json";  // URL to fetch the JSON file

    public float planetGravity = 9.81f;  // Default gravity (Earth's gravity)
    public float planetAtmosphereDrag = 0.0f;  // Default atmosphere drag
    public float massOnEarth = 1.0f;  // Mass of the object on Earth

    public Rigidbody objectRigidbody;
    private Collider objectCollider;
    public bool debugobject=false;

    public Coroutine checkCoroutine;


    public bool setPhysic=false;

    // Class to map the JSON data
    [System.Serializable]
    public class PhysicsProperties
    {
        public float mass;
        public float bounciness;
        public float dynamicFriction;
        public float staticFriction;
        public string frictionCombine;
        public string bounceCombine;
    }

    void Start()
    {

       
      
        // objectRigidbody = GetComponent<Rigidbody>();
        objectCollider = GetComponent<Collider>();
      


       if(debugobject){

        massOnEarth=objectRigidbody.mass;
        setPhysic=true;


       }else{


        generateSpot=GetComponent<GenerateSpot>();

        

        jsonUrl=generateSpot.downloadURL+generateSpot.URLID+"_physicsProperties.json";
        checkCoroutine = StartCoroutine(CheckForJsonOnServer(jsonUrl,3f));

       }
  






       
        solarSystem=FindObjectOfType<SolarSystemSimulation>();

        // Start fetching the JSON data
     //   StartCoroutine(FetchJsonData());
    }

    // Fetch the JSON from the local server
    IEnumerator FetchJsonData()
    {
        UnityWebRequest request = UnityWebRequest.Get(jsonUrl);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            // Parse the JSON response
            string json = request.downloadHandler.text;
            PhysicsProperties properties = JsonConvert.DeserializeObject<PhysicsProperties>(json);

            // Apply the physics properties from JSON
            ApplyProperties(properties);

            // Set planet-specific gravity and atmosphere drag after applying general properties
           // SetPlanetProperties(planetGravity, planetAtmosphereDrag);
        }
        else
        {
            Debug.LogError("Error fetching JSON: " + request.error);
        }
    }


    IEnumerator CheckForJsonOnServer(string jsonURL,float checkInterval)
    {
        while (true)
        {
            using (UnityWebRequest request = UnityWebRequest.Get(jsonURL))
            {
                // Send the request and wait for the response
                yield return request.SendWebRequest();

                // Check if the file exists or there are any errors
                if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
                {
                    Debug.LogWarning("JSON not found or error fetching JSON: " + request.error);
                }
                else
                {
                    // If the file exists, read and parse the JSON
                    string json = request.downloadHandler.text;
                    PhysicsProperties properties = JsonConvert.DeserializeObject<PhysicsProperties>(json);

            // Apply the physics properties from JSON
                    ApplyProperties(properties);

                    // Stop checking after successful reading
                    StopCoroutine(checkCoroutine);
                    yield break;
                }
            }

            // Wait for the specified interval before checking again
            yield return new WaitForSeconds(checkInterval);
        }
    }




    // Apply the properties to Rigidbody and Collider
    void ApplyProperties(PhysicsProperties properties)
    {
        if (objectRigidbody != null)
        {
            // Set the mass of the Rigidbody based on the JSON data
            objectRigidbody.mass = properties.mass;
            massOnEarth=objectRigidbody.mass;
            setPhysic=true;
            objectRigidbody.isKinematic=false;
            AdjustPlanetPhysics();
        }

        // if (objectCollider != null)
        // {
        //     // Create a new PhysicMaterial and apply friction and bounciness properties
        //     PhysicMaterial material = new PhysicMaterial
        //     {
        //         bounciness = properties.bounciness,
        //         dynamicFriction = properties.dynamicFriction,
        //         staticFriction = properties.staticFriction
        //     };

        //     // Set the friction and bounce combine modes
        //     material.frictionCombine = GetCombineMode(properties.frictionCombine);
        //     material.bounceCombine = GetCombineMode(properties.bounceCombine);

        //     // Assign the material to the Collider
        //     objectCollider.material = material;
        // }
    }

    // Helper function to convert string to PhysicMaterialCombine enum
    PhysicMaterialCombine GetCombineMode(string combineMode)
    {
        switch (combineMode)
        {
            case "Average":
                return PhysicMaterialCombine.Average;
            case "Minimum":
                return PhysicMaterialCombine.Minimum;
            case "Multiply":
                return PhysicMaterialCombine.Multiply;
            case "Maximum":
                return PhysicMaterialCombine.Maximum;
            default:
                return PhysicMaterialCombine.Average;  // Default to Average if not recognized
        }
    }

    // Function to set the planet gravity and atmosphere drag
    public void SetPlanetProperties(float newPlanetGravity, float newPlanetAtmosphereDrag)
    {
        planetGravity = newPlanetGravity;
        planetAtmosphereDrag = newPlanetAtmosphereDrag;
        AdjustPlanetPhysics();  // Apply the new values to the Rigidbody

    }

    // Apply the planet's gravity and drag to the Rigidbody
    void AdjustPlanetPhysics()
    {
        if (objectRigidbody != null)
        {
            // Adjust the mass of the object based on planet's gravity relative to Earth's
            objectRigidbody.mass = massOnEarth * (planetGravity / 9.81f);

            // Set drag to simulate atmosphere resistance
            objectRigidbody.drag = planetAtmosphereDrag;

            // Disable Unity's default gravity (to use custom gravity)
            objectRigidbody.useGravity = false;


        
        }
        else
        {
            Debug.LogError("No Rigidbody component found on this object.");
        }
    }

    void FixedUpdate()
    {

        if(solarSystem==null) return;
        
        // if(generateSpot.GenerationisComplete){
            
        //     objectRigidbody.isKinematic=false;
            

        // } 
    
        if(debugobject){
            SetPlanetProperties(solarSystem.planetGravity,solarSystem.planetAtmosphereDrag);

        }else{

  SetPlanetProperties(solarSystem.planetGravity,solarSystem.planetAtmosphereDrag);
        if(generateSpot.GenerationisComplete){


                setPhysic=true;
            objectRigidbody.isKinematic=false;
            AdjustPlanetPhysics();
        } 




        }
        // Apply custom gravity in FixedUpdate
        if (objectRigidbody != null && setPhysic)
        {
            // Apply custom gravity force (F = m * g)
            objectRigidbody.AddForce(Vector3.down * objectRigidbody.mass * planetGravity);
        }


    }
}
