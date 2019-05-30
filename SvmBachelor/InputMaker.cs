using RestSharp;
using RestSharp.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace SvmBachelor
{
    class InputMaker
    {
        public static readonly int WordCount = 360/2;

        public static (string, List<double>) makeInputs(string text, Annotations annos)
        {
            List<double> inputs = new List<double>();
            int sentenceLengthsLimit = 8;


            string t2 = Regex.Replace(text, @"(\\.\\.\\.)|(\w+(?:[A-zĄČĘĖĮŠŲŪŽąčęėįšųūž]{2}))[!?.] ?([A-Z0-9ĄČĘĖĮŠŲŪŽ])", "$2\n$3");
            string[] sentences = t2.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            sentences = sentences.Select(x => x.Trim()).ToArray();


            List<int> commas = buildCommas(sentences, inputs);
            (int shortSentences, int longSentences) = buildSentenceLengths(sentences, inputs, sentenceLengthsLimit);
            Dictionary<(string, string), int> pos2n = buildPos2N(annos, inputs);
            Dictionary<string, int> functionWords = buildFunctionWords(annos, inputs);
            int uniqueWords = buildUniqueWords(annos, inputs);



            // ========================= START =========================
            // ============ Build pretty inputs file output ============
            // =========================================================

            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("======== Commas ======== [how many words between commas]: [how many instances found]\n");
            for (int i = 0; i < commas.Count; i++)
            {
                sb.AppendFormat("{0}: {1}\n", i, commas[i]);
            }

            sb.AppendFormat("======== SentenceLength (limit: {0}) ========\n", sentenceLengthsLimit);
            sb.AppendFormat("Sentences shorter than {0}: {1}\n", sentenceLengthsLimit, shortSentences);
            sb.AppendFormat("Sentences longer than {0}: {1}\n", sentenceLengthsLimit, longSentences);


            sb.AppendFormat("======== Pos2N ========\n");
            foreach (var item in pos2n)
            {
                sb.AppendFormat("{0}->{1}: {2}\n", item.Key.Item1.Trim('.'), item.Key.Item2.Trim('.'), item.Value);
            }

            sb.AppendFormat("======== FunctionWords ========\n");
            foreach (var item in functionWords)
            {
                sb.AppendFormat("{0}: {1}\n", item.Key, item.Value);
            }

            sb.AppendFormat("======== UniqueWords ========\n");
            sb.AppendFormat("{0}\n", uniqueWords);

            sb.AppendFormat("\n======== Word Count (including unknown) ========\n");
            sb.AppendFormat("{0}\n", annos.Count);

            sb.AppendFormat("\n======== Sentence Count (including unknown) ========\n");
            sb.AppendFormat("{0}\n", sentences.Length);

            sb.AppendFormat("\n======== Sentences ========\n");
            sb.AppendFormat("{0}\n", String.Join("\n", sentences));

            sb.AppendFormat("\n======== Annotations ========\n");
            foreach (var item in annos)
            {
                sb.AppendFormat("{0} - {1} - {2}\n", item.Lemma, item.Pos, item.Morph);
            }


            // ========================= END ===========================
            // ============ Build pretty inputs file output ============
            // =========================================================

            // Commas           (50)    / 50
            // SentenceLength   (2)     / 52
            // POS2N            (121)   / 173
            // FunctionWords    (5)     / 178
            // UniqueWords      (1)     / 179

            return (sb.ToString(), inputs);
        }

        public static int buildUniqueWords(Annotations annos, List<double> inputs)
        {
            int uniqueWords = annos.Select(x => x.Lemma).Distinct().Count();
            inputs.Add(uniqueWords / (double)WordCount);
            return uniqueWords;
        }

        public static Dictionary<string, int> buildFunctionWords(Annotations annos, List<double> inputs)
        {
            //TODO: if theres time add function words "ir"=>5, "bet"=>6
            string[] pronouns = new string[] { "aš", "tu", "jis", "tas", "anas", "šitas", "kitas"};
            string[] conjunctions = new string[] { "ir", "bei", "o", "bet", "tačiau", "tik", "ar", "arba", "todėl", "tad", "nei","nė","nebent","tartum","kad","tiktai", "jog", "kol", "nes", "kadangi" };
            string[] fraction = new string[] { "antai","argi", "bene", "bent", "gal", "gi", "jau", "juk", "kažin","ne", "nebe", "net", "pat", "štai", "tegu", "vien", "vos" };

            var fw = new string[]{ "sktv.", "įv.", "prv.", "dll.", "jng." };
            Dictionary<string, int> functionWords = new Dictionary<string, int>();
            for (int i = 0; i < fw.Length; i++)
            {
                //int conj = annos.Count(x => String.Equals(x.Pos, "jng.") && String.Equals(x.Lemma,"ir"));
                int fwCount = annos.Count(x => String.Equals(x.Pos, fw[i]));
                functionWords.Add(fw[i], fwCount);
                inputs.Add(fwCount / (double)WordCount);
            }

            return functionWords;
        }

        public static Dictionary<(string, string), int> buildPos2N(Annotations annos, List<double> inputs)
        {
            Dictionary<(string, string), int> pos2n = annos.Pos2N();
            foreach (var item in pos2n)
            {
                inputs.Add(item.Value / (double)pos2n.Count);
            }
            return pos2n;
        }

        public static (int, int) buildSentenceLengths(IEnumerable<string> sentences, List<double> inputs, int sentenceLength = 10)
        {
            int shortSentences = 0;
            int longSentences = 0;
            List<int> sentenceLengths = new List<int>();
            foreach (var item in sentences)
            {
                int length = item.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Length;
                sentenceLengths.Add(length);
            }
            shortSentences = sentenceLengths.Count(x => x <= sentenceLength);
            longSentences = sentenceLengths.Count(x => x > sentenceLength);
            int totalSentences = shortSentences + longSentences;

            inputs.Add(shortSentences / (double)totalSentences);
            inputs.Add(longSentences / (double)totalSentences);
            return (shortSentences, longSentences);
        }

        public static List<int> buildCommas(IEnumerable<string> sentences, List<double> inputs)
        {
            for (int i = 0; i < 100; i++)
            {
                inputs.Add(0);
            }
            List<int> commas = new List<int>();

            foreach (string line in sentences)
            {
                var dist = 0;
                var arr = line.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var item in arr.Take(arr.Length - 1))
                {
                    int c = item.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Length;
                    dist += c;
                    if (dist >= inputs.Count)
                        break;
                    inputs[dist]++;
                }
            }
            int level = 1;
            for (int i = 0; i < 100; i++)
            {
                if (i % 10 == 0)
                    level++;

                commas.Add((int)inputs[i]);
                inputs[i] /= (WordCount/level - 1);
            }
            return commas;
        }

    }
}
