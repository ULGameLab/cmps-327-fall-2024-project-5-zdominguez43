using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// FSM States for the enemy
public enum EnemyState { STATIC, CHASE, REST, MOVING, DEFAULT };

public enum EnemyBehavior {EnemyBehavior1, EnemyBehavior2, EnemyBehavior3 };

public class Enemy : MonoBehaviour
{
    //pathfinding
    protected PathFinder pathFinder;
    public GenerateMap mapGenerator;
    protected Queue<Tile> path;
    protected GameObject playerGameObject;

    public Tile currentTile;
    protected Tile targetTile;
    public Vector3 velocity;

    //properties
    public float speed = 1.0f;
    public float visionDistance = 5;
    public int maxCounter = 5;
    protected int playerCloseCounter;

    protected EnemyState state = EnemyState.DEFAULT;
    protected Material material;

    public EnemyBehavior behavior = EnemyBehavior.EnemyBehavior1; 

    // Start is called before the first frame update
    void Start()
    {
        path = new Queue<Tile>();
        pathFinder = new PathFinder();
        playerGameObject = GameObject.FindWithTag("Player");
        playerCloseCounter = maxCounter;
        material = GetComponent<MeshRenderer>().material;
    }

    // Update is called once per frame
    void Update()
    {
        if (mapGenerator.state == MapState.DESTROYED) return;

        // Stop Moving the enemy if the player has reached the goal
        if (playerGameObject.GetComponent<Player>().IsGoalReached() || playerGameObject.GetComponent<Player>().IsPlayerDead())
        {
            //Debug.Log("Enemy stopped since the player has reached the goal or the player is dead");
            return;
        }

        switch(behavior)
        {
            case EnemyBehavior.EnemyBehavior1:
                HandleEnemyBehavior1();
                break;
            case EnemyBehavior.EnemyBehavior2:
                HandleEnemyBehavior2();
                break;
            case EnemyBehavior.EnemyBehavior3:
                HandleEnemyBehavior3();
                break;
            default:
                break;
        }

    }

    public void Reset()
    {
        Debug.Log("enemy reset");
        path.Clear();
        state = EnemyState.DEFAULT;
        currentTile = FindWalkableTile();
        transform.position = currentTile.transform.position;
    }

    Tile FindWalkableTile()
    {
        Tile newTarget = null;
        int randomIndex = 0;
        while (newTarget == null || !newTarget.mapTile.Walkable)
        {
            randomIndex = (int)(Random.value * mapGenerator.width * mapGenerator.height - 1);
            newTarget = GameObject.Find("MapGenerator").transform.GetChild(randomIndex).GetComponent<Tile>();
        }
        return newTarget;
    }

    // Dumb Enemy: Keeps Walking in Random direction, Will not chase player
    private void HandleEnemyBehavior1()
    {
        switch (state)
        {
            case EnemyState.DEFAULT: // generate random path 
                
                //Changed the color to white to differentiate from other enemies
                material.color = Color.white;
                
                if (path.Count <= 0) path = pathFinder.RandomPath(currentTile, 20);

                if (path.Count > 0)
                {
                    targetTile = path.Dequeue();
                    state = EnemyState.MOVING;
                }
                break;

            case EnemyState.MOVING:
                //move
                velocity = targetTile.gameObject.transform.position - transform.position;
                transform.position = transform.position + (velocity.normalized * speed) * Time.deltaTime;
                
                //if target reached
                if (Vector3.Distance(transform.position, targetTile.gameObject.transform.position) <= 0.05f)
                {
                    currentTile = targetTile;
                    state = EnemyState.DEFAULT;
                }

                break;
            default:
                state = EnemyState.DEFAULT;
                break;
        }
    }

    // TODO: Enemy chases the player when it is nearby
    private void HandleEnemyBehavior2()
    {
        switch (state)
        {
            case EnemyState.DEFAULT: // Generate random path 
                material.color = Color.black; // Changed color to white to differentiate from other enemies
                
                if (path.Count <= 0) 
                    path = pathFinder.RandomPath(currentTile, 20); // Generate random path

                if (path.Count > 0)
                {
                    targetTile = path.Dequeue(); // Get the first target tile
                    state = EnemyState.MOVING;
                }
                break;

            case EnemyState.MOVING:
                // Move towards the target tile
                velocity = targetTile.gameObject.transform.position - transform.position;
                transform.position = transform.position + (velocity.normalized * speed) * Time.deltaTime;

                // If target reached, reset to DEFAULT state
                if (Vector3.Distance(transform.position, targetTile.gameObject.transform.position) <= 0.05f)
                {
                    currentTile = targetTile;
                    state = EnemyState.DEFAULT;
                }
                break;

            case EnemyState.CHASE:
                GameObject player = GameObject.FindWithTag("Player"); //finds player in the scene
                
                Vector3 playerPosition = player.transform.position; 
                
                // checks to see if players position is within visionDistance   
                if (Vector3.Distance(transform.position, playerPosition) <= visionDistance) 
                {
                    Tile lastKnownTile = GetTileAtPosition(playerPosition); //gets last known tile position

                    //uses pathfinding to find path to last known tile the player was at
                    Queue<Tile> pathToLastKnown = pathFinder.FindPathAStar(currentTile, lastKnownTile); 
                    if (pathToLastKnown.Count > 0)
                    { 
                        targetTile = pathToLastKnown.Dequeue(); // sets first tile in the path as the target tile.
                        state = EnemyState.MOVING; // switches to moving state to follow path
                    }
                    
                }
                break;
        }
    }
    Tile GetTileAtPosition(Vector3 position)
    {
        RaycastHit hit;
            if (Physics.Raycast(position, Vector3.down, out hit)) // uses raycast to see if a tile exists
                {
                    Tile tile = hit.collider.GetComponent<Tile>(); // uses collider to find players last known tile
                    return tile; //returns tile position
                }
        return null; 
    }
       

        // TODO: Third behavior: When enemy sees player it selects tile which is  a few tiles away from player as target tile, then uses
        // pathfinder to find that path to that tile.
        private void HandleEnemyBehavior3()
        {
            switch (state)
            {
            case EnemyState.DEFAULT: // Generate random path 
                material.color = Color.grey; // Changed color to white to differentiate from other enemies
                
                if (path.Count <= 0) 
                    path = pathFinder.RandomPath(currentTile, 20); // Generate random path

                if (path.Count > 0)
                {
                    targetTile = path.Dequeue(); // Get the first target tile
                    state = EnemyState.MOVING;
                }
                break;

            case EnemyState.MOVING:
                // Move towards the target tile
                velocity = targetTile.gameObject.transform.position - transform.position;
                transform.position = transform.position + (velocity.normalized * speed) * Time.deltaTime;

                // If target reached, reset to DEFAULT state
                if (Vector3.Distance(transform.position, targetTile.gameObject.transform.position) <= 0.05f)
                {
                    currentTile = targetTile;
                    state = EnemyState.DEFAULT;
                }
                break;

            case EnemyState.CHASE:
                GameObject player = GameObject.FindWithTag("Player");
            
                Vector3 playerPosition = player.transform.position;

                if (Vector3.Distance(transform.position, playerPosition) <= visionDistance)
                {
                    Tile playerTile = GetTileAtPosition(playerPosition);
                    
                    //sets target tile 2 tiles away from the player tile
                    Tile targetTile = GetTileAwayFromPlayer(playerTile, 2);

                    
                    Queue<Tile> pathToTarget = pathFinder.FindPathAStar(currentTile, targetTile);
                    if (pathToTarget.Count > 0)
                    {
                        this.targetTile = pathToTarget.Dequeue();
                        state = EnemyState.MOVING;
                    }
                         
                }
            
                break;
            }   
        }


    Tile GetTileAwayFromPlayer(Tile playerTile, int distance)
    {
        int targetX = playerTile.indexX + distance;  // moves to the right by set distance
        int targetY = playerTile.indexY + 1;  // moves up 1

        Tile targetTile = GetTileAtPosition(new Vector3(targetX, 0, targetY));
        return targetTile;
    }
}

