using UnityEngine;

public class HeliRotors : MonoBehaviour
{
    [SerializeField] private Transform topRotor;
    [SerializeField] private Transform backRotor;

    [SerializeField] private float rotorSpeed = 1000f;

    void Update()
    {
        topRotor.Rotate(Vector3.up, rotorSpeed * Time.deltaTime);
        backRotor.Rotate(Vector3.right, rotorSpeed * Time.deltaTime);
    }
}
