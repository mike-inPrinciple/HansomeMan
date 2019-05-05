using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public enum ENEMYTYPE {
    _none = -1,
    walker,
    walkerPro
}

public enum DEATHTYPE {
    _none = -1,
    squish,
    hans
}

public enum SLOWMOTYPE {
    _none = -1,
    smallKill,
    largeKill,
    death
}

public enum PICKUPTYPE {
    _none = -1,
    coin
}

public enum HEADERTYPE {
    _none = -1,
    start,
    death,
    win
}

public class GameManager : MonoBehaviour {

    public static GameManager script;

    public float controllerDeadZone = 0.2f;
    public LayerMask floorMask;
    public LayerMask enemyMask;

    [SerializeField] private float[] slowMoDurations;
    [SerializeField] private AnimationCurve[] curve_slowMos;
    private Coroutine COR_SlowMo;

    public CharacterController hans;
    public Level activeLevel;

    [SerializeField] private TextMeshPro tm_Header;
    [SerializeField] private float tmHeaderSpeed;
    [SerializeField] private AnimationCurve curve_TMHeader;
    private Coroutine COR_TMHeader;

    [SerializeField] private string[] headerTexts;
    [SerializeField] private Color[] headerTextColors;

    void Awake() {
        script = this;
    }

    public void RequestSlowMo(SLOWMOTYPE s) {
        StartSlowMo((int)s);
    }

    void StartSlowMo(int index) {
        StopSlowMo();
        COR_SlowMo = StartCoroutine(SlowMoRoutine(index));
    }

    void StopSlowMo() {
        if (COR_SlowMo != null) {
            StopCoroutine(COR_SlowMo);
        }
    }

    IEnumerator SlowMoRoutine(int index) {
        float passingTime = 0f;
        while (passingTime <= slowMoDurations[index]) {
            passingTime += Time.unscaledDeltaTime;
            Time.timeScale = Mathf.Lerp(0f, 1f, curve_slowMos[index].Evaluate(passingTime / slowMoDurations[index]));
            Time.fixedDeltaTime = 0.02f * Time.timeScale;
            yield return new WaitForSecondsRealtime(Time.unscaledDeltaTime);
            //yield return null;
        }
    }

    void Update() {
        if (Input.GetButtonDown("Submit")) {
            activeLevel.ResetLevel();
            StartTMGoRoutine(HEADERTYPE.start);
        }
    }

    public void StartTMGoRoutine(HEADERTYPE ht) {
        StopTMGoRoutine();
        switch (ht) {
            case HEADERTYPE._none:
                COR_TMHeader = StartCoroutine(TMGoRoutine(tm_Header.color));
                break;
            default:
                COR_TMHeader = StartCoroutine(TMGoRoutine(headerTextColors[(int)ht], headerTexts[(int)ht]));
                break;
        }
        
    }

    void StopTMGoRoutine() {
        if (COR_TMHeader != null){
            StopCoroutine(COR_TMHeader);
        }
    }

    IEnumerator TMGoRoutine(Color color, string text = "") {
        float passingTime = 0f;
        tm_Header.SetText(text);
        Color sColor = new Color(color.r, color.g, color.b, 0f);
        Color eColor = new Color(color.r, color.g, color.b, 1f);
        while (passingTime <= 1f) {
            passingTime += Time.unscaledDeltaTime * tmHeaderSpeed;
            tm_Header.color = Color.Lerp(sColor, eColor, curve_TMHeader.Evaluate(passingTime));
            yield return new WaitForSecondsRealtime(Time.unscaledDeltaTime);
        }
    }

}
