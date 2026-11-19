using System;
using AYNI.Core;
using AYNI.Player;

namespace AYNI.Narrative
{
    [Serializable]
    public class MoralChoiceOption
    {
        public string text;
        public float reciprocidad;
        public float equilibrio;
        public float respetoPachamama;
        public AnimalForm? unlocksForm;
        public string narratorReaction;
    }

    [Serializable]
    public class MoralChoice
    {
        public string id;
        public string prompt;
        public string context;
        public MoralChoiceOption[] options;
    }
}