using UnityEngine;
using UnityEngine.UIElements;

public class AudioController : MonoBehaviour
{
    private const string ClickSfxPath = "Audio/SFX/sfx_click";

    private static AudioController instance;

    public static AudioController Instance
    {
        get
        {
            if (instance == null)
            {
                var audioControllerObject = new GameObject("AudioController");
                instance = audioControllerObject.AddComponent<AudioController>();
            }

            return instance;
        }
    }

    private AudioSource sfxSource;
    private AudioClip clickSfx;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        Initialize();
    }

    private void Initialize()
    {
        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSource.spatialBlend = 0f;
            sfxSource.volume = 1f;
        }

        clickSfx ??= Resources.Load<AudioClip>(ClickSfxPath);
    }

    public void PlayClickSfx()
    {
        PlaySfx(clickSfx);
    }

    public void RegisterButtonSounds(VisualElement root)
    {
        if (root == null)
        {
            return;
        }

        Initialize();
        foreach (Button button in root.Query<Button>().ToList())
        {
            RegisterButtonSounds(button);
        }
    }

    private void RegisterButtonSounds(Button button)
    {
        if (button == null)
        {
            return;
        }

        button.RegisterCallback<PointerDownEvent>(OnButtonPointerDown, TrickleDown.TrickleDown);
    }

    private void OnButtonPointerDown(PointerDownEvent evt)
    {
        if (evt.button == 0)
        {
            PlayClickSfx();
        }
    }

    private void PlaySfx(AudioClip clip)
    {
        if (clip == null || sfxSource == null)
        {
            return;
        }

        if (clip.loadState == AudioDataLoadState.Unloaded)
        {
            clip.LoadAudioData();
        }

        sfxSource.PlayOneShot(clip);
    }
}
