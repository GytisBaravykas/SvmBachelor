using RestSharp;
using RestSharp.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace SvmBachelor
{
    class Annotation
    {
        public string Lemma { get; set; }
        public string Morph { get; set; }
        public string Pos { get; set; }

        public Annotation(string l, string m, string p)
        {
            Lemma = l;
            Morph = m;
            Pos = p;
        }

        public override string ToString()
        {
            return $"{Lemma} - {Pos}";
        }
        public string ToFullString()
        {
            return $"{Lemma}|{Morph}|{Pos}";
        }
    }

    class Annotations : List<Annotation>
    {
        static public void OpenFile_Download_Annotate_SaveFile(string pathLoad, string pathSave)
        {
            string data = downloadAnnotations(pathLoad);
            Annotations annos = CreateAnnotations(data);
            annos.SaveAnnotationsToFile(pathSave);
            Console.WriteLine("{0} -> {1} Done.", pathLoad, pathSave);
        }

        public static Annotations LoadAnnotationsFromFile(string filename)
        {
            Annotations annos = new Annotations();
            foreach (var item in File.ReadAllLines(filename))
            {
                string[] data = item.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                annos.Add(new Annotation(data[0], data[1], data[2]));
            }
            return annos;
        }


        public void SaveAnnotationsToFile(string filename)
        {
            StreamWriter sw = new StreamWriter(filename);
            foreach (var item in this)
            {
                sw.WriteLine(item.ToFullString());
            }
            sw.Close();
        }

        static public Annotations CreateAnnotations(string text)
        {
            Annotations annos = new Annotations();
            var lemmas = Regex.Matches(text, @"lemma=""(.+?)\"" type=""(.+?)""")
                .Select(x =>
                new
                {
                    lemma = x.Groups[1].Value,
                    morph = x.Groups[2].Value,
                    pos = x.Groups[2].Value.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)[0]
                });

            foreach (var item in lemmas)
            {
                annos.Add(new Annotation(item.lemma, item.morph, item.pos));
            }
            return annos;
        }

        static public string downloadAnnotations(string filename)
        {
            string text = File.ReadAllText(filename);
            var url = "http://donelaitis.vdu.lt";
            var client = new RestClient(url);
            client.CookieContainer = new System.Net.CookieContainer();

            RestRequest request = new RestRequest("/main_helper.php?id=4&nr=7_2", Method.POST);
            request.AddParameter("tipas", "anotuoti", ParameterType.GetOrPost);
            request.AddParameter("pateikti", "LM", ParameterType.GetOrPost);
            request.AddParameter("veiksmas", "Rezultatas faile", ParameterType.GetOrPost);
            request.AddParameter("tekstas", text, ParameterType.GetOrPost);

            IRestResponse response = client.Execute(request);
            string data = client.DownloadData(new RestRequest("/NLP/downlaod.php", Method.GET)).AsString();
            return data;
        }

        public Dictionary<(string, string), int> Pos2N()
        {
            var posList = new string[] { "dkt.", "bdv.", "sktv.", "įv.", "vksm.", "prv.", "jst.", "išt.", "dll.", "prl.", "jng." };

            Dictionary<(string, string), int> pos2NDict = new Dictionary<(string, string), int>();

            for (int i = 0; i < posList.Length; i++)
            {
                for (int j = 0; j < posList.Length; j++)
                {
                    var postuple = (posList[i], posList[j]);
                    pos2NDict.Add(postuple, 0);
                }
            }

            List<string> POSES = this.Select(x => x.Pos).ToList();
            for (int i = 1; i < POSES.Count; i++)
            {
                var postuple = (POSES[i - 1], POSES[i]);
                if (pos2NDict.ContainsKey(postuple))
                {
                    pos2NDict[postuple]++;
                }
            }
            return pos2NDict;
        }
    }
}
