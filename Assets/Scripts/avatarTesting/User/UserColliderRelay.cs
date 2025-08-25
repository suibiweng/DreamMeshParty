using UnityEngine;

[RequireComponent(typeof(Collider))]
public class UserColliderRelay : MonoBehaviour
{
    public UserEffectRouter Router;

    private void OnTriggerEnter(Collider other)
    {
        Router?.OnUserTriggerEnter(gameObject, other.gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        Router?.OnUserCollisionEnter(gameObject, collision.gameObject);
    }
}
