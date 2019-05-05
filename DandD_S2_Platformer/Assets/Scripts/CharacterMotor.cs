using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterMotor : MonoBehaviour {

    private CharacterController cc;

    private bool setup;
    public bool alive;
    private bool grounded;
    public bool canJump;
    public bool hitUp;
    public bool justHitUp;
    private bool canInteract;
    private bool isActive;

    [SerializeField] private float gravity = 50f;
    private float currentGravity;
    [SerializeField] private AnimationCurve curve_JumpGravity;
    [SerializeField] private float gravityCurveDuration = 0.5f;

    [SerializeField] private int groundRayDensity = 5;
    private Ray[] groundRays;
    private RaycastHit[] groundHits;
    private Vector3[] groundRayOrigins;
    [SerializeField] private float groundRayPadding = 0.05f;
    [SerializeField] private float groundRayLength = 1.2f;
    private float groundPointY;

    [SerializeField] private int upRayDensity = 5;
    private Ray[] upRays;
    private RaycastHit[] upHits;
    private Vector3[] upRayOrigins;
    [SerializeField] private float upRayPadding = 0.05f;
    [SerializeField] private float upRayLength = 1.2f;
    private float upPointY;
    private CharacterController enemyAbove;

    [SerializeField] private int sideRayDensity = 5;
    private Ray[,] sideRays;
    private RaycastHit[,] sideHits;
    private Vector3[] sideRayOrigins;
    private bool[] sideObstaclesFound;
    [SerializeField] private float sideRayPadding = 0.1f;
    [SerializeField] private float sideRayLength = 0.7f;
    private float[] sidePointsX;
    private CharacterController enemySide;
    [SerializeField] private float interactDelay = 0.1f;

    private Rigidbody rb;
    private Collider cCollider;

    [SerializeField] private Vector2 playerSize = new Vector2(1f, 2f);
    [SerializeField] private float speed = 500f;
    [SerializeField] private float airSpeed = 750f;
    [SerializeField] private float stopSpeed = 5f;
    [SerializeField] private float jumpStrength = 15f;
    [SerializeField] private float jumpDeathStrength = 45f;
    [SerializeField] private float bounceStrength = 30f;
    [SerializeField] private int maxAirJumps = 1;
    private int airJumpCount;
    private bool justJumped;
    [SerializeField] private float airJumpStrength = 8f;
    [SerializeField] private float jumpExtensionDuration = 0.5f;
    [SerializeField] private float jumpExtensionStrength = 1f;

    [SerializeField] private float jumpDelay = 0.25f;
    private Coroutine COR_JumpDelay;
    private Coroutine COR_JumpGravity;
    private Coroutine COR_Interact;
    private Coroutine COR_JumpExtension;

    private Animator animator;
    private Coroutine COR_DeathRoutine;
    [SerializeField] private float deathSquishTime = 2f;
    [SerializeField] private float deathFlyForwardSpeed = 5f;

    public void Setup(CharacterController c) {
        StopAllCoroutines();
        cc = c;
        rb = GetComponent<Rigidbody>();
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = false;
        cCollider = GetComponent<Collider>();
        cCollider.enabled = true;
        animator = transform.Find("Model").GetComponent<Animator>();
        currentGravity = gravity;
        SetupRays();
        StopInteract();
        setup = true;
        alive = true;
        grounded = false;
        justJumped = false;
        justHitUp = false;
        isActive = true;
        animator.SetTrigger("Reset");
        if (cc.facingRotation) {
            animator.transform.localRotation = Quaternion.Euler(cc.startFaceRotation);
            animator.SetFloat("MovementSpeed", 0f);
            animator.SetFloat("MovementDirection", (float)Random.Range(0, 2));
        }
    }

    void SetupRays() {
        // Ground
        groundRays = new Ray[groundRayDensity];
        groundHits = new RaycastHit[groundRayDensity];
        groundRayOrigins = new Vector3[groundRayDensity];
        for (int i = 0; i < groundRayDensity; i++) {
            groundRayOrigins[i] =
                Vector3.right * ((float)i / (float)(groundRayDensity - 1)) * (playerSize.x - groundRayPadding * 2f) +
                Vector3.right * groundRayPadding +
                Vector3.left * playerSize.x / 2f +
                Vector3.up * playerSize.y / 2f;
        }
        // Up
        upRays = new Ray[upRayDensity];
        upHits = new RaycastHit[upRayDensity];
        upRayOrigins = new Vector3[upRayDensity];
        for (int i = 0; i < upRayDensity; i++) {
            upRayOrigins[i] =
                Vector3.right * ((float)i / (float)(upRayDensity - 1)) * (playerSize.x - upRayPadding * 2f) +
                Vector3.right * upRayPadding +
                Vector3.left * playerSize.x / 2f +
                Vector3.up * playerSize.y / 2f;
        }
        // Side
        sideRays = new Ray[2, sideRayDensity];
        sideHits = new RaycastHit[2, sideRayDensity];
        sideRayOrigins = new Vector3[sideRayDensity];
        sideObstaclesFound = new bool[2];
        for (int i = 0; i < sideRayDensity; i++) {
            sideRayOrigins[i] = Vector3.up * ((float)i / (float)(sideRayDensity - 1)) * (playerSize.y - sideRayPadding * 2f) + Vector3.up * sideRayPadding;
        }
        sidePointsX = new float[2];
    }

    void FixedUpdate() {
        if (setup) {
            if (isActive) {
                if (alive) {
                    if (!justJumped) {
                        CheckGrounded();
                    }
                    CheckUp();
                    CheckSides();
                    if (hitUp) {
                        if (justHitUp) {
                            justHitUp = false;
                            rb.velocity = new Vector3(rb.velocity.x, -rb.velocity.y, rb.velocity.z);
                            StopJumpRoutine();
                            airJumpCount = maxAirJumps;
                            transform.position = new Vector3(transform.position.x, upPointY - playerSize.y, transform.position.z);
                        }
                    }
                    if (grounded) {
                        transform.position = new Vector3(transform.position.x, groundPointY, transform.position.z);
                        if (Mathf.Abs(cc.movementInput.x) >= GameManager.script.controllerDeadZone) {
                            rb.velocity = new Vector3(cc.movementInput.x * speed * Time.fixedDeltaTime, 0f, 0f);
                        } else {
                            if (Mathf.Abs(rb.velocity.x) > 0f) {
                                if (Mathf.Sign(rb.velocity.x) > 0f) {
                                    if (!sideObstaclesFound[1]) {
                                        rb.AddForce(Vector3.right * stopSpeed * -rb.velocity.x, ForceMode.Acceleration);
                                    }
                                } else {
                                    if (!sideObstaclesFound[0]) {
                                        rb.AddForce(Vector3.right * stopSpeed * -rb.velocity.x, ForceMode.Acceleration);
                                    }
                                }
                            }
                        }
                        rb.velocity = new Vector3(rb.velocity.x, 0f, 0f);
                    } else {
                        if (Mathf.Abs(cc.movementInput.x) >= GameManager.script.controllerDeadZone) {
                            if (!sideObstaclesFound[1]) {
                                if (cc.movementInput.x > 0f) {
                                    rb.AddForce(new Vector3(cc.movementInput.x * airSpeed * Time.fixedDeltaTime, 0f, 0f), ForceMode.Acceleration);
                                }
                            }
                            if (!sideObstaclesFound[0]) {
                                if (cc.movementInput.x < 0f) {
                                    rb.AddForce(new Vector3(cc.movementInput.x * airSpeed * Time.fixedDeltaTime, 0f, 0f), ForceMode.Acceleration);
                                }
                            }
                        }
                        rb.AddForce(Vector3.down * currentGravity, ForceMode.Acceleration);
                    }
                    if (Mathf.Abs(rb.velocity.x) > 0f) {
                        if (rb.velocity.x > 0f) {
                            if (sideObstaclesFound[1]) {
                                rb.velocity = new Vector3(Mathf.Clamp(0f, -speed * Time.fixedDeltaTime, speed * Time.fixedDeltaTime), rb.velocity.y, 0f);
                                transform.position = new Vector3(sidePointsX[1] - playerSize.x / 2f, transform.position.y, transform.position.z);
                                TurnAround();
                            } else {
                                rb.velocity = new Vector3(Mathf.Clamp(rb.velocity.x, -speed * Time.fixedDeltaTime, speed * Time.fixedDeltaTime), rb.velocity.y, 0f);
                            }
                        } else {
                            if (sideObstaclesFound[0]) {
                                rb.velocity = new Vector3(Mathf.Clamp(0f, -speed * Time.fixedDeltaTime, speed * Time.fixedDeltaTime), rb.velocity.y, 0f);
                                transform.position = new Vector3(sidePointsX[0] + playerSize.x / 2f, transform.position.y, transform.position.z);
                                TurnAround();
                            } else {
                                rb.velocity = new Vector3(Mathf.Clamp(rb.velocity.x, -speed * Time.fixedDeltaTime, speed * Time.fixedDeltaTime), rb.velocity.y, 0f);
                            }
                        }
                    }
                    if (cc.facingRotation) {
                        if (Mathf.Abs(cc.movementInput.x) >= GameManager.script.controllerDeadZone) {
                            cc.currentFacingRotation = Mathf.RoundToInt(Mathf.Clamp01(Mathf.Sign(cc.movementInput.x)));
                            animator.transform.localRotation = Quaternion.Euler(cc.faceRotations[cc.currentFacingRotation]);
                        }
                    }
                    switch (cc.enemyType) {
                        case ENEMYTYPE._none:
                            animator.SetFloat("MovementSpeed", Mathf.Abs(rb.velocity.x / (speed * Time.fixedDeltaTime)));
                            animator.SetFloat("MovementDirection", (float)cc.currentFacingRotation);
                            break;
                    }
                } else {
                    switch (cc.enemyType) {
                        case ENEMYTYPE._none:
                            rb.AddForce(new Vector3(0f, -1f, 0f) * currentGravity, ForceMode.Acceleration);
                            transform.position += Vector3.forward * Time.fixedDeltaTime * deathFlyForwardSpeed;
                            break;
                    }
                }
            }
        }
    }

    void CheckGrounded() {
        int groundRayCount = 0;
        groundPointY = 0f;
        for (int i = 0; i < groundRayDensity; i++) {
            groundRays[i] = new Ray(transform.position + groundRayOrigins[i], Vector3.down);
            if (Physics.Raycast(groundRays[i], out groundHits[i], groundRayLength, GameManager.script.floorMask)) {
                Debug.DrawRay(groundRays[i].origin, groundRays[i].direction * groundRayLength, Color.green);
                groundRayCount++;
                groundPointY = groundHits[i].point.y;
            } else {
                Debug.DrawRay(groundRays[i].origin, groundRays[i].direction * groundRayLength, Color.red);
            }
            if (Physics.Raycast(groundRays[i], out groundHits[i], groundRayLength, GameManager.script.enemyMask)) {
                Debug.DrawRay(groundRays[i].origin, groundRays[i].direction * groundRayLength, Color.cyan);
                if (canInteract) {
                    KillEnemy(groundHits[i].transform.GetComponent<CharacterController>());
                }
            }
        }
        if (groundRayCount > 0) {
            if (!grounded) {
                animator.SetTrigger("Land");
            }
            grounded = true;
            StopJumpDelay();
            switch (cc.enemyType) {
                case ENEMYTYPE.walkerPro:
                    if (groundRayCount < groundRayDensity) {
                        if (canInteract) {
                            StartInteract();
                            TurnAround();
                        }
                    }
                    break;
            }
        } else {
            grounded = false;
        }
    }

    void CheckUp() {
        int upRayCount = 0;
        upPointY = 0f;
        enemyAbove = null;
        for (int i = 0; i < upRayDensity; i++) {
            upRays[i] = new Ray(transform.position + upRayOrigins[i], Vector3.up);
            if (Physics.Raycast(upRays[i], out upHits[i], upRayLength, GameManager.script.floorMask)) {
                Debug.DrawRay(upRays[i].origin, upRays[i].direction * upRayLength, Color.green);
                upRayCount++;
                upPointY = upHits[i].point.y;
            } else {
                Debug.DrawRay(upRays[i].origin, upRays[i].direction * upRayLength, Color.red);
            }
            if (Physics.Raycast(upRays[i], out upHits[i], upRayLength, GameManager.script.enemyMask)) {
                Debug.DrawRay(upRays[i].origin, upRays[i].direction * upRayLength, Color.cyan);
                enemyAbove = upHits[i].transform.GetComponent<CharacterController>();
            }
        }
        if (upRayCount > 0) {
            if (!hitUp) {
                hitUp = true;
                justHitUp = true;
            }
        } else {
            hitUp = false;
            justHitUp = false;
        }
    }

    void CheckSides() {
        enemySide = null;
        for (int i = 0; i < 2; i++) {
            sideObstaclesFound[i] = false;
            sidePointsX[i] = 0f; 
            for (int j = 0; j < sideRayDensity; j++) {
                if (i == 0) {
                    sideRays[i, j] = new Ray(transform.position + sideRayOrigins[j], Vector3.left);
                } else {
                    sideRays[i, j] = new Ray(transform.position + sideRayOrigins[j], Vector3.right);
                }
                if (Physics.Raycast(sideRays[i, j], out sideHits[i, j], sideRayLength, GameManager.script.floorMask)) {
                    if (sideObstaclesFound[i] == false) {
                        sideObstaclesFound[i] = true;
                        sidePointsX[i] = sideHits[i, j].point.x;
                    }
                    Debug.DrawRay(sideRays[i, j].origin, sideRays[i, j].direction * sideRayLength, Color.green);
                } else {
                    Debug.DrawRay(sideRays[i, j].origin, sideRays[i, j].direction * sideRayLength, Color.red);
                }
                if (Physics.Raycast(sideRays[i, j], out sideHits[i, j], sideRayLength, GameManager.script.enemyMask)) {
                    Debug.DrawRay(sideRays[i, j].origin, sideRays[i, j].direction * sideRayLength, Color.cyan);
                    enemySide = sideHits[i, j].transform.GetComponent<CharacterController>();
                    switch (cc.enemyType) {
                        case ENEMYTYPE._none:
                              break;
                        default:
                            switch (enemySide.enemyType) {
                                case ENEMYTYPE._none:
                                    if (enemyAbove != enemySide) {
                                        enemySide.cm.Die(DEATHTYPE.hans);
                                    }
                                    break;
                                default:
                                    if (canInteract) {
                                        StartInteract();
                                        TurnAround();
                                    }
                                    break;
                            }
                            break;
                    }
                }
            }
        }
    }

    void TurnAround() {
        float direction = Mathf.Clamp01(cc.AI_TurnAround());
        if (animator != null) {
            animator.SetFloat("WalkDirection", direction);
            animator.SetTrigger("TurnAround");
        }
    }

    public void Jump(bool bouncing = false) {
        if (canJump || bouncing) {
            if (grounded || bouncing) {
                rb.velocity = new Vector3(rb.velocity.x, bouncing ? GetBounceHeight() : jumpStrength, 0f);
                StartJumpRoutine(false);
                grounded = false;
                if (airJumpCount > 0) {
                    airJumpCount = 0;
                }
                animator.SetTrigger("Jump");
                if (!bouncing) {
                    cc.ReceiveExpression(EXPRESSION.stacheJump);
                }
            } else {
                if (airJumpCount < maxAirJumps) {
                    rb.velocity = new Vector3(rb.velocity.x, airJumpStrength, 0f);
                    StartJumpRoutine(true);
                    airJumpCount++;
                    animator.SetTrigger("Jump");
                    if (!bouncing) {
                        cc.ReceiveExpression(EXPRESSION.stacheJump);
                    }
                }
            }
        }
    }

    void StartJumpExtension() {
        if (COR_JumpExtension != null) {
            StopCoroutine(COR_JumpExtension);
        }
        COR_JumpExtension = StartCoroutine(JumpExtensionRoutine());
    }

    IEnumerator JumpExtensionRoutine() {
        float passingTime = 0f;
        while (passingTime <= jumpExtensionDuration && cc.extendingJump) {
            passingTime += Time.deltaTime;
            rb.AddForce(Vector3.up * jumpExtensionStrength, ForceMode.Acceleration);
            yield return new WaitForSeconds(Time.deltaTime);
        }
        cc.StopJumpExtension();
    }

    float GetBounceHeight() {
        return Mathf.Clamp(Mathf.Abs(rb.velocity.y), jumpStrength, bounceStrength);
    }

    void StopJumpDelay() {
        if (COR_JumpDelay != null) {
            StopCoroutine(COR_JumpDelay);
        }
        canJump = true;
    }

    void StartJumpRoutine(bool air) {
        StopJumpDelay();
        COR_JumpDelay = StartCoroutine(JumpDelayRoutine());
        StopJumpRoutine();
        COR_JumpGravity = StartCoroutine(JumpGravityRoutine(air));
        StartJumpExtension();
    }

    void StopJumpRoutine() {
        if (COR_JumpGravity != null) {
            StopCoroutine(COR_JumpGravity);
        }
    }

    IEnumerator JumpDelayRoutine() {
        justJumped = true;
        canJump = false;
        yield return new WaitForSeconds(jumpDelay);
        justJumped = false;
        canJump = true;
    }

    IEnumerator JumpGravityRoutine(bool air) {
        float passingTime = 0f;
        while (passingTime <= 1f) {
            passingTime += Time.deltaTime / gravityCurveDuration;
            currentGravity = Mathf.Lerp(0f, gravity, curve_JumpGravity.Evaluate(passingTime));
            yield return new WaitForSeconds(Time.deltaTime);
        }
    }

    void KillEnemy(CharacterController enemy) {
        cc.ReceiveExpression(EXPRESSION.stacheStomp);
        StartInteract();
        Jump(true);
        enemy.cm.Die(DEATHTYPE.squish);
    }

    void StartInteract() {
        StopInteract();
        canInteract = false;
        COR_Interact = StartCoroutine(InteractDelayRoutine());
    }

    void StopInteract() {
        if (COR_Interact != null) {
            StopCoroutine(COR_Interact);
        }
        canInteract = true;
    }

    IEnumerator InteractDelayRoutine() {
        yield return new WaitForSeconds(interactDelay);
        StopInteract();
    }

    public void Die(DEATHTYPE d) {
        if (COR_DeathRoutine != null) {
            StopCoroutine(COR_DeathRoutine);
        }
        COR_DeathRoutine = StartCoroutine(DeathRoutine(d));
    }

    IEnumerator DeathRoutine(DEATHTYPE d) {
        switch (d) {
            case DEATHTYPE.squish:
                GameManager.script.RequestSlowMo(SLOWMOTYPE.smallKill);
                alive = false;
                cCollider.enabled = false;
                rb.isKinematic = true;
                if (animator != null) {
                    animator.SetTrigger("DieSquish");
                }
                yield return new WaitForSeconds(deathSquishTime);
                cc.KillCharacter();
                break;
            case DEATHTYPE.hans:
                cc.ReceiveState(EXPRESSIONSTATE.dead);
                GameManager.script.RequestSlowMo(SLOWMOTYPE.death);
                animator.transform.localRotation = Quaternion.Euler(cc.startFaceRotation);
                alive = false;
                cCollider.enabled = false;
                rb.velocity = new Vector3(0f, jumpDeathStrength, 0f);
                if (animator != null) {
                    animator.SetTrigger("Die");
                }
                cc.KillCharacter();
                break;
        }
    }

    void OnTriggerEnter(Collider c) {
        if (cc.userControlled) {
            if (c.gameObject.layer == 13) {
                switch (c.gameObject.GetComponent<PickUp>().Collect()) {
                    case PICKUPTYPE.coin:
                        //Need audio, PFX, score
                        break;
                }
            }
        }
    }

    public void Win() {
        isActive = false;
        animator.transform.localRotation = Quaternion.Euler(cc.startFaceRotation);
        if (animator != null) {
            animator.SetTrigger("Win");
        }
        rb.velocity = Vector3.zero;
    }

}
