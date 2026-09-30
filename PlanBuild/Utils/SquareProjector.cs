using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlanBuild.Utils
{
    internal class SquareProjector : MonoBehaviour
    {
        public float cubesSpeed = 1f;
        // Half the width (local X) and half the depth (local Z)
        public float radius = 2f;
        public float depthRadius = 2f;
        public int rotation = 0;
        public GameObject prefab;

        private GameObject cube;
        private float cubesThickness = 0.15f;
        private float cubesHeight = 0.1f;
        private float cubesLength = 1f;
        public LayerMask mask = 0;
        
        private float updatesPerSecond = 60f;
        private Quaternion translatedRotation;
        private bool isRunning = false;

        private Transform center;
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

            StartAnimations();
        }

        private void OnDisable()
        {
            isRunning = false;
        }

        // The template sits in the scene root, not under this projector
        private void OnDestroy()
        {
            if (cube)
            {
                Destroy(cube);
            }
        }

        public void StartProjecting()
        {
            if (isRunning)
            {
                return;
            }
            isRunning = true;
            
            center = new GameObject("center").transform;
            center.SetParent(transform);
            center.position = transform.position;
            center.rotation = Quaternion.Euler(0f, rotation, 0f);

            // North and south run along the width, east and west along the depth
            parentNorth = CreateElements(rotation + 0, cubesNorth, radius);
            parentEast = CreateElements(rotation + 90, cubesEast, depthRadius);
            parentSouth = CreateElements(rotation + 180, cubesSouth, radius);
            parentWest = CreateElements(rotation + 270, cubesWest, depthRadius);

            StartAnimations();
        }

        private void StartAnimations()
        {
            StartCoroutine(AnimateElements(parentNorth, cubesNorth, true));
            StartCoroutine(AnimateElements(parentEast, cubesEast, false));
            StartCoroutine(AnimateElements(parentSouth, cubesSouth, true));
            StartCoroutine(AnimateElements(parentWest, cubesWest, false));
        }

        public void StopProjecting()
        {
            if (!isRunning)
            {
                return;
            }
            isRunning = false;

            StopAllCoroutines();

            Destroy(center.gameObject);

            cubesNorth.Clear();
            cubesEast.Clear();
            cubesSouth.Clear();
            cubesWest.Clear();
        }

        private static int GetCubeCount(float halfLength)
        {
            return Mathf.FloorToInt(halfLength) + 1;
        }

        private void RefreshStuff()
        {
            translatedRotation = Quaternion.Euler(0f, rotation, 0f);

            if (!isRunning)
            {
                return;
            }

            if (GetCubeCount(radius) != cubesNorth.Count || GetCubeCount(depthRadius) != cubesEast.Count)
            {
                StopProjecting();
                StartProjecting();
            }

            if (translatedRotation != center.rotation)
            {
                center.rotation = translatedRotation;
            }
        }

        private Transform CreateElements(int localRotation, List<Transform> cubes, float halfLength)
        {
            // Spawn parent object, each which represent a side of the cube
            Transform cubesParent = new GameObject(localRotation.ToString()).transform;
            cubesParent.transform.position = center.position;
            cubesParent.transform.RotateAround(center.position, Vector3.up, localRotation);
            cubesParent.SetParent(center);

            // Spawn cubes
            for (int i = 0; i < GetCubeCount(halfLength); i++)
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

        private IEnumerator AnimateElements(Transform cubeParent, List<Transform> cubes, bool alongWidth)
        {
            Transform a = cubeParent.Find("Start");
            Transform b = cubeParent.Find("End");

            // Animation
            while (true)
            {
                RefreshStuff(); // R

                // A side runs along one axis and sits at the other axis' distance from the center
                float sideLengthHalved = alongWidth ? radius : depthRadius;
                float sideDistance = alongWidth ? depthRadius : radius;
                float sideLength = sideLengthHalved * 2;
                int cubesPerSide = GetCubeCount(sideLengthHalved) - 1;
                float cubesLength100 = sideLength / cubesPerSide;

                a.position = cubeParent.forward * (sideDistance - cubesThickness / 2) - cubeParent.right * sideLengthHalved + cubeParent.position; // R
                b.position = cubeParent.forward * (sideDistance - cubesThickness / 2) + cubeParent.right * sideLengthHalved + cubeParent.position; // R
                Vector3 dir = b.position - a.position;

                for (int i = 0; i < cubes.Count; i++)
                {
                    Transform cube = cubes[i];
                    cube.gameObject.SetActive(true);

                    // Deterministic, baby
                    float delta = (Time.time * cubesSpeed + (sideLength / cubesPerSide) * i) % (sideLength + cubesLength100);
                    Vector3 pos;
                    Vector3 scale;

                    if (delta < cubesLength)                                              // Is growing
                    {
                        pos = dir.normalized * delta + a.position;
                        scale = new Vector3(cube.localScale.x, cube.localScale.y, (cubesLength - (cubesLength - delta)));
                    }
                    else if (delta >= sideLength && delta <= sideLength + cubesLength)      // Is shrinking
                    {
                        pos = dir.normalized * sideLength + a.position;
                        scale = new Vector3(cube.localScale.x, cube.localScale.y, (cubesLength - (delta - sideLength)));
                    }
                    else if (delta >= sideLength && delta >= sideLength + cubesLength)      // Is waiting
                    {
                        cube.gameObject.SetActive(false);
                        continue;
                    }
                    else                                                                // Need to move
                    {
                        pos = dir.normalized * delta + a.position;
                        scale = new Vector3(cube.localScale.x, cube.localScale.y, cubesLength);
                    }
                    
                    RaycastHit hitInfo;
                    if (Physics.Raycast(pos + Vector3.up * 500f, Vector3.down, out hitInfo, 1000f, mask.value))
                    {
                        pos.y = hitInfo.point.y;
                    }

                    cube.position = pos;
                    cube.localScale = scale;
                }
                yield return new WaitForSecondsRealtime(1 / updatesPerSecond);
            }
        }
    }
}