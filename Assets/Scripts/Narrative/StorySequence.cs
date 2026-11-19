using System.Collections;
using AYNI.Core;
using AYNI.Player;
using UnityEngine;

namespace AYNI.Narrative
{
    public class StorySequence : MonoBehaviour
    {
        [SerializeField] bool autoStart = true;

        bool started;

        void Start()
        {
            if (autoStart)
                StartCoroutine(PlayPrologue());
        }

        public void BeginStory() => StartCoroutine(PlayPrologue());

        IEnumerator PlayPrologue()
        {
            if (started) yield break;
            started = true;

            var narrator = NarratorSystem.Instance;
            var choices = MoralChoiceSystem.Instance;
            var balance = AyniBalanceSystem.Instance;

            yield return new WaitForSeconds(1f);

            narrator.EnqueueMany(new[]
            {
                new DialogueLine
                {
                    speaker = "Cuy Sabio",
                    text = "Ajá... otro joven con prisa. Como si la Pachamama fuera un mercado de Huancayo.",
                    pauseAfter = 2f
                },
                new DialogueLine
                {
                    speaker = "Cuy Sabio",
                    text = "Escucha, mijo. Todo lo que tomas, debes devolver. Eso es ayni: dar y recibir en equilibrio.",
                    pauseAfter = 2.5f
                },
                new DialogueLine
                {
                    speaker = "Cuy Sabio",
                    text = "Pero tú... tomaste del manantial sin dejar ni una flor, ni una palabra de gracia.",
                    pauseAfter = 2f
                }
            });
            narrator.PlayQueue();

            while (narrator.IsSpeaking)
                yield return null;

            yield return new WaitForSeconds(0.5f);

            choices.Present(new MoralChoice
            {
                id = "prologue_water",
                prompt = "¿Qué haces ahora frente al manantial sagrado?",
                context = "El agua brilla como un ojo de la montaña. Sientes que algo te observa.",
                options = new[]
                {
                    new MoralChoiceOption
                    {
                        text = "Dejar una ofrenda de coca y pedir perdón",
                        reciprocidad = 15f,
                        equilibrio = 10f,
                        respetoPachamama = 20f,
                        narratorReaction = "Bien hecho. La Pachamama no olvida, pero sí perdona a quien recuerda el ayni."
                    },
                    new MoralChoiceOption
                    {
                        text = "Irte rápido, como si nada hubiera pasado",
                        reciprocidad = -20f,
                        equilibrio = -25f,
                        respetoPachamama = -30f,
                        unlocksForm = AnimalForm.Zorro,
                        narratorReaction = "Ah, el camino del zorro... Huyes con astucia, pero la deuda espiritual corre más rápido."
                    },
                    new MoralChoiceOption
                    {
                        text = "Beber más agua — la necesitas para seguir",
                        reciprocidad = -10f,
                        equilibrio = -15f,
                        respetoPachamama = -20f,
                        unlocksForm = AnimalForm.Vizcacha,
                        narratorReaction = "La vizcacha sobrevive donde otros perecen. Adaptarse no siempre es virtud, mijo."
                    }
                }
            });

            while (choices.IsChoosing)
                yield return null;

            yield return new WaitForSeconds(1f);

            balance.Modify(AyniValue.Equilibrio, -10f);

            narrator.Say(
                "Por romper el equilibrio, los espíritus te transformarán. Zorro, cóndor o vizcacha... " +
                "cada forma te enseñará lo que olvidaste.");

            while (narrator.IsSpeaking)
                yield return null;

            yield return new WaitForSeconds(1f);
            StartCoroutine(ChapterOne());
        }

        IEnumerator ChapterOne()
        {
            var narrator = NarratorSystem.Instance;
            var choices = MoralChoiceSystem.Instance;

            narrator.Say(
                "Camina hacia el altar de piedra. Allí aprenderás tu primera forma animal. " +
                "Pulsa 1, 2 o 3 para transformarte cuando las desbloquees.");

            yield return new WaitForSeconds(3f);

            choices.Present(new MoralChoice
            {
                id = "condor_altar",
                prompt = "En el altar encuentras plumas de cóndor. ¿Qué haces?",
                context = "El viento susurra desde la cima. Un cóndor dibuja círculos en el cielo.",
                options = new[]
                {
                    new MoralChoiceOption
                    {
                        text = "Meditar y pedir visión al apu",
                        reciprocidad = 10f,
                        equilibrio = 15f,
                        respetoPachamama = 15f,
                        unlocksForm = AnimalForm.Condor,
                        narratorReaction = "El cóndor no vuela por orgullo, sino para ver el todo. Ahora tú también."
                    },
                    new MoralChoiceOption
                    {
                        text = "Tomar una pluma como trofeo",
                        reciprocidad = -15f,
                        equilibrio = -10f,
                        respetoPachamama = -20f,
                        narratorReaction = "Un trofeo sin respeto pesa más que una piedra en la mochila."
                    },
                    new MoralChoiceOption
                    {
                        text = "Dejar tu propia ofrenda antes de tocar nada",
                        reciprocidad = 20f,
                        equilibrio = 10f,
                        respetoPachamama = 25f,
                        unlocksForm = AnimalForm.Condor,
                        narratorReaction = "El ayni primero, la visión después. Así se hace, hijo del altiplano."
                    }
                }
            });
        }
    }
}