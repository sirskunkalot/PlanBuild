using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TSquareProjector : MonoBehaviour
{

    [Header("Zone")]
    [SerializeField] private float sideLength;
    [Range(0, 1)] [SerializeField] private float coverage = 0.5f;

    [Header("Marker")]
    [SerializeField] private GameObject cube;
    [SerializeField] private int cubesPerSide = 5;
    [SerializeField] private float cubesThickness = 0.2f;
    [SerializeField] private float cubesSpeed = 1f;
    [SerializeField] private float updatesPerSecond = 60f;

    private float cubesLength = 1f;
    private float cubesLength100 = 1f;
    private float sideLengthHalved = 1f;

    private List<Transform> cubesNorth = new List<Transform>();
    private List<Transform> cubesEast = new List<Transform>();
    private List<Transform> cubesSouth = new List<Transform>();
    private List<Transform> cubesWest = new List<Transform>();

    private void Start()
    {
        StartProjecting();
    }

    private void Update()
    {
        // if (Input.GetKeyDown(KeyCode.KeypadPlus))
        //     StartProjecting();
        // if (Input.GetKeyDown(KeyCode.KeypadMinus))
        //     StopProjecting();
    }

    public void StartProjecting()
    {
        RefreshStuff();
        StartCoroutine(SideAnimation(0, cubesNorth));
        StartCoroutine(SideAnimation(90, cubesEast));
        StartCoroutine(SideAnimation(180, cubesSouth));
        StartCoroutine(SideAnimation(270, cubesWest));
    }

    public void StopProjecting()
    {
        StopAllCoroutines();
        for (int i = 0; i < transform.childCount; i++)
            Destroy(transform.GetChild(i).gameObject, 0);
        cubesNorth.Clear();
        cubesEast.Clear();
        cubesSouth.Clear();
        cubesWest.Clear();
    }

    void RefreshStuff()
    {
        cubesLength = (sideLength * coverage) / cubesPerSide;
        cubesLength100 = sideLength / cubesPerSide;
        sideLengthHalved = sideLength / 2;
    }

    private IEnumerator SideAnimation(int rotation, List<Transform> cubes)
    {
        // Spawn parent object, each which represent a side of the cube
        Transform parent = new GameObject().transform;
        parent.name = rotation.ToString();
        parent.transform.position = transform.position;
        parent.transform.RotateAround(transform.position, transform.up, rotation);
        parent.SetParent(transform);

        // Spawn cubes
        for (int i = 0; i < cubesPerSide + 1; i++)
            cubes.Add(Instantiate(cube, transform.position, Quaternion.identity, parent).transform);

        // Spawn helper objects
        Transform a = new GameObject().transform;
        Transform b = new GameObject().transform;
        a.SetParent(parent); a.name = "Start";
        b.SetParent(parent); b.name = "End";
        a.position = parent.forward * (sideLengthHalved - cubesThickness / 2) - parent.right * sideLengthHalved + parent.position;
        b.position = parent.forward * (sideLengthHalved - cubesThickness / 2) + parent.right * sideLengthHalved + parent.position;
        Vector3 dir = b.position - a.position;

        // Initial cube values
        for (int i = 0; i < cubes.Count; i++)
        {
            cubes[i].forward = parent.right;
            cubes[i].localScale = new Vector3(cubesThickness, cubesThickness, cubesLength);
        }

        // Animation
        while (true)
        {
            RefreshStuff(); // R

            a.position = parent.forward * (sideLengthHalved - cubesThickness / 2) - parent.right * sideLengthHalved + parent.position; // R
            b.position = parent.forward * (sideLengthHalved - cubesThickness / 2) + parent.right * sideLengthHalved + parent.position; // R
            dir = b.position - a.position;

            for (int i = 0; i < cubes.Count; i++)
            {
                Transform cube = cubes[i];
                cube.gameObject.SetActive(true);
                cube.localScale = new Vector3(cubesThickness, cubesThickness, cubesLength); // R

                // Deterministic, baby
                float pos = (Time.time * cubesSpeed + (sideLength / cubesPerSide) * i) % (sideLength + cubesLength100);

                if (pos < cubesLength)                                              // Is growing
                {
                    cube.position = dir.normalized * pos + a.position;
                    cube.localScale = new Vector3(cube.localScale.x, cube.localScale.y, (cubesLength - (cubesLength - pos)));
                }
                else if (pos >= sideLength && pos <= sideLength + cubesLength)      // Is shrinking
                {
                    cube.position = dir.normalized * sideLength + a.position;
                    cube.localScale = new Vector3(cube.localScale.x, cube.localScale.y, (cubesLength - (pos - sideLength)));
                }
                else if (pos >= sideLength && pos >= sideLength + cubesLength)      // Is waiting
                {
                    cube.gameObject.SetActive(false);
                }
                else                                                                // Need to move
                {
                    cube.position = dir.normalized * pos + a.position;
                    cube.localScale = new Vector3(cube.localScale.x, cube.localScale.y, cubesLength);
                }
            }
            yield return new WaitForSecondsRealtime(1 / updatesPerSecond);
        }
    }

}
