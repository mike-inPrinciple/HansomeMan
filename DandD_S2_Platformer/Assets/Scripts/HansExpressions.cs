using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum EXPRESSIONSTATE {
    _none = -1,
    alive,
    dead,
    win
}

public enum EXPRESSION {
    _none = -1,
    stacheJump,
    stacheStomp
}

public class HansExpressions : MonoBehaviour {

    private Animator eyesAnimator;
    private Animator stacheAnimator;
    private Coroutine COR_BlinkRoutine;

    [SerializeField] private float[] blinkWaits;

    public void SetupExpressions() {
        eyesAnimator = transform.Find(
            "Model/Skeleton/root/hips/spine_001/spine_002/spine_003/neck/head/Face/HansEyes"
        ).GetComponent<Animator>();
        stacheAnimator = transform.Find(
            "Model/Skeleton/root/hips/spine_001/spine_002/spine_003/neck/head/Face/HansStache"
        ).GetComponent<Animator>();
        ChangeState(EXPRESSIONSTATE.alive);
    }

    public void ChangeState(EXPRESSIONSTATE e) {
        switch (e) {
            case EXPRESSIONSTATE.alive:
                eyesAnimator.SetTrigger("Reset");
                stacheAnimator.SetTrigger("Reset");
                StartBlinkRoutine();
                break;
            case EXPRESSIONSTATE.dead:
                eyesAnimator.SetTrigger("Die");
                stacheAnimator.SetTrigger("Die");
                break;
            case EXPRESSIONSTATE.win:
                stacheAnimator.SetTrigger("Win");
                stacheAnimator.SetTrigger("Reset");
                StartBlinkRoutine();
                break;
        }
    }

    public void TriggerExpression(EXPRESSION e) {
        switch (e) {
            case EXPRESSION.stacheJump:
                stacheAnimator.SetTrigger("Jump");
                break;
            case EXPRESSION.stacheStomp:
                stacheAnimator.SetTrigger("Stomp");
                break;
        }
    }

    void StartBlinkRoutine() {
        StopBlinkRoutine();
        COR_BlinkRoutine = StartCoroutine(BlinkRoutine());
    }

    void StopBlinkRoutine() {
        if (COR_BlinkRoutine != null) {
            StopCoroutine(COR_BlinkRoutine);
        }
    }

    IEnumerator BlinkRoutine() {
        while (true) {
            yield return new WaitForSeconds(Random.Range(blinkWaits[0], blinkWaits[1]));
            eyesAnimator.SetTrigger("Blink");
        }
    }

}
