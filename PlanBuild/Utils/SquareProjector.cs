using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlanBuild.Utils
{
    internal class SquareProjector : MonoBehaviour
    {
        public float cubesSpeed = 1f;
        public float radius = 2f;
        public GameObject prefab;

        private GameObject cube;
        private float cubesThickness = 0.15f;
        private float cubesHeight = 0.1f;
        private float cubesLength = 1f;

        private int cubesPerSide;
        private float updatesPerSecond = 60f;
        private float sideLength;
        private float cubesLength100;
        private float sideLengthHalved;
        private bool isRunning = false;

        private Transform parentNorth;
        private Transform parentEast;
        private Transform parentSouth;
        private Transform parentWest;
        private List<Transform> cubesNorth = new List<Transform>();
        private List<Transform> cubesEast = new List<Transform>();
        private List<Transform> cubesSouth = new List<Transform>();
        private List<Transform> cubesWest = new List<Transform>();

        public void Start()
        {
            cube = new GameObject("cube");
            GameObject cubeObject = Instantiate(prefab);
            cubeObject.transform.SetParent(cube.transform);
            cubeObject.transform.localScale = new Vector3(1f, 1f, 1f);
            cubeObject.transform.localPosition = new Vector3(0f, 0f, -0.5f);
            cube.transform.localScale = new Vector3(cubesThickness, cubesHeight, cubesLength);
            cube.SetActive(false);

            RefreshStuff();
            StartProjecting();
        }

        private void OnEnable()
        {
            if (isRunning || parentNorth == null)
            {
                return;
            }
            isRunning = true;

            StartCoroutine(AnimateElements(parentNorth, cubesNorth));
            StartCoroutine(AnimateElements(parentEast, cubesEast));
            StartCoroutine(AnimateElements(parentSouth, cubesSouth));
            StartCoroutine(AnimateElements(parentWest, cubesWest));
        }

        private void OnDisable()
        {
            isRunning = false;
        }

        public void StartProjecting()
        {
            if (isRunning)
            {
                return;
            }
            isRunning = true;

            parentNorth = CreateElements(0, cubesNorth);
            parentEast = CreateElements(90, cubesEast);
            parentSouth = CreateElements(180, cubesSouth);
            parentWest = CreateElements(270, cubesWest);

            StartCoroutine(AnimateElements(parentNorth, cubesNorth));
            StartCoroutine(AnimateElements(parentEast, cubesEast));
            StartCoroutine(AnimateElements(parentSouth, cubesSouth));
            StartCoroutine(AnimateElements(parentWest, cubesWest));
        }

        public void StopProjecting()
        {
            if (!isRunning)
            {
                return;
            }
            isRunning = false;

            StopAllCoroutines();

            Destroy(parentNorth.gameObject);
            Destroy(parentEast.gameObject);
            Destroy(parentSouth.gameObject);
            Destroy(parentWest.gameObject);
            
            cubesNorth.Clear();
            cubesEast.Clear();
            cubesSouth.Clear();
            cubesWest.Clear();
        }

        private void RefreshStuff()
        {
            cubesPerSide = Mathf.FloorToInt(radius);
            sideLength = radius * 2;
            cubesLength100 = sideLength / cubesPerSide;
            sideLengthHalved = sideLength / 2;

            if (isRunning && cubesPerSide + 1 != cubesNorth.Count)
            {
                StopProjecting();
                StartProjecting();
            }
        }

        private Transform CreateElements(int rotation, List<Transform> cubes)
        {
            // Spawn parent object, each which represent a side of the cube
            Transform cubesParent = new GameObject(rotation.ToString()).transform;
            cubesParent.transform.position = transform.position;
            cubesParent.transform.RotateAround(transform.position, Vector3.up, rotation);
            cubesParent.SetParent(transform);

            // Spawn cubes
            for (int i = 0; i < cubesPerSide + 1; i++)
            {
                cubes.Add(Instantiate(cube, transform.position, Quaternion.identity, cubesParent).transform);
            }

            // Spawn helper objects
            Transform a = new GameObject("Start").transform;
            Transform b = new GameObject("End").transform;
            a.SetParent(cubesParent);
            b.SetParent(cubesParent);

            // Initial cube values
            for (int i = 0; i < cubes.Count; i++)
            {
                cubes[i].forward = cubesParent.right;
            }

            return cubesParent;
        }

        private IEnumerator AnimateElements(Transform cubeParent, List<Transform> cubes)
        {
            Transform a = cubeParent.Find("Start");
            Transform b = cubeParent.Find("End");

            // Animation
            while (true)
            {
                RefreshStuff(); // R

                a.position = cubeParent.forward * (sideLengthHalved - cubesThickness / 2) - cubeParent.right * sideLengthHalved + cubeParent.position; // R
                b.position = cubeParent.forward * (sideLengthHalved - cubesThickness / 2) + cubeParent.right * sideLengthHalved + cubeParent.position; // R
                Vector3 dir = b.position - a.position;

                for (int i = 0; i < cubes.Count; i++)
                {
                    Transform cube = cubes[i];
                    cube.gameObject.SetActive(true);

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
}