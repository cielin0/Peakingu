using UnityEngine;

namespace Peakingu
{
    /// <summary>
    /// Animador simple frame-a-frame para SpriteRenderer (vapor blanco, humo negro, etc.).
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class FrameAnimator : MonoBehaviour
    {
        [SerializeField] private Sprite[] frames;
        [SerializeField] private float fps = 6f;
        [SerializeField] private bool loop = true;

        private SpriteRenderer sr;
        private float timer = 0f;
        private int currentIndex = 0;
        private bool isPlaying = false;

        private void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
        }

        public void SetFramesAndPlay(Sprite[] newFrames, float newFps = -1f, bool shouldLoop = true)
        {
            frames = newFrames;
            if (newFps > 0f) fps = newFps;
            loop = shouldLoop;
            Play();
        }

        public void Play()
        {
            if (sr == null) sr = GetComponent<SpriteRenderer>();
            timer = 0f;
            currentIndex = 0;
            isPlaying = frames != null && frames.Length > 0;

            if (sr != null)
            {
                sr.enabled = isPlaying;
                if (isPlaying) sr.sprite = frames[0];
            }
        }

        public void Stop()
        {
            isPlaying = false;
            if (sr != null) sr.enabled = false;
        }

        private void Update()
        {
            if (!isPlaying || frames == null || frames.Length == 0 || fps <= 0f) return;

            timer += Time.deltaTime;
            float frameDuration = 1f / fps;
            if (timer >= frameDuration)
            {
                timer -= frameDuration;
                currentIndex++;
                if (currentIndex >= frames.Length)
                {
                    if (loop)
                    {
                        currentIndex = 0;
                    }
                    else
                    {
                        currentIndex = frames.Length - 1;
                        isPlaying = false;
                    }
                }

                if (sr != null)
                {
                    sr.sprite = frames[currentIndex];
                }
            }
        }
    }
}
