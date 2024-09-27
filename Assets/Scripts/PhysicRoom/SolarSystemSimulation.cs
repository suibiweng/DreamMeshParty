using UnityEngine;


    public enum CelestialBody
    {
        Sun,
        Earth,
        Moon,
        Mars,
        Mercury,
        Venus,
        Jupiter,
        Saturn,
        Uranus,
        Neptune,
        Pluto,
        Europa,
        Titan,
        Ganymede,
        Callisto,
        Io,
        Triton,
        Ceres
    }


public class SolarSystemSimulation : MonoBehaviour
{

    public CelestialBody selectedBody;

    public Rigidbody objectRigidbody;
    public float massOnEarth = 1.0f;



    public  float  planetGravity = 9.81f;
     public float  planetAtmosphereDrag = 1.0f;


    void Start()
    {
        // SetPlanetaryParameters(selectedBody);
        // AdjustPlanetPhysics();
    }

    public void SetPlanetaryParameters(CelestialBody body)
    {


        


        switch (body)
        {
            case CelestialBody.Sun:
                planetGravity = 274.0f;  // Sun's gravity is approximately 28 times stronger than Earth's
                planetAtmosphereDrag = 0.0f;  // No atmosphere for drag
                break;
            case CelestialBody.Mercury:
                planetGravity = 3.7f;
                planetAtmosphereDrag = 0.0f;
                break;
            case CelestialBody.Venus:
                planetGravity = 8.87f;
                planetAtmosphereDrag = 90.0f;
                break;
            case CelestialBody.Earth:
                planetGravity = 9.81f;
                planetAtmosphereDrag = 1.0f;
                break;
            case CelestialBody.Moon:
                planetGravity = 1.62f;
                planetAtmosphereDrag = 0.0f;
                break;
            case CelestialBody.Mars:
                planetGravity = 3.71f;
                planetAtmosphereDrag = 0.02f;
                break;
            case CelestialBody.Jupiter:
                planetGravity = 24.79f;
                planetAtmosphereDrag = 5.0f;
                break;
            case CelestialBody.Saturn:
                planetGravity = 10.44f;
                planetAtmosphereDrag = 4.0f;
                break;
            case CelestialBody.Uranus:
                planetGravity = 8.69f;
                planetAtmosphereDrag = 3.0f;
                break;
            case CelestialBody.Neptune:
                planetGravity = 11.15f;
                planetAtmosphereDrag = 3.5f;
                break;
            case CelestialBody.Pluto:
                planetGravity = 0.62f;
                planetAtmosphereDrag = 0.01f;
                break;
            case CelestialBody.Europa:
                planetGravity = 1.31f;
                planetAtmosphereDrag = 0.0f;
                break;
            case CelestialBody.Titan:
                planetGravity = 1.35f;
                planetAtmosphereDrag = 10.0f;
                break;
            case CelestialBody.Ganymede:
                planetGravity = 1.43f;
                planetAtmosphereDrag = 0.0f;
                break;
            case CelestialBody.Callisto:
                planetGravity = 1.24f;
                planetAtmosphereDrag = 0.0f;
                break;
            case CelestialBody.Io:
                planetGravity = 1.79f;
                planetAtmosphereDrag = 0.0f;
                break;
            case CelestialBody.Triton:
                planetGravity = 0.78f;
                planetAtmosphereDrag = 0.02f;
                break;
            case CelestialBody.Ceres:
                planetGravity = 0.28f;
                planetAtmosphereDrag = 0.0f;
                break;
            default:
                planetGravity = 9.81f;
                planetAtmosphereDrag = 1.0f;

            break; 
        }
    }

    // void AdjustPlanetPhysics()
    // {
    //     // Adjust mass based on Earth's mass for the object
    //     objectRigidbody.mass = massOnEarth * (planetGravity / 9.81f);

    //     // Set drag based on atmosphere
    //     objectRigidbody.drag = planetAtmosphereDrag;

    //     // Disable Unity's default gravity
    //     objectRigidbody.useGravity = false;
    // }

    // void FixedUpdate()
    // {
    //     // Simulate gravity manually
    //     objectRigidbody.AddForce(Vector3.down * objectRigidbody.mass * planetGravity);
    // }
}
