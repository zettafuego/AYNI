using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AYNI.Narrative
{
    [Serializable]
    public class DialogueLine
    {
        public string speaker;
        public string text;
        public float pauseAfter = 1.5f;
    }

    public class NarratorSystem : MonoBehaviour
    {
        public static NarratorSystem Instance { get; private set; }

        [SerializeField] string narratorName = "Cuy Sabio";
        [SerializeField] float charDelay = 0.03f;

        readonly Queue<DialogueLine> queue = new();
        Coroutine playRoutine;

        public bool IsSpeaking { get; private set; }
        public string CurrentSpeaker { get; private set; }
        public string CurrentText { get; private set; }

        public event Action<DialogueLine> OnLineStarted;
        public event Action OnDialogueFinished;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void Enqueue(DialogueLine line) => queue.Enqueue(line);

        public void EnqueueMany(IEnumerable<DialogueLine> lines)
        {
            foreach (var line in lines)
                queue.Enqueue(line);
        }

        public void PlayQueue()
        {
            if (playRoutine != null)
                StopCoroutine(playRoutine);
            playRoutine = StartCoroutine(PlayRoutine());
        }

        public void Say(string text, string speaker = null, float pauseAfter = 1.5f)
        {
            Enqueue(new DialogueLine
            {
                speaker = speaker ?? narratorName,
                text = text,
                pauseAfter = pauseAfter
            });
            PlayQueue();
        }

        IEnumerator PlayRoutine()
        {
            IsSpeaking = true;

            while (queue.Count > 0)
            {
                var line = queue.Dequeue();
                CurrentSpeaker = line.speaker;
                CurrentText = "";
                OnLineStarted?.Invoke(line);

                foreach (char c in line.text)
                {
                    CurrentText += c;
                    yield return new WaitForSeconds(charDelay);
                }

                yield return new WaitForSeconds(line.pauseAfter);
            }

            IsSpeaking = false;
            CurrentSpeaker = "";
            CurrentText = "";
            OnDialogueFinished?.Invoke();
        }

        public void Skip()
        {
            if (playRoutine != null)
            {
                StopCoroutine(playRoutine);
                playRoutine = null;
            }
            queue.Clear();
            IsSpeaking = false;
            OnDialogueFinished?.Invoke();
        }
    }
}