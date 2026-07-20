mergeInto(LibraryManager.library, {

  YG_ShowInterstitial: function () {
    if (typeof window.ysdk === 'undefined' || !window.ysdk || !window.ysdk.adv) {
      SendMessage('AdsManager', 'OnInterstitialError', 'ysdk not initialized');
      return;
    }

    window.ysdk.adv.showFullscreenAdv({
      callbacks: {
        onClose: function (wasShown) {
          SendMessage('AdsManager', 'OnInterstitialClosed', wasShown ? '1' : '0');
        },
        onError: function (error) {
          SendMessage('AdsManager', 'OnInterstitialError', String(error));
        }
      }
    });
  },

  YG_ShowRewarded: function () {
    if (typeof window.ysdk === 'undefined' || !window.ysdk || !window.ysdk.adv) {
      SendMessage('AdsManager', 'OnRewardedError', 'ysdk not initialized');
      return;
    }

    window.ysdk.adv.showRewardedVideo({
      callbacks: {
        onOpen: function () {},
        onRewarded: function () {
          SendMessage('AdsManager', 'OnRewardedGranted', '');
        },
        onClose: function () {
          SendMessage('AdsManager', 'OnRewardedClosed', '');
        },
        onError: function (error) {
          SendMessage('AdsManager', 'OnRewardedError', String(error));
        }
      }
    });
  },

  YG_IsAvailable: function () {
    return (typeof window.ysdk !== 'undefined' && window.ysdk) ? 1 : 0;
  }

});
