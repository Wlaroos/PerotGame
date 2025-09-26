using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateHoverUtil : MonoBehaviour
{
    [SerializeField] private bool usesLocalTime = true;
    private float localTime;

    [SerializeField] private bool canRotate;
    [SerializeField] private Vector3 degreesPerSecond = new Vector3(0f,0f,15f);

    [SerializeField] private bool canHover;
    // Amount the object will go up and down. Range of movement is (position - amplitude) to (position + amplitude)
    [SerializeField] private float amplitude = 0.5f;
    public float Amplitude => amplitude;
    // Time needed for one full cycle
    [SerializeField] private float frequency = 1f;

    private Vector3 posOffset = new Vector3();
    private Vector3 tempPos = new Vector3();

    private float _sine;
    public float Sine => _sine;

    void Awake()
    {
        posOffset = transform.localPosition;
        localTime = Random.Range(0,1000);
        frequency = Random.Range(frequency, frequency+.25f);
    }

    void Update()
    {
        if(usesLocalTime)
        {
            localTime += Time.deltaTime;
        }
        else
        {
            localTime = Time.fixedTime;
        }

        if (canRotate)
        {
            transform.Rotate(Time.deltaTime * degreesPerSecond, Space.Self);
        }

        if (canHover)
        {
            tempPos = posOffset;

            _sine = Mathf.Sin(localTime * Mathf.PI * frequency);
            tempPos.y += (_sine * amplitude);

            transform.localPosition = tempPos;
        }
    }
}
