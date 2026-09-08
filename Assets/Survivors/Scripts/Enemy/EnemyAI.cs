using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

namespace Tanks.Complete
{
    public class EnemyAI : MonoBehaviour
    {
        public GameObject playerObject;
        private Transform m_CurrentTarget = null;       // Which Transform the tank is following
        private Vector3 m_LastTargetPosition;           // The position of the target last frame
        private EnemyMovement m_Movement;
        private float m_PathfindTime = 0.5f;
        private float m_PathfindTimer = 0.0f;           // The time until the next pathfind call
        private NavMeshPath m_CurrentPath = null;       // The current path followed by the tank.
        private int m_CurrentCorner = 0;                // Which corner of the path the tank is currently going forward to
        private GameObject[] m_AllTanks;

        private void Awake()
        {
            if (!isActiveAndEnabled)
                return;
            m_Movement = GetComponent<EnemyMovement>();

            // to avoid all computer controlled tank pathfinding together (and taxing the CPU), AI tank have a random
            // pathfinding time that will stagger them across multiple frame
            m_PathfindTime = Random.Range(0.3f, 0.6f);

            // We use FindObjectsByType to get all EnemyMovement, to not depend on GameManager so user can try adding AI in an
            // empty scene where no GameManager was added yet.
            m_AllTanks = FindObjectsByType<EnemyMovement>(FindObjectsInactive.Exclude).Select(t => t.gameObject).ToArray();
        }
        // If a GameManager exist, it will call this function after creating a computer controlled tank. This just replace
        // the list of tanks with the one from the GameManager

        public void TurnOff()
        {
            enabled = false;
        }
        
        // Update is called once per frame
        void Update()
        {
            // increment the time since last pathfind. The SeekUpdate will check if it goes over the pathfinding time
            // and if it need to trigger a new pathfinding
            m_PathfindTimer += Time.deltaTime;

            Move();
        }

        void Move()
        {
            if (m_PathfindTimer > m_PathfindTime)
            {
                // reset the time since last pathfind
                m_PathfindTimer = 0;

                // This will store each path toward each tank in the scene
                NavMeshPath[] paths = new NavMeshPath[m_AllTanks.Length];


                //---------------------------------
                // NavMeshPath playerPath = new NavMeshPath();

                // if (playerObject != null || playerObject.activeInHierarchy)
                // {
                //     Transform playerTarget = playerObject.transform;
                
                // }

                //---------------------------------

                // Initialize the shorted path length to the max value a float can have, so no matter what is the length
                // of the first found path, it will for sure be shortest than this initial value
                float shortestPath = float.MaxValue;
                // which of the path in the paths array we use. By default none, which is represented by -1 here.
                int usedPath = -1;
                Transform target = null;

                // Calculate a path to every tank and check the closest
                for (var i = 0; i < m_AllTanks.Length; i++)
                {
                    var tank = m_AllTanks[i].gameObject;

                    //we don't want the tank to try to target itself, so ignore itself
                    if (tank == gameObject)
                        continue;

                    // this is a destroyed or deactivated tank, this is not a valid target
                    if (tank == null || !tank.activeInHierarchy)
                        continue;

                    paths[i] = new NavMeshPath();
                    if(tank.GetComponent<Shooting>()) //ГОВНОКОД ЧТОБЫ СДЕЛАТЬ ЦЕЛЬЮ ТОЛЬКО ИГРОКА
                    {
                        // this return true if a path was found
                        if (NavMesh.CalculatePath(transform.position, tank.transform.position, ~0, paths[i]))
                        {
                            // Compute how long the path is...
                            float length = GetPathLength(paths[i]);
                            // And if it's the shortest path so far, this is the one we want to go after
                            if (shortestPath > length)
                            {
                                // so this path become the used path
                                usedPath = i;
                                //and its length is now the shortest length to beat
                                shortestPath = length;
                                //target = tank.transform;
                                target = playerObject.transform;
                            }
                        }
                    }
                }

                // usedPath will still be -1 if the tank could not find a path to any tank, otherwise we have a target
                if (usedPath != -1)
                {
                    // we switched target. The last tank we were seeking got farther away than another tank, this new
                    // tank become our new target, and we reset the last position as this is now a new target
                    if (target != m_CurrentTarget)
                    {
                        m_CurrentTarget = target;
                        m_LastTargetPosition = m_CurrentTarget.position;
                    }

                    m_CurrentTarget = target;
                    m_CurrentPath = paths[usedPath];
                    m_CurrentCorner = 1;
                    //m_IsMoving = true;
                }


            }
        }

        // Contrary to Update (which is called every new frame, so called a variable amount of time per second depending
        // if the game is rendering fast or not), FixedUpdate is called at a given interval define in the Physic Setting
        // of the project. This is where all physic code should be placed.
        private void FixedUpdate()
        {
            // If the tank doesn't have a path currently, exit early.
            if(m_CurrentPath == null || m_CurrentPath.corners.Length == 0)
                return;
            
            var rb = m_Movement.Rigidbody;
            
            // The point we will orient toward. By default, the current corner in our path
            Vector3 orientTarget = m_CurrentPath.corners[Mathf.Min(m_CurrentCorner, m_CurrentPath.corners.Length - 1)];

            Vector3 toOrientTarget = orientTarget - transform.position;
            toOrientTarget.y = 0;
            toOrientTarget.Normalize();

            float orientDot = Vector3.Dot(transform.forward, toOrientTarget);
            
            // If we are moving, move in our forward direction by our max speed
            float moveAmount = Mathf.Clamp01(orientDot) * m_Movement.m_Speed * Time.deltaTime;
            if (moveAmount > 0.000001f)
            {
                rb.MovePosition(rb.position + transform.forward * moveAmount);
            }

            // Rotate toward the target direction using EnemyMovement
            m_Movement.TurnToward(toOrientTarget);

            // If we reached our current corner, move to the next one
            if (Vector3.Distance(rb.position, orientTarget) < 0.5f)
            {
                m_CurrentCorner += 1;
            }
        }
        
        // Utility function which will add the length of all the sections of the given path to get its effective length
        float GetPathLength(NavMeshPath path)
        {
            float dist = 0;
            for (var i = 1; i < path.corners.Length; ++i)
            {
                dist += Vector3.Distance(path.corners[i-1], path.corners[i]);
            }

            return dist;
        }
    }
}