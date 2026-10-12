using UnityEngine;

namespace PotatoHeroes
{
    // Простой покадровый аниматор для спрайт-шитов (без Animator Controller)
    [RequireComponent(typeof(SpriteRenderer))]
    public class SpriteAnimator : MonoBehaviour
    {
        public Sprite[] frames;
        public float fps = 8f;
        public bool loop = true;
        public bool playOnAwake = true;
        float _t; int _i; bool _playing;

        SpriteRenderer _sr;
        void Awake() { _sr = GetComponent<SpriteRenderer>(); }

        public void Play(Sprite[] newFrames, float newFps = -1f)
        {
            frames = newFrames;
            if (newFps > 0) fps = newFps;
            _i = 0; _t = 0; _playing = true;
            if (_sr == null) _sr = GetComponent<SpriteRenderer>();
            if (frames != null && frames.Length > 0) _sr.sprite = frames[0];
        }

        void OnEnable() { if (playOnAwake && frames != null && frames.Length > 0) _playing = true; }

        void Update()
        {
            if (!_playing || frames == null || frames.Length == 0 || _sr == null) return;
            _t += Time.deltaTime;
            float step = 1f / Mathf.Max(.1f, fps);
            while (_t >= step)
            {
                _t -= step; _i++;
                if (_i >= frames.Length)
                {
                    if (loop) _i = 0;
                    else { _i = frames.Length - 1; _playing = false; }
                }
            }
            _sr.sprite = frames[_i];
        }
    }
}
