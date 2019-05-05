using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterController : MonoBehaviour {

    private bool setup;

    private int characterID;
    public bool userControlled;
    public ENEMYTYPE enemyType = ENEMYTYPE._none;
    public Vector2 movementInput;
    public bool extendingJump;

    [HideInInspector] public CharacterMotor cm;
    private HansExpressions he;

    [Header("--- HANS -----------------------------------------------------------------------------------------")]
    public bool facingRotation;
    public int currentFacingRotation;
    public Vector3 startFaceRotation;
    public Vector3[] faceRotations;
    private Vector3 startSize;

    [Header("--- WALKER -----------------------------------------------------------------------------------------")]
    [SerializeField] private float startDirection;
    private float currentDirection;
    public bool ledgeDetection;

    void Start() {
        startSize = transform.localScale;
    }

    public void Setup(int id, Vector3 startPos) {
        characterID = id;
        StopAllCoroutines();
        transform.position = startPos;
        transform.localScale = startSize;
        cm = GetComponent<CharacterMotor>();
        cm.Setup(this);
        he = GetComponent<HansExpressions>();
        if (he != null) {
            he.SetupExpressions();
        }
        switch (enemyType) {
            case ENEMYTYPE.walker:
            case ENEMYTYPE.walkerPro:
                currentDirection = startDirection;
                break;
            case ENEMYTYPE._none:
                break;
        }
        setup = true;
    }

    void Update() {
        if (setup) {
            if (userControlled) {
                if (cm.alive) {
                    movementInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
                    if (Input.GetButtonDown("Fire1")) {
                        extendingJump = true;
                        cm.Jump();
                    }
                    if (extendingJump) {
                        if (Input.GetButtonUp("Fire1")) {
                            extendingJump = false;
                        }
                    }
                }
            } else {
                switch (enemyType) {
                    case ENEMYTYPE.walker:
                    case ENEMYTYPE.walkerPro:
                        movementInput = new Vector2(Mathf.Clamp(currentDirection, -1f, 1f), 0f);
                        break;
                }
            }
        }
    }

    public float AI_TurnAround() {
        currentDirection = -currentDirection;
        return currentDirection;
    }

    public void StopJumpExtension() {
        extendingJump = false;
    }

    public void ReceiveExpression(EXPRESSION e) {
        if (he != null) {
            he.TriggerExpression(e);
        }
    }

    public void ReceiveState(EXPRESSIONSTATE e) {
        if (he != null) {
            he.ChangeState(e);
        }
    }

    public void KillCharacter() {
        GameManager.script.activeLevel.KillCharacter(characterID);
    }

    public void Win() {
        ReceiveState(EXPRESSIONSTATE.win);
        cm.Win();
    }

}
