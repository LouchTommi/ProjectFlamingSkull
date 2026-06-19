using System;
using System.Threading;
using StarterAssets;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;

public class Patrol : MonoBehaviour
{
    private NavMeshAgent agent;
    public Points[] points;
    private int destPoint = 0;
    float timer;
    GameObject player;
    bool searchingForPlayer;
    [SerializeField] float crouchRange = 5;
    [SerializeField] float walkRange = 20;
    [SerializeField] float runRange = 30;
    [SerializeField] FirstPersonController fpc;
    public GameObject sight1;
    public GameObject sight2;
    public GameObject sight3;
    bool inSight1;
    bool inSight2;
    bool inSight3;
    bool chasePlayer;
    bool prevChasePlayer;
    float playerLostTimer;
    bool startTimer;
    bool lostPlayer;
    bool playerIsTargetPos;
    [SerializeField] AudioSource audioSource;
    public AudioClip[] clips;

    float walkTimer;
    float runTimer;
    public Animator anim;

    public NavMeshAgent nav;
    public float vel;
    public bool run;

    float sfxTimer;
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        // Disabling auto-braking allows for continuous movement
        // between points (ie, the agent doesn't slow down as it
        // approaches a destination point).
        agent.autoBraking = false;

        GotoNextPoint();
        timer = 0;
        player = GameObject.FindGameObjectWithTag("Player");
        fpc = player.GetComponent<FirstPersonController>();
        searchingForPlayer = false;
        prevChasePlayer = chasePlayer;
        lostPlayer = false;
        audioSource = GetComponent<AudioSource>();

        nav = GetComponent<NavMeshAgent>();
    }
    void PlaySFX()
    {
        sfxTimer += Time.deltaTime;

        if(sfxTimer > 10)
        {
            sfxTimer = 0;

            int randomClip = UnityEngine.Random.Range(0, (clips.Length));

            audioSource.PlayOneShot(clips[randomClip]);
        }
    }

    void DetectSound()
    {
        if (Vector3.Distance(transform.position, player.transform.position) <= runRange)
        {
            if ((fpc.running) || (fpc.jumpDetection))
                WalkToSound();
        }
        if (Vector3.Distance(transform.position, player.transform.position) <= walkRange)
        {
            if ((fpc.running) || (fpc.walking && !fpc._input.crouch) || (fpc.jumpDetection))
                WalkToSound();
        }
        if (Vector3.Distance(transform.position, player.transform.position) <= crouchRange)
        {
            if ((fpc.running) || (fpc.walking) || (fpc.jumpDetection))
                WalkToSound();
        }
    }
    void DetectSight()
    {
        Debug.DrawRay(sight1.transform.position, sight1.transform.forward * 30, Color.cyan);
        Debug.DrawRay(sight2.transform.position, sight2.transform.forward * 30, Color.cyan);
        Debug.DrawRay(sight3.transform.position, sight3.transform.forward * 30, Color.cyan);
        RaycastHit hit1;
        if (Physics.Raycast(sight1.transform.position, sight1.transform.forward, out hit1, 30))
        {
            if (hit1.collider == null)
                inSight1 = false;
            else if (hit1.collider.tag == "Player" && SoundManager.instance.gameState != GameState.Hiding)
                inSight1 = true;
            else
                inSight1 = false;
        }
        else
            inSight1 = false;
        RaycastHit hit2;
        if (Physics.Raycast(sight2.transform.position, sight2.transform.forward, out hit2, 30))
        {
            if (hit2.collider == null)
                inSight2 = false;
            else if (hit2.collider.tag == "Player" && SoundManager.instance.gameState != GameState.Hiding)
                inSight2 = true;
            else
                inSight2 = false;
        }
        else
            inSight2 = false;
        RaycastHit hit3;
        if (Physics.Raycast(sight3.transform.position, sight3.transform.forward, out hit3, 30))
        {
            if (hit3.collider == null)
                inSight3 = false;
            else if (hit3.collider.tag == "Player" && SoundManager.instance.gameState != GameState.Hiding)
                inSight3 = true;
            else
                inSight3 = false;
        }
        else
            inSight3 = false;
    }

    void WalkToSound()
    {
        //Debug.Log("Walking to " + player.transform.position);
        searchingForPlayer = true;

        timer = 0;

        // Set the agent to go to the currently selected destination.
        if(agent.enabled)
            agent.destination = player.transform.position;

        // Choose the next point in the array as the destination,
        // cycling to the start if necessary.
        destPoint = (destPoint + 1) % points.Length;
    }

    void GotoNextPoint()
    {
        searchingForPlayer = false;
        timer = 0;
        // Returns if no points have been set up
        if (points.Length == 0)
            return;

        // Set the agent to go to the currently selected destination.
        if (agent.enabled) 
            agent.destination = points[destPoint].point.position;

        // Choose the next point in the array as the destination,
        // cycling to the start if necessary.
        destPoint = (destPoint + 1) % points.Length;
    }

    void ChasePlayer()
    {
        if((inSight1) || (inSight2) || (inSight3))
        {
            SoundManager.instance.PlayerSpotted();
            if (agent.enabled) 
                agent.speed = 4;
            chasePlayer = true;
            playerIsTargetPos = true;
            startTimer = false;
            run = true;
        }
        else
        {
            chasePlayer = false;
            run = false;
        }   
        if(prevChasePlayer != chasePlayer)
        {
            timer = 0;
            if (!chasePlayer)
            {
                startTimer = true;
            }
            prevChasePlayer = chasePlayer;
        }
        if(startTimer)
        {
            playerLostTimer += Time.deltaTime;
        }
        if(playerLostTimer > 5)
        {
            lostPlayer = true;
            playerLostTimer = 0;
        }
        if(lostPlayer)
        {
            SoundManager.instance.PlayerLost();
            if (agent.enabled) 
                agent.speed = 2;
            playerIsTargetPos = false;
            startTimer = false;
            lostPlayer = false;
        }
        if (playerIsTargetPos)
        {
            if (agent.enabled) 
                agent.destination = player.transform.position;
            Running();
        }
    }
    void FixedUpdate()
    {
        PlaySFX();
        if (nav != null)
        {
            vel = nav.velocity.magnitude / nav.speed;
        }
        if(!run)
        {
            anim.SetFloat("Speed", vel);
        }
        else
        {
            if(vel > 0.5f)
            {
                anim.SetFloat("Speed", 2);
            }
            else
            {
                anim.SetFloat("Speed", vel);
            }
        }
        // Choose the next destination point when the agent gets
        // close to the current one.
        //Go to this after timer
        if (!searchingForPlayer)
        {
            if (agent.enabled)
                if (agent.remainingDistance < 0.5f)
                    timer += Time.deltaTime;

            if (timer >= points[destPoint].secondsSpentAtPoint)
                GotoNextPoint();
        }
        else
        {
            if (agent.enabled)
                if ((agent.remainingDistance < 2f) || (SoundManager.instance.gameState == GameState.Hiding))
                    GotoNextPoint();
        }
        if(SoundManager.instance.gameState != GameState.Hiding)
            DetectSound();
        DetectSight();
        ChasePlayer();

        if (agent.enabled) 
            if (agent.remainingDistance > 0.5f)
                Walking();

        if (SoundManager.instance.enemyStunned)
            agent.enabled = false;
        else
            agent.enabled = true;
    }
    public void Walking()
    {
        walkTimer += Time.deltaTime;
        if(walkTimer > .75)
        {
            PlayFootstepWalk();
            walkTimer = 0;
        }
    }
    public void Running()
    {
        runTimer += Time.deltaTime;
        if (runTimer > 0.4)
        {
            PlayFootstepRun();
            runTimer = 0;
        }
    }
    public void PlayFootstepWalk()
    {
        var index = UnityEngine.Random.Range(0, SoundManager.instance.dirtSfxWalk.Length - 1);
        audioSource.PlayOneShot(SoundManager.instance.dirtSfxWalk[index]);
    }

    public void PlayFootstepRun()
    {
        var index = UnityEngine.Random.Range(0, SoundManager.instance.dirtSfxRun.Length - 1);
        audioSource.PlayOneShot(SoundManager.instance.dirtSfxRun[index]);
    }
}

[Serializable]
public class Points
{
    public Transform point;
    public float secondsSpentAtPoint;
}
