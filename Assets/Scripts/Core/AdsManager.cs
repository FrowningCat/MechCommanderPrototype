using System;
using System.Runtime.InteropServices;
using UnityEngine;

public class AdsManager : MonoBehaviour
{
    private static AdsManager instance;

    public static AdsManager Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject go = new GameObject("AdsManager");
                instance = go.AddComponent<AdsManager>();
            }

            return instance;
        }
    }

    private const float InterstitialCooldownSeconds = 180f;
    private float lastInterstitialShownAt = -InterstitialCooldownSeconds;

    private Action pendingInterstitialCallback;
    private Action pendingRewardCallback;
    private Action pendingRewardCancelCallback;
    private bool rewardGrantedForPendingCall;

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] private static extern void YG_ShowInterstitial();
    [DllImport("__Internal")] private static extern void YG_ShowRewarded();
    [DllImport("__Internal")] private static extern int YG_IsAvailable();
#endif

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void ShowInterstitial(Action onClosed)
    {
        if (Time.realtimeSinceStartup - lastInterstitialShownAt < InterstitialCooldownSeconds)
        {
            onClosed?.Invoke();
            return;
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        if (YG_IsAvailable() == 0)
        {
            Debug.LogWarning("[AdsManager] Yandex SDK не инициализирован, пропускаю interstitial.");
            onClosed?.Invoke();
            return;
        }

        lastInterstitialShownAt = Time.realtimeSinceStartup;
        pendingInterstitialCallback = onClosed;
        YG_ShowInterstitial();
#else
        lastInterstitialShownAt = Time.realtimeSinceStartup;
        Debug.Log("[AdsManager] Mock: interstitial (Editor/не-WebGL платформа).");
        onClosed?.Invoke();
#endif
    }

    public void ShowRewarded(Action onRewardGranted, Action onClosedWithoutReward)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        if (YG_IsAvailable() == 0)
        {
            Debug.LogWarning("[AdsManager] Yandex SDK не инициализирован, пропускаю rewarded.");
            onClosedWithoutReward?.Invoke();
            return;
        }

        rewardGrantedForPendingCall = false;
        pendingRewardCallback = onRewardGranted;
        pendingRewardCancelCallback = onClosedWithoutReward;
        YG_ShowRewarded();
#else
        Debug.Log("[AdsManager] Mock: rewarded (Editor/не-WebGL платформа), бонус выдан сразу.");
        onRewardGranted?.Invoke();
#endif
    }

    // Вызывается из JS (YandexAds.jslib) через SendMessage('AdsManager', ...).
    public void OnInterstitialClosed(string wasShown)
    {
        Action callback = pendingInterstitialCallback;
        pendingInterstitialCallback = null;
        callback?.Invoke();
    }

    public void OnInterstitialError(string error)
    {
        Debug.LogWarning($"[AdsManager] Interstitial error: {error}");
        Action callback = pendingInterstitialCallback;
        pendingInterstitialCallback = null;
        callback?.Invoke();
    }

    public void OnRewardedGranted(string _)
    {
        rewardGrantedForPendingCall = true;
        Action callback = pendingRewardCallback;
        pendingRewardCallback = null;
        callback?.Invoke();
    }

    public void OnRewardedClosed(string _)
    {
        if (!rewardGrantedForPendingCall)
        {
            Action cancelCallback = pendingRewardCancelCallback;
            pendingRewardCancelCallback = null;
            cancelCallback?.Invoke();
        }

        rewardGrantedForPendingCall = false;
        pendingRewardCallback = null;
    }

    public void OnRewardedError(string error)
    {
        Debug.LogWarning($"[AdsManager] Rewarded error: {error}");
        rewardGrantedForPendingCall = false;
        Action cancelCallback = pendingRewardCancelCallback;
        pendingRewardCallback = null;
        pendingRewardCancelCallback = null;
        cancelCallback?.Invoke();
    }
}
