using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Video;

public sealed class LoadingView : UIBase
{
    [Header("Loading")]
    [SerializeField] private VideoPlayer VideoPlayer_Loading;
    [SerializeField] private TMP_Text Text_Message;
    [SerializeField] private VideoClip[] _loadingVideoClips;

    private int _previousVideoIndex = -1;
    private int _loadingRequestCount;

    private bool _hasCompletedFirstPlayback;
    private string _loadingMessage = "LOADING...";

    private UniTaskCompletionSource _minimumPlaybackCompletionSource;

    public override UILayer Layer => UILayer.VeryFront;

    public bool HasLoadingRequests => _loadingRequestCount > 0;

    public void AddLoadingRequest(string message = null)
    {
        _loadingRequestCount++;

        if (!string.IsNullOrWhiteSpace(message))
        {
            _loadingMessage = message;
        }

        RefreshLoadingMessage();
    }

    public bool RemoveLoadingRequest()
    {
        _loadingRequestCount = Mathf.Max(0, _loadingRequestCount - 1);

        return _loadingRequestCount == 0;
    }

    public void ClearLoadingRequests()
    {
        _loadingRequestCount = 0;
        CompleteMinimumPlayback();
    }

    public UniTask WaitForMinimumPlaybackAsync()
    {
        if (_hasCompletedFirstPlayback || _minimumPlaybackCompletionSource == null)
        {
            return UniTask.CompletedTask;
        }

        return _minimumPlaybackCompletionSource.Task;
    }

    protected override bool ValidateReferences()
    {
        if (!base.ValidateReferences())
        {
            return false;
        }

        if (VideoPlayer_Loading == null)
        {
            Debug.LogError("LoadingView - VideoPlayer 연결되지 않음");
            return false;
        }

        if (_loadingVideoClips == null || _loadingVideoClips.Length == 0)
        {
            Debug.LogError("LoadingView - 로딩 영상 목록이 비어 있음");
            return false;
        }

        return true;
    }

    protected override void InitializeUI()
    {
        _loadingRequestCount = 0;

        VideoPlayer_Loading.isLooping = true;
        VideoPlayer_Loading.audioOutputMode = VideoAudioOutputMode.None;
        VideoPlayer_Loading.loopPointReached += HandleVideoLoopPointReached;

        RefreshLoadingMessage();
    }

    protected override void RefreshUI()
    {
        ResetMinimumPlayback();
        RefreshLoadingMessage();
        SelectRandomVideo();

        VideoPlayer_Loading.Stop();

        if (VideoPlayer_Loading.clip == null)
        {
            CompleteMinimumPlayback();
            return;
        }

        VideoPlayer_Loading.Play();
    }

    protected override void PlayCloseAnimation()
    {
        VideoPlayer_Loading.Stop();
        CompleteClose();
    }

    protected override void ReleaseUI()
    {
        _loadingRequestCount = 0;

        CompleteMinimumPlayback();

        if (VideoPlayer_Loading == null)
        {
            return;
        }

        VideoPlayer_Loading.loopPointReached -= HandleVideoLoopPointReached;

        VideoPlayer_Loading.Stop();
    }

    private void HandleVideoLoopPointReached(VideoPlayer videoPlayer)
    {
        CompleteMinimumPlayback();
    }

    private void ResetMinimumPlayback()
    {
        _minimumPlaybackCompletionSource?.TrySetResult();

        _hasCompletedFirstPlayback = false;
        _minimumPlaybackCompletionSource = new UniTaskCompletionSource();
    }

    private void CompleteMinimumPlayback()
    {
        if (_hasCompletedFirstPlayback)
        {
            return;
        }

        _hasCompletedFirstPlayback = true;
        _minimumPlaybackCompletionSource?.TrySetResult();
    }

    private void RefreshLoadingMessage()
    {
        if (Text_Message == null)
        {
            return;
        }

        Text_Message.text = _loadingMessage;
    }

    private void SelectRandomVideo()
    {
        int selectedIndex = Random.Range(0, _loadingVideoClips.Length);

        if (_loadingVideoClips.Length > 1)
        {
            while (selectedIndex == _previousVideoIndex)
            {
                selectedIndex = Random.Range(0, _loadingVideoClips.Length);
            }
        }

        _previousVideoIndex = selectedIndex;

        VideoPlayer_Loading.clip = _loadingVideoClips[selectedIndex];
    }
}