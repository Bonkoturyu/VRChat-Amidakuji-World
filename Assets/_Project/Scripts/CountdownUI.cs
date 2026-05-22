using UdonSharp;
using UnityEngine;
using TMPro;
using VRC.SDKBase;
using VRC.Udon;

// 冒頭 3-2-1(STATE_RUNNING 遷移直後の Countdown フェーズ)と A モード末尾 FinaleCountdown の
// 共通カウントダウン UI。サーバー時刻ベースで動作し、複数クライアントで表示同期する。
// コールバック先は gameManager 固定(Inspector バインド)。
public class CountdownUI : UdonSharpBehaviour
{
    [Header("References")]
    [Tooltip("コールバックの SendCustomEvent 先")]
    public GameManager gameManager;

    [Header("Display")]
    [Tooltip("カウントダウン中だけ Active にする表示ルート")]
    public GameObject displayRoot;
    [Tooltip("残り秒数を表示する TextMeshProUGUI")]
    public TextMeshProUGUI text;

    [Header("Behavior")]
    [Tooltip("ON のとき冒頭 3-2-1 では起動せず、末尾 FinaleCountdown のみで起動する(賞品エリア内 Canvas 用)")]
    public bool isFinaleOnly = false;
    [Tooltip("冒頭 Countdown の 0 秒到達時に表示する文字列(Inspector で変更可)")]
    public string startupFinishText = "GO!";
    [Tooltip("末尾 FinaleCountdown の 0 秒到達時に表示する文字列(Inspector で変更可)")]
    public string finaleFinishText = "FINALE!";
    [Tooltip("0 到達後に表示を残す秒数(GO! 表示時間)")]
    public float postZeroLingerSeconds = 0.5f;

    private bool _active;
    private double _targetServerTime;
    private string _callbackEventName;
    private bool _callbackFired;
    private int _lastDisplayedInt = -999;
    private string _finishText = "GO!";

    void Start()
    {
        _Hide();
    }

    // targetServerTime までの残り秒を表示。0 到達時に callbackEventName を gameManager に
    // SendCustomEvent(空文字 / null なら発火なし)。1 回の呼出につき最大 1 回発火。
    // isFinaleMode=false なら冒頭 Countdown(startupFinishText 表示)、true なら末尾 FinaleCountdown(finaleFinishText 表示)。
    // 0 秒到達時の表示文字列は Inspector フィールドから決定する。
    public void _StartCountdown(double targetServerTime, string callbackEventName, bool isFinaleMode)
    {
        _targetServerTime = targetServerTime;
        _callbackEventName = callbackEventName;
        string src = isFinaleMode ? finaleFinishText : startupFinishText;
        _finishText = (src != null && src.Length > 0) ? src : "GO!";
        _callbackFired = false;
        _active = true;
        _lastDisplayedInt = -999;
        if (displayRoot != null) displayRoot.SetActive(true);
    }

    public void _CancelCountdown()
    {
        _active = false;
        _Hide();
    }

    void Update()
    {
        if (!_active) return;

        double now = Networking.GetServerTimeInSeconds();
        // ADR-0003: 生の引き算ではなく CalculateServerDeltaTime を使う(引数順 later, earlier)
        double remaining = Networking.CalculateServerDeltaTime(_targetServerTime, now);

        if (remaining > 0.0)
        {
            int displayed = (int)System.Math.Ceiling(remaining);
            if (displayed != _lastDisplayedInt && text != null)
            {
                text.text = displayed.ToString();
                _lastDisplayedInt = displayed;
            }
        }
        else
        {
            if (!_callbackFired)
            {
                if (text != null) text.text = _finishText;
                _callbackFired = true;
                if (gameManager != null
                    && _callbackEventName != null
                    && _callbackEventName.Length > 0)
                {
                    gameManager.SendCustomEvent(_callbackEventName);
                }
            }
            // GO! 表示を一定時間維持してから消す
            if (remaining <= -postZeroLingerSeconds)
            {
                _active = false;
                _Hide();
            }
        }
    }

    private void _Hide()
    {
        if (displayRoot != null) displayRoot.SetActive(false);
        _lastDisplayedInt = -999;
    }
}
