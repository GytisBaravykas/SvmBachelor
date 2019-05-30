using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace SvmBachelor
{
    public class InputData
    {

        public List<double> inputs { get; set; }
        public double[] outputs { get; set; }
        public string ReadableInputs { get; set; }
        public string InputFileName { get; set; }
        public string AnnoFileName { get; set; }

        public InputData(string filename, string annoFilePrefix)
        {
            InputFileName = filename;
            AnnoFileName = annoFilePrefix + filename;
            string[] lines = File.ReadAllLines(InputFileName);
            Console.WriteLine($"Reading {InputFileName}...");
            outputs = new double[Program.Authors.Length];
            for (int i = 0; i < Program.Authors.Length; i++)
            {
                outputs[i] = lines[0] == Program.Authors[i] ? 1 : -1;
            }
            Annotations annos = Annotations.LoadAnnotationsFromFile(AnnoFileName);

            (ReadableInputs, inputs) = InputMaker.makeInputs(lines[1], annos);
        }

        public void WritePrettyInputFile(string filePrefix)
        {
            File.WriteAllText(filePrefix + InputFileName, ReadableInputs);
        }

        public void WriteInputOutputFile(string filePrefix)
        {
            StreamWriter sw = new StreamWriter(filePrefix + InputFileName);
            sw.WriteLine(string.Join(" ", outputs));
            foreach (var item in inputs)
            {
                sw.WriteLine(item);
            }
            sw.Close();
        }

        // TODO? read inputdata from inputfile
    }

    public class InputDataList : List<InputData>
    {
        public InputDataList(string inputDataPath, string annoFilePrefix)
        {
            string[] trainDir = Directory.GetFiles(inputDataPath);

            List<Task<InputData>> tasks = new List<Task<InputData>>();

            for (int i = 0; i < trainDir.Length; i++)
            {
                int m = i;
                Task<InputData> task = Task<InputData>.Run(() =>
                {
                    InputData id = new InputData(trainDir[m], annoFilePrefix);

                    id.WritePrettyInputFile("PrettyInputs");
                    id.WriteInputOutputFile("Inputs");
                    Console.WriteLine("{0} done.", m);
                    return id;
                });
                tasks.Add(task);
            }

            Task.WhenAll(tasks).Wait();
            foreach (var item in tasks)
            {
                Add(item.Result);
            }

        }

        // TODO? read inputdatalist from inputfile folder

    }
}
